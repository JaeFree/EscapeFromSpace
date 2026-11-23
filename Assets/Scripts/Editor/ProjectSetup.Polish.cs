using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 효과와 사운드 (2026-11-17 디자인). 다른 에셋과 같은 규칙을 따른다:
    // 없는 것만 만들고, 아직 단색 도형인 것만 바꾸고, 비어 있는 칸만 채운다.
    public static partial class ProjectSetup
    {
        private const string EffectArtFolder = "Assets/Art/Effects";
        private const string GlowPath = EffectArtFolder + "/Glow.png";
        private const string FlarePath = EffectArtFolder + "/Flare.png";
        private const string SplatPath = EffectArtFolder + "/Splat.png";
        private const string RingPath = EffectArtFolder + "/Ring.png";
        private const string HazardPath = EffectArtFolder + "/Hazard.png";
        private const string BeamPath = EffectArtFolder + "/Beam.png";
        private const string BeamMaterialPath = EffectArtFolder + "/Beam.mat";
        private const string AlertPath = EffectArtFolder + "/Alert.png";
        private const string MuzzleFlashPrefabPath = "Assets/Prefabs/MuzzleFlash.prefab";
        private const string RobotMuzzleFlashPrefabPath = "Assets/Prefabs/RobotMuzzleFlash.prefab";
        private const string ExplosionPrefabPath = "Assets/Prefabs/Explosion.prefab";

        // 소리는 Kenney의 CC0 효과음 팩에서 골라 저장소에 넣어 둔 것이다 (폴더마다 License.txt).
        private const string SciFiAudio = "Assets/Audio/Kenney/SciFiSounds/";
        private const string DigitalAudio = "Assets/Audio/Kenney/DigitalAudio/";
        private const string ImpactAudio = "Assets/Audio/Kenney/ImpactSounds/";
        private const string InterfaceAudio = "Assets/Audio/Kenney/InterfaceSounds/";
        private const string SoundLibraryPath = "Assets/Data/SoundLibrary.asset";

        // 대기실 음악: Fupi의 "Bird in Hand Night" (CC0, 폴더에 License.txt)
        private const string LobbyMusicPath = "Assets/Audio/Music/BirdInHandNight.ogg";

        private delegate float AlphaAt(float x, float y);

        private delegate Color ColorAt(float x, float y);

        // ---------- 효과 ----------

        private static void EnsureEffects(Sprite circle)
        {
            Directory.CreateDirectory(EffectArtFolder);

            // 가운데가 밝고 가장자리로 갈수록 옅어지는 빛
            Sprite glow = EnsureEffectSprite(GlowPath, 64, (x, y) =>
            {
                float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y));
                return falloff * falloff;
            });

            // 네 갈래로 뻗는 섬광. 총구 불빛에 쓴다.
            Sprite flare = EnsureEffectSprite(FlarePath, 64, (x, y) =>
            {
                float star = Mathf.Clamp01(1f - (Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y))));
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 2.2f);
                return Mathf.Clamp01(star * 2.5f + core * core);
            });

            // 고르지 않게 튄 얼룩. 혈흔에 쓴다. 매번 같은 모양이 나오게 씨앗을 고정한다.
            var random = new System.Random(7);
            var blobs = new Vector3[9];
            blobs[0] = new Vector3(0f, 0f, 0.42f);
            for (int i = 1; i < blobs.Length; i++)
            {
                float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
                float distance = 0.32f + (float)random.NextDouble() * 0.42f;
                float radius = 0.08f + (float)random.NextDouble() * 0.14f;
                blobs[i] = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, radius);
            }

            Sprite splat = EnsureEffectSprite(SplatPath, 64, (x, y) =>
            {
                float alpha = 0f;
                foreach (Vector3 blob in blobs)
                {
                    float distance = Mathf.Sqrt((x - blob.x) * (x - blob.x) + (y - blob.y) * (y - blob.y));
                    alpha = Mathf.Max(alpha, Mathf.Clamp01((blob.z - distance) * 24f));
                }

                return alpha;
            });

            // 폭발의 충격파
            Sprite ring = EnsureEffectSprite(RingPath, 128, (x, y) =>
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                return Mathf.Clamp01(1f - Mathf.Abs(distance - 0.86f) / 0.1f);
            });

            // 사격장 바닥의 빗금과 테두리
            EnsureEffectSprite(HazardPath, 64, (x, y) =>
            {
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) > 0.93f)
                {
                    return 1f;
                }

                return Mathf.Repeat((x + y) * 2f, 1f) < 0.5f ? 0.85f : 0.2f;
            });

            EnsureFlashPrefab(MuzzleFlashPrefabPath, "MuzzleFlash", flare, new Color(1f, 0.95f, 0.7f, 0.95f), 0.07f, 0.35f, 0.75f);
            EnsureFlashPrefab(RobotMuzzleFlashPrefabPath, "RobotMuzzleFlash", flare, new Color(1f, 0.45f, 0.3f, 0.95f), 0.07f, 0.35f, 0.75f);
            EnsureExplosionPrefab(glow, ring);
            UpgradeHitMarker(circle, glow);
            UpgradeBloodDecal(circle, splat);
            UpgradeSpark(glow);

            // 새 효과를 쓰는 쪽에 잇는다.
            SetPrefabReference<PlayerShooter>(PlayerPrefabPath, "_muzzleFlashPrefab", MuzzleFlashPrefabPath);
            SetPrefabReference<RobotAttack>(RobotPrefabPath, "_muzzleFlashPrefab", RobotMuzzleFlashPrefabPath);
            SetPrefabReference<Robot>(RobotPrefabPath, "_explosionPrefab", ExplosionPrefabPath);

            EnsureBeamsAndMarks(glow);
        }

        // 궤적 선, 저격 조준선, 로봇의 "!" 표시 (2026-11-17 디자인 넷째 범위).
        private static void EnsureBeamsAndMarks(Sprite glow)
        {
            // 선의 굵기 방향으로 가운데는 꽉 차고 가장자리로 갈수록 옅어지는 빛줄기. 선에 입히면 광선처럼 보인다.
            EnsureEffectSprite(BeamPath, 32, (AlphaAt)((x, y) =>
            {
                float edge = Mathf.Clamp01((1f - Mathf.Abs(y)) / 0.72f);
                return edge * edge;
            }));

            Material spriteMaterial = GetSpriteMaterial();
            var beam = AssetDatabase.LoadAssetAtPath<Material>(BeamMaterialPath);
            if (beam == null)
            {
                beam = new Material(spriteMaterial) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BeamPath) };
                AssetDatabase.CreateAsset(beam, BeamMaterialPath);
            }

            // 굵은 막대와 점으로 된 "!". 위는 노랑, 아래는 주황이고 어두운 테두리를 둘러 어떤 바닥 위에서도 보인다.
            Sprite alert = EnsureEffectSprite(AlertPath, 64, (ColorAt)((x, y) =>
            {
                float along = Mathf.Clamp01((0.72f - y) / 0.82f);
                float barY = 0.72f - 0.82f * along;
                float barDistance = Mathf.Sqrt(x * x + (y - barY) * (y - barY)) - Mathf.Lerp(0.2f, 0.1f, along);
                float dotDistance = Mathf.Sqrt(x * x + (y + 0.62f) * (y + 0.62f)) - 0.17f;
                float distance = Mathf.Min(barDistance, dotDistance);

                Color fill = Color.Lerp(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.95f, 0.35f), Mathf.InverseLerp(-0.8f, 0.9f, y));
                var outline = new Color(0.16f, 0.07f, 0.05f);
                Color color = Color.Lerp(fill, outline, Mathf.Clamp01(distance / 0.03f + 0.5f));
                color.a = 1f - Mathf.Clamp01((distance - 0.1f) / 0.03f + 0.5f);
                return color;
            }));

            // 궤적 선: 가는 단색 선을 빛줄기로 바꾼다.
            foreach (string path in new[] { TracerPrefabPath, RobotTracerPrefabPath })
            {
                GameObject tracer = PrefabUtility.LoadPrefabContents(path);
                var line = tracer.GetComponent<LineRenderer>();
                if (line.sharedMaterial == spriteMaterial)
                {
                    line.sharedMaterial = beam;
                    line.textureMode = LineTextureMode.Stretch;
                    line.startWidth = 0.1f;
                    line.endWidth = 0.18f;
                    PrefabUtility.SaveAsPrefabAsset(tracer, path);
                }

                PrefabUtility.UnloadPrefabContents(tracer);
            }

            GameObject robot = PrefabUtility.LoadPrefabContents(RobotPrefabPath);
            bool changed = false;

            // "!" 표시: 막대와 점 도형을 그림 한 장으로 바꾸고, 켜질 때 튀어나오게 한다.
            Transform alertMark = robot.transform.Find("AlertMark");
            Transform oldBar = alertMark != null ? alertMark.Find("Bar") : null;
            if (oldBar != null)
            {
                Object.DestroyImmediate(oldBar.gameObject);
                Transform oldDot = alertMark.Find("Dot");
                if (oldDot != null)
                {
                    Object.DestroyImmediate(oldDot.gameObject);
                }

                alertMark.localScale = new Vector3(0.8f, 0.8f, 1f);
                var mark = alertMark.gameObject.AddComponent<SpriteRenderer>();
                mark.sprite = alert;
                mark.sortingOrder = 12;
                alertMark.gameObject.AddComponent<PopIn>();
                changed = true;
            }

            // 저격 조준선: 빛줄기로 바꾸고, 닿는 곳에 점을 찍는다.
            var telegraph = robot.GetComponentInChildren<AttackTelegraph>(true);
            if (telegraph != null)
            {
                Transform aimLine = telegraph.transform.Find("AimLine");
                var line = aimLine != null ? aimLine.GetComponent<LineRenderer>() : null;
                if (line != null && line.sharedMaterial == spriteMaterial)
                {
                    line.sharedMaterial = beam;
                    line.textureMode = LineTextureMode.Stretch;

                    // 빛줄기는 가장자리가 옅어서 같은 굵기면 더 가늘어 보인다. 처음 값 그대로일 때만 굵힌다.
                    var so = new SerializedObject(telegraph);
                    SerializedProperty tracking = so.FindProperty("_trackingWidth");
                    SerializedProperty locked = so.FindProperty("_lockedWidth");
                    if (Mathf.Approximately(tracking.floatValue, 0.03f))
                    {
                        tracking.floatValue = 0.08f;
                    }

                    if (Mathf.Approximately(locked.floatValue, 0.07f))
                    {
                        locked.floatValue = 0.2f;
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (EnsureChild(telegraph.transform, "AimDot", out Transform aimDot))
                {
                    var dot = aimDot.gameObject.AddComponent<SpriteRenderer>();
                    dot.sprite = glow;
                    dot.sortingOrder = 8;
                    dot.enabled = false;

                    var so = new SerializedObject(telegraph);
                    so.FindProperty("_endDot").objectReferenceValue = dot;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(robot, RobotPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(robot);
        }

        // 흰색 그림을 한 장 만든다. draw는 가운데가 (0, 0)이고 가장자리가 ±1인 좌표에서 진하기(0~1)를 돌려준다.
        private static Sprite EnsureEffectSprite(string path, int size, AlphaAt draw)
        {
            return EnsureEffectSprite(path, size, (ColorAt)((x, y) => new Color(1f, 1f, 1f, Mathf.Clamp01(draw(x, y)))));
        }

        // 색이 있는 그림을 한 장 만든다. 좌표는 위와 같다.
        private static Sprite EnsureEffectSprite(string path, int size, ColorAt draw)
        {
            if (!File.Exists(path))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float half = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        pixels[y * size + x] = draw((x + 0.5f - half) / half, (y + 0.5f - half) / half);
                    }
                }

                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                // 그림 전체가 1칸이 되게 읽는다. 크기는 쓰는 쪽에서 정한다.
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFlashPrefab(
            string path, string prefabName, Sprite sprite, Color color, float lifetime, float startSize, float endSize)
        {
            if (File.Exists(path))
            {
                return;
            }

            var root = new GameObject(prefabName);
            AddFlash(root, sprite, color, 11, lifetime, startSize, endSize, true);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void AddFlash(
            GameObject target, Sprite sprite, Color color, int sortingOrder, float lifetime, float startSize, float endSize, bool randomRotation)
        {
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var flash = new SerializedObject(target.AddComponent<FlashEffect>());
            flash.FindProperty("_lifetime").floatValue = lifetime;
            flash.FindProperty("_startSize").floatValue = startSize;
            flash.FindProperty("_endSize").floatValue = endSize;
            flash.FindProperty("_randomRotation").boolValue = randomRotation;
            flash.ApplyModifiedPropertiesWithoutUndo();
        }

        // 로봇이 부서질 때: 파편이 튀고, 빛이 번쩍이고, 충격파 고리가 퍼진다.
        private static void EnsureExplosionPrefab(Sprite glow, Sprite ring)
        {
            if (File.Exists(ExplosionPrefabPath))
            {
                return;
            }

            var root = new GameObject("Explosion");
            var particles = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.35f), new Color(1f, 0.4f, 0.15f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 다 튀고 나면 자식 효과까지 함께 사라진다.
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            // 파편은 날아가며 느려지고 작아진다.
            ParticleSystem.LimitVelocityOverLifetimeModule limit = particles.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.12f;
            limit.limit = 1f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = GetSpriteMaterial();
            particleRenderer.sortingOrder = 10;

            var flash = new GameObject("Flash");
            flash.transform.SetParent(root.transform, false);
            AddFlash(flash, glow, new Color(1f, 0.8f, 0.4f, 0.95f), 11, 0.22f, 0.6f, 2.4f, false);

            var wave = new GameObject("Shockwave");
            wave.transform.SetParent(root.transform, false);
            AddFlash(wave, ring, new Color(1f, 0.85f, 0.6f, 0.8f), 11, 0.35f, 0.5f, 3f, false);

            PrefabUtility.SaveAsPrefabAsset(root, ExplosionPrefabPath);
            Object.DestroyImmediate(root);
        }

        // 명중점: 노란 점 대신 퍼지며 사라지는 섬광
        private static void UpgradeHitMarker(Sprite circle, Sprite glow)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HitMarkerPrefabPath);
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer.sprite == circle)
            {
                Object.DestroyImmediate(renderer);
                var timed = root.GetComponent<TimedDestroy>();
                if (timed != null)
                {
                    Object.DestroyImmediate(timed);
                }

                root.transform.localScale = Vector3.one;
                AddFlash(root, glow, new Color(1f, 0.9f, 0.45f, 0.95f), 10, 0.12f, 0.18f, 0.6f, false);
                PrefabUtility.SaveAsPrefabAsset(root, HitMarkerPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        // 혈흔: 타원 대신 튄 얼룩. 놓일 때마다 BloodSplatter가 무작위로 돌린다.
        private static void UpgradeBloodDecal(Sprite circle, Sprite splat)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BloodDecalPrefabPath);
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer.sprite == circle)
            {
                renderer.sprite = splat;
                root.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                PrefabUtility.SaveAsPrefabAsset(root, BloodDecalPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        // 불꽃: 튀는 조각에 번쩍이는 빛을 더한다.
        private static void UpgradeSpark(Sprite glow)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SparkPrefabPath);
            if (root.transform.Find("Flash") == null)
            {
                var flash = new GameObject("Flash");
                flash.transform.SetParent(root.transform, false);
                AddFlash(flash, glow, new Color(1f, 0.65f, 0.25f, 0.85f), 10, 0.12f, 0.2f, 0.75f, false);
                PrefabUtility.SaveAsPrefabAsset(root, SparkPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void SetPrefabReference<T>(string prefabPath, string propertyName, string valuePath) where T : Component
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            var so = new SerializedObject(root.GetComponent<T>());
            SerializedProperty property = so.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(valuePath);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        // 씬에 직접 놓은 것들의 효과: 탈출 포드의 빛, 포탈의 움직임, 사격장 바닥, 표적의 폭발.
        private static void EnsureSceneEffects()
        {
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            var glow = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);

            foreach (EscapePod pod in Object.FindObjectsByType<EscapePod>(FindObjectsSortMode.None))
            {
                // 열린 포드 위에 숨 쉬듯 커졌다 작아지는 초록빛. 닫혀 있으면 EscapePod가 꺼 둔다.
                if (EnsureChild(pod.transform, "OpenGlow", out Transform openGlow))
                {
                    openGlow.localScale = new Vector3(1.7f, 1.7f, 1f);
                    var renderer = openGlow.gameObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = glow;
                    renderer.color = new Color(0.35f, 1f, 0.55f, 0.4f);
                    renderer.sortingOrder = -3;

                    var pulse = new SerializedObject(openGlow.gameObject.AddComponent<Pulse>());
                    pulse.FindProperty("_scaleAmount").floatValue = 0.1f;
                    pulse.FindProperty("_period").floatValue = 1.4f;
                    pulse.ApplyModifiedPropertiesWithoutUndo();
                }

                SetReferenceIfEmpty(pod, "_openEffect", openGlow.gameObject);
            }

            GameObject portal = GameObject.Find("Portal");
            if (portal != null && EnsureComponent(portal, out Pulse portalPulse))
            {
                var pulse = new SerializedObject(portalPulse);
                pulse.FindProperty("_spinSpeed").floatValue = 25f;
                pulse.FindProperty("_scaleAmount").floatValue = 0.04f;
                pulse.FindProperty("_period").floatValue = 2f;
                pulse.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject range = GameObject.Find("ShootingRange");
            if (range != null)
            {
                var renderer = range.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite == square)
                {
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HazardPath);
                    renderer.color = new Color(0.95f, 0.8f, 0.3f, 0.55f);
                    EditorUtility.SetDirty(renderer);
                }
            }

            var explosion = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);
            foreach (RangeTarget target in Object.FindObjectsByType<RangeTarget>(FindObjectsSortMode.None))
            {
                SetReferenceIfEmpty(target, "_explosionPrefab", explosion);
            }
        }

        // ---------- 사운드 ----------

        private static void EnsureSounds()
        {
            AssetDatabase.Refresh();

            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(library, SoundLibraryPath);
            }

            SetCue(library, "_footstep", 0.3f, 1f, 0.12f, ImpactAudio, "footstep_concrete_000", "footstep_concrete_001",
                "footstep_concrete_002", "footstep_concrete_003", "footstep_concrete_004");
            SetCue(library, "_roll", 0.6f, 1f, 0.08f, ImpactAudio, "impactSoft_heavy_000", "impactSoft_heavy_001");
            SetCue(library, "_reloadStart", 0.6f, 1f, 0.03f, InterfaceAudio, "switch_003");
            SetCue(library, "_reloadEnd", 0.7f, 0.9f, 0.03f, InterfaceAudio, "toggle_002");
            SetCue(library, "_playerHurt", 0.8f, 1f, 0.08f, ImpactAudio, "impactPunch_medium_000", "impactPunch_medium_001",
                "impactPunch_medium_003");
            SetCue(library, "_playerDeath", 0.9f, 0.8f, 0f, DigitalAudio, "lowThreeTone");
            SetCue(library, "_itemUseStart", 0.6f, 1f, 0.05f, InterfaceAudio, "pluck_002");
            SetCue(library, "_healDone", 0.6f, 1f, 0.03f, DigitalAudio, "powerUp2");
            SetCue(library, "_oxygenCan", 0.6f, 1f, 0.03f, DigitalAudio, "phaserUp5");
            SetCue(library, "_oxygenWarning", 0.35f, 1f, 0f, DigitalAudio, "tone1");

            SetCue(library, "_wallImpact", 0.35f, 1f, 0.12f, ImpactAudio, "impactGeneric_light_000", "impactGeneric_light_001",
                "impactGeneric_light_002");
            SetCue(library, "_robotHit", 0.5f, 1f, 0.1f, SciFiAudio, "impactMetal_000", "impactMetal_002", "impactMetal_004");
            SetCue(library, "_robotDestroyed", 0.85f, 1f, 0.08f, SciFiAudio, "explosionCrunch_001", "explosionCrunch_002");
            SetCue(library, "_robotAlert", 0.6f, 1.1f, 0.03f, DigitalAudio, "twoTone1");
            SetCue(library, "_robotCharge", 0.6f, 0.9f, 0.03f, DigitalAudio, "phaserUp2");
            SetCue(library, "_robotAimLock", 0.7f, 1.2f, 0f, DigitalAudio, "pepSound3");

            SetCue(library, "_lootSearch", 0.45f, 1f, 0.12f, InterfaceAudio, "scratch_004");
            SetCue(library, "_lootOpen", 0.6f, 1f, 0.03f, InterfaceAudio, "open_002");
            SetCue(library, "_bagOpen", 0.5f, 1f, 0.03f, InterfaceAudio, "maximize_007");
            SetCue(library, "_bagClose", 0.5f, 1f, 0.03f, InterfaceAudio, "minimize_007");
            SetCue(library, "_itemPick", 0.5f, 1.1f, 0.05f, InterfaceAudio, "click_001");
            SetCue(library, "_itemPlace", 0.55f, 1f, 0.05f, InterfaceAudio, "drop_001");
            SetCue(library, "_itemRefuse", 0.5f, 1f, 0f, InterfaceAudio, "error_001");
            SetCue(library, "_itemDropToWorld", 0.55f, 0.9f, 0.05f, InterfaceAudio, "drop_003");

            SetCue(library, "_podOpen", 0.7f, 1f, 0f, SciFiAudio, "doorOpen_000");
            SetCue(library, "_escapeSuccess", 0.8f, 1f, 0f, DigitalAudio, "zapThreeToneUp");
            SetCue(library, "_uiPrompt", 0.6f, 1f, 0f, InterfaceAudio, "question_001");
            SetCue(library, "_uiConfirm", 0.6f, 1f, 0f, InterfaceAudio, "confirmation_001");
            SetCue(library, "_uiCancel", 0.6f, 1f, 0f, InterfaceAudio, "back_003");
            SetCue(library, "_mapOpen", 0.5f, 1f, 0f, InterfaceAudio, "maximize_008");
            SetCue(library, "_mapClose", 0.5f, 1f, 0f, InterfaceAudio, "minimize_008");
            SetCue(library, "_portalLaunch", 0.8f, 1f, 0f, SciFiAudio, "forceField_000");

            // 무기마다 다른 발사음 (2026-11-23 사용자 의견: 총소리가 작다 → 0.2씩 올림): 권총은 가볍게, 소총은 짧게 끊어서, 저격은 굵게, 샷건은 터지듯이, 광선검은 가르는 소리.
            SetCue(AssetDatabase.LoadAssetAtPath<WeaponData>(LaserPistolPath), "_fireSound", 0.75f, 1f, 0.06f, SciFiAudio,
                "laserSmall_000", "laserSmall_001", "laserSmall_002");
            SetCue(AssetDatabase.LoadAssetAtPath<WeaponData>(PulseRiflePath), "_fireSound", 0.65f, 1f, 0.06f, SciFiAudio,
                "laserRetro_000", "laserRetro_001", "laserRetro_002");
            SetCue(AssetDatabase.LoadAssetAtPath<WeaponData>(Nebula7Path), "_fireSound", 1f, 0.85f, 0.04f, SciFiAudio,
                "laserLarge_000", "laserLarge_001");
            SetCue(AssetDatabase.LoadAssetAtPath<WeaponData>(PlasmaScatterPath), "_fireSound", 0.95f, 1.15f, 0.05f, SciFiAudio,
                "explosionCrunch_000");
            SetCue(AssetDatabase.LoadAssetAtPath<WeaponData>(LightSaberPath), "_fireSound", 0.5f, 1f, 0.1f, DigitalAudio,
                "phaserDown2");

            // 로봇은 플레이어 무기보다 낮은 소리로 구분한다.
            SetRobotSound("PistolRobot", 0.45f, 0.85f, SciFiAudio, "laserRetro_003");
            SetRobotSound("RifleRobot", 0.4f, 0.9f, SciFiAudio, "laserRetro_004");
            SetRobotSound("SniperRobot", 0.8f, 0.7f, SciFiAudio, "laserLarge_003");
            SetRobotSound("ShotgunRobot", 0.7f, 1.2f, SciFiAudio, "lowFrequency_explosion_001");
            SetRobotSound("MeleeRobot", 0.5f, 0.75f, DigitalAudio, "phaserDown1");
        }

        private static void SetRobotSound(string robotName, float volume, float pitch, string folder, params string[] clips)
        {
            SetCue(AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(robotName)), "_attackSound", volume, pitch, 0.05f, folder, clips);
        }

        // 소리 파일이 비어 있는 칸만 채운다. 이미 파일을 넣어 둔 칸은 크기와 높낮이도 건드리지 않는다.
        private static void SetCue(
            Object target, string propertyName, float volume, float pitch, float pitchVariation, string folder, params string[] clipNames)
        {
            var so = new SerializedObject(target);
            SerializedProperty cue = so.FindProperty(propertyName);
            SerializedProperty clips = cue.FindPropertyRelative("_clips");
            if (clips.arraySize > 0)
            {
                return;
            }

            clips.arraySize = clipNames.Length;
            for (int i = 0; i < clipNames.Length; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}{clipNames[i]}.ogg");
                if (clip == null)
                {
                    Debug.LogError($"[ProjectSetup] 소리 파일이 없다: {folder}{clipNames[i]}.ogg");
                }

                clips.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }

            cue.FindPropertyRelative("_volume").floatValue = volume;
            cue.FindPropertyRelative("_pitch").floatValue = pitch;
            cue.FindPropertyRelative("_pitchVariation").floatValue = pitchVariation;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 씬마다 소리를 내는 오브젝트를 하나 둔다. 정거장은 낮은 엔진음이 깔리고, 대기실은 편안한 재즈가 흐른다 (2026-11-17 사용자 결정).
        // ambienceClip이나 musicPath가 null이면 그 씬에는 없다.
        // musicStart는 곡의 앞부분을 건너뛰는 길이(초)다 (2026-11-17 사용자 결정: 대기실 곡의 앞 10초쯤을 뺀다).
        private static void EnsureSceneAudio(
            string ambienceClip, float ambienceVolume, string musicPath, float musicVolume, float musicStart)
        {
            GameObject audioObject = GameObject.Find("Audio");
            if (audioObject == null)
            {
                audioObject = new GameObject("Audio");
            }

            if (EnsureComponent(audioObject, out SoundPlayer player) && ambienceClip != null)
            {
                var so = new SerializedObject(player);
                so.FindProperty("_ambience").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SciFiAudio}{ambienceClip}.ogg");
                so.FindProperty("_ambienceVolume").floatValue = ambienceVolume;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(player, "_library", AssetDatabase.LoadAssetAtPath<SoundLibrary>(SoundLibraryPath));

            if (musicPath == null)
            {
                return;
            }

            // 긴 곡을 통째로 풀어 메모리에 올리지 않게 흘려서 튼다.
            var importer = (AudioImporter)AssetImporter.GetAtPath(musicPath);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.Streaming)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            var music = new SerializedObject(player);
            SerializedProperty clip = music.FindProperty("_music");
            if (clip.objectReferenceValue == null)
            {
                clip.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
                music.FindProperty("_musicVolume").floatValue = musicVolume;
                music.ApplyModifiedPropertiesWithoutUndo();
            }

            // 시작 지점은 나중에 추가한 칸이다. 그 곡이 그대로 있고 값이 0일 때만 채운다.
            SerializedProperty start = music.FindProperty("_musicStartTime");
            if (start.floatValue == 0f && AssetDatabase.GetAssetPath(clip.objectReferenceValue) == musicPath)
            {
                start.floatValue = musicStart;
                music.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
