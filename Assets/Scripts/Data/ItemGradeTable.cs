using UnityEngine;

namespace EscapeFromSpace
{
    // 아이템 한 개의 가치로 정하는 등급과 색 (기획서 20.2). 어느 것이 값진지 칸만 보고 알게 하려는 것이다.
    [CreateAssetMenu(fileName = "ItemGrades", menuName = "EscapeFromSpace/Item Grade Table")]
    public class ItemGradeTable : ScriptableObject
    {
        [System.Serializable]
        public class Grade
        {
            [SerializeField] private string _name;
            [SerializeField, Tooltip("가치가 이 값 이상이면 이 등급 (위 줄부터 차례로 본다)")] private int _minValue;
            [SerializeField] private Color _color = Color.gray;

            public Grade()
            {
            }

            public Grade(string name, int minValue, Color color)
            {
                _name = name;
                _minValue = minValue;
                _color = color;
            }

            public string Name => _name;
            public int MinValue => _minValue;
            public Color Color => _color;
        }

        [SerializeField, Tooltip("낮은 등급부터 적는다")] private Grade[] _grades;

        // 가장 높은 기준을 넘는 등급. 표가 비었으면 null.
        public Grade Of(ItemData item)
        {
            if (item == null || _grades == null)
            {
                return null;
            }

            Grade result = null;
            foreach (Grade grade in _grades)
            {
                if (item.Value >= grade.MinValue)
                {
                    result = grade;
                }
            }

            return result;
        }
    }
}
