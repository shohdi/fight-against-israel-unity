using UnityEngine;

namespace FightAgainstIsrael
{
    /// <summary>Original looping score and pooled cannon / explosion audio.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const int VoiceCount = 24;
        private readonly AudioSource[] voices = new AudioSource[VoiceCount];
        private AudioSource musicSource;
        private AudioClip playerShot;
        private AudioClip enemyShot;
        private AudioClip explosion;
        private bool initialized;
        private bool musicEnabled = true;
        private bool effectsEnabled = true;
        private bool playing;
        private bool musicStarted;
        private float musicTarget;
        private float nextPlayerShot;
        private float nextEnemyShot;
        private int nextVoice;

        public bool MusicEnabled => musicEnabled;
        public bool EffectsEnabled => effectsEnabled;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            playerShot = Resources.Load<AudioClip>("Audio/PlayerShot");
            enemyShot = Resources.Load<AudioClip>("Audio/EnemyShot");
            explosion = Resources.Load<AudioClip>("Audio/Explosion");

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = Resources.Load<AudioClip>("Audio/Music");
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 32;
            musicSource.volume = 0f;

            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
                voice.priority = 96;
                voices[i] = voice;
            }
        }

        public void SetPlaying(bool isPlaying)
        {
            Initialize();
            playing = isPlaying;
            // Called by the explicit Start / Restart click, allowing browser audio.
            if (isPlaying && musicEnabled && !musicStarted && musicSource.clip != null)
            {
                musicSource.Play();
                musicStarted = true;
            }
            UpdateMusicTarget();
        }

        public void SetMusicEnabled(bool enabled)
        {
            Initialize();
            musicEnabled = enabled;
            if (enabled && playing && !musicStarted && musicSource.clip != null)
            {
                musicSource.Play();
                musicStarted = true;
            }
            UpdateMusicTarget();
        }

        public void SetEffectsEnabled(bool enabled)
        {
            Initialize();
            effectsEnabled = enabled;
            if (!enabled)
            {
                for (int i = 0; i < voices.Length; i++) voices[i].Stop();
            }
        }

        public void PlayPlayerShot()
        {
            Initialize();
            float now = Time.unscaledTime;
            if (now < nextPlayerShot) return;
            nextPlayerShot = now + 0.025f;
            PlayEffect(playerShot, 0.34f, Random.Range(0.96f, 1.055f), -0.08f);
        }

        public void PlayEnemyShot()
        {
            Initialize();
            float now = Time.unscaledTime;
            if (now < nextEnemyShot) return;
            nextEnemyShot = now + 0.035f;
            PlayEffect(enemyShot, 0.24f, Random.Range(0.91f, 1.045f), 0.08f);
        }

        public void PlayExplosion()
        {
            Initialize();
            PlayEffect(explosion, 0.58f, Random.Range(0.91f, 1.075f), Random.Range(-0.25f, 0.25f));
        }

        private void PlayEffect(AudioClip clip, float volume, float pitch, float pan)
        {
            if (!effectsEnabled || clip == null) return;
            // Each shot gets its own source: varying pitch never retunes an existing shot.
            // A fixed pool prevents allocations and unbounded simultaneous voices.
            int selected = nextVoice;
            for (int i = 0; i < VoiceCount; i++)
            {
                int candidate = (nextVoice + i) % VoiceCount;
                if (!voices[candidate].isPlaying)
                {
                    selected = candidate;
                    break;
                }
            }
            AudioSource voice = voices[selected];
            voice.Stop();
            voice.clip = clip;
            voice.volume = volume;
            voice.pitch = pitch;
            voice.panStereo = pan;
            voice.Play();
            nextVoice = (selected + 1) % VoiceCount;
        }

        private void UpdateMusicTarget()
        {
            musicTarget = musicEnabled ? (playing ? 0.38f : 0.13f) : 0f;
        }

        private void Update()
        {
            if (musicSource != null)
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicTarget,
                    Time.unscaledDeltaTime * 0.4f);
        }
    }
}
