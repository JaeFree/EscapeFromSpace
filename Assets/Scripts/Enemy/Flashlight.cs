using UnityEngine;

namespace EscapeFromSpace
{
    // 정전된 배의 로봇이 비추는 손전등 원뿔 (기획서 19.4). 어둠 위에 그려서 플레이어가 로봇의 시선을 보고 피할 수 있다.
    public static class Flashlight
    {
        private static Sprite _cone;

        public static Transform Create(Transform owner, float length, float angle)
        {
            var light = new GameObject("Flashlight");
            light.transform.SetParent(owner, false);
            var renderer = light.AddComponent<SpriteRenderer>();
            renderer.sprite = Cone;
            renderer.color = new Color(1f, 0.92f, 0.6f, 0.28f);

            // 어둠(90)보다 위에 그린다.
            renderer.sortingOrder = 95;

            // 그림은 +X를 향한 부채꼴(반각 30°, 길이 1)이다. 시야각과 거리에 맞춰 늘린다.
            float width = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(30f * Mathf.Deg2Rad);
            light.transform.localScale = new Vector3(length, length * Mathf.Clamp(width, 0.3f, 3f), 1f);
            return light.transform;
        }

        private static Sprite Cone
        {
            get
            {
                if (_cone == null)
                {
                    const int size = 64;
                    var texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    float half = 30f * Mathf.Deg2Rad;
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            // 왼쪽 가운데가 꼭짓점, 오른쪽으로 펼쳐진다.
                            float dx = (x + 0.5f) / size;
                            float dy = ((y + 0.5f) / size - 0.5f) * 2f * Mathf.Tan(half);
                            float a = Mathf.Abs(Mathf.Atan2(dy, dx)) <= half && dx <= 1f ? 1f - dx * dx : 0f;
                            texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                        }
                    }

                    texture.Apply();
                    _cone = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0f, 0.5f), size);
                }

                return _cone;
            }
        }
    }
}
