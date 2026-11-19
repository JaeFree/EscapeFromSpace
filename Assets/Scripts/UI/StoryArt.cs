using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace EscapeFromSpace
{
    // 스토리 화면과 시작 화면의 장면. 장마다 하나를 고른다.
    public enum StoryShot
    {
        Stars,
        Station,
        Attack,
        Shelter,
        Hangar,
        Launch,
        Alarm,
        Escape,
        Farewell,
    }

    // 화면에 올린 장면 하나. 배경 한 장과 그 위에서 움직이는 조각들이다.
    public sealed class StoryStage
    {
        private readonly List<Action<float>> _animations = new List<Action<float>>();

        public StoryStage(RectTransform root)
        {
            Root = root;
        }

        public RectTransform Root { get; }

        public void Animate(Action<float> animation)
        {
            _animations.Add(animation);
        }

        // time은 장면이 나타난 뒤 흐른 시간(초). 게임이 멈춘 동안에도 돌아야 해서 부르는 쪽이 실제 시간을 넘긴다.
        public void Tick(float time)
        {
            foreach (Action<float> animation in _animations)
            {
                animation(time);
            }
        }

        public void Destroy()
        {
            Object.Destroy(Root.gameObject);
        }
    }

    // 프롤로그·엔딩·시작 화면의 그림을 코드로 그린다 (기획서 18장: 맞는 CC0 그림이 없으면 코드로 그린다).
    // 480×270 픽셀에 그린 뒤 점 필터로 4배 키워, 게임 맵과 같은 픽셀아트 느낌을 낸다.
    public static class StoryArt
    {
        public const int Width = 480;
        public const int Height = 270;
        public const float PixelSize = 4f;

        private static readonly Dictionary<StoryShot, Sprite> Backgrounds = new Dictionary<StoryShot, Sprite>();
        private static Sprite _glow;
        private static Sprite _pod;

        // 생존자 다섯과 플레이어의 우주복 색. 상인 색과 맞춘다.
        private static readonly Color32[] SuitColors =
        {
            new Color32(200, 120, 70, 255),
            new Color32(90, 150, 210, 255),
            new Color32(120, 190, 120, 255),
            new Color32(210, 190, 90, 255),
            new Color32(170, 110, 200, 255),
            new Color32(235, 140, 40, 255),
        };

        // 정거장 그림의 가운데에서 잰 불빛 자리 (크기 1 기준): 기둥 꼭대기·아래, 전지판 끝, 고리 양 끝
        private static readonly Vector2[] StationLights =
        {
            new Vector2(0f, 64f), new Vector2(0f, -45f), new Vector2(-70f, 34f), new Vector2(70f, 34f), new Vector2(-70f, 0f), new Vector2(70f, 0f),
        };

        public static StoryStage Build(StoryShot shot, RectTransform parent)
        {
            RectTransform root = UiBuilder.Rect($"Shot_{shot}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(Width * PixelSize, Height * PixelSize));
            Image background = UiBuilder.Image(root, Color.white, Background(shot));
            background.preserveAspect = false;
            var stage = new StoryStage(root);

            // 모든 장면이 천천히 다가가 정지 화면처럼 보이지 않게 한다.
            stage.Animate(t => root.localScale = Vector3.one * (1f + Mathf.Min(t, 20f) * 0.0025f));
            AddTwinkles(stage, shot);

            switch (shot)
            {
                case StoryShot.Station:
                case StoryShot.Farewell:
                    AddStationLights(stage, shot == StoryShot.Station ? new Vector2(200f, 140f) : new Vector2(250f, 150f), shot == StoryShot.Station ? 1.4f : 0.35f);
                    break;
                case StoryShot.Attack:
                    AddStationLights(stage, new Vector2(120f, 150f), 1f);
                    AddAttack(stage);
                    break;
                case StoryShot.Shelter:
                    AddShelter(stage);
                    break;
                case StoryShot.Hangar:
                    AddSparks(stage);
                    break;
                case StoryShot.Launch:
                    AddEngine(stage, true);
                    break;
                case StoryShot.Alarm:
                    AddEngine(stage, false);
                    AddAlarm(stage);
                    break;
                case StoryShot.Escape:
                    AddStationLights(stage, new Vector2(130f, 150f), 0.8f);
                    AddEscape(stage);
                    break;
            }

            return stage;
        }

        // ---------- 움직이는 조각 ----------

        private static Image Piece(StoryStage stage, string name, Vector2 pixel, Vector2 pixelSize, Color color, Sprite sprite = null)
        {
            RectTransform rect = UiBuilder.Rect(name, stage.Root, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), pixel * PixelSize, pixelSize * PixelSize);
            return UiBuilder.Image(rect, color, sprite ?? UiBuilder.WhiteSprite);
        }

        private static void Place(Image image, Vector2 pixel)
        {
            image.rectTransform.anchoredPosition = pixel * PixelSize;
        }

        private static void AddTwinkles(StoryStage stage, StoryShot shot)
        {
            if (shot == StoryShot.Shelter || shot == StoryShot.Hangar || shot == StoryShot.Launch || shot == StoryShot.Alarm)
            {
                return;
            }

            var random = new Random(7 + (int)shot);
            for (int i = 0; i < 14; i++)
            {
                Vector2 at = new Vector2(random.Next(4, Width - 4), random.Next(40, Height - 4));
                float phase = (float)random.NextDouble() * 6f;
                float speed = 1.2f + (float)random.NextDouble() * 2f;
                Image star = Piece(stage, "Twinkle", at, new Vector2(1f, 1f), Color.white);
                stage.Animate(t =>
                {
                    float a = Mathf.Clamp01(Mathf.Sin(t * speed + phase) * 1.5f);
                    star.color = new Color(1f, 1f, 1f, a);
                });
            }
        }

        private static void AddStationLights(StoryStage stage, Vector2 center, float scale)
        {
            for (int i = 0; i < StationLights.Length; i++)
            {
                Vector2 at = center + StationLights[i] * scale;
                Color color = i % 3 == 0 ? new Color(1f, 0.3f, 0.25f) : new Color(0.45f, 1f, 0.55f);
                Image light = Piece(stage, "Light", at, Vector2.one * Mathf.Max(1f, Mathf.Round(scale * 1.5f)), color);
                float phase = i * 0.7f;
                stage.Animate(t => light.enabled = Mathf.Repeat(t * 0.8f + phase, 1.6f) < 0.6f);
            }
        }

        // 정거장 포문에서 회수선으로 붉은 광선이 쏟아지고 회수선이 불탄다.
        private static void AddAttack(StoryStage stage)
        {
            Vector2 turret = new Vector2(175f, 165f);
            Vector2 ship = new Vector2(370f, 120f);
            Vector2 delta = ship - turret;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            var beams = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                Image beam = Piece(stage, "Beam", (turret + ship) * 0.5f, new Vector2(delta.magnitude, 1.5f), new Color(1f, 0.25f, 0.2f));
                beam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                beams.Add(beam);
            }

            var blasts = new List<Image>();
            for (int i = 0; i < 4; i++)
            {
                blasts.Add(Piece(stage, "Blast", ship, new Vector2(30f, 30f), new Color(1f, 0.6f, 0.2f), Glow));
            }

            Image flash = Piece(stage, "Flash", new Vector2(Width * 0.5f, Height * 0.5f), new Vector2(Width, Height), new Color(1f, 0.1f, 0.05f, 0f));
            stage.Animate(t =>
            {
                for (int i = 0; i < beams.Count; i++)
                {
                    float local = Mathf.Repeat(t * 1.3f + i * 0.33f, 1f);
                    beams[i].enabled = local < 0.18f;
                    beams[i].rectTransform.anchoredPosition = ((turret + ship) * 0.5f + new Vector2(0f, (i - 1) * 3f)) * PixelSize;
                }

                for (int i = 0; i < blasts.Count; i++)
                {
                    float local = Mathf.Repeat(t * 1.1f + i * 0.27f, 1f);
                    Vector2 offset = new Vector2(Mathf.Sin(i * 12.9f) * 22f, Mathf.Cos(i * 7.3f) * 8f);
                    Place(blasts[i], ship + offset);
                    blasts[i].rectTransform.localScale = Vector3.one * (0.3f + local * 1.2f);
                    blasts[i].color = new Color(1f, 0.55f + local * 0.3f, 0.2f, 1f - local);
                }

                flash.color = new Color(1f, 0.1f, 0.05f, Mathf.Max(0f, Mathf.Sin(t * 8.2f)) * 0.12f);
            });
        }

        // 피난 구역: 깜빡이는 천장 등과 단말기 화면
        private static void AddShelter(StoryStage stage)
        {
            Image lamp = Piece(stage, "Lamp", new Vector2(240f, 200f), new Vector2(260f, 200f), new Color(1f, 0.85f, 0.55f, 0.3f), Glow);
            Image screen = Piece(stage, "Screen", new Vector2(400f, 130f), new Vector2(26f, 14f), new Color(0.4f, 1f, 0.8f, 0.5f));
            stage.Animate(t =>
            {
                // 가끔 짧게 꺼져 낡은 전기 시설처럼 보이게 한다.
                bool off = Mathf.Repeat(t, 4.3f) > 4.05f || Mathf.Repeat(t, 2.9f) > 2.8f;
                lamp.color = new Color(1f, 0.85f, 0.55f, off ? 0.05f : 0.28f + Mathf.Sin(t * 13f) * 0.02f);
                screen.color = new Color(0.4f, 1f, 0.8f, 0.35f + Mathf.Sin(t * 3f) * 0.15f);
            });
        }

        // 고장 난 포드 곳곳에서 튀는 불꽃
        private static void AddSparks(StoryStage stage)
        {
            Vector2[] points = { new Vector2(222f, 124f), new Vector2(254f, 104f), new Vector2(232f, 110f) };
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 point = points[i];
                var sparks = new List<Image>();
                for (int k = 0; k < 4; k++)
                {
                    sparks.Add(Piece(stage, "Spark", point, new Vector2(1f, 1f), new Color(1f, 0.9f, 0.4f)));
                }

                float phase = i * 1.1f;
                stage.Animate(t =>
                {
                    float burst = Mathf.Repeat(t + phase, 2.4f);
                    for (int k = 0; k < sparks.Count; k++)
                    {
                        bool on = burst < 0.5f;
                        sparks[k].enabled = on;
                        if (on)
                        {
                            Vector2 dir = new Vector2(Mathf.Cos(k * 1.9f + phase), Mathf.Abs(Mathf.Sin(k * 1.9f + phase)) + 0.3f);
                            Place(sparks[k], point + dir * burst * 26f + new Vector2(0f, -burst * burst * 40f));
                        }
                    }
                });
            }
        }

        // 포드의 엔진. launching이면 크게 타오르고 화면이 흔들린다.
        private static void AddEngine(StoryStage stage, bool launching)
        {
            Vector2 nozzle = new Vector2(170f, 114f);
            Image glow = Piece(stage, "Engine", nozzle, new Vector2(60f, 40f), new Color(0.5f, 0.85f, 1f, 0.8f), Glow);
            Image core = Piece(stage, "EngineCore", nozzle + new Vector2(-6f, 0f), new Vector2(22f, 10f), new Color(0.85f, 0.97f, 1f, 0.9f), Glow);
            RectTransform root = stage.Root;
            stage.Animate(t =>
            {
                float power = launching ? Mathf.Clamp01(t * 0.35f) : 0.35f;
                float flicker = 0.85f + Mathf.Sin(t * 31f) * 0.1f + Mathf.Sin(t * 17f) * 0.05f;
                glow.rectTransform.localScale = new Vector3(0.6f + power * 1.8f * flicker, 0.6f + power * 0.7f * flicker, 1f);
                Place(glow, nozzle - new Vector2(power * 30f, 0f));
                core.rectTransform.localScale = new Vector3(0.6f + power * 1.2f, 0.8f + power * 0.4f, 1f);
                if (launching)
                {
                    float shake = power * 2.2f;
                    root.anchoredPosition = new Vector2(Mathf.Sin(t * 47f) * shake, Mathf.Cos(t * 39f) * shake) * PixelSize;
                }
            });
        }

        private static void AddAlarm(StoryStage stage)
        {
            Image red = Piece(stage, "Alarm", new Vector2(Width * 0.5f, Height * 0.5f), new Vector2(Width, Height), new Color(1f, 0f, 0f, 0f));
            Image left = Piece(stage, "AlarmLamp", new Vector2(40f, 230f), new Vector2(70f, 70f), new Color(1f, 0.2f, 0.1f), Glow);
            Image right = Piece(stage, "AlarmLamp", new Vector2(440f, 230f), new Vector2(70f, 70f), new Color(1f, 0.2f, 0.1f), Glow);
            stage.Animate(t =>
            {
                float pulse = Mathf.Max(0f, Mathf.Sin(t * 5f));
                red.color = new Color(1f, 0f, 0f, pulse * 0.18f);
                left.color = right.color = new Color(1f, 0.2f, 0.1f, pulse);
            });
        }

        // 포드가 정거장을 떠나 멀어진다. 엔진 꼬리를 남긴다.
        private static void AddEscape(StoryStage stage)
        {
            Vector2 from = new Vector2(190f, 130f);
            Vector2 to = new Vector2(420f, 170f);
            var trail = new List<Image>();
            for (int i = 0; i < 10; i++)
            {
                trail.Add(Piece(stage, "Trail", from, new Vector2(8f, 8f), new Color(0.5f, 0.85f, 1f, 0.5f), Glow));
            }

            Image pod = Piece(stage, "Pod", from, new Vector2(40f, 20f), Color.white, PodSprite);
            Image flame = Piece(stage, "Flame", from, new Vector2(24f, 14f), new Color(0.6f, 0.9f, 1f, 0.9f), Glow);
            stage.Animate(t =>
            {
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 9f));
                Vector2 at = Vector2.Lerp(from, to, progress);
                float scale = Mathf.Lerp(1f, 0.35f, progress);
                Place(pod, at);
                pod.rectTransform.localScale = Vector3.one * scale;
                Place(flame, at - new Vector2(22f * scale, 0f));
                flame.rectTransform.localScale = Vector3.one * scale * (0.9f + Mathf.Sin(t * 30f) * 0.15f);
                for (int i = 0; i < trail.Count; i++)
                {
                    float back = Mathf.Max(0f, progress - (i + 1) * 0.03f);
                    Place(trail[i], Vector2.Lerp(from, to, back) - new Vector2(18f * scale, 0f));
                    trail[i].color = new Color(0.5f, 0.85f, 1f, 0.45f * (1f - i / (float)trail.Count));
                    trail[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, back);
                }
            });
        }

        // ---------- 그림 ----------

        private static Sprite Glow => _glow != null ? _glow : _glow = MakeGlow();

        private static Sprite PodSprite
        {
            get
            {
                if (_pod == null)
                {
                    var p = new Painter(40, 20, new Color32(0, 0, 0, 0));
                    DrawPod(p, 20, 10, false);
                    _pod = p.ToSprite();
                }

                return _pod;
            }
        }

        private static Sprite Background(StoryShot shot)
        {
            // 같은 그림은 한 번만 그린다. 엔딩 뒤 대기실로 돌아와도 다시 그리지 않는다.
            if (Backgrounds.TryGetValue(shot, out Sprite cached) && cached != null)
            {
                return cached;
            }

            var p = new Painter(Width, Height, new Color32(4, 4, 10, 255));
            switch (shot)
            {
                case StoryShot.Stars:
                    DrawSpace(p, 1);
                    DrawPlanet(p, 400, -40, 150, new Color32(120, 70, 160, 255), new Color32(230, 150, 110, 255));
                    break;
                case StoryShot.Station:
                    DrawSpace(p, 2);
                    DrawPlanet(p, 430, -60, 190, new Color32(70, 60, 140, 255), new Color32(240, 140, 90, 255));
                    DrawStation(p, 200, 140, 1.4f);
                    break;
                case StoryShot.Attack:
                    DrawSpace(p, 3);
                    DrawStation(p, 120, 150, 1f);
                    DrawShip(p, 370, 120);
                    break;
                case StoryShot.Shelter:
                    DrawShelter(p);
                    break;
                case StoryShot.Hangar:
                case StoryShot.Launch:
                case StoryShot.Alarm:
                    DrawHangar(p, shot == StoryShot.Hangar);
                    break;
                case StoryShot.Escape:
                    DrawSpace(p, 4);
                    DrawPlanet(p, 60, -90, 170, new Color32(60, 90, 150, 255), new Color32(150, 200, 230, 255));
                    DrawStation(p, 130, 150, 0.8f);
                    break;
                case StoryShot.Farewell:
                    DrawSpace(p, 5);
                    DrawPlanet(p, 120, -120, 220, new Color32(90, 60, 130, 255), new Color32(230, 150, 110, 255));
                    DrawStation(p, 250, 150, 0.35f);
                    break;
            }

            Sprite sprite = p.ToSprite();
            Backgrounds[shot] = sprite;
            return sprite;
        }

        private static void DrawSpace(Painter p, int seed)
        {
            // 위가 조금 더 밝은 깊은 남색에 성운을 얹는다.
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float n = Noise(x * 0.012f + seed * 3.1f, y * 0.018f) * Noise(x * 0.03f, y * 0.03f + seed);
                    float t = y / (float)Height;
                    var c = new Color(0.015f + n * 0.18f, 0.015f + n * 0.05f + t * 0.02f, 0.05f + n * 0.22f + t * 0.03f);
                    p.Set(x, y, Dither(c, x, y));
                }
            }

            var random = new Random(seed * 31);
            for (int i = 0; i < 520; i++)
            {
                int x = random.Next(Width);
                int y = random.Next(Height);
                int kind = random.Next(100);
                byte v = (byte)(90 + random.Next(150));
                var color = kind < 70 ? new Color32(v, v, (byte)Mathf.Min(255, v + 30), 255) : new Color32(255, (byte)(v * 0.9f), (byte)(v * 0.7f), 255);
                p.Set(x, y, color);
                if (kind > 96)
                {
                    // 밝은 별은 십자 모양으로 빛난다.
                    var dim = new Color32(color.r, color.g, color.b, 110);
                    p.Blend(x + 1, y, dim);
                    p.Blend(x - 1, y, dim);
                    p.Blend(x, y + 1, dim);
                    p.Blend(x, y - 1, dim);
                }
            }
        }

        private static void DrawPlanet(Painter p, int cx, int cy, int radius, Color32 dark, Color32 light)
        {
            Vector3 sun = new Vector3(-0.6f, 0.6f, 0.55f).normalized;
            for (int y = cy - radius - 6; y <= cy + radius + 6; y++)
            {
                for (int x = cx - radius - 6; x <= cx + radius + 6; x++)
                {
                    float dx = (x - cx) / (float)radius;
                    float dy = (y - cy) / (float)radius;
                    float d2 = dx * dx + dy * dy;
                    if (d2 > 1f)
                    {
                        // 대기의 얇은 빛
                        float rim = Mathf.Sqrt(d2) - 1f;
                        if (rim < 0.04f)
                        {
                            p.Blend(x, y, new Color32(light.r, light.g, light.b, (byte)(120 * (1f - rim / 0.04f))));
                        }

                        continue;
                    }

                    float dz = Mathf.Sqrt(1f - d2);
                    float lit = Mathf.Clamp01(Vector3.Dot(new Vector3(dx, dy, dz), sun));
                    float band = Noise(x * 0.01f, y * 0.06f) * 0.5f + Mathf.Sin(dy * 14f + Noise(x * 0.02f, y * 0.02f) * 3f) * 0.12f;
                    Color c = Color.Lerp(dark, light, Mathf.Clamp01(band + 0.25f)) * (0.12f + lit * 0.95f);
                    c.a = 1f;
                    p.Set(x, y, Dither(c, x, y));
                }
            }
        }

        // 오르비스-9: 가운데 기둥, 바퀴 모양 고리, 태양 전지판, 고리 위의 모듈
        private static void DrawStation(Painter p, int cx, int cy, float s)
        {
            var hull = new Color32(150, 155, 170, 255);
            var hullDark = new Color32(80, 84, 100, 255);
            var hullLight = new Color32(200, 205, 215, 255);
            var panel = new Color32(40, 70, 140, 255);
            var panelLine = new Color32(90, 130, 200, 255);
            var window = new Color32(255, 220, 130, 255);

            int rx = Mathf.RoundToInt(70 * s);
            int ry = Mathf.RoundToInt(22 * s);
            int thick = Mathf.Max(1, Mathf.RoundToInt(6 * s));

            // 고리의 뒤쪽 절반을 먼저 그리고 기둥, 앞쪽 절반 순서로 겹친다.
            DrawRing(p, cx, cy, rx, ry, thick, hullDark, hull, true);

            // 전지판 두 쌍
            int armY = cy + Mathf.RoundToInt(34 * s);
            p.Rect(cx - Mathf.RoundToInt(70 * s), armY, Mathf.RoundToInt(140 * s), Mathf.Max(1, Mathf.RoundToInt(2 * s)), hullDark);
            foreach (int side in new[] { -1, 1 })
            {
                for (int k = 0; k < 2; k++)
                {
                    int px = cx + side * Mathf.RoundToInt((22 + k * 26) * s) - (side < 0 ? Mathf.RoundToInt(22 * s) : 0);
                    int w = Mathf.RoundToInt(22 * s);
                    int h = Mathf.RoundToInt(16 * s);
                    p.Rect(px, armY - h / 2, w, h, panel);
                    for (int gx = 0; gx < w; gx += Mathf.Max(2, Mathf.RoundToInt(4 * s)))
                    {
                        p.Rect(px + gx, armY - h / 2, 1, h, panelLine);
                    }

                    p.Rect(px, armY, w, 1, panelLine);
                }
            }

            // 가운데 기둥: 왼쪽이 밝고 오른쪽이 어둡다 (햇빛이 왼쪽 위에서 온다).
            int hw = Mathf.Max(3, Mathf.RoundToInt(9 * s));
            int top = cy + Mathf.RoundToInt(48 * s);
            int bottom = cy - Mathf.RoundToInt(40 * s);
            for (int x = -hw; x <= hw; x++)
            {
                float shade = 1f - (x + hw) / (2f * hw);
                Color c = Color.Lerp(hullDark, hullLight, shade);
                p.Rect(cx + x, bottom, 1, top - bottom, c);
            }

            p.Rect(cx - hw - 2, top, 2 * hw + 5, Mathf.Max(2, Mathf.RoundToInt(4 * s)), hullLight);
            p.Rect(cx - hw - 3, bottom - Mathf.Max(2, Mathf.RoundToInt(5 * s)), 2 * hw + 7, Mathf.Max(2, Mathf.RoundToInt(5 * s)), hullDark);
            p.Rect(cx - 1, top + Mathf.RoundToInt(4 * s), 2, Mathf.RoundToInt(12 * s), hullDark);
            for (int y = bottom + 3; y < top - 2; y += Mathf.Max(2, Mathf.RoundToInt(5 * s)))
            {
                p.Set(cx - hw / 2, y, window);
                p.Set(cx + hw / 3, y, new Color32(200, 160, 90, 255));
            }

            // 바퀴살
            foreach (int side in new[] { -1, 1 })
            {
                p.Line(cx + side * hw, cy, cx + side * (rx - thick), cy, hullDark);
                p.Line(cx + side * hw, cy + 1, cx + side * (rx - thick), cy + 1, hull);
            }

            DrawRing(p, cx, cy, rx, ry, thick, hullDark, hull, false);

            // 고리 위의 모듈과 창문
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.PI * (1.15f + i * 0.14f);
                int mx = cx + Mathf.RoundToInt(Mathf.Cos(a) * rx);
                int my = cy + Mathf.RoundToInt(Mathf.Sin(a) * ry);
                int size = Mathf.Max(2, Mathf.RoundToInt(6 * s));
                p.Rect(mx - size / 2, my - size / 2, size, size, i % 2 == 0 ? hullLight : hull);
                p.Set(mx, my, window);
            }

            for (int i = 0; i < 28; i++)
            {
                float a = Mathf.PI * (1.02f + i * 0.035f);
                p.Set(cx + Mathf.RoundToInt(Mathf.Cos(a) * rx), cy + Mathf.RoundToInt(Mathf.Sin(a) * ry), i % 3 == 0 ? window : hullLight);
            }

        }

        private static void DrawRing(Painter p, int cx, int cy, int rx, int ry, int thick, Color32 dark, Color32 light, bool back)
        {
            for (float a = 0f; a < Mathf.PI * 2f; a += 0.6f / Mathf.Max(rx, 1))
            {
                float sin = Mathf.Sin(a);
                if (back != sin > 0f)
                {
                    continue;
                }

                int x = cx + Mathf.RoundToInt(Mathf.Cos(a) * rx);
                int y = cy + Mathf.RoundToInt(sin * ry);
                for (int k = 0; k < thick; k++)
                {
                    Color32 c = k == 0 ? light : k == thick - 1 ? dark : Color32.Lerp(light, dark, k / (float)thick);
                    if (back)
                    {
                        c = Color32.Lerp(c, new Color32(20, 20, 30, 255), 0.35f);
                    }

                    p.Set(x, y - k, c);
                }
            }
        }

        // 회수선: 길쭉한 화물선. 컨테이너를 매달고 오른쪽에 조종석이 있다.
        private static void DrawShip(Painter p, int cx, int cy)
        {
            var hull = new Color32(170, 150, 120, 255);
            var dark = new Color32(90, 75, 60, 255);
            var stripe = new Color32(220, 160, 50, 255);
            p.Rect(cx - 50, cy - 8, 90, 16, hull);
            p.Rect(cx - 50, cy - 8, 90, 3, dark);
            p.Rect(cx - 30, cy + 2, 60, 2, stripe);
            for (int i = 0; i < 9; i++)
            {
                p.Rect(cx + 40 + i, cy - 8 + i / 2, 1, 16 - i, hull);
            }

            p.Rect(cx + 34, cy + 2, 6, 3, new Color32(120, 220, 255, 255));
            for (int i = 0; i < 4; i++)
            {
                Color32 box = i % 2 == 0 ? new Color32(180, 70, 50, 255) : new Color32(60, 120, 150, 255);
                p.Rect(cx - 44 + i * 19, cy - 20, 16, 11, box);
                p.Rect(cx - 44 + i * 19, cy - 20, 16, 2, dark);
            }

            p.Rect(cx - 58, cy - 6, 8, 12, dark);
            p.Rect(cx - 60, cy - 4, 2, 8, new Color32(255, 140, 60, 255));

            // 이미 맞은 자리: 검게 탄 구멍
            p.Rect(cx - 12, cy - 4, 6, 5, new Color32(30, 20, 20, 255));
            p.Rect(cx + 14, cy, 4, 4, new Color32(30, 20, 20, 255));
        }

        // 장거리 탈출 포드 옆모습. broken이면 덮개가 빠져 속이 보인다.
        private static void DrawPod(Painter p, int cx, int cy, bool broken, int scale = 1)
        {
            var hull = new Color32(205, 210, 220, 255);
            var shade = new Color32(120, 125, 140, 255);
            var stripe = new Color32(235, 140, 40, 255);
            for (int x = -18; x <= 18; x++)
            {
                // 앞이 둥글고 뒤가 각진 몸통
                float front = x > 8 ? Mathf.Sqrt(Mathf.Max(0f, 1f - (x - 8) * (x - 8) / 100f)) : 1f;
                int half = Mathf.RoundToInt(7 * front);
                for (int y = -half; y <= half; y++)
                {
                    Color32 c = y < -half + 3 ? shade : hull;
                    p.Rect(cx * scale + x * scale, cy * scale + y * scale, scale, scale, c);
                }
            }

            p.Rect((cx - 14) * scale, (cy - 1) * scale, 24 * scale, 2 * scale, stripe);
            p.Rect((cx + 7) * scale, (cy + 2) * scale, 6 * scale, 3 * scale, new Color32(110, 200, 240, 255));
            p.Rect((cx - 21) * scale, (cy - 5) * scale, 3 * scale, 10 * scale, shade);
            p.Rect((cx - 16) * scale, (cy + 6) * scale, 6 * scale, 3 * scale, shade);
            p.Rect((cx - 16) * scale, (cy - 9) * scale, 6 * scale, 3 * scale, shade);
            if (broken)
            {
                var hole = new Color32(30, 30, 40, 255);
                var wire = new Color32(220, 80, 60, 255);
                p.Rect((cx - 10) * scale, (cy + 1) * scale, 8 * scale, 4 * scale, hole);
                p.Rect((cx - 8) * scale, (cy + 2) * scale, 1 * scale, 2 * scale, wire);
                p.Rect((cx - 5) * scale, (cy + 2) * scale, 1 * scale, 2 * scale, new Color32(90, 200, 120, 255));
                p.Rect((cx + 2) * scale, (cy - 5) * scale, 5 * scale, 3 * scale, hole);
            }
        }

        private static void DrawFigure(Painter p, int x, int y, Color32 suit, bool sitting)
        {
            var visor = new Color32(40, 50, 70, 255);
            var visorLight = new Color32(140, 200, 230, 255);
            var dark = new Color32((byte)(suit.r * 0.55f), (byte)(suit.g * 0.55f), (byte)(suit.b * 0.55f), 255);
            int legs = sitting ? 3 : 7;
            p.Rect(x - 3, y, 2, legs, dark);
            p.Rect(x + 1, y, 2, legs, dark);
            p.Rect(x - 4, y + legs, 8, 9, suit);
            p.Rect(x - 4, y + legs, 2, 9, dark);
            p.Rect(x - 6, y + legs + 2, 2, 6, suit);
            p.Rect(x + 4, y + legs + 2, 2, 6, suit);
            p.Rect(x - 4, y + legs + 9, 8, 7, suit);
            p.Rect(x - 3, y + legs + 10, 6, 4, visor);
            p.Set(x + 1, y + legs + 13, visorLight);
        }

        // 피난 구역: 벽 판과 창문, 침대, 단말기, 모여 있는 생존자 다섯과 플레이어
        private static void DrawShelter(Painter p)
        {
            var wall = new Color32(44, 48, 62, 255);
            var seam = new Color32(30, 32, 42, 255);
            var light = new Color32(64, 70, 88, 255);
            var floor = new Color32(36, 34, 40, 255);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    bool isFloor = y < 70;
                    Color32 c = isFloor ? floor : wall;
                    if (!isFloor && (x % 60 == 0 || (y - 70) % 50 == 0))
                    {
                        c = seam;
                    }
                    else if (!isFloor && (x % 60 == 1 || (y - 70) % 50 == 1))
                    {
                        c = light;
                    }
                    else if (isFloor && (y % 12 == 0 || (x + y * 2) % 48 == 0))
                    {
                        c = new Color32(28, 26, 32, 255);
                    }

                    p.Set(x, y, c);
                }
            }

            // 바닥과 벽의 경계 띠
            p.Rect(0, 70, Width, 4, new Color32(70, 66, 60, 255));
            p.Rect(0, 74, Width, 2, new Color32(220, 160, 50, 255));

            // 창문 너머의 별과 행성
            var window = new Painter(140, 70, new Color32(4, 4, 12, 255));
            DrawSpace(window, 9);
            DrawPlanet(window, 120, -10, 50, new Color32(70, 60, 140, 255), new Color32(240, 140, 90, 255));
            p.Rect(168, 148, 144, 74, seam);
            p.Paste(window, 170, 150);
            p.Rect(238, 150, 3, 70, seam);

            // 왼쪽 2층 침대, 오른쪽 단말기와 상자
            var metal = new Color32(90, 96, 110, 255);
            var blanket = new Color32(110, 70, 70, 255);
            p.Rect(20, 72, 4, 90, metal);
            p.Rect(110, 72, 4, 90, metal);
            p.Rect(20, 100, 94, 8, blanket);
            p.Rect(20, 140, 94, 8, new Color32(70, 90, 110, 255));
            p.Rect(380, 72, 40, 46, metal);
            p.Rect(384, 120, 32, 20, new Color32(25, 40, 45, 255));
            p.Rect(430, 72, 34, 26, new Color32(120, 100, 70, 255));
            p.Rect(430, 84, 34, 2, new Color32(80, 66, 46, 255));

            // 천장 등
            p.Rect(220, 255, 40, 6, metal);
            p.Rect(226, 252, 28, 3, new Color32(255, 230, 170, 255));

            // 생존자 다섯 (앉거나 서 있다)과 가운데의 플레이어
            DrawFigure(p, 60, 108, SuitColors[0], true);
            DrawFigure(p, 140, 72, SuitColors[1], false);
            DrawFigure(p, 180, 76, SuitColors[2], true);
            DrawFigure(p, 300, 72, SuitColors[3], false);
            DrawFigure(p, 345, 76, SuitColors[4], true);
            DrawFigure(p, 240, 66, SuitColors[5], false);
        }

        // 격납고: 커다란 문, 천장 들보, 바닥 표시, 받침대 위의 장거리 탈출 포드
        private static void DrawHangar(Painter p, bool broken)
        {
            var wall = new Color32(38, 40, 52, 255);
            var beam = new Color32(58, 60, 74, 255);
            var floor = new Color32(46, 44, 50, 255);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color32 c = y < 80 ? floor : wall;
                    if (y >= 80 && x % 40 < 2)
                    {
                        c = beam;
                    }

                    p.Set(x, y, c);
                }
            }

            // 격납고 문: 큰 판 두 장과 아래쪽 경고 줄무늬
            var door = new Color32(70, 74, 88, 255);
            p.Rect(110, 80, 260, 160, new Color32(28, 30, 38, 255));
            p.Rect(114, 84, 124, 152, door);
            p.Rect(242, 84, 124, 152, door);
            for (int y = 100; y < 236; y += 20)
            {
                p.Rect(114, y, 252, 1, new Color32(52, 56, 68, 255));
            }

            for (int x = 114; x < 366; x++)
            {
                for (int y = 84; y < 96; y++)
                {
                    p.Set(x, y, (x + y) / 8 % 2 == 0 ? new Color32(230, 170, 40, 255) : new Color32(30, 28, 30, 255));
                }
            }

            // 천장 들보와 조명
            p.Rect(0, 246, Width, 10, beam);
            for (int x = 30; x < Width; x += 90)
            {
                p.Rect(x, 240, 30, 4, new Color32(255, 225, 160, 255));
            }

            // 바닥의 노란 선
            p.Rect(0, 78, Width, 2, new Color32(80, 76, 70, 255));
            p.Rect(60, 40, 360, 2, new Color32(200, 160, 50, 255));
            p.Rect(60, 40, 2, 38, new Color32(200, 160, 50, 255));
            p.Rect(418, 40, 2, 38, new Color32(200, 160, 50, 255));

            // 받침대와 포드 (3배 크기)
            p.Rect(170, 70, 140, 12, new Color32(80, 84, 96, 255));
            p.Rect(190, 82, 6, 14, new Color32(60, 62, 72, 255));
            p.Rect(284, 82, 6, 14, new Color32(60, 62, 72, 255));
            DrawPod(p, 80, 38, broken, 3);

            // 양옆의 상자와 장비
            p.Rect(20, 60, 50, 34, new Color32(120, 100, 70, 255));
            p.Rect(20, 76, 50, 2, new Color32(80, 66, 46, 255));
            p.Rect(400, 60, 60, 60, new Color32(70, 90, 80, 255));
            p.Rect(408, 100, 20, 10, new Color32(120, 230, 160, 255));
        }

        // ---------- 그리기 도구 ----------

        private static Sprite MakeGlow()
        {
            const int size = 64;
            var p = new Painter(size, size, new Color32(255, 255, 255, 0));
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    p.Set(x, y, new Color32(255, 255, 255, (byte)(a * a * 255)));
                }
            }

            return p.ToSprite(FilterMode.Bilinear);
        }

        private static float Noise(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }

        // 두 단계 색 사이를 바둑판 무늬로 섞어 픽셀아트처럼 보이게 한다.
        private static Color32 Dither(Color c, int x, int y)
        {
            const float levels = 24f;
            float threshold = ((x + y * 2) % 4) / 4f - 0.375f;
            return new Color32(
                (byte)(Mathf.Clamp01(Mathf.Round(c.r * levels + threshold) / levels) * 255),
                (byte)(Mathf.Clamp01(Mathf.Round(c.g * levels + threshold) / levels) * 255),
                (byte)(Mathf.Clamp01(Mathf.Round(c.b * levels + threshold) / levels) * 255),
                255);
        }

        private sealed class Painter
        {
            private readonly int _width;
            private readonly int _height;
            private readonly Color32[] _pixels;

            public Painter(int width, int height, Color32 clear)
            {
                _width = width;
                _height = height;
                _pixels = new Color32[width * height];
                for (int i = 0; i < _pixels.Length; i++)
                {
                    _pixels[i] = clear;
                }
            }

            public void Set(int x, int y, Color32 color)
            {
                if (x >= 0 && y >= 0 && x < _width && y < _height)
                {
                    _pixels[y * _width + x] = color;
                }
            }

            public void Blend(int x, int y, Color32 color)
            {
                if (x >= 0 && y >= 0 && x < _width && y < _height)
                {
                    Color32 under = _pixels[y * _width + x];
                    Color32 mixed = Color32.Lerp(under, color, color.a / 255f);
                    mixed.a = (byte)Mathf.Max(under.a, color.a);
                    _pixels[y * _width + x] = mixed;
                }
            }

            public void Rect(int x, int y, int width, int height, Color32 color)
            {
                for (int yy = y; yy < y + height; yy++)
                {
                    for (int xx = x; xx < x + width; xx++)
                    {
                        Set(xx, yy, color);
                    }
                }
            }

            public void Rect(int x, int y, int width, int height, Color color)
            {
                Rect(x, y, width, height, (Color32)color);
            }

            public void Line(int x0, int y0, int x1, int y1, Color32 color)
            {
                int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                for (int i = 0; i <= steps; i++)
                {
                    float t = steps == 0 ? 0f : i / (float)steps;
                    Set(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), color);
                }
            }

            public void Paste(Painter other, int x, int y)
            {
                for (int yy = 0; yy < other._height; yy++)
                {
                    for (int xx = 0; xx < other._width; xx++)
                    {
                        Set(x + xx, y + yy, other._pixels[yy * other._width + xx]);
                    }
                }
            }

            public Sprite ToSprite(FilterMode filter = FilterMode.Point)
            {
                var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
                {
                    filterMode = filter,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave,
                };
                texture.SetPixels32(_pixels);
                texture.Apply(false, true);
                return Sprite.Create(texture, new Rect(0f, 0f, _width, _height), new Vector2(0.5f, 0.5f), 100f);
            }
        }
    }
}
