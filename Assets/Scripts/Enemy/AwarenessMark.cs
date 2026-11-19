using UnityEngine;

namespace EscapeFromSpace
{
    // 로봇 머리 위의 "?"와 발견 게이지 (기획서 19.3). 그림 에셋 없이 코드로 만든 점 그림을 쓴다.
    public class AwarenessMark : MonoBehaviour
    {
        // 5×7 점으로 그린 물음표
        private static readonly string[] Glyph =
        {
            ".###.",
            "#...#",
            "....#",
            "...#.",
            "..#..",
            ".....",
            "..#..",
        };

        private static Sprite _glyph;
        private static Sprite _white;

        private SpriteRenderer _question;
        private SpriteRenderer _back;
        private SpriteRenderer _fill;

        public static AwarenessMark Create(Transform owner, float height)
        {
            var root = new GameObject("Awareness");
            root.transform.SetParent(owner, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);
            var mark = root.AddComponent<AwarenessMark>();
            mark.Build();
            mark.Set(0f);
            return mark;
        }

        // 0이면 숨기고, 0과 1 사이면 보인다. 다 차면 "!"(경계 표시)가 대신하므로 숨긴다.
        public void Set(float value)
        {
            bool visible = value > 0.02f && value < 1f;
            _question.enabled = _back.enabled = _fill.enabled = visible;
            if (!visible)
            {
                return;
            }

            _fill.transform.localScale = new Vector3(0.8f * value, 0.1f, 1f);
            _fill.transform.localPosition = new Vector3(-0.4f + 0.4f * value, -0.18f, 0f);
            _fill.color = Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.35f, 0.2f), value);
            _question.color = _fill.color;
        }

        private void Build()
        {
            _question = Part("Question", Glyph7, Vector3.zero, new Vector3(1f, 1f, 1f), Color.white);
            _back = Part("Back", White, new Vector3(0f, -0.18f, 0f), new Vector3(0.84f, 0.14f, 1f), new Color(0f, 0f, 0f, 0.7f));
            _fill = Part("Fill", White, new Vector3(0f, -0.18f, 0f), new Vector3(0.8f, 0.1f, 1f), Color.yellow);
            _fill.sortingOrder = _back.sortingOrder + 1;
        }

        private SpriteRenderer Part(string name, Sprite sprite, Vector3 position, Vector3 scale, Color color)
        {
            var part = new GameObject(name);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 60;
            return renderer;
        }

        private static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _white = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                }

                return _white;
            }
        }

        private static Sprite Glyph7
        {
            get
            {
                if (_glyph == null)
                {
                    var texture = new Texture2D(5, 7) { filterMode = FilterMode.Point };
                    for (int y = 0; y < 7; y++)
                    {
                        for (int x = 0; x < 5; x++)
                        {
                            // 그림 줄은 위에서 아래로, 텍스처는 아래에서 위로 센다.
                            bool on = Glyph[6 - y][x] == '#';
                            texture.SetPixel(x, y, on ? Color.white : Color.clear);
                        }
                    }

                    texture.Apply();
                    _glyph = Sprite.Create(texture, new Rect(0f, 0f, 5f, 7f), new Vector2(0.5f, 0f), 24f);
                }

                return _glyph;
            }
        }
    }
}
