using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 피격 시 바닥에 혈흔을 남긴다. 판이 끝날 때까지 남기되 개수 상한을 넘으면 오래된 것부터 지운다.
    public class BloodSplatter : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private PlayerData _data;
        [SerializeField] private GameObject _decalPrefab;

        private readonly Queue<GameObject> _decals = new Queue<GameObject>();

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            // 캐릭터를 따라다니면 안 되므로 자식으로 두지 않는다.
            Quaternion rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            _decals.Enqueue(Instantiate(_decalPrefab, transform.position, rotation));

            while (_decals.Count > _data.MaxBloodDecals)
            {
                Destroy(_decals.Dequeue());
            }
        }
    }
}
