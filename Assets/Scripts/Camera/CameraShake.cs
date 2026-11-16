using UnityEngine;

namespace EscapeFromSpace
{
    // 카메라 위치는 CameraFollow만 쓴다. 여기서는 더할 값만 계산한다.
    // 두 스크립트가 각자 위치를 쓰면 실행 순서에 따라 흔들림이 덮인다.
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private CombatConfig _config;

        private float _amplitude;
        private float _timeLeft;

        public Vector2 Offset { get; private set; }

        // 무기 반동(도/발)만큼 흔든다.
        public void AddRecoil(float recoilDegrees)
        {
            _amplitude = recoilDegrees * _config.CameraShakePerRecoilDegree;
            _timeLeft = _config.CameraShakeDuration;
        }

        private void Update()
        {
            if (_timeLeft <= 0f)
            {
                Offset = Vector2.zero;
                return;
            }

            _timeLeft -= Time.deltaTime;

            // 끝으로 갈수록 약해져야 뚝 끊기지 않는다.
            float strength = _amplitude * Mathf.Clamp01(_timeLeft / _config.CameraShakeDuration);
            Offset = Random.insideUnitCircle * strength;
        }
    }
}
