using UnityEngine;

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

        [Header("탈출 포드")]
        [SerializeField, Tooltip("초. 포드 안에서 버텨야 하는 시간")] private float _escapeCountdown;
        [SerializeField, Tooltip("초. 레이드를 시작하고 이 시간이 지나야 포드가 열린다")] private float _podOpenDelay;
        [SerializeField, Tooltip("테스트용. 켜면 시작부터 열려 있다")] private bool _podsAlwaysOpen;

        public float BaseOxygenSeconds => _baseOxygenSeconds;
        public float SuffocationDamagePerSecond => _suffocationDamagePerSecond;
        public float OxygenWarningSeconds => _oxygenWarningSeconds;
        public float EscapeCountdown => _escapeCountdown;
        public float PodOpenDelay => _podOpenDelay;
        public bool PodsAlwaysOpen => _podsAlwaysOpen;
    }
}
