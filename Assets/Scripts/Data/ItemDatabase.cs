using UnityEngine;

namespace EscapeFromSpace
{
    // 저장 파일에는 아이템을 에셋 이름으로 적는다. 불러올 때 이름으로 에셋을 다시 찾는 목록이다.
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "EscapeFromSpace/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemData[] _items;

        // 에셋 이름을 바꾸거나 지운 아이템은 null이 나온다. 그 아이템은 저장 파일에서 건너뛴다.
        public ItemData Find(string id)
        {
            foreach (ItemData item in _items)
            {
                if (item != null && item.name == id)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
