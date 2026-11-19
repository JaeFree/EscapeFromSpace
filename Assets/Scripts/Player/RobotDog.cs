using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 플레이어를 따라다니는 로봇 개 (기획서 19.5). 싸우지 않고 가방(보관함 역할)만 진다.
    // 적은 개를 보지도 공격하지도 않으므로 충돌체 없이 그림만 움직인다. 벽을 뚫지 않게 플레이어가 지나간 자리를 밟아 따라간다.
    public class RobotDog : MonoBehaviour
    {
        private const float FollowDistance = 2f;
        private const float TeleportDistance = 12f;
        private const float TrailSpacing = 0.3f;
        private const float Speed = 7f;

        private readonly Queue<Vector2> _trail = new Queue<Vector2>();
        private Transform _owner;
        private SpriteRenderer _renderer;
        private Vector2 _lastMark;
        private float _bob;

        public static void Ensure(Transform owner, Sprite sprite)
        {
            foreach (RobotDog existing in FindObjectsByType<RobotDog>(FindObjectsSortMode.None))
            {
                if (existing._owner == owner)
                {
                    return;
                }
            }

            var dog = new GameObject("RobotDog");
            dog.transform.position = owner.position + new Vector3(-1f, -1f, 0f);
            var component = dog.AddComponent<RobotDog>();
            component._owner = owner;
            component._lastMark = owner.position;
            component._renderer = dog.AddComponent<SpriteRenderer>();
            component._renderer.sprite = sprite;
            component._renderer.sortingOrder = 4;
            if (sprite != null)
            {
                float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                dog.transform.localScale = Vector3.one * (0.8f / Mathf.Max(0.01f, size));
            }
        }

        private void Update()
        {
            if (_owner == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 owner = _owner.position;
            if (Vector2.Distance(owner, _lastMark) >= TrailSpacing)
            {
                _trail.Enqueue(owner);
                _lastMark = owner;
            }

            Vector2 position = transform.position;
            float distance = Vector2.Distance(position, owner);
            if (distance > TeleportDistance)
            {
                // 너무 멀어지면 (탈출 포드 이동, 구르기 연속 등) 옆으로 순간 이동한다.
                _trail.Clear();
                transform.position = owner + new Vector2(-1f, -0.6f);
                return;
            }

            // 가까우면 멈추고, 멀면 자취를 따라 걷는다. 지나간 자취 점은 버린다.
            while (_trail.Count > 0 && Vector2.Distance(position, _trail.Peek()) < 0.15f)
            {
                _trail.Dequeue();
            }

            bool moving = distance > FollowDistance && _trail.Count > 0;
            if (moving)
            {
                Vector2 target = _trail.Peek();
                Vector2 next = Vector2.MoveTowards(position, target, Speed * Time.deltaTime);
                Vector2 step = next - position;
                transform.position = next;
                if (step.sqrMagnitude > 0.00001f)
                {
                    // 그림은 앞이 아래를 본다. 가는 쪽을 보게 돌린다.
                    float angle = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg + 90f;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }

            // 걸을 때 살짝 들썩인다.
            _bob = moving ? _bob + Time.deltaTime * 14f : 0f;
            _renderer.transform.localPosition = Vector3.zero;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x * (1f + Mathf.Sin(_bob) * 0.06f), 1f);
        }
    }
}
