using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 우주선 이변과 에일리언 (기획서 19.4, 2026-11-19 사용자 결정).
    // 에일리언도 로봇과 같은 상태 머신(Robot)을 쓰고, RobotData의 Is Biological로 구분한다.
    public static partial class ProjectSetup
    {
        private const string AlienArtFolder = "Assets/Art/Pixel/Aliens";

        // 위에서 본 모습, 앞(머리)이 아래. 빠른 근접형 곤충
        private static readonly string[] StalkerArt =
        {
            "..k..........k..",
            "...k..kkkk..k...",
            "....kkaaaakk....",
            "..kkaaaaaaaakk..",
            ".k.kaammmmaak.k.",
            "k..kaammmmaak..k",
            "...kaaaaaaaak...",
            "..kkaaaaaaaakk..",
            ".k..kaaaaaak..k.",
            "k....kaaaak....k",
            ".....kaaaak.....",
            "....karaarak....",
            "....kaaaaaak....",
            ".....kk..kk.....",
            "....k......k....",
        };

        // 부풀어 오른 산성 주머니와 아래쪽의 입
        private static readonly string[] SpitterArt =
        {
            "......kkkk......",
            "....kkaaaakk....",
            "...kaallllaak...",
            "..kaallaallaak..",
            "..kaalaaaalaak..",
            ".kaaaaaaaaaaaak.",
            ".kaammaaaammaak.",
            ".kaaaaaaaaaaaak.",
            "..kaaaaaaaaaak..",
            "..kkaaaaaaaakk..",
            "...kaar..raak...",
            "....kaddddak....",
            ".....kkkkkk.....",
        };

        private static readonly string[] TissueArt =
        {
            "................",
            "................",
            ".....kkkkk......",
            "...kkaaaaakk....",
            "..kaallaaaaak...",
            "..kalaaaaaadk...",
            ".kaaaaaddaaak...",
            ".kaaddaaaaaak...",
            "..kaaaaaadak....",
            "...kkaaaakk.....",
            ".....kkkk.......",
        };

        private static void EnsureAliens()
        {
            Directory.CreateDirectory(AlienArtFolder);
            var green = new Color32(120, 210, 90, 255);

            ItemData tissue = EnsureItem("AlienTissue", "에일리언 조직", ItemCategory.Loot, new Color(0.5f, 0.8f, 0.35f), 0.3f, 120, 5, 1);
            SetReferenceIfEmpty(tissue, "_icon", EnsurePointArt($"{AlienArtFolder}/AlienTissue.png", TissueArt, green));
            EnsureItemDatabase();
            EnsureLootTable("AlienLoot", true, 0, 0, ("AlienTissue", 60f));

            EnsureAlien("AlienStalker", "스토커", new Color(0.55f, 0.85f, 0.35f), 45f, RobotAttackKind.Melee, 15f, 120f, 1.5f, 0f, 1.5f,
                2.5f, 5.5f, 5f, 120f, 12f, 10, 0f, StalkerArt, green, MeleeRobotSoundSource, 1.35f);
            EnsureAlien("AlienSpitter", "스피터", new Color(0.45f, 0.8f, 0.5f), 60f, RobotAttackKind.Projectile, 12f, 4f, 9f, 0.6f, 2.5f,
                2f, 3f, 8f, 90f, 8f, 15, 8f, SpitterArt, new Color32(150, 230, 80, 255), ShotgunRobotSoundSource, 1.6f);

            // 에일리언 습격 때 일반 로봇 대신 놓는 것
            var raidConfig = AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath);
            var raidSo = new SerializedObject(raidConfig);
            SerializedProperty invaders = raidSo.FindProperty("_invaders");
            if (invaders.arraySize == 0)
            {
                invaders.arraySize = 2;
                invaders.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath("AlienStalker"));
                invaders.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath("AlienSpitter"));
                raidSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private const string MeleeRobotSoundSource = "MeleeRobot";
        private const string ShotgunRobotSoundSource = "ShotgunRobot";

        private static void EnsureAlien(string file, string name, Color color, float hp, RobotAttackKind kind, float damage, float spread,
            float range, float windup, float rest, float patrol, float chase, float sight, float sightAngle, float hearing, int xp,
            float projectileSpeed, string[] art, Color32 accent, string soundFrom, float pitch)
        {
            bool created = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(file)) == null;
            EnsureRobot(file, name, color, hp, kind, damage, 1, spread, range, windup, 1, 0f, rest, patrol, chase, sight, sightAngle);
            if (!created)
            {
                return;
            }

            var data = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(file));
            var so = new SerializedObject(data);
            so.FindProperty("_isBiological").boolValue = true;
            so.FindProperty("_sprite").objectReferenceValue = EnsurePointArt($"{AlienArtFolder}/{file}.png", art, accent);
            so.FindProperty("_visualSize").floatValue = 1f;
            so.FindProperty("_lootTable").objectReferenceValue = LoadLoot("AlienLoot");
            so.FindProperty("_xpReward").intValue = xp;
            so.FindProperty("_wreckSize").vector2IntValue = new Vector2Int(1, 2);
            so.FindProperty("_hearingRadius").floatValue = hearing;
            so.FindProperty("_projectileSpeed").floatValue = projectileSpeed;

            // 소리는 비슷한 로봇의 것을 높은 음으로 쓴다 (생물 소리 팩이 없어서).
            var source = AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(soundFrom));
            if (source != null)
            {
                so.CopyFromSerializedProperty(new SerializedObject(source).FindProperty("_attackSound"));
                so.FindProperty("_attackSound._pitch").floatValue = pitch;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 정거장 씬: 이번 판의 이변을 적용하는 오브젝트
        private static void EnsureRaidEvents(GameObject player)
        {
            GameObject events = GameObject.Find("RaidEvents");
            if (events == null)
            {
                events = new GameObject("RaidEvents");
            }

            EnsureComponent(events, out RaidEventDirector director);
            SetReferenceIfEmpty(director, "_config", AssetDatabase.LoadAssetAtPath<RaidConfig>(RaidConfigPath));
            SetReferenceIfEmpty(director, "_player", player.transform);
            GameObject containers = GameObject.Find("Containers");
            if (containers != null)
            {
                SetReferenceIfEmpty(director, "_containers", containers.transform);
            }

            GameObject canvas = GameObject.Find("UI");
            if (canvas != null)
            {
                SetReferenceIfEmpty(director, "_canvas", canvas.GetComponent<Canvas>());
            }
        }
    }
}
