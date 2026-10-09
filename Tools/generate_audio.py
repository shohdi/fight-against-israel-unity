"""Create all original game audio. Requires Python 3 and NumPy; no samples used.

Run: python -m pip install numpy
     python Tools/generate_audio.py

The musical composition and synthesized recordings generated here are CC0.
"""

from pathlib import Path
import math
import wave

import numpy as np


SR = 44100
SEED = 29035
RNG = np.random.default_rng(SEED)
OUTPUT = Path(__file__).resolve().parents[1] / "Assets" / "Resources" / "Audio"


def seconds(duration):
    return np.arange(round(duration * SR), dtype=np.float64) / SR


def frequency(midi):
    return 440 * 2 ** ((midi - 69) / 12)


def color_noise(duration, low=0, high=20000):
    """Band-limited white noise with gentle spectral edges."""
    count = round(duration * SR)
    noise = RNG.normal(0, 1, count)
    bins = np.fft.rfftfreq(count, 1 / SR)
    band = 1 / (1 + (bins / max(high, 1)) ** 6)
    if low:
        band *= 1 - 1 / (1 + (bins / low) ** 6)
    result = np.fft.irfft(np.fft.rfft(noise) * band, n=count)
    return result / max(float(np.std(result)), 1e-6)


def fade(signal, attack=0.002, release=0.025):
    shaped = np.array(signal, copy=True)
    count = len(shaped)
    a, r = min(round(SR * attack), count), min(round(SR * release), count)
    shape = np.ones(count)
    if a:
        shape[:a] = np.sin(np.linspace(0, math.pi / 2, a)) ** 2
    if r:
        shape[-r:] *= np.sin(np.linspace(math.pi / 2, 0, r)) ** 2
    return shaped * (shape[:, None] if shaped.ndim == 2 else shape)


def write(name, samples, peak=0.88):
    samples = np.nan_to_num(samples)
    maximum = float(np.max(np.abs(samples)))
    samples *= peak / max(maximum, 1e-6)
    pcm = np.rint(np.clip(samples, -1, 1) * 32767).astype("<i2")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as wav:
        wav.setnchannels(samples.shape[1] if samples.ndim == 2 else 1)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(pcm.tobytes())
    rms = float(np.sqrt(np.mean(samples * samples)))
    edge = float(np.max(np.abs(samples[0] - samples[-1])))
    print(f"{name}: {len(samples) / SR:.2f}s, peak {peak:.3f}, "
          f"RMS {20 * math.log10(max(rms, 1e-8)):.1f} dBFS, loop-edge step {edge:.5f}")


def shot(enemy=False):
    duration = 0.24 if enemy else 0.18
    t = seconds(duration)
    # Short mechanical attack plus a descending electronic cannon report.
    sweep = (920 if enemy else 1550) * np.exp(-t * 26) + (155 if enemy else 240)
    phase = 2 * math.pi * np.cumsum(sweep) / SR
    body = (np.sin(phase) + 0.28 * np.sin(phase * 1.97)) * np.exp(-t * 30)
    punch = np.sin(2 * math.pi * (70 if enemy else 95) * t) * np.exp(-t * 36)
    crack = color_noise(duration, 1500, 11500) * np.exp(-t * 135)
    air = color_noise(duration, 700, 6200) * np.exp(-t * 32)
    return fade(0.6 * body + 0.32 * punch + 0.34 * crack + 0.11 * air,
                attack=0.0006, release=0.032)


def explosion():
    duration = 1.6
    t = seconds(duration)
    rumble = color_noise(duration, 18, 270)
    debris = color_noise(duration, 550, 7500)
    impact = color_noise(duration, 120, 10500)
    pitch = 75 * np.exp(-t * 7) + 31
    boom = np.sin(2 * math.pi * np.cumsum(pitch) / SR)
    primary = (0.65 * rumble + 0.65 * boom) * np.exp(-t * 4.2)
    primary += 0.7 * impact * np.exp(-t * 21)
    primary += 0.19 * debris * np.exp(-t * 3.7)
    # A subdued stereo reflection gives the blast size without long overlap.
    stereo = np.stack([primary, primary], axis=1)
    for delay, gain in [(0.045, 0.12), (0.093, 0.08), (0.163, 0.035)]:
        shift = round(delay * SR)
        stereo[shift:, int(delay * 1000) % 2] += primary[:-shift] * gain
    return fade(stereo, attack=0.0015, release=0.2)


def music():
    # 16 bars at 100 BPM = 38.4 seconds, in D minor: Dm / Bb / F / C.
    beat = 0.6
    bar = beat * 4
    total = round(bar * 16 * SR)
    bed = np.zeros((total, 2), dtype=np.float64)
    drums = np.zeros_like(bed)
    synth = np.zeros_like(bed)

    def add(bus, signal, start, gain=1, pan=0):
        if signal.ndim == 1:
            signal = np.stack([signal * math.sqrt((1 - pan) / 2),
                               signal * math.sqrt((1 + pan) / 2)], axis=1)
        signal = signal * gain
        # Wrap sustained notes and reverb into the beginning for a real loop.
        offset = round(start * SR) % total
        remaining = total - offset
        take = min(remaining, len(signal))
        bus[offset:offset + take] += signal[:take]
        if len(signal) > take:
            bus[:len(signal) - take] += signal[take:]

    chords = [(50, 53, 57, 62), (46, 50, 53, 58),
              (48, 53, 57, 60), (48, 52, 55, 60)]
    roots = [38, 34, 41, 36]
    arp_patterns = [(62, 69, 65, 74), (58, 65, 62, 70),
                    (60, 69, 65, 72), (60, 67, 64, 72)]

    for index in range(16):
        chord = index % 4
        begin = index * bar
        t = seconds(bar + 0.7)
        for voice, midi in enumerate(chords[chord]):
            hz = frequency(midi)
            # Slight oscillator detuning creates a wide, moving warm pad.
            left = np.sin(2 * math.pi * hz * 0.9985 * t + voice)
            right = np.sin(2 * math.pi * hz * 1.0015 * t + voice)
            harmonic = np.sin(2 * math.pi * hz * 2 * t + voice) * 0.22
            tremolo = 0.88 + 0.12 * np.sin(2 * math.pi * 0.5 * t + voice)
            pad = fade(np.stack([left + harmonic, right + harmonic], axis=1)
                       * tremolo[:, None], attack=0.45, release=0.65)
            add(bed, pad, begin - 0.35, 0.066)

        for step in range(16):
            at = begin + step * beat / 4
            midi = arp_patterns[chord][step % 4]
            if index >= 8 and step in (7, 15):
                midi += 12
            t = seconds(0.42)
            hz = frequency(midi)
            lead = (np.sin(2 * math.pi * hz * t)
                    + 0.3 * np.sin(2 * math.pi * hz * 2 * t) * np.exp(-t * 12)
                    + 0.12 * np.sin(2 * math.pi * hz * 3 * t) * np.exp(-t * 18))
            lead *= np.exp(-t * 12)
            add(synth, fade(lead), at, 0.10, pan=-0.38 if step % 2 else 0.38)

        for step in range(8):
            at = begin + step * beat / 2
            t = seconds(0.29)
            hz = frequency(roots[chord] + (12 if step in (3, 7) else 0))
            phase = 2 * math.pi * hz * t
            bass = np.sin(phase) + 0.19 * np.sin(phase * 2) * np.exp(-t * 12)
            add(drums, fade(bass * np.exp(-t * 8), 0.009), at + 0.012, 0.23)

        for step in range(4):
            at = begin + step * beat
            if step in (0, 2) or (index % 4 == 3 and step == 3):
                t = seconds(0.27)
                pitch = 125 * np.exp(-t * 46) + 47
                kick = np.sin(2 * math.pi * np.cumsum(pitch) / SR) * np.exp(-t * 21)
                kick += 0.08 * color_noise(0.27, 1800, 9000) * np.exp(-t * 150)
                add(drums, fade(kick, 0.001, 0.035), at, 0.46)
            if step in (1, 3):
                t = seconds(0.19)
                snare = color_noise(0.19, 650, 10500) * np.exp(-t * 26)
                snare += 0.4 * np.sin(2 * math.pi * 185 * t) * np.exp(-t * 40)
                add(drums, fade(snare, 0.001), at, 0.14, pan=0.10)

        for step in range(8 if index < 8 else 16):
            division = 2 if index < 8 else 4
            t = seconds(0.065)
            hat = color_noise(0.065, 5200, 17000) * np.exp(-t * 85)
            volume = 0.043 if step % division == 0 else 0.023
            add(drums, fade(hat, 0.0007, 0.01), begin + step * beat / division,
                volume, pan=(-0.3 if step % 2 == 0 else 0.3))

    # Cross-channel rhythmic delay, with tails wrapped into the loop start.
    delayed = synth.copy()
    for repeat in range(1, 5):
        reflected = synth[:, ::-1] if repeat % 2 else synth
        delayed += np.roll(reflected, round(beat * 0.5 * repeat * SR), axis=0) * 0.30 ** repeat
    for delay, gain in [(0.083, 0.17), (0.157, 0.09), (0.271, 0.05)]:
        bed += np.roll(bed[:, ::-1], round(delay * SR), axis=0) * gain
    mix = drums + bed + delayed
    # Gentle saturation catches transient peaks; leave digital headroom.
    return np.tanh(mix * 1.16)


if __name__ == "__main__":
    write("Music", music(), 0.86)
    write("PlayerShot", shot(), 0.88)
    write("EnemyShot", shot(True), 0.88)
    write("Explosion", explosion(), 0.90)
