# Original audio

All four audio assets are original, synthesized recordings made for **Fight against Israel**.
No third-party recordings, samples, melodies, voices, or commercial music were used.
The musical composition and recordings are released under **CC0 1.0 Universal**:
https://creativecommons.org/publicdomain/zero/1.0/

| Resource | Content | Format |
| --- | --- | --- |
| `Assets/Resources/Audio/Music.wav` | Seamless 38.4-second, 100 BPM synth score; D minor / B-flat / F / C, atmospheric stereo pads, arpeggio, sub bass, percussion, and tempo delay | 44.1 kHz / 16-bit / stereo |
| `Assets/Resources/Audio/PlayerShot.wav` | Layered ascending-register mechanical cannon transient and descending electronic report | 44.1 kHz / 16-bit / mono |
| `Assets/Resources/Audio/EnemyShot.wav` | Lower-register variant with its own sweep and impact body | 44.1 kHz / 16-bit / mono |
| `Assets/Resources/Audio/Explosion.wav` | Low rumble, pitch-dropping boom, debris hiss, and short stereo reflections | 44.1 kHz / 16-bit / stereo |

## Regeneration

The deterministic source is `Tools/generate_audio.py` (seed 29035). Install Python 3 and NumPy,
then run these commands from the project root:

```sh
python -m pip install numpy
python Tools/generate_audio.py
```

The script prints duration, peak, RMS level, and loop-edge sample difference. Peaks leave
digital headroom; short effects taper to zero. Pad tails and musical delays wrap into the
beginning, so the score loops without a fade or silent gap.

## Runtime

`Assets/Scripts/GameAudio.cs` loads `Resources/Audio` clips once. Call `Initialize()` during
game setup, then `SetPlaying(true)` from the Start or Restart button. This keeps initial
music playback behind a user gesture in a WebGL player. `SetPlaying(false)` ducks the score
for menu or defeat; `SetMusicEnabled(bool)` and `SetEffectsEnabled(bool)` control the two buses.
Use `PlayPlayerShot()`, `PlayEnemyShot()`, and `PlayExplosion()` for effects.

The soundtrack uses one source. Effects use 24 reusable voices; each keeps its pitch and
stereo position for its full lifetime. No new objects or coroutines are created per shot.
The scene must contain one enabled `AudioListener`, normally on the game camera.
