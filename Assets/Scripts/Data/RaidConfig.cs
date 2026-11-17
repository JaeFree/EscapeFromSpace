using UnityEngine;
using UnityEngine.Serialization;

namespace EscapeFromSpace
{
    // 레이드 규칙의 수치 (기획서 7장). 초기값이라 에셋에 두고 Inspector에서 밸런싱한다.
    [CreateAssetMenu(fileName = "RaidConfig", menuName = "EscapeFromSpace/Raid Config")]
    public class RaidConfig : ScriptableObject
    {
        [Header("산소")]
        [SerializeField, Tooltip("초. 헬멧 없이 가진 산소")] private float _baseOxygenSeconds;
        [SerializeField, Tooltip("산소가 0일 때 초당 HP 감소")] private float _suffocationDamagePerSecond;
        [SerializeField, Tooltip("초. 산소가 이만큼 남으면 게이지가 빨갛게 깜빡인다")] private float _oxygenWarningSeconds;

        [SerializeField, Tooltip("초. 산소가 얼마 안 남았을 때 경고음이 울리는 간격")] private float _oxygenWarningBeepInterval = 2.5f;
        [SerializeField, Tooltip("초. 질식 중에 경고음이 울리는 간격")] private float _suffocationBeepInterval = 0.8f;

        [Header("탈출 포드")]
        [FormerlySerializedAs("_escapeCountdown")]
        [SerializeField, Tooltip("초. 포드 안에서 탈출 키를 누르고 있어야 하는 시간")] private float _escapeHoldTime;
        [SerializeField, Tooltip("초. 레이드를 시작하고 이 시간이 지나야 포드가 열린다")] private float _podOpenDelay;
        [SerializeField, Tooltip("테스트용. 켜면 시작부터 열려 있다")] private bool _podsAlwaysOpen;

        public float BaseOxygenSeconds => _baseOxygenSeconds;
        public float SuffocationDamagePerSecond => _suffocationDamagePerSecond;
        public float OxygenWarningSeconds => _oxygenWarningSeconds;
        public float OxygenWarningBeepInterval => _oxygenWarningBeepInterval;
        public float SuffocationBeepInterval => _suffocationBeepInterval;
        public float EscapeHoldTime => _escapeHoldTime;
        public float PodOpenDelay => _podOpenDelay;
        public bool PodsAlwaysOpen => _podsAlwaysOpen;
    }
}
