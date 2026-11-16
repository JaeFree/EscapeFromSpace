using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace EscapeFromSpace.EditorTools
{
    // 에셋을 자동으로 만든다. Inspector에서 조정한 값을 되돌리지 않도록
    // 이미 있는 에셋·컴포넌트·참조는 건드리지 않고 없는 것만 채운다.
    public static class ProjectSetup
    {
        private const string SquarePath = "Assets/Art/Placeholder/Square.png";
        private const string CirclePath = "Assets/Art/Placeholder/Circle.png";
        private const string WallTilePath = "Assets/Tiles/WallTile.asset";
        private const string FloorTilePath = "Assets/Tiles/FloorTile.asset";
        private const string PlayerDataPath = "Assets/Data/PlayerData.asset";
        private const string InputPath = "Assets/Input/GameInput.inputactions";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/Scenes/Station.unity";
        private const string NoFrictionPath = "Assets/Physics/NoFriction.physicsMaterial2D";
        private const string LaserPistolPath = "Assets/Data/Weapons/LaserPistol.asset";
        private const string LightSaberPath = "Assets/Data/Weapons/LightSaber.asset";
        private const string PulseRiflePath = "Assets/Data/Weapons/PulseRifle.asset";
        private const string Nebula7Path = "Assets/Data/Weapons/Nebula7.asset";
        private const string PlasmaScatterPath = "Assets/Data/Weapons/PlasmaScatter.asset";
        private const string TracerPrefabPath = "Assets/Prefabs/BulletTracer.prefab";
        private const string SwingArcPrefabPath = "Assets/Prefabs/SwingArc.prefab";
        private const string RobotDataFolder = "Assets/Data/Robots";
        private const string RobotPrefabPath = "Assets/Prefabs/Robot.prefab";
        private const string RobotTracerPrefabPath = "Assets/Prefabs/RobotTracer.prefab";
        private const string RobotSwingArcPrefabPath = "Assets/Prefabs/RobotSwingArc.prefab";
        private const string CombatConfigPath = "Assets/Data/CombatConfig.asset";
        private const string HitMarkerPrefabPath = "Assets/Prefabs/HitMarker.prefab";
        private const string BloodDecalPrefabPath = "Assets/Prefabs/BloodDecal.prefab";
        private const string SparkPrefabPath = "Assets/Prefabs/Spark.prefab";

        private static readonly string[] Folders =
        {
            "Assets/Art/Placeholder", "Assets/Data", "Assets/Data/Weapons", RobotDataFolder, "Assets/Prefabs", "Assets/Tiles", "Assets/Physics",
        };

        [MenuItem("EscapeFromSpace/에셋 생성·갱신")]
        public static void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            foreach (string folder in Folders)
            {
                Directory.CreateDirectory(folder);
            }

            AssetDatabase.Refresh();

            int playerLayer = EnsureLayer("Player");
            int wallLayer = EnsureLayer("Wall");
            int enemyLayer = EnsureLayer("Enemy");

            Sprite square = EnsureSprite(SquarePath, 32, false);
            Sprite circle = EnsureSprite(CirclePath, 64, true);
            EnsureTile(WallTilePath, square, new Color(0.55f, 0.57f, 0.6f), Tile.ColliderType.Grid);
            EnsureTile(FloorTilePath, square, new Color(0.16f, 0.17f, 0.2f), Tile.ColliderType.None);

            PlayerData playerData = EnsurePlayerData();
            EnsureWeapons();
            var laserPistol = AssetDatabase.LoadAssetAtPath<WeaponData>(LaserPistolPath);
            CombatConfig combatConfig = EnsureCombatConfig();
            EnsureHitMarkerPrefab(circle);
            EnsureTracerPrefab(TracerPrefabPath, new Color(1f, 0.95f, 0.6f));
            EnsureSwingArcPrefab(SwingArcPrefabPath, new Color(0.55f, 0.9f, 1f, 0.6f));
            EnsureBloodDecalPrefab(circle);
            EnsureSparkPrefab();
            EnsureRobots();
            EnsureTracerPrefab(RobotTracerPrefabPath, new Color(1f, 0.3f, 0.25f));
            EnsureSwingArcPrefab(RobotSwingArcPrefabPath, new Color(1f, 0.3f, 0.25f, 0.6f));
            EnsureRobotPrefab(combatConfig, square, circle, playerLayer, wallLayer, enemyLayer);
            EnsurePlayerPrefab(playerData, laserPistol, combatConfig, square, circle, playerLayer, wallLayer, enemyLayer);
            AssetDatabase.SaveAssets();

            EnsureScene(wallLayer);

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] 에셋 생성·갱신 완료");
        }

        // ---------- 레이어 ----------

        private static int EnsureLayer(string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return i;
                }
            }

            // 0~5번은 Unity 기본 레이어라 6번부터 빈 칸을 찾는다.
            for (int i = 6; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            }

            throw new System.InvalidOperationException($"빈 레이어 칸이 없어 '{layerName}' 레이어를 만들 수 없다.");
        }

        // ---------- 스프라이트와 타일 ----------

        private static Sprite EnsureSprite(string path, int size, bool isCircle)
        {
            if (!File.Exists(path))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float radius = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float alpha = 1f;
                        if (isCircle)
                        {
                            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                            alpha = Mathf.Clamp01(radius - distance);
                        }

                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.mipmapEnabled = false;
                importer.filterMode = isCircle ? FilterMode.Bilinear : FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                // 타일 사이에 틈이 보이지 않도록 사각형 전체를 메시로 쓴다.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureTile(string path, Sprite sprite, Color color, Tile.ColliderType colliderType)
        {
            if (AssetDatabase.LoadAssetAtPath<Tile>(path) != null)
            {
                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = colliderType;
            AssetDatabase.CreateAsset(tile, path);
        }

        // ---------- 데이터 에셋 ----------

        private static PlayerData EnsurePlayerData()
        {
            var data = AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath);
            if (data != null)
            {
                // 나중에 추가한 필드는 기존 에셋에 0으로 들어온다. 0일 때만 초기값을 채운다.
                SetFloatIfZero(data, "_reloadMoveSpeedMultiplier", 0.7f);
                return data;
            }

            // 기획서 초기값. 에셋을 처음 만들 때만 넣는다.
            data = ScriptableObject.CreateInstance<PlayerData>();
            var so = new SerializedObject(data);
            so.FindProperty("_moveSpeed").floatValue = 5f;
            so.FindProperty("_reloadMoveSpeedMultiplier").floatValue = 0.7f;
            so.FindProperty("_maxHp").floatValue = 100f;
            so.FindProperty("_rollDistance").floatValue = 3f;
            so.FindProperty("_rollDuration").floatValue = 0.4f;
            so.FindProperty("_rollInvincibleDuration").floatValue = 0.4f;
            so.FindProperty("_rollCooldown").floatValue = 3f;
            so.FindProperty("_hitFlashDuration").floatValue = 0.1f;
            so.FindProperty("_hitStunDuration").floatValue = 0.2f;
            so.FindProperty("_maxBloodDecals").intValue = 100;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, PlayerDataPath);
            return data;
        }

        private static PhysicsMaterial2D EnsureNoFrictionMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (material != null)
            {
                return material;
            }

            material = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, NoFrictionPath);
            return material;
        }

        private static CombatConfig EnsureCombatConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatConfig>(CombatConfigPath);
            if (config != null)
            {
                SetFloatIfZero(config, "_cameraShakePerRecoilDegree", 0.03f);
                SetFloatIfZero(config, "_cameraShakeDuration", 0.1f);
                return config;
            }

            // 기획서 4.1의 방어력 초기값. 에셋을 처음 만들 때만 넣는다.
            config = ScriptableObject.CreateInstance<CombatConfig>();
            var so = new SerializedObject(config);
            so.FindProperty("_armorReductionPerPoint").floatValue = 0.04f;
            so.FindProperty("_maxArmor").intValue = 12;
            so.FindProperty("_cameraShakePerRecoilDegree").floatValue = 0.03f;
            so.FindProperty("_cameraShakeDuration").floatValue = 0.1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(config, CombatConfigPath);
            return config;
        }

        // 기획서 4.2 표의 초기값. 에셋을 처음 만들 때만 넣는다.
        private static void EnsureWeapons()
        {
            EnsureWeapon(LightSaberPath, "광선검", WeaponCategory.Melee, 40f, 1, 1.25f, 1.8f, 0, 0f, 120f, 0f, 0f, 1.5f);
            EnsureWeapon(LaserPistolPath, "레이저 피스톨", WeaponCategory.Pistol, 15f, 1, 3f, 12f, 12, 1.2f, 2f, 0f, 1.5f, 1f);
            EnsureWeapon(PulseRiflePath, "펄스 라이플", WeaponCategory.AssaultRifle, 10f, 1, 8f, 18f, 30, 2f, 3f, 0f, 0.8f, 3.5f);
            EnsureWeapon(Nebula7Path, "네뷸라-7", WeaponCategory.Sniper, 85f, 1, 0.6f, 30f, 5, 3f, 6f, 0.5f, 6f, 4.5f);
            EnsureWeapon(PlasmaScatterPath, "플라즈마 스캐터", WeaponCategory.Shotgun, 9f, 8, 1f, 6f, 6, 2.5f, 18f, 0f, 4f, 4f);
        }

        private static void EnsureWeapon(
            string path, string displayName, WeaponCategory category, float damage, int pelletCount, float fireRate,
            float range, int magazineSize, float reloadTime, float spreadAngle, float aimSpreadAngle, float recoilPerShot,
            float weight)
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (data != null)
            {
                // 나중에 추가한 필드는 기존 에셋에 0으로 들어온다. 0일 때만 초기값을 채운다.
                SetFloatIfZero(data, "_recoilRecovery", 4f);
                SetFloatIfZero(data, "_maxRecoil", 10f);
                SetFloatIfZero(data, "_aimSpreadAngle", aimSpreadAngle);
                return;
            }

            data = ScriptableObject.CreateInstance<WeaponData>();
            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_category").enumValueIndex = (int)category;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_pelletCount").intValue = pelletCount;
            so.FindProperty("_fireRate").floatValue = fireRate;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_magazineSize").intValue = magazineSize;
            so.FindProperty("_reloadTime").floatValue = reloadTime;
            so.FindProperty("_spreadAngle").floatValue = spreadAngle;
            so.FindProperty("_aimSpreadAngle").floatValue = aimSpreadAngle;
            so.FindProperty("_recoilPerShot").floatValue = recoilPerShot;
            so.FindProperty("_recoilRecovery").floatValue = 4f;
            so.FindProperty("_maxRecoil").floatValue = 10f;
            so.FindProperty("_weight").floatValue = weight;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, path);
        }

        // 기획서 5.3 표의 초기값. 에셋을 처음 만들 때만 넣는다.
        // 사격 패턴은 (충전) → 발 수만큼 [쏘고 발 간격만큼 기다림] → 쉼 순서다.
        // 발 간격 1/3초와 1/8초는 기획서 5.4의 초당 피해(9.6, 14.1)에 맞춘 값이다.
        private static void EnsureRobots()
        {
            EnsureRobot("MeleeRobot", "근접 보안 로봇", new Color(1f, 0.55f, 0.2f), 70f, RobotAttackKind.Melee,
                20f, 1, 120f, 1.8f, 0f, 1, 0f, 1.2f, 2.5f, 4.5f, 8f, 120f);
            EnsureRobot("PistolRobot", "권총 보안 로봇", new Color(0.9f, 0.35f, 0.3f), 60f, RobotAttackKind.Gun,
                8f, 1, 2f, 9f, 0f, 2, 1f / 3f, 1f, 2f, 3.5f, 10f, 90f);
            EnsureRobot("RifleRobot", "AR 보안 로봇", new Color(0.75f, 0.4f, 0.9f), 90f, RobotAttackKind.Gun,
                6f, 1, 3f, 12f, 0f, 5, 0.125f, 1.5f, 2f, 3f, 12f, 90f);
            EnsureRobot("SniperRobot", "저격 보안 로봇", new Color(0.95f, 0.3f, 0.7f), 50f, RobotAttackKind.Sniper,
                40f, 1, 0.5f, 25f, 1.5f, 1, 0f, 1.5f, 0f, 0f, 25f, 30f);
            EnsureRobot("ShotgunRobot", "샷건 보안 로봇", new Color(0.7f, 0.2f, 0.2f), 110f, RobotAttackKind.Shotgun,
                5f, 8, 18f, 5f, 0.5f, 1, 0f, 1f, 2f, 3.5f, 8f, 90f);
        }

        private static void EnsureRobot(
            string fileName, string displayName, Color color, float maxHp, RobotAttackKind attackKind, float damage,
            int pelletCount, float spreadAngle, float range, float windupTime, int burstCount, float burstInterval,
            float restTime, float patrolSpeed, float chaseSpeed, float sightDistance, float sightAngle)
        {
            // 충전하는 공격만 방향을 고정한다. 샷건은 충전 내내, 저격은 마지막 0.3초.
            float aimLockTime = attackKind == RobotAttackKind.Shotgun ? windupTime
                : attackKind == RobotAttackKind.Sniper ? 0.3f : 0f;

            string path = RobotDataPath(fileName);
            var existing = AssetDatabase.LoadAssetAtPath<RobotData>(path);
            if (existing != null)
            {
                // 나중에 추가한 필드는 기존 에셋에 0으로 들어온다. 0일 때만 초기값을 채운다.
                SetFloatIfZero(existing, "_aimLockTime", aimLockTime);
                return;
            }

            var data = ScriptableObject.CreateInstance<RobotData>();
            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_maxHp").floatValue = maxHp;
            so.FindProperty("_attackKind").enumValueIndex = (int)attackKind;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_pelletCount").intValue = pelletCount;
            so.FindProperty("_spreadAngle").floatValue = spreadAngle;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_windupTime").floatValue = windupTime;
            so.FindProperty("_aimLockTime").floatValue = aimLockTime;
            so.FindProperty("_burstCount").intValue = burstCount;
            so.FindProperty("_burstInterval").floatValue = burstInterval;
            so.FindProperty("_restTime").floatValue = restTime;
            so.FindProperty("_patrolSpeed").floatValue = patrolSpeed;
            so.FindProperty("_chaseSpeed").floatValue = chaseSpeed;
            so.FindProperty("_turnSpeed").floatValue = 360f;
            so.FindProperty("_sweepAngle").floatValue = 45f;
            so.FindProperty("_sweepSpeed").floatValue = 30f;
            so.FindProperty("_sightDistance").floatValue = sightDistance;
            so.FindProperty("_sightAngle").floatValue = sightAngle;
            so.FindProperty("_hearingRadius").floatValue = 12f;
            so.FindProperty("_alertDuration").floatValue = 0.6f;
            so.FindProperty("_chaseTimeout").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, path);
        }

        private static string RobotDataPath(string fileName)
        {
            return $"{RobotDataFolder}/{fileName}.asset";
        }

        // ---------- 프리팹 ----------

        // 로봇 5종이 이 프리팹 하나를 쓴다. 종류별 차이는 RobotData가 정한다.
        private static void EnsureRobotPrefab(
            CombatConfig combatConfig, Sprite square, Sprite circle, int playerLayer, int wallLayer, int enemyLayer)
        {
            bool exists = File.Exists(RobotPrefabPath);
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(RobotPrefabPath) : new GameObject("Robot");

            if (!exists)
            {
                root.layer = enemyLayer;
            }

            if (EnsureComponent(root, out Rigidbody2D body))
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (EnsureComponent(root, out CircleCollider2D collider))
            {
                collider.radius = 0.4f;
                collider.sharedMaterial = EnsureNoFrictionMaterial();
            }

            // 플레이어(원)와 구분되게 사각형으로 둔다.
            if (EnsureChild(root.transform, "Body", out Transform bodyVisual))
            {
                bodyVisual.localScale = new Vector3(0.75f, 0.75f, 1f);
                bodyVisual.gameObject.AddComponent<SpriteRenderer>().sprite = square;
            }

            EnsureChild(root.transform, "FacingPivot", out Transform facingPivot);

            if (EnsureChild(facingPivot, "Barrel", out Transform barrel))
            {
                barrel.localPosition = new Vector3(0.45f, 0f, 0f);
                barrel.localScale = new Vector3(0.5f, 0.15f, 1f);
                var renderer = barrel.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = new Color(0.2f, 0.2f, 0.22f);
                renderer.sortingOrder = 1;
            }

            EnsureComponent(root, out Health health);
            SetReferenceIfEmpty(health, "_config", combatConfig);

            EnsureComponent(root, out SparkOnHit spark);
            SetReferenceIfEmpty(spark, "_health", health);
            SetReferenceIfEmpty(spark, "_sparkPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(SparkPrefabPath));

            EnsureComponent(root, out RobotMotor motor);
            SetReferenceIfEmpty(motor, "_facingPivot", facingPivot);

            // 경계 상태의 "!" 표시. 막대와 점으로 만든다.
            if (EnsureChild(root.transform, "AlertMark", out Transform alertMark))
            {
                alertMark.localPosition = new Vector3(0f, 0.95f, 0f);
                var markColor = new Color(1f, 0.9f, 0.2f);

                EnsureChild(alertMark, "Bar", out Transform bar);
                bar.localPosition = new Vector3(0f, 0.1f, 0f);
                bar.localScale = new Vector3(0.12f, 0.4f, 1f);
                var barRenderer = bar.gameObject.AddComponent<SpriteRenderer>();
                barRenderer.sprite = square;
                barRenderer.color = markColor;
                barRenderer.sortingOrder = 12;

                EnsureChild(alertMark, "Dot", out Transform dot);
                dot.localPosition = new Vector3(0f, -0.22f, 0f);
                dot.localScale = new Vector3(0.14f, 0.14f, 1f);
                var dotRenderer = dot.gameObject.AddComponent<SpriteRenderer>();
                dotRenderer.sprite = circle;
                dotRenderer.color = markColor;
                dotRenderer.sortingOrder = 12;

                alertMark.gameObject.SetActive(false);
            }

            // 공격 예고 표시: 저격 조준선과 샷건 충전 범위
            EnsureChild(root.transform, "Telegraph", out Transform telegraphTransform);

            if (EnsureChild(telegraphTransform, "AimLine", out Transform aimLine))
            {
                var line = aimLine.gameObject.AddComponent<LineRenderer>();
                line.sharedMaterial = GetSpriteMaterial();
                line.sortingOrder = 8;
                line.enabled = false;
            }

            if (EnsureChild(telegraphTransform, "ChargeFan", out Transform chargeFan))
            {
                chargeFan.gameObject.AddComponent<MeshFilter>();
                var fanRenderer = chargeFan.gameObject.AddComponent<MeshRenderer>();
                fanRenderer.sharedMaterial = GetSpriteMaterial();
                fanRenderer.sortingOrder = 8;
                fanRenderer.enabled = false;
            }

            EnsureComponent(telegraphTransform.gameObject, out AttackTelegraph telegraph);
            SetReferenceIfEmpty(telegraph, "_line", aimLine.GetComponent<LineRenderer>());
            SetReferenceIfEmpty(telegraph, "_fanFilter", chargeFan.GetComponent<MeshFilter>());
            SetReferenceIfEmpty(telegraph, "_fanRenderer", chargeFan.GetComponent<MeshRenderer>());

            if (EnsureComponent(root, out RobotAttack attack))
            {
                var attackObject = new SerializedObject(attack);
                attackObject.FindProperty("_shotMask").intValue = (1 << wallLayer) | (1 << playerLayer);
                attackObject.FindProperty("_wallMask").intValue = 1 << wallLayer;
                attackObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(attack, "_tracerPrefab", AssetDatabase.LoadAssetAtPath<BulletTracer>(RobotTracerPrefabPath));
            SetReferenceIfEmpty(attack, "_swingArcPrefab", AssetDatabase.LoadAssetAtPath<SwingArc>(RobotSwingArcPrefabPath));
            SetReferenceIfEmpty(attack, "_telegraph", telegraph);

            EnsureComponent(root, out Robot robot);

            // 벽 레이어는 나중에 추가한 칸이라 컴포넌트가 이미 있어도 비어 있으면 채운다.
            var robotObject = new SerializedObject(robot);
            SerializedProperty robotWallMask = robotObject.FindProperty("_wallMask");
            if (robotWallMask.intValue == 0)
            {
                robotWallMask.intValue = 1 << wallLayer;
                robotObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(robot, "_attack", attack);
            SetReferenceIfEmpty(robot, "_alertMark", alertMark.gameObject);
            SetReferenceIfEmpty(robot, "_health", health);
            SetReferenceIfEmpty(robot, "_motor", motor);
            SetReferenceIfEmpty(robot, "_body", bodyVisual.GetComponent<SpriteRenderer>());

            PrefabUtility.SaveAsPrefabAsset(root, RobotPrefabPath);

            if (exists)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void EnsureTracerPrefab(string path, Color color)
        {
            if (File.Exists(path))
            {
                return;
            }

            var root = new GameObject("BulletTracer");
            var line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.03f;
            line.endWidth = 0.05f;
            line.numCapVertices = 2;

            // 총구 쪽은 옅게, 맞은 쪽은 진하게 해서 날아간 방향이 보이게 한다.
            line.startColor = new Color(color.r, color.g, color.b, 0.15f);
            line.endColor = new Color(color.r, color.g, color.b, 0.9f);
            line.sharedMaterial = GetSpriteMaterial();
            line.sortingOrder = 9;
            root.AddComponent<BulletTracer>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsureSwingArcPrefab(string path, Color color)
        {
            if (File.Exists(path))
            {
                return;
            }

            var root = new GameObject("SwingArc");
            root.AddComponent<MeshFilter>();
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetSpriteMaterial();
            renderer.sortingOrder = 9;
            var arc = new SerializedObject(root.AddComponent<SwingArc>());
            arc.FindProperty("_color").colorValue = color;
            arc.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // 선과 파티클의 기본 재질은 2D URP에서 분홍색으로 나오므로 스프라이트 기본 재질을 쓴다.
        private static Material GetSpriteMaterial()
        {
            var probe = new GameObject("MaterialProbe");
            Material material = probe.AddComponent<SpriteRenderer>().sharedMaterial;
            Object.DestroyImmediate(probe);
            return material;
        }

        private static void EnsureBloodDecalPrefab(Sprite circle)
        {
            if (File.Exists(BloodDecalPrefabPath))
            {
                return;
            }

            var root = new GameObject("BloodDecal");
            root.transform.localScale = new Vector3(0.6f, 0.45f, 1f);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = new Color(0.55f, 0.05f, 0.08f);

            // 바닥과 벽 위, 캐릭터 아래
            renderer.sortingOrder = -1;
            PrefabUtility.SaveAsPrefabAsset(root, BloodDecalPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureSparkPrefab()
        {
            if (File.Exists(SparkPrefabPath))
            {
                return;
            }

            var root = new GameObject("Spark");
            var particles = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new Color(1f, 0.8f, 0.2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 다 튀고 나면 스스로 사라진다.
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;

            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = GetSpriteMaterial();
            particleRenderer.sortingOrder = 10;

            PrefabUtility.SaveAsPrefabAsset(root, SparkPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureHitMarkerPrefab(Sprite circle)
        {
            if (File.Exists(HitMarkerPrefabPath))
            {
                return;
            }

            var root = new GameObject("HitMarker");
            root.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = new Color(1f, 0.9f, 0.3f);
            renderer.sortingOrder = 10;
            root.AddComponent<TimedDestroy>();
            PrefabUtility.SaveAsPrefabAsset(root, HitMarkerPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsurePlayerPrefab(
            PlayerData playerData, WeaponData weapon, CombatConfig combatConfig, Sprite square, Sprite circle,
            int playerLayer, int wallLayer, int enemyLayer)
        {
            bool exists = File.Exists(PlayerPrefabPath);
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(PlayerPrefabPath) : new GameObject("Player");

            if (!exists)
            {
                root.layer = playerLayer;
            }

            if (EnsureComponent(root, out Rigidbody2D body))
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            if (EnsureComponent(root, out CircleCollider2D collider))
            {
                collider.radius = 0.4f;
            }

            // 기본 마찰이 있으면 벽에 비스듬히 붙어 움직일 때 속도가 줄어든다.
            if (collider.sharedMaterial == null)
            {
                collider.sharedMaterial = EnsureNoFrictionMaterial();
            }

            if (EnsureChild(root.transform, "Body", out Transform bodyVisual))
            {
                bodyVisual.localScale = new Vector3(0.8f, 0.8f, 1f);
                var renderer = bodyVisual.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = circle;
                renderer.color = new Color(0.3f, 0.75f, 1f);
            }

            EnsureChild(root.transform, "AimPivot", out Transform aimPivot);

            if (EnsureChild(aimPivot, "Gun", out Transform gun))
            {
                gun.localPosition = new Vector3(0.45f, 0f, 0f);
                gun.localScale = new Vector3(0.5f, 0.15f, 1f);
                var renderer = gun.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = new Color(0.9f, 0.9f, 0.9f);
                renderer.sortingOrder = 1;
            }

            EnsureComponent(root, out PlayerInputReader input);
            EnsureComponent(root, out PlayerController controller);
            EnsureComponent(root, out PlayerRoll roll);

            SetReferenceIfEmpty(input, "_actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath));
            SetReferenceIfEmpty(controller, "_data", playerData);
            SetReferenceIfEmpty(controller, "_input", input);
            SetReferenceIfEmpty(controller, "_aimPivot", aimPivot);
            SetReferenceIfEmpty(controller, "_roll", roll);
            SetReferenceIfEmpty(roll, "_data", playerData);
            SetReferenceIfEmpty(roll, "_input", input);
            SetReferenceIfEmpty(roll, "_controller", controller);

            EnsureComponent(root, out Health health);
            SetReferenceIfEmpty(health, "_config", combatConfig);
            SetReferenceIfEmpty(controller, "_health", health);
            SetReferenceIfEmpty(roll, "_health", health);

            EnsureComponent(root, out HitFlash flash);
            SetReferenceIfEmpty(flash, "_health", health);
            SetReferenceIfEmpty(flash, "_renderer", bodyVisual.GetComponent<SpriteRenderer>());
            SetReferenceIfEmpty(flash, "_data", playerData);

            EnsureComponent(root, out BloodSplatter blood);
            SetReferenceIfEmpty(blood, "_health", health);
            SetReferenceIfEmpty(blood, "_data", playerData);
            SetReferenceIfEmpty(blood, "_decalPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(BloodDecalPrefabPath));

            // 총구 위치. 총 막대의 끝.
            if (EnsureChild(aimPivot, "Muzzle", out Transform muzzle))
            {
                muzzle.localPosition = new Vector3(0.7f, 0f, 0f);
            }

            if (EnsureComponent(root, out PlayerShooter shooter))
            {
                var shooterObject = new SerializedObject(shooter);
                shooterObject.FindProperty("_hitMask").intValue = (1 << wallLayer) | (1 << enemyLayer);
                shooterObject.FindProperty("_wallMask").intValue = 1 << wallLayer;
                shooterObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(shooter, "_weapon", weapon);
            SetReferenceIfEmpty(shooter, "_input", input);
            SetReferenceIfEmpty(shooter, "_controller", controller);
            SetReferenceIfEmpty(shooter, "_roll", roll);
            SetReferenceIfEmpty(shooter, "_muzzle", muzzle);
            SetReferenceIfEmpty(shooter, "_hitMarkerPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(HitMarkerPrefabPath));
            SetReferenceIfEmpty(shooter, "_tracerPrefab", AssetDatabase.LoadAssetAtPath<BulletTracer>(TracerPrefabPath));
            SetReferenceIfEmpty(shooter, "_swingArcPrefab", AssetDatabase.LoadAssetAtPath<SwingArc>(SwingArcPrefabPath));
            SetReferenceIfEmpty(controller, "_shooter", shooter);

            // 저격총 조준 원. 마우스 위치에 그리므로 플레이어를 따라 돌지 않게 뿌리 바로 아래에 둔다.
            if (EnsureChild(root.transform, "AimReticle", out Transform reticleTransform))
            {
                var line = reticleTransform.gameObject.AddComponent<LineRenderer>();
                line.startWidth = 0.04f;
                line.endWidth = 0.04f;
                line.startColor = new Color(1f, 0.9f, 0.3f);
                line.endColor = new Color(1f, 0.9f, 0.3f);
                line.sharedMaterial = GetSpriteMaterial();
                line.sortingOrder = 11;
            }

            EnsureComponent(reticleTransform.gameObject, out AimReticle reticle);
            SetReferenceIfEmpty(reticle, "_shooter", shooter);
            SetReferenceIfEmpty(reticle, "_input", input);
            SetReferenceIfEmpty(reticle, "_muzzle", muzzle);

            // 가방이 생기기 전까지 쓰는 임시 예비탄. 탄창 2개분.
            var reserveObject = new SerializedObject(shooter);
            SerializedProperty reserveAmmo = reserveObject.FindProperty("_reserveAmmo");
            if (reserveAmmo.intValue == 0)
            {
                reserveAmmo.intValue = weapon.MagazineSize * 2;
                reserveObject.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);

            if (exists)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- 씬 ----------

        private static void EnsureScene(int wallLayer)
        {
            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 씬을 열면 Unity가 참조 없는 에셋을 메모리에서 내린다.
            // 그 전에 잡아 둔 타일 참조로 칠하면 빈 타일이 되므로 씬을 연 뒤에 다시 불러온다.
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var wallTile = AssetDatabase.LoadAssetAtPath<Tile>(WallTilePath);
            var floorTile = AssetDatabase.LoadAssetAtPath<Tile>(FloorTilePath);

            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(StationLayout.SpawnX, StationLayout.SpawnY, -10f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
                cameraObject.AddComponent<AudioListener>();
            }

            EnsureComponent(cameraObject, out CameraFollow follow);
            EnsureComponent(cameraObject, out CameraShake shake);
            SetReferenceIfEmpty(shake, "_config", AssetDatabase.LoadAssetAtPath<CombatConfig>(CombatConfigPath));
            SetReferenceIfEmpty(follow, "_shake", shake);

            if (GameObject.Find("Global Light 2D") == null)
            {
                // 2D URP는 조명이 없으면 스프라이트가 검게 나온다.
                var lightObject = new GameObject("Global Light 2D");
                var light = lightObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
            }

            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
            {
                BuildMap(wallTile, floorTile, wallLayer);
            }
            else if (IsMapEmpty(grid))
            {
                // 타일이 하나도 없으면 사용자가 그린 맵이 아니라 생성이 실패한 것이므로 다시 만든다.
                Object.DestroyImmediate(grid);
                BuildMap(wallTile, floorTile, wallLayer);
            }

            EnsureWallColliders();
            EnsureRobotZones(wallLayer);

            // 사격 확인용 표적은 로봇으로 대체했다. 예전에 만든 씬에 남아 있으면 지운다.
            GameObject oldTargets = GameObject.Find("TestTargets");
            if (oldTargets != null)
            {
                Object.DestroyImmediate(oldTargets);
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = new Vector3(StationLayout.SpawnX, StationLayout.SpawnY, 0f);
            }

            SetReferenceIfEmpty(follow, "_target", player.transform);

            foreach (RobotZone zone in Object.FindObjectsByType<RobotZone>(FindObjectsSortMode.None))
            {
                SetReferenceIfEmpty(zone, "_player", player.GetComponent<Health>());
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
        }

        // 로봇 초기 배치 (기획서 9.1: 화물칸 2~3기, 연구 구역 4~5기).
        // 처음 한 번만 만든다. 이후에는 Scene 뷰에서 웨이포인트를 옮기고 RobotZone의 목록을 고친다.
        private static void EnsureRobotZones(int wallLayer)
        {
            GameObject pathfinding = GameObject.Find("Pathfinding");
            if (pathfinding == null)
            {
                pathfinding = new GameObject("Pathfinding");
            }

            if (EnsureComponent(pathfinding, out WallGrid grid))
            {
                var gridObject = new SerializedObject(grid);
                gridObject.FindProperty("_wallMask").intValue = 1 << wallLayer;
                gridObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(grid, "_walls", GameObject.Find("Grid").transform.Find("Walls").GetComponent<Tilemap>());

            if (GameObject.Find("Robots") != null)
            {
                return;
            }

            var parent = new GameObject("Robots");

            BuildRobotZone(parent.transform, "CargoZone", grid, new[]
            {
                ("MeleeRobot", new[] { new Vector2(10.5f, 10.5f), new Vector2(17.5f, 10.5f), new Vector2(17.5f, 15.5f) }),
                ("PistolRobot", new[] { new Vector2(23.5f, 14.5f), new Vector2(28.5f, 14.5f), new Vector2(28.5f, 21.5f) }),
                ("PistolRobot", new[] { new Vector2(42.5f, 10.5f), new Vector2(49.5f, 10.5f), new Vector2(49.5f, 16.5f) }),
            });

            // 저격 로봇은 움직이지 않는다. 두 번째 점은 훑는 방향의 중심만 정한다.
            BuildRobotZone(parent.transform, "ResearchZone", grid, new[]
            {
                ("MeleeRobot", new[] { new Vector2(15.5f, 27.5f), new Vector2(26.5f, 27.5f), new Vector2(26.5f, 31.5f) }),
                ("PistolRobot", new[] { new Vector2(33.5f, 27.5f), new Vector2(44.5f, 27.5f), new Vector2(44.5f, 31.5f) }),
                ("RifleRobot", new[] { new Vector2(25.5f, 36.5f), new Vector2(14.5f, 36.5f), new Vector2(14.5f, 33.5f) }),
                ("ShotgunRobot", new[] { new Vector2(33.5f, 36.5f), new Vector2(44.5f, 36.5f), new Vector2(44.5f, 33.5f) }),
                ("SniperRobot", new[] { new Vector2(56.5f, 37.5f), new Vector2(44.5f, 30.5f) }),
            });
        }

        private static void BuildRobotZone(
            Transform parent, string zoneName, WallGrid grid, (string dataName, Vector2[] waypoints)[] robots)
        {
            var zoneObject = new GameObject(zoneName);
            zoneObject.transform.SetParent(parent, false);
            var zone = zoneObject.AddComponent<RobotZone>();

            var so = new SerializedObject(zone);
            so.FindProperty("_robotPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Robot>(RobotPrefabPath);
            so.FindProperty("_grid").objectReferenceValue = grid;
            SerializedProperty spawns = so.FindProperty("_spawns");
            spawns.arraySize = robots.Length;

            for (int i = 0; i < robots.Length; i++)
            {
                var route = new GameObject($"{robots[i].dataName} {i + 1}");
                route.transform.SetParent(zoneObject.transform, false);

                SerializedProperty spawn = spawns.GetArrayElementAtIndex(i);
                spawn.FindPropertyRelative("_data").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(robots[i].dataName));

                SerializedProperty waypoints = spawn.FindPropertyRelative("_waypoints");
                waypoints.arraySize = robots[i].waypoints.Length;
                for (int j = 0; j < robots[i].waypoints.Length; j++)
                {
                    var waypoint = new GameObject($"Waypoint {j + 1}");
                    waypoint.transform.SetParent(route.transform, false);
                    waypoint.transform.position = robots[i].waypoints[j];
                    waypoints.GetArrayElementAtIndex(j).objectReferenceValue = waypoint.transform;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildMap(Tile wallTile, Tile floorTile, int wallLayer)
        {
            var gridObject = new GameObject("Grid");
            gridObject.AddComponent<Grid>();

            var floorObject = new GameObject("Floor");
            floorObject.transform.SetParent(gridObject.transform, false);
            var floor = floorObject.AddComponent<Tilemap>();
            floorObject.AddComponent<TilemapRenderer>().sortingOrder = -10;

            var wallObject = new GameObject("Walls");
            wallObject.transform.SetParent(gridObject.transform, false);
            wallObject.layer = wallLayer;
            var walls = wallObject.AddComponent<Tilemap>();
            wallObject.AddComponent<TilemapRenderer>().sortingOrder = -5;

            // 타일마다 따로 충돌체를 두면 타일 경계에 걸리므로 하나로 합친다.
            var tilemapCollider = wallObject.AddComponent<TilemapCollider2D>();
            wallObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var composite = wallObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            bool[,] layout = StationLayout.BuildWalls();
            for (int x = 0; x < StationLayout.Width; x++)
            {
                for (int y = 0; y < StationLayout.Height; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    floor.SetTile(cell, floorTile);
                    if (layout[x, y])
                    {
                        walls.SetTile(cell, wallTile);
                    }
                }
            }

            if (floor.GetUsedTilesCount() == 0 || walls.GetUsedTilesCount() == 0)
            {
                throw new System.InvalidOperationException("타일맵에 타일이 칠해지지 않았다.");
            }

            Debug.Log($"[ProjectSetup] 맵 생성: 바닥 {floor.cellBounds.size.x}×{floor.cellBounds.size.y}칸");
        }

        // 에디터 밖(배치 모드)에서 칠한 타일은 충돌체에 바로 반영되지 않아
        // 합쳐진 충돌체가 빈 채로 저장된다. 그러면 벽을 통과하므로 저장 전에 직접 만든다.
        private static void EnsureWallColliders()
        {
            foreach (CompositeCollider2D composite in Object.FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
            {
                var tilemapCollider = composite.GetComponent<TilemapCollider2D>();
                if (tilemapCollider == null)
                {
                    continue;
                }

                // 씬을 막 연 상태에서는 타일 충돌체가 아직 도형을 만들지 않았다.
                // 합치기를 잠시 끄고 타일을 전부 다시 읽혀 도형을 만든 뒤 다시 합친다.
                tilemapCollider.compositeOperation = Collider2D.CompositeOperation.None;
                composite.GetComponent<Tilemap>().RefreshAllTiles();
                tilemapCollider.ProcessTilemapChanges();
                int shapeCount = tilemapCollider.shapeCount;

                tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
                composite.GenerateGeometry();
                EditorUtility.SetDirty(tilemapCollider);
                EditorUtility.SetDirty(composite);

                if (composite.pathCount == 0)
                {
                    throw new System.InvalidOperationException(
                        $"'{composite.name}' 타일맵의 벽 충돌체가 만들어지지 않았다. (타일 도형 {shapeCount}개, 합친 경로 0개)");
                }

                Debug.Log($"[ProjectSetup] 벽 충돌체: {composite.name} 타일 도형 {shapeCount}개, 합친 경로 {composite.pathCount}개");
            }
        }

        private static bool IsMapEmpty(GameObject grid)
        {
            foreach (Tilemap tilemap in grid.GetComponentsInChildren<Tilemap>())
            {
                if (tilemap.GetUsedTilesCount() > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene buildScene in scenes)
            {
                if (buildScene.path == ScenePath)
                {
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            updated[0] = new EditorBuildSettingsScene(ScenePath, true);
            scenes.CopyTo(updated, 1);
            EditorBuildSettings.scenes = updated;
        }

        // ---------- 공통 ----------

        // 새로 붙였을 때만 true를 돌려준다. 초기값은 그때만 넣는다.
        private static bool EnsureComponent<T>(GameObject target, out T component) where T : Component
        {
            component = target.GetComponent<T>();
            if (component != null)
            {
                return false;
            }

            component = target.AddComponent<T>();
            return true;
        }

        private static bool EnsureChild(Transform parent, string childName, out Transform child)
        {
            child = parent.Find(childName);
            if (child != null)
            {
                return false;
            }

            child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            child.gameObject.layer = parent.gameObject.layer;
            return true;
        }

        private static void SetFloatIfZero(Object target, string propertyName, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property.floatValue == 0f)
            {
                property.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetReferenceIfEmpty(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
