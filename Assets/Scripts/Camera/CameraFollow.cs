using UnityEngine;

namespace EscapeFromSpace
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private CameraShake _shake;

        // 플레이어 이동이 끝난 뒤에 따라가야 화면이 떨리지 않는다.
        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 position = _target.position;
            if (_shake != null)
            {
                position += (Vector3)_shake.Offset;
            }

            position.z = transform.position.z;
            transform.position = position;
        }
    }
}
