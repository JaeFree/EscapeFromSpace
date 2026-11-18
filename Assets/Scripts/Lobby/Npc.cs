using UnityEngine;

namespace EscapeFromSpace
{
    // 대기실에 서 있는 상인 (기획서 14.2). 말을 걸면 MerchantUI가 이 상인의 창을 연다.
    public class Npc : MonoBehaviour
    {
        [SerializeField] private MerchantData _merchant;
        [SerializeField] private SpriteRenderer _renderer;

        public MerchantData Merchant => _merchant;

        private void Start()
        {
            if (_renderer != null && _merchant != null)
            {
                _renderer.color = _merchant.Tint;
            }
        }
    }
}
