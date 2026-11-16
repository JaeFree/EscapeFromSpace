using UnityEngine;

namespace EscapeFromSpace
{
    // 방어력 계수는 기획서 11.2에서 플레이하며 확정하기로 한 값이라 에셋에 둔다.
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "EscapeFromSpace/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [SerializeField, Tooltip("방어력 1당 피해 감소 비율 (0.04 = 4%)")] private float _armorReductionPerPoint;
        [SerializeField, Tooltip("이 값을 넘는 방어력은 계산에 넣지 않는다")] private int _maxArmor;

        [Header("반동 카메라 흔들림")]
        [SerializeField, Tooltip("칸/도. 무기 반동(도/발)에 곱해 흔들림 세기를 정한다")] private float _cameraShakePerRecoilDegree;
        [SerializeField, Tooltip("초")] private float _cameraShakeDuration;

        public float CameraShakePerRecoilDegree => _cameraShakePerRecoilDegree;
        public float CameraShakeDuration => _cameraShakeDuration;
        public float ArmorReductionPerPoint => _armorReductionPerPoint;
        public int MaxArmor => _maxArmor;
    }
}
