using UnityEngine;

namespace EscapeFromSpace
{
    // 컨테이너·잔해·버린 아이템을 E로 여는 일과 아이템을 바닥에 버리는 일을 맡는다 (기획서 6.2).
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _controller;
        [SerializeField] private Health _health;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField, Tooltip("칸. 이 거리 안에 있어야 열 수 있고, 벗어나면 닫힌다")] private float _range = 1.5f;
        [SerializeField] private Lootable _droppedItemPrefab;

        // 뒤지는 소리가 되풀이되는 간격(초)
        private const float SearchSoundInterval = 0.35f;

        private float _holdTime;
        private float _nextSearchSoundTime;
        private float _nextHackNoiseTime;

        // 지금 열 수 있는 가장 가까운 대상. 없으면 null.
        public Lootable Target { get; private set; }

        // 열려 있는 대상. 화면(InventoryUI)이 이것을 보고 루팅 칸을 띄운다.
        public Lootable Opened { get; private set; }

        // E를 누르고 있는 진행도 0~1
        public float HoldProgress => Target != null && Target.OpenTime > 0f ? Mathf.Clamp01(_holdTime / OpenTime) : 0f;

        // 지금 대상을 여는 데 걸리는 시간. 강화한 만큼 짧아진다.
        private float OpenTime => Target.OpenTime * _progress.TimeMultiplier(TechStat.LootSpeed);

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
        }

        private void Update()
        {
            Vector2 position = transform.position;

            // 열어 둔 대상이 사라졌으면(버린 아이템을 다 줍거나 시간이 다 됨) 닫힌 것으로 본다.
            if (!ReferenceEquals(Opened, null) && Opened == null)
            {
                Opened = null;
            }

            if (Opened != null)
            {
                // 범위를 벗어나거나 대상이 사라지면(버린 아이템의 시간이 다 됨) 닫는다.
                Target = null;
                _holdTime = 0f;
                if (_controller.IsDead || Vector2.Distance(position, Opened.transform.position) > _range)
                {
                    Opened = null;
                }

                return;
            }

            Lootable nearest = _controller.IsDead ? null : Lootable.FindNearest(position, _range);
            if (nearest != Target)
            {
                Target = nearest;
                _holdTime = 0f;
            }

            if (Target == null || !_input.InteractHeld)
            {
                _holdTime = 0f;
                _nextSearchSoundTime = 0f;
                _nextHackNoiseTime = 0f;
                return;
            }

            // 함선 단말기를 해킹하는 동안 1초마다 소리가 난다. 둘레의 로봇이 의심하고 다가온다 (기획서 19.6).
            if (!Target.IsOpened && Target.HackNoiseRadius > 0f && _holdTime >= _nextHackNoiseTime)
            {
                _nextHackNoiseTime = _holdTime + 1f;
                Footsteps.Emit(Target.transform.position, Target.HackNoiseRadius);
            }

            // 처음 여는 것은 뒤지는 동안 부스럭거리는 소리가 되풀이된다.
            if (!Target.IsOpened && Target.OpenTime > 0f && _holdTime >= _nextSearchSoundTime)
            {
                _nextSearchSoundTime = _holdTime + SearchSoundInterval;
                Sfx.PlayAt(SfxId.LootSearch, Target.transform.position);
            }

            _holdTime += Time.deltaTime;
            if (Target.IsOpened || _holdTime >= OpenTime)
            {
                Sfx.PlayAt(SfxId.LootOpen, Target.transform.position);
                Target.IsOpened = true;
                Opened = Target;
                Target = null;
                _holdTime = 0f;
                _nextSearchSoundTime = 0f;
            }
        }

        public void Close()
        {
            Opened = null;
        }

        // 가방 화면 밖에 놓은 아이템을 발밑에 남긴다. 다시 주울 수 있고 시간이 지나면 사라진다.
        public void DropToWorld(ItemStack stack)
        {
            // 여러 개를 버려도 한 점에 겹치지 않게 조금 흩는다.
            Vector2 position = (Vector2)transform.position + Random.insideUnitCircle * 0.3f;
            Lootable dropped = Instantiate(_droppedItemPrefab, position, Quaternion.identity);
            dropped.SetContents(stack);
            dropped.IsOpened = true;

            var renderer = dropped.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            // 그림이 있으면 그 그림으로, 없으면 종류 색 사각형으로 보인다. 그림의 크기는 원래 사각형의 크기에 맞춘다.
            if (stack.Data.Icon != null)
            {
                float size = renderer.bounds.size.x;
                renderer.sprite = stack.Data.Icon;
                renderer.color = Color.white;
                Vector2 spriteSize = stack.Data.Icon.bounds.size;
                float scale = size / Mathf.Max(spriteSize.x, spriteSize.y);
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                renderer.color = stack.Data.Color;
            }
        }

        // 여는 동안 맞으면 취소된다.
        private void OnDamaged(float damage, Vector2 hitPoint)
        {
            _holdTime = 0f;
            _nextSearchSoundTime = 0f;
        }
    }
}
