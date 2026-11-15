using UnityEngine;

namespace EscapeFromSpace
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        // 플레이어 이동이 끝난 뒤에 따라가야 화면이 떨리지 않는다.
        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 position = _target.position;
            position.z = transform.position.z;
            transform.position = position;
        }
    }
}
