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

        private static readonly string[] Folders =
        {
            "Assets/Art/Placeholder", "Assets/Data", "Assets/Data/Weapons", "Assets/Prefabs", "Assets/Tiles", "Assets/Physics",
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

            Sprite square = EnsureSprite(SquarePath, 32, false);
            Sprite circle = EnsureSprite(CirclePath, 64, true);
            EnsureTile(WallTilePath, square, new Color(0.55f, 0.57f, 0.6f), Tile.ColliderType.Grid);
            EnsureTile(FloorTilePath, square, new Color(0.16f, 0.17f, 0.2f), Tile.ColliderType.None);

            PlayerData playerData = EnsurePlayerData();
            EnsurePlayerPrefab(playerData, square, circle, playerLayer);
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
                return data;
            }

            // 기획서 초기값. 에셋을 처음 만들 때만 넣는다.
            data = ScriptableObject.CreateInstance<PlayerData>();
            var so = new SerializedObject(data);
            so.FindProperty("_moveSpeed").floatValue = 5f;
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

        // ---------- 프리팹 ----------

        private static void EnsurePlayerPrefab(PlayerData playerData, Sprite square, Sprite circle, int playerLayer)
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

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = new Vector3(StationLayout.SpawnX, StationLayout.SpawnY, 0f);
            }

            SetReferenceIfEmpty(follow, "_target", player.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
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
