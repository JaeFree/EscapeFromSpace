using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 대기실 상인 한 명 (기획서 14.2, 16장). 파는 물건, 제작법, 판매(암시장) 여부를 가진다.
    // 가격과 해금 퀘스트는 초기값이라 에셋의 Inspector에서 고친다.
    [CreateAssetMenu(fileName = "Merchant", menuName = "EscapeFromSpace/Merchant")]
    public class MerchantData : ScriptableObject
    {
        [Serializable]
        public class Offer
        {
            [SerializeField] private ItemData _item;
            [SerializeField, Tooltip("한 번 살 때 받는 수량. 0이면 아이템의 기본 수량 (탄약의 발 수)")] private int _count;
            [SerializeField, Tooltip("크레딧")] private int _price;
            [SerializeField, Tooltip("이 퀘스트를 끝내야 살 수 있다. 비우면 처음부터 판다")] private string _unlockQuest;

            public Offer(ItemData item, int count, int price, string unlockQuest)
            {
                _item = item;
                _count = count;
                _price = price;
                _unlockQuest = unlockQuest;
            }

            public ItemData Item => _item;
            public int Count => _count > 0 ? _count : (_item != null ? _item.DefaultCount : 1);
            public int Price => _price;
            public string UnlockQuest => _unlockQuest;
        }

        [Serializable]
        public class Recipe
        {
            [SerializeField] private ItemData _output;
            [SerializeField, Tooltip("만들어지는 수량. 0이면 아이템의 기본 수량")] private int _count;
            [SerializeField] private List<TechTree.Cost> _inputs = new List<TechTree.Cost>();
            [SerializeField, Tooltip("이 퀘스트를 끝내야 만들 수 있다. 비우면 처음부터 만든다")] private string _unlockQuest;

            public ItemData Output => _output;
            public int Count => _count > 0 ? _count : (_output != null ? _output.DefaultCount : 1);
            public IReadOnlyList<TechTree.Cost> Inputs => _inputs;
            public string UnlockQuest => _unlockQuest;
        }

        [SerializeField, Tooltip("저장·퀘스트에서 이 상인을 가리키는 이름. 정한 뒤에는 바꾸지 않는다")] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Tooltip("이름 옆에 붙는 하는 일 (무기상 등)")] private string _title;
        [SerializeField, TextArea(2, 5)] private string _greeting;
        [SerializeField, Tooltip("대기실에서 이 상인 그림에 입히는 색")] private Color _tint = Color.white;
        [SerializeField] private List<Offer> _offers = new List<Offer>();
        [SerializeField] private List<Recipe> _recipes = new List<Recipe>();

        [Header("암시장")]
        [SerializeField, Tooltip("켜면 물건을 사들인다 (판매 탭)")] private bool _buysItems;
        [SerializeField, Tooltip("무기·방어구를 팔 때 상점 가격에 곱하는 값")] private float _equipmentSellRate = 0.5f;
        [SerializeField, Tooltip("무기·방어구의 상점 가격을 찾아볼 상인들")] private List<MerchantData> _priceSources = new List<MerchantData>();

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Title => _title;
        public string Greeting => _greeting;
        public Color Tint => _tint;
        public IReadOnlyList<Offer> Offers => _offers;
        public IReadOnlyList<Recipe> Recipes => _recipes;
        public bool BuysItems => _buysItems;

        // 기획서 16장: 값은 가치 그대로, 무기·방어구는 상점 가격의 50%. 0이면 팔 수 없다.
        public int SellPrice(ItemStack stack)
        {
            ItemData item = stack.Data;
            if (item.CannotSell)
            {
                return 0;
            }

            bool equipment = item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Helmet
                || item.Category == ItemCategory.Vest || item.Category == ItemCategory.Attachment;
            if (equipment)
            {
                int price = EquipmentPrice(item);

                // 부착물을 단 채로 팔면 부착물 값도 같이 받는다.
                if (stack.Attachments != null)
                {
                    foreach (ItemData attachment in stack.Attachments)
                    {
                        if (attachment != null)
                        {
                            price += EquipmentPrice(attachment);
                        }
                    }
                }

                return price;
            }

            // 탄약 가치는 기본 수량 한 묶음의 값이다. 나눠 놓은 묶음은 그 비율만큼 받는다.
            if (item.MaxStack > 1 && item.DefaultCount > 1)
            {
                return Mathf.Max(1, Mathf.RoundToInt(item.Value * stack.Count / (float)item.DefaultCount));
            }

            return item.Value * stack.Count;
        }

        private int EquipmentPrice(ItemData item)
        {
            int shopPrice = FindShopPrice(item);
            return shopPrice > 0 ? Mathf.RoundToInt(shopPrice * _equipmentSellRate) : item.Value;
        }

        private int FindShopPrice(ItemData item)
        {
            foreach (MerchantData merchant in _priceSources)
            {
                if (merchant == null)
                {
                    continue;
                }

                foreach (Offer offer in merchant.Offers)
                {
                    if (offer.Item == item)
                    {
                        return offer.Price;
                    }
                }
            }

            return 0;
        }
    }
}
