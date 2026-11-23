using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 화면 틀의 색 (2026-11-17 디자인). 낡은 정거장의 단말기 같은 호박색 선에 어두운 남보라 바탕이다.
    // 맵의 벽(청록)과 바닥(회색)에 없는 색을 골라서 가방과 창이 맵에 묻히지 않게 했다.
    public static class UiTheme
    {
        public static readonly Color PanelFill = new Color(0.075f, 0.06f, 0.11f, 1f);
        public static readonly Color PanelBorder = new Color(1f, 0.68f, 0.22f, 1f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color Cell = new Color(0.38f, 0.33f, 0.5f, 1f);
        public static readonly Color SecureCell = new Color(0.22f, 0.5f, 0.4f, 1f);
        public static readonly Color SlotFill = new Color(0.1f, 0.085f, 0.15f, 0.92f);
        public static readonly Color SlotBorder = new Color(0.62f, 0.48f, 0.3f, 1f);
        public static readonly Color Text = new Color(1f, 0.95f, 0.86f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.58f, 0.66f, 1f);
        public static readonly Color Accent = new Color(1f, 0.74f, 0.3f, 1f);
        public static readonly Color ButtonFill = new Color(0.82f, 0.48f, 0.12f, 1f);
        public static readonly Color ButtonQuietFill = new Color(0.24f, 0.21f, 0.31f, 1f);
        public static readonly Color Dim = new Color(0.02f, 0.01f, 0.04f, 0.72f);
    }

    // 화면 요소를 코드로 만들 때 쓰는 공통 도구. 틀에 쓰는 그림(모서리를 깎은 판, 테두리, 고리)도 여기서 실행 중에 그린다.
    public static class UiBuilder
    {
        private const int FrameSize = 32;

        private static Font _font;
        private static Sprite _whiteSprite;
        private static Sprite _panelSprite;
        private static Sprite _borderSprite;
        private static Sprite _cellSprite;
        private static Sprite _gradeFrameSprite;
        private static Sprite _shadowSprite;
        private static Sprite _ringSprite;
        private static Sprite _vignetteSprite;

        // 기본 글꼴에는 한글이 없다. Windows에 깔린 글꼴을 불러 쓴다.
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

        // 왼쪽 위와 오른쪽 아래 모서리를 깎은 판. 어떤 크기로 늘려도 모서리가 그대로다.
        public static Sprite PanelSprite => Cached(ref _panelSprite, () => DrawFrame(10, 0, 1f));

        // 같은 모양의 테두리 선만 있는 그림. 판 위에 겹친다.
        public static Sprite BorderSprite => Cached(ref _borderSprite, () => DrawFrame(10, 3, 1f));

        // 칸 하나. 테두리가 안쪽보다 조금 밝아서 한 가지 색만 칠해도 칸의 경계가 보인다.
        public static Sprite CellSprite => Cached(ref _cellSprite, () => DrawFrame(4, 2, 0.62f));

        // 아이템 칸 위에 겹쳐 그리는 등급 테두리 (기획서 20.2). 칸과 같은 모서리에 선만 있다.
        public static Sprite GradeFrameSprite => Cached(ref _gradeFrameSprite, () => DrawFrame(4, 4, 1f));

        // 창 뒤에 깔아 맵과 떼어 놓는 부드러운 그림자
        public static Sprite ShadowSprite => Cached(ref _shadowSprite, DrawShadow);

        // 원 게이지의 테두리
        public static Sprite RingSprite => Cached(ref _ringSprite, () => DrawRing(128, 7f));

        // 화면 가장자리만 물드는 그림. 피격과 산소 부족을 알릴 때 쓴다.
        public static Sprite VignetteSprite => Cached(ref _vignetteSprite, DrawVignette);

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

        // 틀이 있는 판. 바탕 이미지를 돌려준다 (칸의 색을 바꿀 때 쓴다). 테두리는 "Border" 자식이다.
        public static Image Panel(RectTransform rect, Color fill, Color border)
        {
            Image back = Sliced(rect, fill, PanelSprite);
            RectTransform frame = Stretch("Border", rect);
            Sliced(frame, border, BorderSprite);
            return back;
        }

        // 맵 위에 뜨는 큰 창. 뒤에 그림자를 깔아 맵의 색과 떼어 놓는다.
        public static Image Window(RectTransform rect)
        {
            RectTransform shadow = Stretch("Shadow", rect);
            shadow.offsetMin = new Vector2(-22f, -22f);
            shadow.offsetMax = new Vector2(22f, 22f);
            Sliced(shadow, UiTheme.Shadow, ShadowSprite);

            RectTransform body = Stretch("Body", rect);
            return Panel(body, UiTheme.PanelFill, UiTheme.PanelBorder);
        }

        // 아이템 칸 하나
        public static Image Cell(RectTransform rect, Color color)
        {
            return Sliced(rect, color, CellSprite);
        }

        // 칸에 등급 색 테두리를 덧그린다.
        public static Image GradeFrame(RectTransform rect, Color color)
        {
            RectTransform frame = Stretch("Grade", rect);
            return Sliced(frame, color, GradeFrameSprite);
        }

        public static RectTransform Button(string label, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size, bool primary)
        {
            RectTransform button = Rect(label, parent, anchor, anchor, position, size);
            Panel(button, primary ? UiTheme.ButtonFill : UiTheme.ButtonQuietFill, primary ? UiTheme.Accent : UiTheme.SlotBorder);
            Text text = Text(label, button, 28, TextAnchor.MiddleCenter, UiTheme.Text);
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        // 화면 전체를 덮는 어두운 막. 창이 떠 있는 동안 뒤의 게임을 가린다.
        public static RectTransform Dim(string name, Transform parent)
        {
            RectTransform dim = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f));
            Image(dim, UiTheme.Dim);
            return dim;
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
            RectTransform rect = Stretch("Text", parent);
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

        // 그림이나 밝은 색 위에서도 읽히게 글자에 어두운 테두리를 두른다.
        public static void Outline(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // 왼쪽에서 오른쪽으로 차오르는 진행 바. 채워지는 쪽을 돌려주고, 배경은 그 부모다.
        public static Image Bar(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color fillColor)
        {
            RectTransform back = Rect(name, parent, anchor, pivot, position, size);
            Cell(back, UiTheme.SlotFill);

            RectTransform fillRect = Rect("Fill", back, Vector2.zero, Vector2.zero, new Vector2(3f, 3f), size - new Vector2(6f, 6f));
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

        // ---------- 틀 그림 ----------

        public static RectTransform Stretch(string name, RectTransform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image Sliced(RectTransform rect, Color color, Sprite sprite)
        {
            Image image = Image(rect, color, sprite);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        // 씬이 바뀌면 실행 중에 만든 그림이 지워질 수 있어서 쓸 때마다 살아 있는지 본다.
        private static Sprite Cached(ref Sprite sprite, System.Func<Sprite> draw)
        {
            if (sprite == null)
            {
                sprite = draw();
            }

            return sprite;
        }

        // 왼쪽 위와 오른쪽 아래를 chamfer만큼 깎은 사각형. border가 0이면 속을 채우고, 아니면 그 두께의 선만 그린다.
        // inner는 선 안쪽의 밝기다 (1이면 선과 같은 밝기, 0이면 투명).
        private static Sprite DrawFrame(int chamfer, int border, float inner)
        {
            var texture = NewTexture(FrameSize, FrameSize);
            var pixels = new Color32[FrameSize * FrameSize];

            for (int y = 0; y < FrameSize; y++)
            {
                for (int x = 0; x < FrameSize; x++)
                {
                    float depth = FrameDepth(x + 0.5f, y + 0.5f, chamfer);
                    float alpha = Mathf.Clamp01(depth + 0.5f);
                    float brightness = 1f;

                    if (border > 0)
                    {
                        // 선 안쪽: inner가 1이면 선만 남기고 비우고, 아니면 그 밝기로 채운다.
                        float inside = Mathf.Clamp01(depth - border + 0.5f);
                        if (inner >= 1f)
                        {
                            alpha *= 1f - inside;
                        }
                        else
                        {
                            brightness = Mathf.Lerp(1f, inner, inside);
                        }
                    }

                    byte value = (byte)Mathf.RoundToInt(brightness * 255f);
                    pixels[y * FrameSize + x] = new Color32(value, value, value, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            float slice = chamfer + border + 1;
            return Sprite.Create(texture, new Rect(0f, 0f, FrameSize, FrameSize), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }

        // 깎은 사각형의 가장자리에서 안쪽으로 들어온 거리(픽셀). 밖이면 음수다.
        private static float FrameDepth(float x, float y, int chamfer)
        {
            float depth = Mathf.Min(Mathf.Min(x, FrameSize - x), Mathf.Min(y, FrameSize - y));

            // 텍스처의 y는 아래에서 위로 간다. 왼쪽 위와 오른쪽 아래의 대각선까지의 거리도 잰다.
            float topLeft = (x + (FrameSize - y) - chamfer) * 0.7071f;
            float bottomRight = ((FrameSize - x) + y - chamfer) * 0.7071f;
            return Mathf.Min(depth, Mathf.Min(topLeft, bottomRight));
        }

        private static Sprite DrawShadow()
        {
            const int size = 64;
            const float margin = 22f;
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float depth = Mathf.Min(Mathf.Min(x + 0.5f, size - x - 0.5f), Mathf.Min(y + 0.5f, size - y - 0.5f));
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(depth / margin));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(margin + 2f, margin + 2f, margin + 2f, margin + 2f));
        }

        private static Sprite DrawRing(int size, float thickness)
        {
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01(radius - distance) * Mathf.Clamp01(distance - (radius - thickness));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite DrawVignette()
        {
            const int size = 64;
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 가운데는 비우고 가장자리로 갈수록 진해진다. 화면이 가로로 길어도 네 변이 고르게 물들도록 변까지의 거리로 잰다.
                    float edge = Mathf.Max(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half)) / half;
                    float corner = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / (half * 1.4142f);
                    float ramp = Mathf.Clamp01((Mathf.Max(edge, corner * 1.1f) - 0.7f) / 0.3f);
                    float alpha = ramp * ramp;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }
    }
}
