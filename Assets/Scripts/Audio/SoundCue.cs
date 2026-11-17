using System;
using UnityEngine;

namespace EscapeFromSpace
{
    // 효과음 하나. 같은 소리가 반복될 때 기계적으로 들리지 않게 여러 파일 중 하나를 고르고 높낮이를 조금씩 바꾼다.
    [Serializable]
    public class SoundCue
    {
        [SerializeField, Tooltip("여러 개를 넣으면 낼 때마다 하나를 무작위로 고른다")] private AudioClip[] _clips;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField, Tooltip("1이 원래 높이. 낮추면 묵직하게, 올리면 가볍게 들린다")] private float _pitch = 1f;
        [SerializeField, Range(0f, 0.3f), Tooltip("낼 때마다 높낮이를 ±이만큼 바꾼다")] private float _pitchVariation = 0.05f;

        public float Volume => _volume;
        public bool HasClip => _clips != null && _clips.Length > 0;

        public AudioClip PickClip()
        {
            return HasClip ? _clips[UnityEngine.Random.Range(0, _clips.Length)] : null;
        }

        public float PickPitch()
        {
            return _pitch + UnityEngine.Random.Range(-_pitchVariation, _pitchVariation);
        }
    }
}
