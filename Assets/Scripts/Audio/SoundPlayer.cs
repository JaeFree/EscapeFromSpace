using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 효과음을 내는 곳. 씬마다 하나를 두지만 실제로는 처음 것 하나만 남아서 씬이 바뀌어도 나던 소리가 끊기지 않는다.
    // 게임은 2D라서 거리와 좌우는 직접 계산한다 (멀수록 작게, 왼쪽에서 나면 왼쪽으로).
    public class SoundPlayer : MonoBehaviour
    {
        private const int VoiceCount = 20;

        // 샷건 펠릿처럼 같은 프레임에 같은 소리가 여러 번 겹치면 소리만 커지므로 이 간격 안에서는 한 번만 낸다.
        private const float MinRepeatInterval = 0.04f;

        // 화면 끝에서 난 소리가 한쪽 귀로만 쏠리지 않게 좌우 치우침에 상한을 둔다.
        private const float MaxPan = 0.7f;
        private const float PanDistance = 14f;

        [SerializeField] private SoundLibrary _library;
        [SerializeField, Tooltip("이 씬에서 계속 깔리는 배경 소리. 비우면 없다")] private AudioClip _ambience;
        [SerializeField, Range(0f, 1f)] private float _ambienceVolume = 0.15f;
        [SerializeField, Tooltip("이 씬의 음악. 비우면 없다")] private AudioClip _music;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.3f;
        [SerializeField, Tooltip("초. 씬이 바뀔 때 음악이 잦아들고 다시 커지는 데 걸리는 시간")] private float _musicFadeTime = 1.2f;
        [SerializeField, Tooltip("초. 곡의 앞부분을 이만큼 건너뛰고 튼다. 되풀이할 때도 여기서부터 다시 시작한다")] private float _musicStartTime;

        private readonly Dictionary<SoundCue, float> _lastPlayTime = new Dictionary<SoundCue, float>();
        private AudioSource[] _voices;
        private AudioSource _ambienceSource;
        private AudioSource _musicSource;
        private AudioClip _nextMusic;
        private float _nextMusicVolume;
        private float _playingMusicVolume;
        private float _nextMusicStart;
        private float _playingMusicStart;
        private float _musicFade;
        private int _nextVoice;
        private Transform _listener;

        public static SoundPlayer Instance { get; private set; }

        public SoundLibrary Library => _library;

        // 에디터에서 도메인을 다시 불러오지 않는 설정이면 앞 판의 값이 남는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 이미 있는 쪽에 이 씬의 배경 소리만 넘기고 물러난다.
                Instance.SetAmbience(_ambience, _ambienceVolume);
                Instance.SetMusic(_music, _musicVolume, _musicStartTime);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                _voices[i] = CreateSource();
            }

            _ambienceSource = CreateSource();
            _ambienceSource.loop = true;
            SetAmbience(_ambience, _ambienceVolume);

            // 되풀이는 직접 한다. 앞부분을 건너뛴 자리로 돌아가야 해서 AudioSource의 loop를 쓸 수 없다.
            _musicSource = CreateSource();
            SetMusic(_music, _musicVolume, _musicStartTime);
        }

        // 음악은 뚝 끊기면 거슬리므로 잦아들었다가 다음 곡이 커진다. 게임이 멈춘 동안에도 이어져야 해서 멈추지 않는 시계로 잰다.
        private void Update()
        {
            bool changing = _musicSource.clip != _nextMusic;

            // 곡이 끝나면 건너뛴 자리에서 다시 튼다.
            if (!changing && _musicSource.clip != null && !_musicSource.isPlaying)
            {
                PlayMusicFromStart();
            }

            float target = changing ? 0f : 1f;
            if (Mathf.Approximately(_musicFade, target) && !changing)
            {
                return;
            }

            float step = _musicFadeTime > 0f ? Time.unscaledDeltaTime / _musicFadeTime : 1f;
            _musicFade = Mathf.MoveTowards(_musicFade, target, step);

            if (changing && _musicFade <= 0f)
            {
                _musicSource.Stop();
                _musicSource.clip = _nextMusic;
                _playingMusicVolume = _nextMusicVolume;
                _playingMusicStart = _nextMusicStart;
                if (_nextMusic != null)
                {
                    PlayMusicFromStart();
                }
            }

            _musicSource.volume = _musicFade * _playingMusicVolume * _library.MasterVolume;
        }

        private void SetMusic(AudioClip clip, float volume, float startTime)
        {
            _nextMusic = clip;
            _nextMusicVolume = volume;
            _nextMusicStart = startTime;
        }

        private void PlayMusicFromStart()
        {
            _musicSource.Play();

            // 곡보다 긴 값을 넣어도 멈추지 않게 곡 길이 안으로 자른다.
            _musicSource.time = Mathf.Clamp(_playingMusicStart, 0f, Mathf.Max(0f, _musicSource.clip.length - 1f));
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void SetAmbience(AudioClip clip, float volume)
        {
            _ambienceSource.volume = volume * _library.MasterVolume;
            if (_ambienceSource.clip == clip && _ambienceSource.isPlaying)
            {
                return;
            }

            _ambienceSource.clip = clip;
            if (clip != null)
            {
                _ambienceSource.Play();
            }
            else
            {
                _ambienceSource.Stop();
            }
        }

        // 화면에서 나는 소리 (버튼, 가방). 거리와 상관없이 그대로 들린다.
        public void Play(SoundCue cue)
        {
            PlayInternal(cue, 1f, 0f);
        }

        // 맵의 한 지점에서 나는 소리. 카메라(플레이어)에서 멀수록 작아진다.
        public void PlayAt(SoundCue cue, Vector2 position)
        {
            if (_listener == null)
            {
                Camera camera = Camera.main;
                if (camera == null)
                {
                    PlayInternal(cue, 1f, 0f);
                    return;
                }

                _listener = camera.transform;
            }

            Vector2 offset = position - (Vector2)_listener.position;
            float distance = offset.magnitude;
            if (distance >= _library.HearingDistance)
            {
                return;
            }

            float range = Mathf.Max(0.01f, _library.HearingDistance - _library.FullVolumeDistance);
            float fade = 1f - Mathf.Clamp01((distance - _library.FullVolumeDistance) / range);
            float pan = Mathf.Clamp(offset.x / PanDistance, -1f, 1f) * MaxPan;

            // 귀에는 크기가 제곱으로 줄어야 자연스럽게 멀어지는 것으로 들린다.
            PlayInternal(cue, fade * fade, pan);
        }

        private void PlayInternal(SoundCue cue, float volumeScale, float pan)
        {
            if (cue == null || !cue.HasClip)
            {
                return;
            }

            // 결과 화면처럼 게임이 멈춘 동안에도 소리는 나야 해서 멈추지 않는 시계로 잰다.
            float now = Time.unscaledTime;
            if (_lastPlayTime.TryGetValue(cue, out float last) && now - last < MinRepeatInterval)
            {
                return;
            }

            _lastPlayTime[cue] = now;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % VoiceCount;

            voice.clip = cue.PickClip();
            voice.volume = cue.Volume * volumeScale * _library.MasterVolume;
            voice.pitch = cue.PickPitch();
            voice.panStereo = pan;
            voice.Play();
        }
    }

    // 다른 스크립트가 소리를 낼 때 쓰는 짧은 창구. 씬에 SoundPlayer가 없으면(임시 테스트 씬) 아무 일도 하지 않는다.
    public static class Sfx
    {
        public static void Play(SfxId id)
        {
            SoundPlayer player = SoundPlayer.Instance;
            if (player != null)
            {
                player.Play(player.Library.Get(id));
            }
        }

        public static void PlayAt(SfxId id, Vector2 position)
        {
            SoundPlayer player = SoundPlayer.Instance;
            if (player != null)
            {
                player.PlayAt(player.Library.Get(id), position);
            }
        }

        public static void PlayAt(SoundCue cue, Vector2 position)
        {
            SoundPlayer player = SoundPlayer.Instance;
            if (player != null)
            {
                player.PlayAt(cue, position);
            }
        }
    }
}
