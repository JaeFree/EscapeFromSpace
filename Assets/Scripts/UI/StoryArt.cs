using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace EscapeFromSpace
{
    // 스토리 화면과 시작 화면의 장면. 장마다 하나를 고른다.
    // 에셋에는 순서 번호로 저장되므로 새 장면은 맨 뒤에 붙인다 (Planet은 옛 Station 자리).
    public enum StoryShot
    {
        Stars,
        Planet,
        Attack,
        Shelter,
        Hangar,
        Launch,
        Alarm,
        Escape,
        Farewell,
        Graveyard,
        Crash,
        Liftoff,
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
        private static Sprite _ship;

        // 협곡 장면마다 잔해 위 경고등 자리. 배경을 그릴 때 정해진다.
        private static readonly Dictionary<StoryShot, List<Vector2>> WreckLights = new Dictionary<StoryShot, List<Vector2>>();

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

        // 센티널 위성이 늘어선 자리 (2026-11-19 우주선 무덤 이야기, 기획서 19.1)
        private static readonly Vector2[] OrbitSentinels = MakeOrbit(new Vector2(240f, 70f), 250f, 120f, 9);
        private static readonly Vector2[] EscapeSentinels = MakeOrbit(new Vector2(60f, -40f), 260f, 220f, 8);
        private static readonly Vector2[] FarewellSentinels = MakeOrbit(new Vector2(250f, 150f), 62f, 24f, 7);

        private static Vector2[] MakeOrbit(Vector2 center, float rx, float ry, int count)
        {
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(18f, 162f, i / (float)(count - 1)) * Mathf.Deg2Rad;
                points[i] = center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            }

            return points;
        }

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
                case StoryShot.Planet:
                    AddBlinkers(stage, OrbitSentinels, new Color(1f, 0.3f, 0.25f), 2f);
                    break;
                case StoryShot.Attack:
                    AddAttack(stage);
                    break;
                case StoryShot.Crash:
                    AddBlinkers(stage, WreckLights[shot], new Color(1f, 0.55f, 0.2f), 2f);
                    AddFire(stage, new Vector2(282f, 66f));
                    AddSmoke(stage, new Vector2(296f, 80f));
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
                case StoryShot.Liftoff:
                    AddBlinkers(stage, WreckLights[shot], new Color(1f, 0.55f, 0.2f), 2f);
                    AddLiftoff(stage);
                    break;
                case StoryShot.Escape:
                    AddBlinkers(stage, EscapeSentinels, new Color(1f, 0.3f, 0.25f), 2f);
                    AddEscape(stage);
                    break;
                case StoryShot.Farewell:
                    AddBlinkers(stage, FarewellSentinels, new Color(1f, 0.3f, 0.25f), 1f);
                    break;
                case StoryShot.Graveyard:
                    AddBlinkers(stage, WreckLights[shot], new Color(1f, 0.55f, 0.2f), 2f);
                    AddDust(stage);
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
            // 실내와 협곡 장면에는 반짝이는 별을 두지 않는다 (협곡 하늘의 별은 배경에 희미하게 그려 둔다).
            if (shot == StoryShot.Shelter || shot == StoryShot.Hangar || shot == StoryShot.Launch || shot == StoryShot.Alarm
                || shot == StoryShot.Graveyard || shot == StoryShot.Crash || shot == StoryShot.Liftoff)
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

        // 깜빡이는 작은 불빛 (센티널 위성, 잔해의 경고등)
        private static void AddBlinkers(StoryStage stage, IReadOnlyList<Vector2> points, Color color, float size)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Image light = Piece(stage, "Light", points[i], Vector2.one * size, color);
                float phase = i * 0.7f;
                stage.Animate(t => light.enabled = Mathf.Repeat(t * 0.8f + phase, 1.6f) < 0.6f);
            }
        }

        // 협곡에 처박힌 회수선의 불길
        private static void AddFire(StoryStage stage, Vector2 at)
        {
            var flames = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                flames.Add(Piece(stage, "Fire", at + new Vector2(i * 9f - 9f, 0f), new Vector2(18f, 22f), new Color(1f, 0.55f, 0.15f, 0.8f), Glow));
            }

            stage.Animate(t =>
            {
                for (int i = 0; i < flames.Count; i++)
                {
                    float flicker = 0.8f + Mathf.Sin(t * (11f + i * 3f) + i) * 0.2f;
                    flames[i].rectTransform.localScale = new Vector3(flicker, 0.8f + flicker * 0.4f, 1f);
                    flames[i].color = new Color(1f, 0.45f + flicker * 0.2f, 0.15f, 0.7f * flicker);
                }
            });
        }

        // 위로 피어오르며 커지고 옅어지는 연기
        private static void AddSmoke(StoryStage stage, Vector2 at)
        {
            var puffs = new List<Image>();
            for (int i = 0; i < 8; i++)
            {
                puffs.Add(Piece(stage, "Smoke", at, new Vector2(26f, 26f), new Color(0.25f, 0.22f, 0.24f, 0.6f), Glow));
            }

            stage.Animate(t =>
            {
                for (int i = 0; i < puffs.Count; i++)
                {
                    float local = Mathf.Repeat(t * 0.33f + i / (float)puffs.Count, 1f);
                    Place(puffs[i], at + new Vector2(Mathf.Sin(i * 2.1f) * 6f - local * 40f, local * 120f));
                    puffs[i].rectTransform.localScale = Vector3.one * (0.6f + local * 2.4f);
                    puffs[i].color = new Color(0.25f, 0.22f, 0.24f, 0.55f * (1f - local));
                }
            });
        }

        // 바닥을 스치는 모래 먼지
        private static void AddDust(StoryStage stage)
        {
            var random = new Random(5);
            for (int i = 0; i < 18; i++)
            {
                float y = 10f + (float)random.NextDouble() * 90f;
                float speed = 12f + (float)random.NextDouble() * 18f;
                float offset = (float)random.NextDouble() * Width;
                Image grain = Piece(stage, "Dust", new Vector2(0f, y), new Vector2(2f, 1f), new Color(0.95f, 0.8f, 0.6f, 0.5f));
                stage.Animate(t => Place(grain, new Vector2(Mathf.Repeat(offset + t * speed, Width), y + Mathf.Sin(t + offset) * 2f)));
            }
        }

        // 회수선이 협곡 바닥에서 떠오른다. 아래로 불꽃을 뿜고 바닥에서 먼지가 퍼진다.
        private static void AddLiftoff(StoryStage stage)
        {
            Vector2 from = new Vector2(240f, 48f);
            Vector2 to = new Vector2(240f, 215f);
            var dust = new List<Image>();
            for (int i = 0; i < 6; i++)
            {
                dust.Add(Piece(stage, "Dust", from, new Vector2(40f, 18f), new Color(0.8f, 0.6f, 0.42f, 0.6f), Glow));
            }

            Image left = Piece(stage, "Thrust", from, new Vector2(14f, 26f), new Color(0.6f, 0.9f, 1f, 0.9f), Glow);
            Image right = Piece(stage, "Thrust", from, new Vector2(14f, 26f), new Color(0.6f, 0.9f, 1f, 0.9f), Glow);
            Image ship = Piece(stage, "Ship", from, new Vector2(112f, 32f), Color.white, ShipSprite);
            RectTransform root = stage.Root;
            stage.Animate(t =>
            {
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1f) / 8f));
                Vector2 at = Vector2.Lerp(from, to, progress);
                float scale = Mathf.Lerp(1f, 0.55f, progress);
                Place(ship, at);
                ship.rectTransform.localScale = Vector3.one * scale;
                float flicker = 0.85f + Mathf.Sin(t * 30f) * 0.15f;
                Place(left, at + new Vector2(-30f, -16f) * scale);
                Place(right, at + new Vector2(24f, -16f) * scale);
                left.rectTransform.localScale = right.rectTransform.localScale = new Vector3(scale, scale * flicker * (1f + progress), 1f);
                for (int i = 0; i < dust.Count; i++)
                {
                    float local = Mathf.Repeat(t * 0.5f + i / (float)dust.Count, 1f);
                    float side = i % 2 == 0 ? -1f : 1f;
                    Place(dust[i], from + new Vector2(side * local * 120f, -8f + local * 6f));
                    dust[i].rectTransform.localScale = Vector3.one * (0.6f + local * 1.6f);
                    dust[i].color = new Color(0.8f, 0.6f, 0.42f, 0.55f * (1f - local) * (1f - progress));
                }

                // 떠오르는 첫 몇 초만 화면이 흔들린다.
                float shake = Mathf.Clamp01(1f - progress * 2f) * 1.6f;
                root.anchoredPosition = new Vector2(Mathf.Sin(t * 47f) * shake, Mathf.Cos(t * 39f) * shake) * PixelSize;
            });
        }

        // 센티널 위성의 포문에서 회수선으로 붉은 광선이 쏟아지고 회수선이 불탄다.
        private static void AddAttack(StoryStage stage)
        {
            Vector2 turret = new Vector2(116f, 153f);
            Vector2 ship = new Vector2(370f, 110f);
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

        // 회수선이 센티널 위성 사이를 지나 멀어진다. 엔진 꼬리를 남긴다.
        private static void AddEscape(StoryStage stage)
        {
            Vector2 from = new Vector2(150f, 90f);
            Vector2 to = new Vector2(420f, 200f);
            var trail = new List<Image>();
            for (int i = 0; i < 10; i++)
            {
                trail.Add(Piece(stage, "Trail", from, new Vector2(8f, 8f), new Color(0.5f, 0.85f, 1f, 0.5f), Glow));
            }

            Image pod = Piece(stage, "Ship", from, new Vector2(56f, 16f), Color.white, ShipSprite);
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

        // 회수선 옆모습 (떠오르는 장면과 떠나는 장면에서 움직인다)
        private static Sprite ShipSprite
        {
            get
            {
                if (_ship == null)
                {
                    var p = new Painter(112, 32, new Color32(0, 0, 0, 0));
                    DrawShip(p, 60, 22);
                    _ship = p.ToSprite();
                }

                return _ship;
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
                case StoryShot.Planet:
                    DrawSpace(p, 2);
                    DrawPlanet(p, 240, -170, 290, PlanetDark, PlanetLight);
                    foreach (Vector2 at in OrbitSentinels)
                    {
                        DrawSentinel(p, Mathf.RoundToInt(at.x), Mathf.RoundToInt(at.y), 1);
                    }

                    break;
                case StoryShot.Attack:
                    DrawSpace(p, 3);
                    DrawPlanet(p, 300, -330, 380, PlanetDark, PlanetLight);
                    DrawSentinel(p, 110, 172, 3);
                    DrawShip(p, 370, 110);
                    break;
                case StoryShot.Crash:
                    DrawCanyon(p, shot, 11, 3, 0.6f);
                    DrawCrashedShip(p, 300, 58);
                    break;
                case StoryShot.Shelter:
                    DrawShelter(p);
                    break;
                case StoryShot.Hangar:
                case StoryShot.Launch:
                case StoryShot.Alarm:
                    DrawHangar(p, shot == StoryShot.Hangar);
                    break;
                case StoryShot.Liftoff:
                    DrawCanyon(p, shot, 12, 3, 0.6f);
                    break;
                case StoryShot.Escape:
                    DrawSpace(p, 4);
                    DrawPlanet(p, 60, -90, 170, PlanetDark, PlanetLight);
                    foreach (Vector2 at in EscapeSentinels)
                    {
                        DrawSentinel(p, Mathf.RoundToInt(at.x), Mathf.RoundToInt(at.y), 1);
                    }

                    break;
                case StoryShot.Farewell:
                    DrawSpace(p, 5);
                    DrawPlanet(p, 250, 150, 40, PlanetDark, PlanetLight);
                    foreach (Vector2 at in FarewellSentinels)
                    {
                        p.Set(Mathf.RoundToInt(at.x), Mathf.RoundToInt(at.y), new Color32(170, 175, 190, 255));
                    }

                    break;
                case StoryShot.Graveyard:
                    DrawCanyon(p, shot, 21, 6);
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

        // 케르-7: 먼지 낀 붉은 사막 행성
        private static readonly Color32 PlanetDark = new Color32(110, 58, 42, 255);
        private static readonly Color32 PlanetLight = new Color32(232, 160, 104, 255);

        // 센티널 방어 위성: 네모난 몸체, 양쪽 태양 전지판, 아래로 향한 포신, 붉은 눈
        private static void DrawSentinel(Painter p, int cx, int cy, int s)
        {
            var body = new Color32(150, 150, 160, 255);
            var dark = new Color32(70, 72, 84, 255);
            var panel = new Color32(50, 60, 110, 255);
            p.Rect(cx - 3 * s, cy - 3 * s, 6 * s, 6 * s, body);
            p.Rect(cx - 3 * s, cy - 3 * s, 6 * s, s, dark);
            p.Rect(cx - 9 * s, cy - s, 5 * s, 3 * s, panel);
            p.Rect(cx + 4 * s, cy - s, 5 * s, 3 * s, panel);
            p.Rect(cx - 4 * s, cy, s, s, dark);
            p.Rect(cx + 3 * s, cy, s, s, dark);
            p.Rect(cx + s, cy - 6 * s, 2 * s, 3 * s, dark);
            p.Rect(cx - s, cy, 2 * s, 2 * s, new Color32(255, 70, 50, 255));
        }

        private static void DrawMoon(Painter p, int cx, int cy, int radius, Color32 color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radius * radius)
                    {
                        continue;
                    }

                    // 왼쪽 위가 밝고, 오른쪽 아래는 그림자에 잠긴다.
                    float lit = Mathf.Clamp01(0.6f - (x - y) / (float)(radius * 2));
                    p.Set(cx + x, cy + y, Dither(Color.Lerp(new Color(0.35f, 0.25f, 0.3f), color, lit), cx + x, cy + y));
                }
            }
        }

        // 협곡의 우주선 무덤: 노을 하늘, 먼 능선, 양쪽 절벽, 모래 바닥, 반쯤 묻힌 우주선 잔해들
        private static void DrawCanyon(Painter p, StoryShot shot, int seed, int wrecks, float nearest = 0f)
        {
            const int horizon = 120;
            var random = new Random(seed);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color c;
                    if (y >= horizon)
                    {
                        float t = (y - horizon) / (float)(Height - horizon);
                        c = Color.Lerp(new Color(0.78f, 0.42f, 0.28f), new Color(0.16f, 0.09f, 0.22f), Mathf.Pow(t, 0.7f));
                    }
                    else
                    {
                        float n = Noise(x * 0.05f + seed, y * 0.12f);
                        c = Color.Lerp(SandNear, SandFar, y / (float)horizon) * (0.85f + n * 0.3f);
                    }

                    c.a = 1f;
                    p.Set(x, y, Dither(c, x, y));
                }
            }

            for (int i = 0; i < 70; i++)
            {
                p.Blend(random.Next(Width), random.Next(205, Height), new Color32(255, 230, 220, (byte)(50 + random.Next(110))));
            }

            DrawMoon(p, 372, 228, 13, new Color32(235, 215, 195, 255));
            DrawMoon(p, 412, 204, 5, new Color32(205, 175, 165, 255));

            // 먼 능선과 가까운 능선
            for (int x = 0; x < Width; x++)
            {
                int far = horizon + Mathf.RoundToInt(Noise(x * 0.015f + seed, 1.3f) * 36f);
                p.Rect(x, horizon - 4, 1, far - horizon + 4, new Color32(96, 52, 62, 255));
                int near = horizon - 6 + Mathf.RoundToInt(Noise(x * 0.03f + seed * 2f, 4.1f) * 22f);
                p.Rect(x, horizon - 8, 1, near - horizon + 8, new Color32(70, 38, 44, 255));
            }

            // 잔해는 먼 것부터 그려 가까운 것이 앞을 가린다. nearest가 크면 앞쪽을 비워 둔다 (주인공 배가 놓일 자리).
            var lights = new List<Vector2>();
            for (int i = 0; i < wrecks; i++)
            {
                float depth = Mathf.Lerp(1f, nearest, i / (float)Mathf.Max(1, wrecks - 1));
                int ground = Mathf.RoundToInt(Mathf.Lerp(18f, 104f, depth));
                int width = Mathf.RoundToInt(Mathf.Lerp(120f, 46f, depth));
                int x = 110 + random.Next(260);
                DrawWreck(p, x, ground, width, depth, random, lights);
            }

            WreckLights[shot] = lights;

            // 양쪽 절벽: 가로 지층이 보이는 바위가 장면을 감싼다.
            for (int y = 0; y < Height; y++)
            {
                int left = Mathf.RoundToInt(30f + Noise(y * 0.04f, seed) * 55f + y * 0.06f);
                int right = Mathf.RoundToInt(Width - 30f - Noise(y * 0.04f, seed + 7f) * 55f - y * 0.06f);
                for (int x = 0; x < left; x++)
                {
                    p.Set(x, y, Strata(x, y, left - x));
                }

                for (int x = right; x < Width; x++)
                {
                    p.Set(x, y, Strata(x, y, x - right));
                }
            }
        }

        private static readonly Color SandNear = new Color(0.58f, 0.4f, 0.26f);
        private static readonly Color SandFar = new Color(0.42f, 0.26f, 0.22f);

        // 절벽의 지층. 안쪽 가장자리일수록 밝게 해서 협곡 벽이 도드라진다.
        private static Color32 Strata(int x, int y, int fromEdge)
        {
            int band = (y + Mathf.RoundToInt(Noise(x * 0.08f, y * 0.02f) * 8f)) / 7 % 3;
            Color c = band == 0 ? new Color(0.47f, 0.28f, 0.2f) : band == 1 ? new Color(0.39f, 0.23f, 0.18f) : new Color(0.53f, 0.33f, 0.23f);
            c *= fromEdge < 3 ? 1.2f : 0.9f;
            c.a = 1f;
            return Dither(c, x, y);
        }

        // 반쯤 묻힌 우주선 잔해 하나. 앞이 뾰족한 선체가 비스듬히 박혀 있고 골조가 드러나 있다.
        private static void DrawWreck(Painter p, int cx, int ground, int width, float depth, Random random, List<Vector2> lights)
        {
            Color[] hulls = { new Color(0.6f, 0.62f, 0.68f), new Color(0.68f, 0.6f, 0.48f), new Color(0.48f, 0.56f, 0.52f), new Color(0.64f, 0.48f, 0.48f) };
            Color hull = Color.Lerp(hulls[random.Next(hulls.Length)], new Color(0.35f, 0.2f, 0.25f), depth * 0.5f);
            Color dark = hull * 0.55f;
            Color light = hull * 1.2f;
            dark.a = light.a = hull.a = 1f;
            int height = width / 3;
            int left = cx - width / 2;
            float tilt = (float)(random.NextDouble() - 0.5) * 0.4f;
            for (int dx = 0; dx < width; dx++)
            {
                float u = dx / (float)width;
                int top = Mathf.Max(2, Mathf.RoundToInt(height * (u < 0.78f ? 1f : (1f - u) / 0.22f) + dx * tilt));
                for (int dy = 0; dy < top; dy++)
                {
                    Color c = dy >= top - 2 ? light : dy < 3 ? dark : hull;
                    p.Set(left + dx, ground + dy, Dither(c, left + dx, ground + dy));
                }
            }

            // 창문 줄, 찢어진 구멍, 꼬리 쪽의 드러난 골조
            for (int k = 0; k * 9 + 8 < width * 0.7f; k++)
            {
                Color32 window = random.Next(4) == 0 ? new Color32(255, 200, 120, 255) : (Color32)dark;
                p.Rect(left + 6 + k * 9, ground + height / 2, 3, 2, window);
            }

            p.Rect(left + width / 3, ground + height / 4, Mathf.Max(3, width / 7), Mathf.Max(2, height / 3), new Color32(20, 14, 18, 255));
            for (int k = 0; k < 4; k++)
            {
                int x = left + 2 + k * 4;
                p.Line(x, ground + height, x + 2, ground + height + height / 2 + k % 2 * 3, dark);
            }

            lights.Add(new Vector2(left + width * 0.7f, ground + height + 1));

            // 아래쪽은 모래에 묻혀 있다.
            Color sand = Color.Lerp(SandNear, SandFar, ground / 120f);
            sand.a = 1f;
            for (int dx = -4; dx < width + 4; dx++)
            {
                int mound = 2 + Mathf.RoundToInt(Noise((left + dx) * 0.2f, ground) * 4f);
                p.Rect(left + dx, ground - 1, 1, mound, Dither(sand, left + dx, ground));
            }
        }

        // 협곡에 처박힌 회수선: 기울어진 선체, 땅을 긁은 자국, 앞이 묻힌 모래 둔덕
        private static void DrawCrashedShip(Painter p, int cx, int cy)
        {
            var scorch = new Color32(60, 40, 34, 255);
            for (int x = 120; x < cx - 40; x++)
            {
                int width = 2 + (x - 120) / 40;
                p.Rect(x, cy - 6 - width / 2, 1, width, scorch);
            }

            DrawShip(p, cx, cy);
            Color sand = SandNear;
            sand.a = 1f;
            for (int x = cx + 20; x < cx + 70; x++)
            {
                int mound = Mathf.RoundToInt(Mathf.Sin((x - cx - 20) / 50f * Mathf.PI) * 14f);
                p.Rect(x, cy - 10, 1, mound + 4, Dither(sand * 1.05f, x, cy));
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

            // 창문 너머: 노을 진 협곡과 멀리 보이는 다른 배의 잔해
            var window = new Painter(140, 70, new Color32(4, 4, 12, 255));
            for (int y = 0; y < 70; y++)
            {
                for (int x = 0; x < 140; x++)
                {
                    float ridge = 22f + Noise(x * 0.05f, 2f) * 16f;
                    Color c = y < ridge
                        ? new Color(0.36f, 0.2f, 0.2f) * (0.85f + Noise(x * 0.1f, y * 0.1f) * 0.3f)
                        : Color.Lerp(new Color(0.8f, 0.45f, 0.3f), new Color(0.2f, 0.1f, 0.25f), (y - ridge) / (70f - ridge));
                    c.a = 1f;
                    window.Set(x, y, Dither(c, x, y));
                }
            }

            window.Rect(84, 22, 30, 7, new Color32(70, 70, 80, 255));
            window.Rect(92, 29, 14, 4, new Color32(70, 70, 80, 255));
            window.Set(110, 28, new Color32(255, 150, 60, 255));
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
