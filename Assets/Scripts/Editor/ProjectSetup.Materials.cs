using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 강화 재료 3종의 그림 (2026-11-18 사용자 요청: 이름에 맞는 그림으로).
    // 합금은 쌓은 금속 주괴, 섬유는 실이 감긴 실패, 결정은 빛나는 결정 무더기로 그린다.
    public static partial class ProjectSetup
    {
        private const int MaterialIconSize = 128;

        // 처음 그렸던 추상 도형. 아이템이 아직 이것을 쓰고 있을 때만 새 그림으로 바꾼다 (Inspector에서 바꾼 그림은 두기 위해).
        private static readonly string[] OldMaterialIcons = { "Alloy.png", "NanoFiber.png", "EnergyCrystal.png" };

        private static void EnsureMaterialIcons()
        {
            ReplaceIcon("Alloy", OldMaterialIcons[0], ItemArtFolder + "/AlloyIngots.png", DrawIngots);
            ReplaceIcon("NanoFiber", OldMaterialIcons[1], ItemArtFolder + "/NanoFiberSpool.png", DrawSpool);
            ReplaceIcon("EnergyCrystal", OldMaterialIcons[2], ItemArtFolder + "/EnergyCrystals.png", DrawCrystals);

            // 옛 그림을 아무도 쓰지 않으면 지운다. 남겨 두면 어느 것이 쓰이는지 헷갈린다.
            foreach (string file in OldMaterialIcons)
            {
                string path = ItemArtFolder + "/" + file;
                if (File.Exists(path) && !IsSpriteUsedByItems(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static void ReplaceIcon(string itemName, string oldFile, string newPath, ColorAt draw)
        {
            Sprite sprite = EnsureSmoothSprite(newPath, MaterialIconSize, draw);
            ItemData item = LoadItem(itemName);
            if (item == null)
            {
                return;
            }

            var so = new SerializedObject(item);
            SerializedProperty icon = so.FindProperty("_icon");
            string current = icon.objectReferenceValue != null ? AssetDatabase.GetAssetPath(icon.objectReferenceValue) : string.Empty;
            if (icon.objectReferenceValue == null || current.EndsWith("/" + oldFile))
            {
                icon.objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static bool IsSpriteUsedByItems(string path)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                var icon = new SerializedObject(item).FindProperty("_icon").objectReferenceValue;
                if (icon != null && AssetDatabase.GetAssetPath(icon) == path)
                {
                    return true;
                }
            }

            return false;
        }

        // 한 점에서 3×3번 재서 평균을 낸다. 가는 선과 비스듬한 모서리가 계단처럼 보이지 않게 하려는 것이다.
        private static Sprite EnsureSmoothSprite(string path, int size, ColorAt draw)
        {
            if (!File.Exists(path))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                float half = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Color sum = Color.clear;
                        for (int sy = 0; sy < 3; sy++)
                        {
                            for (int sx = 0; sx < 3; sx++)
                            {
                                Color c = draw((x + (sx + 0.5f) / 3f - half) / half, (y + (sy + 0.5f) / 3f - half) / half);

                                // 투명한 곳의 색이 가장자리에 번지지 않게 알파를 곱해 더한다.
                                sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                            }
                        }

                        sum /= 9f;
                        pixels[y * size + x] = sum.a > 0f ? new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, sum.a) : Color.clear;
                    }
                }

                texture.SetPixels(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- 그리는 도구 ----------

        // 위에 칠한 색을 알파로 덮는다.
        private static Color Over(Color bottom, Color top)
        {
            float a = top.a + bottom.a * (1f - top.a);
            if (a <= 0f)
            {
                return Color.clear;
            }

            Color rgb = (top * top.a + bottom * bottom.a * (1f - top.a)) / a;
            rgb.a = a;
            return rgb;
        }

        // 볼록 다각형까지의 거리 (안쪽이 음수). 꼭짓점은 시계 반대 방향.
        private static float PolygonDistance(float x, float y, Vector2[] points)
        {
            float distance = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Length];
                Vector2 edge = (b - a).normalized;
                var normal = new Vector2(edge.y, -edge.x);
                distance = Mathf.Max(distance, Vector2.Dot(new Vector2(x, y) - a, normal));
            }

            return distance;
        }

        // 안쪽이면 1. 테두리 두께만큼 어두운 선을 두른다.
        private static Color Fill(float distance, Color fill, Color outline, float outlineWidth)
        {
            if (distance > 0f)
            {
                return Color.clear;
            }

            Color color = distance > -outlineWidth ? outline : fill;
            color.a = 1f;
            return color;
        }

        // ---------- 강화 합금: 금속 주괴 3개를 피라미드로 쌓음 ----------

        private static Color DrawIngots(float x, float y)
        {
            Color color = Color.clear;

            // 바닥 그림자
            float shadow = Mathf.Sqrt(x * x / 0.8f + (y + 0.66f) * (y + 0.66f) / 0.012f);
            color = Over(color, new Color(0f, 0f, 0f, Mathf.Clamp01(1f - shadow) * 0.45f));

            color = Over(color, Ingot(x, y, -0.43f, -0.52f));
            color = Over(color, Ingot(x, y, 0.43f, -0.52f));
            color = Over(color, Ingot(x, y, 0f, -0.06f));
            return color;
        }

        // 비스듬히 위에서 본 주괴 하나. 윗면(밝음)은 좁은 사다리꼴, 앞면(어두움)은 넓은 사다리꼴.
        private static Color Ingot(float x, float y, float cx, float cy)
        {
            float px = x - cx;
            float py = y - cy;
            var outline = new Color(0.08f, 0.09f, 0.12f);

            Vector2[] front =
            {
                new Vector2(-0.42f, -0.14f), new Vector2(0.42f, -0.14f), new Vector2(0.36f, 0.06f), new Vector2(-0.36f, 0.06f),
            };
            Vector2[] top =
            {
                new Vector2(-0.36f, 0.06f), new Vector2(0.36f, 0.06f), new Vector2(0.27f, 0.3f), new Vector2(-0.27f, 0.3f),
            };

            float frontDistance = PolygonDistance(px, py, front);
            float topDistance = PolygonDistance(px, py, top);

            // 위쪽이 밝고 아래가 어두운 강철. 비스듬한 반사 띠를 한 줄 넣어 금속처럼 보이게 한다.
            float streak = Mathf.Clamp01(1f - Mathf.Abs(px * 0.9f + py - 0.05f) / 0.05f);
            var steelDark = new Color(0.36f, 0.4f, 0.47f);
            var steelLight = new Color(0.8f, 0.84f, 0.9f);

            Color result = Color.clear;
            if (frontDistance <= 0.02f)
            {
                Color face = Color.Lerp(steelDark * 0.8f, steelDark, Mathf.InverseLerp(-0.14f, 0.06f, py));
                face = Color.Lerp(face, steelLight, streak * 0.35f);
                face.a = 1f;
                result = Over(result, Fill(frontDistance - 0.02f, face, outline, 0.025f));
            }

            if (topDistance <= 0.02f)
            {
                Color face = Color.Lerp(steelLight * 0.88f, steelLight, Mathf.InverseLerp(0.06f, 0.3f, py));
                face = Color.Lerp(face, Color.white, streak * 0.6f);

                // 윗면에 찍힌 각인 (작은 사각 홈)
                if (Mathf.Abs(px) < 0.12f && Mathf.Abs(py - 0.18f) < 0.045f)
                {
                    face = Color.Lerp(face, steelDark, Mathf.Abs(py - 0.18f) < 0.025f && Mathf.Abs(px) < 0.1f ? 0.55f : 0.3f);
                }

                face.a = 1f;
                result = Over(result, Fill(topDistance - 0.02f, face, outline, 0.025f));
            }

            return result;
        }

        // ---------- 나노 섬유: 빛나는 실이 감긴 실패와 풀린 실 한 가닥 ----------

        private static Color DrawSpool(float x, float y)
        {
            Color color = Color.clear;
            var outline = new Color(0.07f, 0.08f, 0.1f);
            var metal = new Color(0.45f, 0.48f, 0.55f);
            var thread = new Color(0.25f, 0.95f, 0.85f);

            // 뒤로 은은한 빛
            float glow = Mathf.Clamp01(1f - Mathf.Sqrt(x * x * 1.6f + y * y) / 0.95f);
            color = Over(color, new Color(thread.r, thread.g, thread.b, glow * glow * 0.45f));

            // 풀린 실: 실패 오른쪽 아래에서 물결치며 뻗는다
            if (x > 0.2f)
            {
                float curve = -0.12f - (x - 0.2f) * 0.85f + Mathf.Sin((x - 0.2f) * 9f) * 0.07f;
                float strand = Mathf.Abs(y - curve);
                if (strand < 0.045f)
                {
                    Color line = strand < 0.022f ? Color.Lerp(thread, Color.white, 0.4f) : thread * 0.6f;
                    line.a = x < 0.9f ? 1f : Mathf.Clamp01((0.98f - x) / 0.08f);
                    color = Over(color, line);
                }
            }

            // 실패의 아래 테 (타원)
            color = Over(color, Flange(x, y, -0.6f, metal, outline));

            // 감긴 실: 원통. 가운데가 밝고 양쪽이 어둡고, 비스듬한 줄무늬가 감긴 결을 만든다.
            if (Mathf.Abs(x) < 0.44f && y > -0.6f && y < 0.56f)
            {
                float edge = Mathf.Abs(x) / 0.44f;
                float light = Mathf.Sqrt(Mathf.Max(0f, 1f - edge * edge));
                float wind = 0.5f + 0.5f * Mathf.Sin(y * 70f + x * 12f);
                Color body = Color.Lerp(thread * 0.35f, thread, light);
                body = Color.Lerp(body, body * 0.65f, wind * 0.5f);
                body = Color.Lerp(body, Color.white, Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(x + 0.14f) / 0.08f), 2f) * 0.55f);
                body.a = 1f;
                if (Mathf.Abs(x) > 0.41f)
                {
                    body = outline;
                }

                color = Over(color, body);
            }

            // 위 테
            color = Over(color, Flange(x, y, 0.56f, metal, outline));

            // 가운데 축 구멍
            float hole = Mathf.Sqrt(x * x / 0.012f + (y - 0.56f) * (y - 0.56f) / 0.0035f);
            if (hole < 1f)
            {
                color = Over(color, new Color(0.05f, 0.05f, 0.07f, 1f));
            }

            return color;
        }

        // 실패 끝의 둥근 금속 테. 위에서 비스듬히 보아 납작한 타원이다.
        private static Color Flange(float x, float y, float cy, Color metal, Color outline)
        {
            float rx = 0.62f;
            float ry = 0.17f;
            float thickness = 0.08f;

            // 테의 옆면 (두께)
            float side = Mathf.Max(Mathf.Abs(x) / rx, 0f);
            bool inSide = side < 1f && y < cy && y > cy - thickness - ry * Mathf.Sqrt(Mathf.Max(0f, 1f - side * side));
            float top = Mathf.Sqrt(x * x / (rx * rx) + (y - cy) * (y - cy) / (ry * ry));

            if (top < 1f)
            {
                if (top > 0.9f)
                {
                    return outline;
                }

                Color face = Color.Lerp(metal * 1.35f, metal, top);
                face.a = 1f;
                return face;
            }

            if (inSide)
            {
                Color face = metal * Mathf.Lerp(0.85f, 0.55f, side);
                face.a = 1f;
                return side > 0.95f ? outline : face;
            }

            return Color.clear;
        }

        // ---------- 에너지 결정: 빛나는 육각 결정 세 개가 돌 받침에서 솟음 ----------

        private static Color DrawCrystals(float x, float y)
        {
            Color color = Color.clear;
            var core = new Color(0.45f, 0.95f, 1f);
            var deep = new Color(0.45f, 0.2f, 0.9f);

            // 뒤로 퍼지는 빛
            float glow = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + (y - 0.05f) * (y - 0.05f)) / 0.95f);
            color = Over(color, new Color(core.r, core.g, core.b, glow * glow * 0.55f));

            color = Over(color, Shard(x, y, -0.36f, -0.5f, 0.17f, 0.72f, 0.42f, core, deep));
            color = Over(color, Shard(x, y, 0.38f, -0.5f, 0.16f, 0.62f, -0.48f, core, deep));
            color = Over(color, Shard(x, y, 0f, -0.55f, 0.22f, 1.25f, 0.05f, core, deep));

            // 받침 돌
            float rock = Mathf.Sqrt(x * x / 0.42f + (y + 0.62f) * (y + 0.62f) / 0.03f);
            if (rock < 1f)
            {
                Color stone = Color.Lerp(new Color(0.32f, 0.3f, 0.36f), new Color(0.16f, 0.15f, 0.2f), Mathf.InverseLerp(-0.55f, -0.75f, y));
                stone.a = 1f;
                color = Over(color, rock > 0.9f ? new Color(0.06f, 0.05f, 0.08f, 1f) : stone);
            }

            return color;
        }

        // 뾰족한 육각 기둥 하나. (bx, by)는 뿌리, height는 길이, tilt는 기운 각(라디안, 왼쪽이 양수).
        private static Color Shard(float x, float y, float bx, float by, float halfWidth, float height, float tilt, Color core, Color deep)
        {
            // 결정의 좌표로 돌린다: u는 폭 방향, v는 길이 방향
            float dx = x - bx;
            float dy = y - by;
            float cos = Mathf.Cos(tilt);
            float sin = Mathf.Sin(tilt);
            float u = dx * cos + dy * sin;
            float v = -dx * sin + dy * cos;

            float tip = height * 0.22f;
            Vector2[] shape =
            {
                new Vector2(-halfWidth, 0f), new Vector2(halfWidth, 0f), new Vector2(halfWidth, height - tip),
                new Vector2(0f, height), new Vector2(-halfWidth, height - tip),
            };

            float distance = PolygonDistance(u, v, shape);
            if (distance > 0f)
            {
                return Color.clear;
            }

            var outline = new Color(0.12f, 0.06f, 0.25f, 1f);
            if (distance > -0.022f)
            {
                return outline;
            }

            // 가운데 모서리를 기준으로 왼쪽 면은 짙고 오른쪽 면은 밝다. 뿌리에서 끝으로 갈수록 빛이 강해진다.
            float along = Mathf.Clamp01(v / height);
            bool leftFace = u < -halfWidth * 0.25f;
            bool ridge = Mathf.Abs(u + halfWidth * 0.25f) < 0.018f;
            Color face = leftFace ? Color.Lerp(deep, core * 0.75f, along * 0.6f) : Color.Lerp(deep * 1.3f, core, 0.35f + along * 0.65f);
            if (ridge)
            {
                face = Color.Lerp(face, Color.white, 0.75f);
            }

            // 안쪽에서 빛나는 심
            float heart = Mathf.Clamp01(1f - Mathf.Abs(u - halfWidth * 0.3f) / (halfWidth * 0.35f)) * Mathf.Clamp01(1f - Mathf.Abs(along - 0.55f) / 0.35f);
            face = Color.Lerp(face, Color.white, heart * 0.45f);
            face.a = 1f;
            return face;
        }
    }
}
