using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string StoragePrefabPath = "Assets/Prefabs/Storage.prefab";
        private const string ItemDatabasePath = "Assets/Data/ItemDatabase.asset";

        // 그림은 Kenney의 CC0 에셋 팩에서 골라 저장소에 넣어 둔 것이다 (폴더마다 License.txt).
        private const string AstronautFolder = "Assets/Art/Kenney/SpaceShooterExtension";
        private const string WeaponArtFolder = "Assets/Art/Kenney/SpaceShooterRedux";
        private const string TileArtFolder = "Assets/Art/Kenney/TopDownShooter";
        private const string FlashShaderName = "EscapeFromSpace/SpriteFlash";
        private const string FlashMaterialPath = "Assets/Art/Shaders/SpriteFlash.mat";
        private const string MeshShaderName = "EscapeFromSpace/VertexColor";
        private const string MeshMaterialPath = "Assets/Art/Shaders/VertexColor.mat";
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
        private const string ItemFolder = "Assets/Data/Items";
        private const string LootFolder = "Assets/Data/Loot";
        private const string ContainerPrefabPath = "Assets/Prefabs/Container.prefab";
        private const string ResearchContainerPrefabPath = "Assets/Prefabs/ResearchContainer.prefab";
        private const string RobotWreckPrefabPath = "Assets/Prefabs/RobotWreck.prefab";
        private const string DroppedItemPrefabPath = "Assets/Prefabs/DroppedItem.prefab";
        private const string RaidConfigPath = "Assets/Data/RaidConfig.asset";
        private const string CombatConfigPath = "Assets/Data/CombatConfig.asset";
        private const string HitMarkerPrefabPath = "Assets/Prefabs/HitMarker.prefab";
        private const string BloodDecalPrefabPath = "Assets/Prefabs/BloodDecal.prefab";
        private const string SparkPrefabPath = "Assets/Prefabs/Spark.prefab";

        private static readonly string[] Folders =
        {
            "Assets/Art/Placeholder", "Assets/Data", "Assets/Data/Weapons", RobotDataFolder, ItemFolder, LootFolder, "Assets/Prefabs", "Assets/Tiles", "Assets/Physics",
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
            EnsureWeaponArt();
            EnsureItems();
            EnsureItemDatabase();
            EnsureLootTables();
            CombatConfig combatConfig = EnsureCombatConfig();
            EnsureRaidConfig();
            EnsureHitMarkerPrefab(circle);
            EnsureTracerPrefab(TracerPrefabPath, new Color(1f, 0.95f, 0.6f));
            EnsureSwingArcPrefab(SwingArcPrefabPath, new Color(0.55f, 0.9f, 1f, 0.6f));
            EnsureBloodDecalPrefab(circle);
            EnsureSparkPrefab();
            EnsureRobots();
            foreach (string robotName in new[] { "MeleeRobot", "PistolRobot", "RifleRobot", "SniperRobot", "ShotgunRobot" })
            {
                SetReferenceIfEmpty(
                    AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(robotName)), "_lootTable", LoadLoot(robotName + "Loot"));
            }

            // 컨테이너는 이동·사격·시야를 모두 막도록 벽 레이어에 둔다.
            EnsureLootablePrefab(ContainerPrefabPath, "Container", square, wallLayer, new Color(0.6f, 0.45f, 0.25f), 0.9f, 0,
                true, 3, 2, LoadLoot("NormalContainerLoot"), 1f, 0f, false);
            EnsureLootablePrefab(ResearchContainerPrefabPath, "ResearchContainer", square, wallLayer, new Color(0.25f, 0.6f, 0.6f),
                0.9f, 0, true, 3, 3, LoadLoot("ResearchContainerLoot"), 1f, 0f, false);
            EnsureLootablePrefab(RobotWreckPrefabPath, "RobotWreck", square, 0, new Color(0.25f, 0.25f, 0.28f), 0.6f, -1,
                false, 3, 1, null, 1f, 0f, false);

            // 바닥에 버린 아이템. 다시 주울 수 있고 1분 뒤 사라진다.
            EnsureLootablePrefab(DroppedItemPrefabPath, "DroppedItem", square, 0, Color.white, 0.35f, -1,
                false, 1, 1, null, 0f, 60f, true);

            // 대기실 창고 5×10 (기획서 7장). 누르면 바로 열리고 내용은 저장 파일에서 채운다.
            EnsureLootablePrefab(StoragePrefabPath, "Storage", square, wallLayer, new Color(0.35f, 0.45f, 0.7f), 1.6f, 0,
                true, 5, 10, null, 0f, 0f, false);
            EnsureTracerPrefab(RobotTracerPrefabPath, new Color(1f, 0.3f, 0.25f));
            EnsureSwingArcPrefab(RobotSwingArcPrefabPath, new Color(1f, 0.3f, 0.25f, 0.6f));
            EnsureRobotPrefab(combatConfig, square, circle, playerLayer, wallLayer, enemyLayer);
            EnsurePlayerPrefab(playerData, combatConfig, square, circle, playerLayer, wallLayer, enemyLayer);
            EnsureArt(square);
            FixMeshMaterials();
            AssetDatabase.SaveAssets();

            // 레이드 씬을 마지막에 열어 둔다. 메뉴를 돌린 뒤 에디터에 남는 씬이다.
            EnsureLobbyScene(wallLayer);
            EnsureScene(wallLayer);
            EnsureBuildScenes();

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
                SetFloatIfZero(data, "_maxWeight", 20f);
                SetFloatIfZero(data, "_slowStartRatio", 0.5f);
                SetFloatIfZero(data, "_slowAtStart", 0.2f);
                SetFloatIfZero(data, "_slowStepRatio", 0.1f);
                SetFloatIfZero(data, "_slowPerStep", 0.1f);
                SetFloatIfZero(data, "_minSpeedRatio", 0.2f);
                SetFloatIfZero(data, "_overloadRatio", 1.5f);
                SetFloatIfZero(data, "_overloadDamagePerSecond", 0.5f);
                return data;
            }

            // 기획서 초기값. 에셋을 처음 만들 때만 넣는다.
            data = ScriptableObject.CreateInstance<PlayerData>();
            var so = new SerializedObject(data);
            so.FindProperty("_moveSpeed").floatValue = 5f;
            so.FindProperty("_reloadMoveSpeedMultiplier").floatValue = 0.7f;
            so.FindProperty("_maxWeight").floatValue = 20f;
            so.FindProperty("_slowStartRatio").floatValue = 0.5f;
            so.FindProperty("_slowAtStart").floatValue = 0.2f;
            so.FindProperty("_slowStepRatio").floatValue = 0.1f;
            so.FindProperty("_slowPerStep").floatValue = 0.1f;
            so.FindProperty("_minSpeedRatio").floatValue = 0.2f;
            so.FindProperty("_overloadRatio").floatValue = 1.5f;
            so.FindProperty("_overloadDamagePerSecond").floatValue = 0.5f;
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

        // 기획서 7장의 초기값. 에셋을 처음 만들 때만 넣는다.
        private static void EnsureRaidConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath) != null)
            {
                return;
            }

            var config = ScriptableObject.CreateInstance<RaidConfig>();
            var so = new SerializedObject(config);
            so.FindProperty("_baseOxygenSeconds").floatValue = 480f;
            so.FindProperty("_suffocationDamagePerSecond").floatValue = 5f;
            so.FindProperty("_oxygenWarningSeconds").floatValue = 60f;
            so.FindProperty("_escapeHoldTime").floatValue = 5f;

            // 포드는 시작 1분 뒤에 열린다 (사용자 결정). 지금은 테스트 중이라 항상 열어 둔다.
            so.FindProperty("_podOpenDelay").floatValue = 60f;
            so.FindProperty("_podsAlwaysOpen").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(config, RaidConfigPath);
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

        // 무기별 손에 든 모습 (2026-11-16 디자인 단계). 그림 칸이 비어 있을 때만 채운다.
        // 길이는 무기 사이의 크기 차이가 보이도록 정한 초기값이고 Inspector에서 조정한다.
        private static void EnsureWeaponArt()
        {
            SetWeaponArt(LightSaberPath, "laserBlue01", 0.9f);
            SetWeaponArt(LaserPistolPath, "gun01", 0.45f);
            SetWeaponArt(PulseRiflePath, "gun04", 0.7f);
            SetWeaponArt(Nebula7Path, "gun08", 0.95f);
            SetWeaponArt(PlasmaScatterPath, "gun05", 0.65f);
        }

        private static void SetWeaponArt(string weaponPath, string spriteName, float visualLength)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            SetReferenceIfEmpty(weapon, "_sprite", EnsureArtSprite($"{WeaponArtFolder}/{spriteName}.png"));
            SetFloatIfZero(weapon, "_visualLength", visualLength);
        }

        // 저장소에 넣어 둔 그림 파일을 스프라이트로 읽히게 한다. 한 번 설정한 뒤에는 Inspector에서 바꾼 값을 건드리지 않는다.
        // pixelsPerUnit은 그림의 몇 픽셀이 1칸인지다. 타일은 칸 사이에 틈이 보이지 않게 isTile을 켠다.
        private static Sprite EnsureArtSprite(string path, float pixelsPerUnit = 100f, bool isTile = false)
        {
            const string marker = "EscapeFromSpace:configured";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.userData != marker)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.filterMode = isTile ? FilterMode.Point : FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.userData = marker;

                if (isTile)
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                }

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- 디자인 단계의 그림 ----------

        // 단색 도형을 에셋 팩의 그림으로 바꾼다 (2026-11-16 디자인 단계). 아직 단색 도형(사각형 그림)인 것만 바꾸고,
        // 그림 칸이 비어 있는 데이터만 채운다. 이미 다른 그림을 넣었으면 건드리지 않는다.
        private static void EnsureArt(Sprite square)
        {
            // 맵: 벽은 무늬가 있는 밝은 타일, 바닥은 어두운 타일. 타일 에셋을 바꾸면 칠해 둔 맵 전체가 바뀐다.
            ReplaceTileArt(WallTilePath, square, $"{TileArtFolder}/wall.png", Color.white);
            ReplaceTileArt(FloorTilePath, square, $"{TileArtFolder}/floor.png", new Color(0.62f, 0.64f, 0.72f));

            // 뒤질 수 있는 것들. 프리팹의 크기는 그대로 두고 그림이 1칸을 채우게 읽는다.
            ReplacePrefabArt(ContainerPrefabPath, square, $"{AstronautFolder}/crate_gray.png", 84f);
            ReplacePrefabArt(ResearchContainerPrefabPath, square, $"{AstronautFolder}/crate_red.png", 84f);
            ReplacePrefabArt(StoragePrefabPath, square, $"{AstronautFolder}/locker_gray.png", 168f);
            ReplacePrefabArt(RobotWreckPrefabPath, square, $"{WeaponArtFolder}/meteorGrey_med1.png", 43f);

            // 로봇 5종은 모양이 다른 보안 드론이다.
            SetRobotArt("MeleeRobot", "enemyRed4");
            SetRobotArt("PistolRobot", "enemyRed1");
            SetRobotArt("RifleRobot", "enemyRed2");
            SetRobotArt("SniperRobot", "enemyRed3");
            SetRobotArt("ShotgunRobot", "enemyRed5");

            // 가방과 HUD의 아이템 그림
            (string item, string sprite)[] icons =
            {
                ("PistolCell", "powerupYellow_bolt"), ("RifleCell", "powerupBlue_bolt"), ("SniperCell", "powerupRed_bolt"),
                ("PlasmaCapsule", "powerupGreen_bolt"), ("MediGel", "pill_green"), ("MediKit", "pill_red"), ("OxygenCan", "pill_blue"),
                ("CircuitPart", "things_silver"), ("QuantumCore", "star_gold"),
                ("LightSaberItem", "laserBlue01"), ("LaserPistolItem", "gun01"), ("PulseRifleItem", "gun04"),
                ("Nebula7Item", "gun08"), ("PlasmaScatterItem", "gun05"),
                ("Vest1", "shield_bronze"), ("Vest2", "shield_silver"), ("Vest3", "shield_gold"),
                ("Helmet1", "ufoBlue"), ("Helmet2", "ufoGreen"), ("Helmet3", "ufoRed"),
            };
            foreach ((string item, string sprite) in icons)
            {
                SetReferenceIfEmpty(LoadItem(item), "_icon", EnsureArtSprite($"{WeaponArtFolder}/{sprite}.png"));
            }
        }

        private static void ReplaceTileArt(string tilePath, Sprite placeholder, string spritePath, Color color)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile.sprite != placeholder)
            {
                return;
            }

            tile.sprite = EnsureArtSprite(spritePath, 64f, true);
            tile.color = color;
            EditorUtility.SetDirty(tile);
        }

        private static void ReplacePrefabArt(string prefabPath, Sprite placeholder, string spritePath, float pixelsPerUnit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer.sprite == placeholder)
            {
                renderer.sprite = EnsureArtSprite(spritePath, pixelsPerUnit);
                renderer.color = Color.white;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        // 씬에 직접 놓은 오브젝트(탈출 포드, 포탈)의 그림을 바꾼다. 색은 스크립트가 상태에 따라 칠하므로 그대로 둔다.
        private static void ReplaceSceneArt(SpriteRenderer renderer, Sprite placeholder, string spritePath, float pixelsPerUnit)
        {
            if (renderer != null && renderer.sprite == placeholder)
            {
                renderer.sprite = EnsureArtSprite(spritePath, pixelsPerUnit);
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void SetRobotArt(string robotName, string spriteName)
        {
            var robot = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(robotName));
            SetReferenceIfEmpty(robot, "_sprite", EnsureArtSprite($"{WeaponArtFolder}/{spriteName}.png"));
            SetFloatIfZero(robot, "_visualSize", 1f);
        }

        // 코드로 만든 메시(광선검 범위, 근접 로봇의 베기, 샷건 충전 범위)는 스프라이트 기본 재질로는 그려지지 않는다.
        // 예전에 그 재질로 만든 프리팹을 꼭짓점 색 재질로 바꾼다. 다른 재질을 넣어 두었으면 건드리지 않는다.
        private static void FixMeshMaterials()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MeshMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find(MeshShaderName));
                AssetDatabase.CreateAsset(material, MeshMaterialPath);
            }

            Material spriteMaterial = GetSpriteMaterial();
            foreach (string path in new[] { SwingArcPrefabPath, RobotSwingArcPrefabPath, RobotPrefabPath })
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.sharedMaterial == spriteMaterial)
                    {
                        renderer.sharedMaterial = material;
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 피격 번쩍임용 재질. 우주인 그림이 원래 흰색이라 색을 곱하는 기본 재질로는 번쩍임이 안 보인다.
        private static Material EnsureFlashMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find(FlashShaderName));
                AssetDatabase.CreateAsset(material, FlashMaterialPath);
            }

            return material;
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

        // ---------- 아이템과 루팅 표 ----------

        // 기획서 6.3 아이템 표와 6.5 방어구 표의 초기값. 에셋을 처음 만들 때만 넣는다.
        // 크기는 사용자 결정(2026-11-16)에 따라 모두 1×1이다. 무기·방어구의 가치는 8장의 판매가다.
        private static void EnsureItems()
        {
            var ammoColor = new Color(0.95f, 0.85f, 0.35f);
            var medicalColor = new Color(0.4f, 0.9f, 0.5f);
            var consumableColor = new Color(0.45f, 0.8f, 1f);
            var lootColor = new Color(0.8f, 0.6f, 0.9f);
            var weaponColor = new Color(0.85f, 0.85f, 0.9f);
            var vestColor = new Color(0.6f, 0.7f, 0.5f);
            var helmetColor = new Color(0.75f, 0.65f, 0.5f);

            EnsureItem("PistolCell", "권총 에너지 셀", ItemCategory.Ammo, ammoColor, 0.3f, 10, 60, 12);
            EnsureItem("RifleCell", "라이플 에너지 셀", ItemCategory.Ammo, ammoColor, 0.6f, 20, 60, 30);
            EnsureItem("SniperCell", "저격 에너지 셀", ItemCategory.Ammo, ammoColor, 0.4f, 25, 60, 5);
            EnsureItem("PlasmaCapsule", "플라즈마 캡슐", ItemCategory.Ammo, ammoColor, 0.5f, 20, 60, 6);

            EnsureItem("MediGel", "메디젤", ItemCategory.Medical, medicalColor, 0.2f, 15, 1, 1, healAmount: 20f, useTime: 2f);
            EnsureItem("MediKit", "메디킷", ItemCategory.Medical, medicalColor, 0.8f, 40, 1, 1, healAmount: 60f, useTime: 4f);
            EnsureItem("OxygenCan", "산소 캔", ItemCategory.Consumable, consumableColor, 0.5f, 25, 1, 1, oxygenSeconds: 60f);
            EnsureItem("CircuitPart", "회로 부품", ItemCategory.Loot, lootColor, 2f, 30, 1, 1);
            EnsureItem("QuantumCore", "퀀텀 코어", ItemCategory.Loot, lootColor, 1.5f, 200, 1, 1);

            EnsureWeaponItem("LightSaberItem", LightSaberPath, weaponColor, 75, null);
            EnsureWeaponItem("LaserPistolItem", LaserPistolPath, weaponColor, 50, "PistolCell");
            EnsureWeaponItem("PulseRifleItem", PulseRiflePath, weaponColor, 300, "RifleCell");
            EnsureWeaponItem("Nebula7Item", Nebula7Path, weaponColor, 450, "SniperCell");
            EnsureWeaponItem("PlasmaScatterItem", PlasmaScatterPath, weaponColor, 350, "PlasmaCapsule");

            EnsureItem("Vest1", "1단계 방탄복", ItemCategory.Vest, vestColor, 3f, 100, 1, 1, armor: 2, extraBagRows: 1);
            EnsureItem("Vest2", "2단계 방탄복", ItemCategory.Vest, vestColor, 5f, 250, 1, 1, armor: 4, extraBagRows: 2);
            EnsureItem("Vest3", "3단계 방탄복", ItemCategory.Vest, vestColor, 8f, 500, 1, 1, armor: 6, extraBagRows: 3);
            EnsureItem("Helmet1", "1단계 헬멧", ItemCategory.Helmet, helmetColor, 1f, 75, 1, 1, armor: 2, extraOxygenSeconds: 60f);
            EnsureItem("Helmet2", "2단계 헬멧", ItemCategory.Helmet, helmetColor, 1.5f, 200, 1, 1, armor: 4, extraOxygenSeconds: 120f);
            EnsureItem("Helmet3", "3단계 헬멧", ItemCategory.Helmet, helmetColor, 2f, 400, 1, 1, armor: 6, extraOxygenSeconds: 180f);
        }

        private static ItemData EnsureItem(
            string fileName, string displayName, ItemCategory category, Color color, float weight, int value, int maxStack,
            int defaultCount, WeaponData weapon = null, int armor = 0, int extraBagRows = 0, float extraOxygenSeconds = 0f,
            float healAmount = 0f, float useTime = 0f, float oxygenSeconds = 0f)
        {
            string path = $"{ItemFolder}/{fileName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<ItemData>();
            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_category").enumValueIndex = (int)category;
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_width").intValue = 1;
            so.FindProperty("_height").intValue = 1;
            so.FindProperty("_weight").floatValue = weight;
            so.FindProperty("_value").intValue = value;
            so.FindProperty("_maxStack").intValue = maxStack;
            so.FindProperty("_defaultCount").intValue = defaultCount;
            so.FindProperty("_weapon").objectReferenceValue = weapon;
            so.FindProperty("_armor").intValue = armor;
            so.FindProperty("_extraBagRows").intValue = extraBagRows;
            so.FindProperty("_extraOxygenSeconds").floatValue = extraOxygenSeconds;
            so.FindProperty("_healAmount").floatValue = healAmount;
            so.FindProperty("_useTime").floatValue = useTime;
            so.FindProperty("_oxygenSeconds").floatValue = oxygenSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        // 무기 아이템은 이름과 무게를 무기 에셋에서 가져온다. 무기가 쓰는 탄약도 여기서 이어 준다.
        private static void EnsureWeaponItem(string fileName, string weaponPath, Color color, int value, string ammoName)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            EnsureItem(fileName, weapon.DisplayName, ItemCategory.Weapon, color, weapon.Weight, value, 1, 1, weapon);

            if (ammoName != null)
            {
                SetReferenceIfEmpty(weapon, "_ammoItem", LoadItem(ammoName));
            }
        }

        // 저장 파일이 아이템을 이름으로 찾을 때 쓰는 목록. 폴더에 있는데 목록에 없는 아이템만 덧붙인다.
        private static void EnsureItemDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(database, ItemDatabasePath);
            }

            var so = new SerializedObject(database);
            SerializedProperty list = so.FindProperty("_items");
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { ItemFolder }))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                bool listed = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    listed |= list.GetArrayElementAtIndex(i).objectReferenceValue == item;
                }

                if (!listed)
                {
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemData LoadItem(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemFolder}/{fileName}.asset");
        }

        // 기획서에는 나오는 곳만 있어서 비율과 개수는 PLAN.md 4부에서 정한 초기값이다.
        // 방탄복은 연구 컨테이너에서만 나온다 (사용자 결정. 높은 단계는 나중에 추가할 네임드 로봇에서).
        private static void EnsureLootTables()
        {
            EnsureLootTable("NormalContainerLoot", false, 2, 3,
                ("PistolCell", 25f), ("RifleCell", 12f), ("PlasmaCapsule", 12f), ("MediGel", 18f), ("OxygenCan", 10f),
                ("CircuitPart", 13f), ("Helmet1", 6f));
            EnsureLootTable("ResearchContainerLoot", false, 2, 3,
                ("QuantumCore", 22f), ("SniperCell", 15f), ("MediKit", 15f), ("OxygenCan", 12f), ("Helmet2", 10f),
                ("Vest1", 6f), ("Vest2", 8f), ("LightSaberItem", 3f), ("LaserPistolItem", 3f), ("PulseRifleItem", 3f),
                ("Nebula7Item", 3f), ("PlasmaScatterItem", 3f), ("Helmet3", 2f), ("Vest3", 1f));

            // 로봇 잔해는 줄마다 확률(%)을 따로 굴린다. 자기 무기의 탄약은 항상, 무기는 가끔.
            EnsureLootTable("MeleeRobotLoot", true, 0, 0, ("LightSaberItem", 10f));
            EnsureLootTable("PistolRobotLoot", true, 0, 0, ("PistolCell", 100f), ("LaserPistolItem", 10f));
            EnsureLootTable("RifleRobotLoot", true, 0, 0, ("RifleCell", 100f), ("PulseRifleItem", 10f));
            EnsureLootTable("SniperRobotLoot", true, 0, 0, ("SniperCell", 100f), ("Nebula7Item", 10f), ("Helmet3", 5f));
            EnsureLootTable("ShotgunRobotLoot", true, 0, 0, ("PlasmaCapsule", 100f), ("PlasmaScatterItem", 10f));
        }

        private static void EnsureLootTable(
            string fileName, bool rollEachEntry, int minCount, int maxCount, params (string item, float weight)[] entries)
        {
            string path = $"{LootFolder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<LootTable>(path) != null)
            {
                return;
            }

            var table = ScriptableObject.CreateInstance<LootTable>();
            var so = new SerializedObject(table);
            so.FindProperty("_rollEachEntry").boolValue = rollEachEntry;
            so.FindProperty("_minCount").intValue = minCount;
            so.FindProperty("_maxCount").intValue = maxCount;
            SerializedProperty list = so.FindProperty("_entries");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_item").objectReferenceValue = LoadItem(entries[i].item);
                entry.FindPropertyRelative("_weight").floatValue = entries[i].weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(table, path);
        }

        private static LootTable LoadLoot(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<LootTable>($"{LootFolder}/{fileName}.asset");
        }

        // ---------- 프리팹 ----------

        // 뒤질 수 있는 것(컨테이너, 로봇 잔해, 버린 아이템)의 프리팹. 처음 만들 때만 값을 넣는다.
        private static void EnsureLootablePrefab(
            string path, string objectName, Sprite sprite, int layer, Color color, float scale, int sortingOrder, bool blocks,
            int width, int height, LootTable table, float openTime, float lifetime, bool destroyWhenEmpty)
        {
            if (File.Exists(path))
            {
                return;
            }

            var root = new GameObject(objectName);
            root.layer = layer;
            root.transform.localScale = new Vector3(scale, scale, 1f);

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (blocks)
            {
                root.AddComponent<BoxCollider2D>();
            }

            var lootable = new SerializedObject(root.AddComponent<Lootable>());
            lootable.FindProperty("_width").intValue = width;
            lootable.FindProperty("_height").intValue = height;
            lootable.FindProperty("_table").objectReferenceValue = table;
            lootable.FindProperty("_openTime").floatValue = openTime;
            lootable.FindProperty("_lifetime").floatValue = lifetime;
            lootable.FindProperty("_destroyWhenEmpty").boolValue = destroyWhenEmpty;
            lootable.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

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

            EnsureChild(root.transform, "FacingPivot", out Transform facingPivot);

            // 몸은 바라보는 방향으로 함께 돌아야 해서 FacingPivot 아래에 둔다. 예전 프리팹은 뿌리 바로 아래에 있었으므로 옮긴다.
            // 그림이 없는 로봇은 플레이어와 구분되게 사각형으로 보인다.
            Transform bodyVisual = facingPivot.Find("Body");
            if (bodyVisual == null)
            {
                bodyVisual = root.transform.Find("Body");
                if (bodyVisual == null)
                {
                    bodyVisual = new GameObject("Body").transform;
                    bodyVisual.gameObject.layer = root.layer;
                    bodyVisual.localScale = new Vector3(0.75f, 0.75f, 1f);
                    bodyVisual.gameObject.AddComponent<SpriteRenderer>().sprite = square;
                }

                bodyVisual.SetParent(facingPivot, false);
            }

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

            SetReferenceIfEmpty(robot, "_wreckPrefab", AssetDatabase.LoadAssetAtPath<Lootable>(RobotWreckPrefabPath));
            SetReferenceIfEmpty(robot, "_attack", attack);
            SetReferenceIfEmpty(robot, "_alertMark", alertMark.gameObject);
            SetReferenceIfEmpty(robot, "_health", health);
            SetReferenceIfEmpty(robot, "_motor", motor);
            SetReferenceIfEmpty(robot, "_body", bodyVisual.GetComponent<SpriteRenderer>());
            SetReferenceIfEmpty(robot, "_barrel", barrel.GetComponent<SpriteRenderer>());

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
            PlayerData playerData, CombatConfig combatConfig, Sprite square, Sprite circle,
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

            // 가방: 장비 칸, 가방, 보관함. 시작 장비는 레이드 씬을 바로 Play할 때만 쓰는 테스트용 기본 세트다.
            if (EnsureComponent(root, out PlayerInventory inventory))
            {
                var inventoryObject = new SerializedObject(inventory);
                inventoryObject.FindProperty("_startingWeapon").objectReferenceValue = LoadItem("LaserPistolItem");
                SerializedProperty starting = inventoryObject.FindProperty("_startingItems");
                starting.arraySize = 2;
                starting.GetArrayElementAtIndex(0).FindPropertyRelative("_item").objectReferenceValue = LoadItem("PistolCell");
                starting.GetArrayElementAtIndex(0).FindPropertyRelative("_count").intValue = 24;
                starting.GetArrayElementAtIndex(1).FindPropertyRelative("_item").objectReferenceValue = LoadItem("LightSaberItem");
                starting.GetArrayElementAtIndex(1).FindPropertyRelative("_count").intValue = 1;
                inventoryObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(inventory, "_health", health);

            EnsureComponent(root, out PlayerInteractor interactor);
            SetReferenceIfEmpty(interactor, "_input", input);
            SetReferenceIfEmpty(interactor, "_controller", controller);
            SetReferenceIfEmpty(interactor, "_health", health);
            SetReferenceIfEmpty(interactor, "_droppedItemPrefab", AssetDatabase.LoadAssetAtPath<Lootable>(DroppedItemPrefabPath));

            SetReferenceIfEmpty(shooter, "_inventory", inventory);
            SetReferenceIfEmpty(controller, "_inventory", inventory);

            // 산소와 도구칸
            EnsureComponent(root, out PlayerOxygen oxygen);
            SetReferenceIfEmpty(oxygen, "_config", AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath));
            SetReferenceIfEmpty(oxygen, "_health", health);
            SetReferenceIfEmpty(oxygen, "_inventory", inventory);

            if (EnsureComponent(root, out PlayerItemUser itemUser))
            {
                // 쓸 수 있는 아이템이 세 가지라 도구칸 1, 2, 3에 하나씩 놓는다.
                var userObject = new SerializedObject(itemUser);
                SerializedProperty slots = userObject.FindProperty("_slots");
                slots.arraySize = 3;
                slots.GetArrayElementAtIndex(0).objectReferenceValue = LoadItem("MediGel");
                slots.GetArrayElementAtIndex(1).objectReferenceValue = LoadItem("MediKit");
                slots.GetArrayElementAtIndex(2).objectReferenceValue = LoadItem("OxygenCan");
                userObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(itemUser, "_input", input);
            SetReferenceIfEmpty(itemUser, "_controller", controller);
            SetReferenceIfEmpty(itemUser, "_roll", roll);
            SetReferenceIfEmpty(itemUser, "_inventory", inventory);
            SetReferenceIfEmpty(itemUser, "_oxygen", oxygen);
            SetReferenceIfEmpty(itemUser, "_health", health);
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

            // 겉모습: 단색 도형(원 + 막대)을 우주인 그림과 무기 그림으로 바꾼다. 처음 붙일 때 한 번만 바꾼다.
            var bodyRenderer = bodyVisual.GetComponent<SpriteRenderer>();
            var gunRenderer = aimPivot.Find("Gun").GetComponent<SpriteRenderer>();
            if (EnsureComponent(root, out PlayerVisual visual))
            {
                bodyVisual.localScale = Vector3.one;
                bodyRenderer.sprite = EnsureArtSprite($"{AstronautFolder}/astronaut_stand.png");
                bodyRenderer.color = Color.white;

                // 무기 그림은 위를 보고 있어서 조준 방향(오른쪽)으로 눕힌다. 크기와 위치는 PlayerVisual이 무기마다 정한다.
                Transform gunTransform = gunRenderer.transform;
                gunTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                gunTransform.localScale = Vector3.one;
                gunRenderer.color = Color.white;

                var visualSo = new SerializedObject(visual);
                visualSo.FindProperty("_unarmedSprite").objectReferenceValue = EnsureArtSprite($"{AstronautFolder}/astronaut_stand.png");
                visualSo.FindProperty("_armedSprite").objectReferenceValue = EnsureArtSprite($"{AstronautFolder}/astronaut_hold.png");
                visualSo.ApplyModifiedPropertiesWithoutUndo();

                SetReferenceIfEmpty(flash, "_flashMaterial", EnsureFlashMaterial());
            }

            SetReferenceIfEmpty(visual, "_controller", controller);
            SetReferenceIfEmpty(visual, "_input", input);
            SetReferenceIfEmpty(visual, "_roll", roll);
            SetReferenceIfEmpty(visual, "_shooter", shooter);
            SetReferenceIfEmpty(visual, "_muzzle", muzzle);
            SetReferenceIfEmpty(visual, "_inventory", inventory);
            SetReferenceIfEmpty(visual, "_body", bodyRenderer);
            SetReferenceIfEmpty(visual, "_weapon", gunRenderer);

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

            CameraFollow follow = EnsureCameraAndLight(new Vector2(StationLayout.SpawnX, StationLayout.SpawnY));

            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
            {
                BuildMap(wallTile, floorTile, wallLayer, StationLayout.BuildWalls());
            }
            else if (IsMapEmpty(grid))
            {
                // 타일이 하나도 없으면 사용자가 그린 맵이 아니라 생성이 실패한 것이므로 다시 만든다.
                Object.DestroyImmediate(grid);
                BuildMap(wallTile, floorTile, wallLayer, StationLayout.BuildWalls());
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

            EnsureContainers();
            RaidManager raid = EnsureRaid(player);
            EnsureUi(player, raid);

            // 탈출 포드는 착륙장 모양. 열림·닫힘 색은 EscapePod가 그 위에 칠한다.
            var placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            foreach (EscapePod pod in Object.FindObjectsByType<EscapePod>(FindObjectsSortMode.None))
            {
                ReplaceSceneArt(pod.GetComponent<SpriteRenderer>(), placeholder, $"{AstronautFolder}/pad.png", 108f);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static CameraFollow EnsureCameraAndLight(Vector2 position)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(position.x, position.y, -10f);
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

            return follow;
        }

        // ---------- 대기실 씬 ----------

        // 걸어 다니는 방. 왼쪽 위에 창고, 중앙 위쪽에 레이드로 가는 포탈 (2026-11-16 사용자 결정).
        // 처음 한 번만 놓고 이후에는 Scene 뷰에서 옮긴다.
        private static void EnsureLobbyScene(int wallLayer)
        {
            Scene scene = File.Exists(LobbyScenePath)
                ? EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 씬을 연 뒤에 에셋을 불러와야 참조가 살아 있다.
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var wallTile = AssetDatabase.LoadAssetAtPath<Tile>(WallTilePath);
            var floorTile = AssetDatabase.LoadAssetAtPath<Tile>(FloorTilePath);
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);

            CameraFollow follow = EnsureCameraAndLight(new Vector2(LobbyLayout.SpawnX, LobbyLayout.SpawnY));

            if (GameObject.Find("Grid") == null)
            {
                BuildMap(wallTile, floorTile, wallLayer, LobbyLayout.BuildWalls());
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = new Vector3(LobbyLayout.SpawnX, LobbyLayout.SpawnY, 0f);

                // 대기실에서는 아이템을 쓸 수 없다. 사격은 사격장이 허락할 때만 되고, 산소는 LobbyManager가 줄지 않게 한다.
                DisableComponent(player.GetComponent<PlayerItemUser>());
            }

            GameObject range = EnsureShootingRange(player, wallTile, square);
            EnsureWallColliders();

            SetReferenceIfEmpty(follow, "_target", player.transform);

            GameObject storage = GameObject.Find("Storage");
            if (storage == null)
            {
                storage = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StoragePrefabPath));
                storage.transform.position = new Vector3(LobbyLayout.StorageX, LobbyLayout.StorageY, 0f);
            }

            GameObject portal = GameObject.Find("Portal");
            if (portal == null)
            {
                portal = new GameObject("Portal");
                portal.transform.position = new Vector3(LobbyLayout.PortalX, LobbyLayout.PortalY, 0f);
                portal.transform.localScale = new Vector3(3f, 2f, 1f);

                // 바닥 위, 캐릭터 아래
                var renderer = portal.AddComponent<SpriteRenderer>();
                renderer.sprite = square;
                renderer.color = new Color(0.6f, 0.4f, 1f);
                renderer.sortingOrder = -4;
            }

            GameObject canvasObject = EnsureCanvas();
            if (EnsureComponent(canvasObject, out InventoryUI ui))
            {
                // 대기실에서는 아이템을 버릴 수 없다. 화면 밖에 놓으면 원래 자리로 돌아간다.
                var uiObject = new SerializedObject(ui);
                uiObject.FindProperty("_canDropToWorld").boolValue = false;
                uiObject.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(ui, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(ui, "_interactor", player.GetComponent<PlayerInteractor>());
            SetReferenceIfEmpty(ui, "_input", player.GetComponent<PlayerInputReader>());
            SetReferenceIfEmpty(ui, "_controller", player.GetComponent<PlayerController>());

            GameObject lobbyObject = GameObject.Find("LobbyManager");
            if (lobbyObject == null)
            {
                lobbyObject = new GameObject("LobbyManager");
            }

            if (EnsureComponent(lobbyObject, out LobbyManager lobby))
            {
                // 기획서 7장의 기본 세트: 레이저 피스톨 + 권총 에너지 셀 24발 + 광선검. 처음 시작할 때 3세트.
                var lobbySo = new SerializedObject(lobby);
                SerializedProperty starter = lobbySo.FindProperty("_starterSet");
                (string item, int count)[] set = { ("LaserPistolItem", 1), ("PistolCell", 24), ("LightSaberItem", 1) };
                starter.arraySize = set.Length;
                for (int i = 0; i < set.Length; i++)
                {
                    starter.GetArrayElementAtIndex(i).FindPropertyRelative("_item").objectReferenceValue = LoadItem(set[i].item);
                    starter.GetArrayElementAtIndex(i).FindPropertyRelative("_count").intValue = set[i].count;
                }

                lobbySo.FindProperty("_starterSetCount").intValue = 3;
                lobbySo.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(lobby, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(lobby, "_controller", player.GetComponent<PlayerController>());
            SetReferenceIfEmpty(lobby, "_roll", player.GetComponent<PlayerRoll>());
            SetReferenceIfEmpty(lobby, "_oxygen", player.GetComponent<PlayerOxygen>());
            SetReferenceIfEmpty(lobby, "_inventoryUi", ui);
            SetReferenceIfEmpty(lobby, "_storage", storage.GetComponent<Lootable>());
            SetReferenceIfEmpty(lobby, "_items", items);
            SetReferenceIfEmpty(lobby, "_portal", portal.transform);

            EnsureComponent(canvasObject, out LobbyUI lobbyUi);
            SetReferenceIfEmpty(lobbyUi, "_lobby", lobby);

            // 대기실에도 레이드와 같은 HUD를 둔다 (2026-11-16 사용자 결정). 탈출 포드가 없으므로 RaidManager 칸은 비워 둔다.
            if (EnsureComponent(canvasObject, out HudUI hud))
            {
                // 산소 게이지가 헬멧에 따른 최대치를 보여 주도록 산소 컴포넌트를 다시 켠다. 줄지 않게 하는 것은 LobbyManager가 한다.
                var oxygenSo = new SerializedObject(player.GetComponent<PlayerOxygen>());
                oxygenSo.FindProperty("m_Enabled").boolValue = true;
                oxygenSo.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(hud, "_playerData", AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath));
            SetReferenceIfEmpty(hud, "_health", player.GetComponent<Health>());
            SetReferenceIfEmpty(hud, "_oxygen", player.GetComponent<PlayerOxygen>());
            SetReferenceIfEmpty(hud, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(hud, "_shooter", player.GetComponent<PlayerShooter>());
            SetReferenceIfEmpty(hud, "_itemUser", player.GetComponent<PlayerItemUser>());
            SetReferenceIfEmpty(hud, "_roll", player.GetComponent<PlayerRoll>());
            SetReferenceIfEmpty(hud, "_controller", player.GetComponent<PlayerController>());
            SetReferenceIfEmpty(hud, "_circleSprite", AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath));
            SetReferenceIfEmpty(lobbyUi, "_shootingRange", range.transform);

            // 포탈은 둥근 막 모양. 그림이 원이라 구역(3×2칸)을 덮도록 크기를 맞춘다. 색은 그대로 보라색을 칠한다.
            var portalRenderer = portal.GetComponent<SpriteRenderer>();
            if (portalRenderer.sprite == square)
            {
                ReplaceSceneArt(portalRenderer, square, $"{WeaponArtFolder}/shield3.png", 144f);
                portal.transform.localScale = new Vector3(3f, 3f, 1f);
            }

            foreach (RangeTarget target in Object.FindObjectsByType<RangeTarget>(FindObjectsSortMode.None))
            {
                SetReferenceIfEmpty(target, "_barSprite", square);
            }

            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        // 대기실 왼쪽 아래의 사격장 (2026-11-16 사용자 결정): 서서 쏘는 구역, 칸막이 벽으로 막은 사로, 위아래로 오가는 표적 로봇 2기.
        // 처음 한 번만 만들고 이후에는 Scene 뷰에서 옮긴다.
        private static GameObject EnsureShootingRange(GameObject player, Tile wallTile, Sprite square)
        {
            GameObject range = GameObject.Find("ShootingRange");
            if (range != null)
            {
                return range;
            }

            // 사로와 방을 나누는 벽. 표적이 방 안을 돌아다니는 길을 막지 않게 한다.
            var walls = GameObject.Find("Grid").transform.Find("Walls").GetComponent<Tilemap>();
            for (int x = LobbyLayout.LaneWallStartX; x <= LobbyLayout.LaneEndX; x++)
            {
                walls.SetTile(new Vector3Int(x, LobbyLayout.LaneWallY, 0), wallTile);
            }

            for (int y = 1; y < LobbyLayout.LaneWallY; y++)
            {
                walls.SetTile(new Vector3Int(LobbyLayout.LaneEndX, y, 0), wallTile);
            }

            range = new GameObject("ShootingRange");
            range.transform.position = new Vector3(LobbyLayout.RangeX, LobbyLayout.RangeY, 0f);
            range.transform.localScale = new Vector3(4f, 4f, 1f);

            // 바닥 위, 캐릭터 아래
            var renderer = range.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = new Color(0.95f, 0.8f, 0.3f, 0.35f);
            renderer.sortingOrder = -4;

            // 예전에 만든 대기실 씬에서는 사격 컴포넌트를 꺼 두었다. 사격장이 생기면 다시 켠다.
            var shooter = player.GetComponent<PlayerShooter>();
            var shooterSo = new SerializedObject(shooter);
            shooterSo.FindProperty("m_Enabled").boolValue = true;
            shooterSo.ApplyModifiedPropertiesWithoutUndo();

            var rangeSo = new SerializedObject(range.AddComponent<ShootingRange>());
            rangeSo.FindProperty("_shooter").objectReferenceValue = shooter;
            rangeSo.ApplyModifiedPropertiesWithoutUndo();

            // 표적은 레이드 로봇의 HP를 그대로 쓴다. 하나는 아래에서, 하나는 위에서 출발해 엇갈려 움직인다.
            var parent = new GameObject("RangeTargets");
            CreateRangeTarget(parent.transform, "RangeTarget 1", "PistolRobot", new Vector2(9.5f, 1.5f), 3f, square);
            CreateRangeTarget(parent.transform, "RangeTarget 2", "RifleRobot", new Vector2(13.5f, 4.5f), -3f, square);
            return range;
        }

        private static void CreateRangeTarget(Transform parent, string targetName, string robotName, Vector2 position, float travel, Sprite square)
        {
            var target = new GameObject(targetName);
            target.transform.SetParent(parent, false);
            target.transform.position = position;
            target.layer = LayerMask.NameToLayer("Enemy");

            var body = target.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = target.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;

            // 레이드의 로봇과 같은 사각형 모양
            EnsureChild(target.transform, "Body", out Transform bodyVisual);
            bodyVisual.localScale = new Vector3(0.75f, 0.75f, 1f);
            var renderer = bodyVisual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = square;

            var health = target.AddComponent<Health>();
            SetReferenceIfEmpty(health, "_config", AssetDatabase.LoadAssetAtPath<CombatConfig>(CombatConfigPath));

            var spark = target.AddComponent<SparkOnHit>();
            SetReferenceIfEmpty(spark, "_health", health);
            SetReferenceIfEmpty(spark, "_sparkPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(SparkPrefabPath));

            var so = new SerializedObject(target.AddComponent<RangeTarget>());
            so.FindProperty("_data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(robotName));
            so.FindProperty("_health").objectReferenceValue = health;
            so.FindProperty("_body").objectReferenceValue = renderer;
            so.FindProperty("_collider").objectReferenceValue = collider;
            so.FindProperty("_travel").floatValue = travel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 씬에 놓인 프리팹 인스턴스의 컴포넌트를 끈다. 프리팹 원본은 건드리지 않는다.
        private static void DisableComponent(Behaviour component)
        {
            var so = new SerializedObject(component);
            so.FindProperty("m_Enabled").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 컨테이너 초기 배치 (기획서 9.1: 일반 8개, 연구 2개). 처음 한 번만 만들고 이후에는 Scene 뷰에서 옮긴다.
        // 칸 중심에 놓아야 로봇 길찾기가 그 칸을 막힌 칸으로 읽는다.
        private static void EnsureContainers()
        {
            if (GameObject.Find("Containers") != null)
            {
                return;
            }

            var parent = new GameObject("Containers");
            var normal = AssetDatabase.LoadAssetAtPath<GameObject>(ContainerPrefabPath);
            var research = AssetDatabase.LoadAssetAtPath<GameObject>(ResearchContainerPrefabPath);

            Vector2[] normalPositions =
            {
                new Vector2(9.5f, 22.5f), new Vector2(15.5f, 16.5f), new Vector2(24.5f, 22.5f), new Vector2(33.5f, 10.5f),
                new Vector2(37.5f, 16.5f), new Vector2(41.5f, 22.5f), new Vector2(50.5f, 22.5f), new Vector2(21.5f, 10.5f),
            };
            Vector2[] researchPositions = { new Vector2(8.5f, 37.5f), new Vector2(52.5f, 37.5f) };

            foreach (Vector2 position in normalPositions)
            {
                var container = (GameObject)PrefabUtility.InstantiatePrefab(normal, parent.transform);
                container.transform.position = position;
            }

            foreach (Vector2 position in researchPositions)
            {
                var container = (GameObject)PrefabUtility.InstantiatePrefab(research, parent.transform);
                container.transform.position = position;
            }
        }

        // 가방 화면과 루팅 칸을 그리는 캔버스. 내용은 InventoryUI가 실행 중에 만든다.
        // 탈출 포드 2곳(서쪽·동쪽 포드 방)과 한 판의 진행을 맡는 RaidManager.
        private static RaidManager EnsureRaid(GameObject player)
        {
            GameObject podParent = GameObject.Find("EscapePods");
            if (podParent == null)
            {
                podParent = new GameObject("EscapePods");
                var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
                CreatePod(podParent.transform, "WestPod", new Vector2(4f, 17.5f), square);
                CreatePod(podParent.transform, "EastPod", new Vector2(56f, 17.5f), square);
            }

            GameObject raidObject = GameObject.Find("RaidManager");
            if (raidObject == null)
            {
                raidObject = new GameObject("RaidManager");
            }

            if (EnsureComponent(raidObject, out RaidManager raid))
            {
                EscapePod[] pods = podParent.GetComponentsInChildren<EscapePod>();
                var raidSo = new SerializedObject(raid);
                SerializedProperty list = raidSo.FindProperty("_pods");
                list.arraySize = pods.Length;
                for (int i = 0; i < pods.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = pods[i];
                }

                raidSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 전체 맵의 "가 본 곳" 기록. 벽 타일맵을 읽으므로 길찾기와 같은 오브젝트에 둔다.
            GameObject pathfinding = GameObject.Find("Pathfinding");
            if (EnsureComponent(pathfinding, out MapExplorer map))
            {
                var mapSo = new SerializedObject(map);
                mapSo.FindProperty("_wallMask").intValue = 1 << LayerMask.NameToLayer("Wall");
                mapSo.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(map, "_walls", GameObject.Find("Grid").transform.Find("Walls").GetComponent<Tilemap>());
            SetReferenceIfEmpty(map, "_player", player.transform);

            SetReferenceIfEmpty(raid, "_config", AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath));
            SetReferenceIfEmpty(raid, "_playerHealth", player.GetComponent<Health>());
            SetReferenceIfEmpty(raid, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(raid, "_input", player.GetComponent<PlayerInputReader>());
            SetReferenceIfEmpty(raid, "_controller", player.GetComponent<PlayerController>());
            SetReferenceIfEmpty(raid, "_roll", player.GetComponent<PlayerRoll>());
            SetReferenceIfEmpty(raid, "_items", AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath));
            SetReferenceIfEmpty(raid, "_map", map);
            return raid;
        }

        private static void CreatePod(Transform parent, string podName, Vector2 position, Sprite square)
        {
            var podObject = new GameObject(podName);
            podObject.transform.SetParent(parent, false);
            podObject.transform.position = position;
            podObject.transform.localScale = new Vector3(2f, 2f, 1f);

            // 바닥 위, 캐릭터 아래
            var renderer = podObject.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.sortingOrder = -4;

            var pod = new SerializedObject(podObject.AddComponent<EscapePod>());
            pod.FindProperty("_renderer").objectReferenceValue = renderer;
            pod.ApplyModifiedPropertiesWithoutUndo();
        }

        // 화면 요소가 모두 붙는 캔버스. 내용은 각 화면 스크립트가 실행 중에 만든다.
        private static GameObject EnsureCanvas()
        {
            GameObject canvasObject = GameObject.Find("UI");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("UI");
                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 1f;
            }

            return canvasObject;
        }

        private static void EnsureUi(GameObject player, RaidManager raid)
        {
            GameObject canvasObject = EnsureCanvas();

            EnsureComponent(canvasObject, out InventoryUI ui);
            SetReferenceIfEmpty(ui, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(ui, "_interactor", player.GetComponent<PlayerInteractor>());
            SetReferenceIfEmpty(ui, "_input", player.GetComponent<PlayerInputReader>());
            SetReferenceIfEmpty(ui, "_controller", player.GetComponent<PlayerController>());

            EnsureComponent(canvasObject, out HudUI hud);
            SetReferenceIfEmpty(hud, "_playerData", AssetDatabase.LoadAssetAtPath<PlayerData>(PlayerDataPath));
            SetReferenceIfEmpty(hud, "_health", player.GetComponent<Health>());
            SetReferenceIfEmpty(hud, "_oxygen", player.GetComponent<PlayerOxygen>());
            SetReferenceIfEmpty(hud, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(hud, "_shooter", player.GetComponent<PlayerShooter>());
            SetReferenceIfEmpty(hud, "_itemUser", player.GetComponent<PlayerItemUser>());
            SetReferenceIfEmpty(hud, "_roll", player.GetComponent<PlayerRoll>());
            SetReferenceIfEmpty(hud, "_controller", player.GetComponent<PlayerController>());
            SetReferenceIfEmpty(hud, "_raid", raid);
            SetReferenceIfEmpty(hud, "_circleSprite", AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath));

            EnsureComponent(canvasObject, out ResultUI result);
            SetReferenceIfEmpty(result, "_raid", raid);

            if (EnsureComponent(canvasObject, out MapUI mapUi))
            {
                EscapePod[] pods = GameObject.Find("EscapePods").GetComponentsInChildren<EscapePod>();
                var mapUiSo = new SerializedObject(mapUi);
                SerializedProperty list = mapUiSo.FindProperty("_pods");
                list.arraySize = pods.Length;
                for (int i = 0; i < pods.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = pods[i];
                }

                mapUiSo.ApplyModifiedPropertiesWithoutUndo();
            }

            SetReferenceIfEmpty(mapUi, "_map", GameObject.Find("Pathfinding").GetComponent<MapExplorer>());
            SetReferenceIfEmpty(mapUi, "_raid", raid);
            SetReferenceIfEmpty(mapUi, "_input", player.GetComponent<PlayerInputReader>());
            SetReferenceIfEmpty(mapUi, "_controller", player.GetComponent<PlayerController>());
            SetReferenceIfEmpty(mapUi, "_circleSprite", AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath));
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

        private static void BuildMap(Tile wallTile, Tile floorTile, int wallLayer, bool[,] layout)
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

            for (int x = 0; x < layout.GetLength(0); x++)
            {
                for (int y = 0; y < layout.GetLength(1); y++)
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

        // 게임은 대기실에서 시작하므로 빌드 목록의 맨 앞이 대기실, 그다음이 레이드 씬이어야 한다.
        // 나머지 씬은 있던 순서대로 뒤에 둔다.
        private static void EnsureBuildScenes()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length >= 2 && scenes[0].path == LobbyScenePath && scenes[1].path == ScenePath)
            {
                return;
            }

            var updated = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(LobbyScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };

            foreach (EditorBuildSettingsScene buildScene in scenes)
            {
                if (buildScene.path != LobbyScenePath && buildScene.path != ScenePath)
                {
                    updated.Add(buildScene);
                }
            }

            EditorBuildSettings.scenes = updated.ToArray();
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
