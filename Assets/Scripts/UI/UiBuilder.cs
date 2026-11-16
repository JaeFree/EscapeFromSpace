using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 화면 요소를 코드로 만들 때 쓰는 공통 도구. 단색 도형 단계라 프리팹 없이 코드로 그린다.
    public static class UiBuilder
    {
        private static Font _font;
        private static Sprite _whiteSprite;

        // 기본 글꼴에는 한글이 없다. 단색 도형 단계에서는 Windows에 깔린 글꼴을 불러 쓴다.
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 16);
                }

                return _font;
            }
        }

        // 채워지는 이미지는 스프라이트가 있어야 한다.
        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                return _whiteSprite;
            }
        }

        // anchor와 pivot을 같은 점으로 둔다. position은 그 점에서 잰 위치다.
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Image(RectTransform rect, Color color, Sprite sprite = null)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // 칸 가운데에 그리는 아이템 그림. 그림의 비율을 지키고, 그림이 없으면 보이지 않는다.
        public static Image Icon(RectTransform parent, float size)
        {
            RectTransform rect = Rect("Icon", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Image icon = Image(rect, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;
            return icon;
        }

        public static void SetIcon(Image icon, Sprite sprite, Color color)
        {
            icon.sprite = sprite;
            icon.color = color;
            icon.enabled = sprite != null;
        }

        // 부모를 가득 채우는 글자
        public static Text Text(string content, RectTransform parent, int fontSize, TextAnchor alignment, Color color)
        {
            var rect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);

            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.text = content;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        // 왼쪽에서 오른쪽으로 차오르는 진행 바. 채워지는 쪽을 돌려주고, 배경은 그 부모다.
        public static Image Bar(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color fillColor)
        {
            RectTransform back = Rect(name, parent, anchor, pivot, position, size);
            Image(back, new Color(0.08f, 0.09f, 0.12f, 0.9f));

            RectTransform fillRect = Rect("Fill", back, Vector2.zero, Vector2.zero, new Vector2(2f, 2f), size - new Vector2(4f, 4f));
            Image fill = Image(fillRect, fillColor, WhiteSprite);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            return fill;
        }

        // 캔버스가 화면에 바로 그려지면(Overlay) 카메라 없이 화면 좌표를 그대로 쓴다.
        public static Camera CameraOf(Canvas canvas)
        {
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        // 화면 좌표의 점으로 옮긴다. 캔버스 방식에 상관없이 맞도록 캔버스 기준 좌표로 바꿔 넣는다.
        public static void MoveToScreenPoint(Canvas canvas, RectTransform rect, Vector2 screenPoint)
        {
            var root = (RectTransform)canvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, CameraOf(canvas), out Vector2 local))
            {
                rect.localPosition = local;
            }
        }
    }
}
