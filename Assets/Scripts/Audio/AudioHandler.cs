using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

namespace Audio {
    public enum SoundEvent {
        BonePickup,
        CardPickup,
        SoulPickup,
        TurnChange,
        RoundEnd,
        CardDeal
    }

    [Serializable]
    public class SoundBank {
        public SoundEvent type;
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Range(0f, 0.3f)] public float pitchRandom = 0.05f;
    }

    public class AudioHandler : MonoBehaviour {
        public static AudioHandler Instance { get; private set; }

        [Header("Sources")] [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Music Playlist")] [SerializeField]
        private AudioClip[] backgroundMusicPlaylist;

        private int currentMusicIndex = -1;
        private Coroutine musicRoutine;

        [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float musicFadeDuration = 2f;

        [Header("SFX")] [SerializeField] private SoundBank[] soundBanks;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartMusicPlaylist();
        }
        
        public void StartMusicPlaylist()
        {
            if (musicRoutine != null)
                StopCoroutine(musicRoutine);

            if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Length == 0)
                return;

            musicRoutine = StartCoroutine(MusicPlaylistRoutine());
        }

        private IEnumerator MusicPlaylistRoutine()
        {
            while (true)
            {
                currentMusicIndex = (currentMusicIndex + 1) % backgroundMusicPlaylist.Length;

                AudioClip clip = backgroundMusicPlaylist[currentMusicIndex];

                if (clip == null)
                {
                    yield return null;
                    continue;
                }

                musicSource.clip = clip;
                musicSource.loop = false;
                musicSource.volume = 0f;
                musicSource.Play();

                yield return FadeMusic(0f, 1f, musicFadeDuration);

                float waitTime = Mathf.Max(0f, clip.length - musicFadeDuration * 2f);

                yield return new WaitForSeconds(waitTime);

                yield return FadeMusic(1f, 0f, musicFadeDuration);

                musicSource.Stop();
            }
        }

        private IEnumerator FadeMusic(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                musicSource.volume = to;
                yield break;
            }

            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(from, to, timer / duration);
                yield return null;
            }

            musicSource.volume = to;
        }

        public void Play(SoundEvent soundEvent) {
            SoundBank bank = GetBank(soundEvent);

            if (sfxSource == null || bank == null || bank.clips == null || bank.clips.Length == 0)
                return;

            AudioClip clip = bank.clips[Random.Range(0, bank.clips.Length)];

            if (clip == null)
                return;

            float originalPitch = sfxSource.pitch;
            sfxSource.pitch = 1f + Random.Range(-bank.pitchRandom, bank.pitchRandom);
            float volume = bank.volume > 0f ? bank.volume : 1f;
            sfxSource.PlayOneShot(clip, volume);
            sfxSource.pitch = originalPitch;
        }

        public static void PlayEvent(SoundEvent soundEvent) {
            Instance?.Play(soundEvent);
        }

        public void PlayMusic(AudioClip music) {
            if (musicSource == null || music == null)
                return;

            musicSource.clip = music;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }

        private SoundBank GetBank(SoundEvent soundEvent) {
            foreach (SoundBank bank in soundBanks) {
                if (bank.type == soundEvent)
                    return bank;
            }

            return null;
        }
    }
}
