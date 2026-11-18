using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 제작소 (기획서 16장): 고출력 탄약과 장거리 탈출 포드 부품 (기획서 14.5). 없는 것만 만든다.
    public static partial class ProjectSetup
    {
        private static readonly string[] HighCellArt =
        {
            "................",
            "......kkkk......",
            ".....kllllk.....",
            "....kkkkkkkk....",
            "....kmmmmmmk....",
            "....kaaaaaak....",
            "....kmmmmmmk....",
            "....kaaaaaak....",
            "....kmmrrmmk....",
            "....kmrrrrmk....",
            "....kmmrrmmk....",
            "....kaaaaaak....",
            "....kmmmmmmk....",
            "....kddddddk....",
            "....kkkkkkkk....",
        };

        private static readonly string[] HullPanelArt =
        {
            "kkkkkkkkkkkkkkkk",
            "kllllllllllllllk",
            "klkmmmmmmmmmmklk",
            "klmmmmmmmmmmmmlk",
            "klmmddddddddmmlk",
            "klmmdaaaaaadmmlk",
            "klmmddddddddmmlk",
            "klmmmmmmmmmmmmlk",
            "klmmmmmmmmmmmmlk",
            "klmmddddddddmmlk",
            "klmmdaaaaaadmmlk",
            "klmmddddddddmmlk",
            "klmmmmmmmmmmmmlk",
            "klkmmmmmmmmmmklk",
            "kddddddddddddddk",
            "kkkkkkkkkkkkkkkk",
        };

        private static readonly string[] ThrusterArt =
        {
            "....kkkkkkkk....",
            "....kllllllk....",
            "....kmmmmmmk....",
            "....kaammaak....",
            "...kkmmmmmmkk...",
            "...kllllllllk...",
            "..kmmmmmmmmmmk..",
            "..kmddddddddmk..",
            ".kmmddddddddmmk.",
            ".kmdddrrrrdddmk.",
            "kmmddrrrrrrddmmk",
            "kmdddrrrrrrdddmk",
            "kmmddrrrrrrddmmk",
            "kkkkkkkkkkkkkkkk",
        };

        private static void EnsureWorkshop()
        {
            // 고출력 탄약: 기본 탄약과 같은 크기·수량, 피해 1.25배. 색은 기본 탄약과 같은 계열로 칸을 칠한다.
            (string file, string name, string baseAmmo, string weaponPath, Color32 accent)[] ammo =
            {
                ("PistolCellHigh", "고출력 권총 셀", "PistolCell", LaserPistolPath, new Color32(255, 214, 40, 255)),
                ("RifleCellHigh", "고출력 라이플 셀", "RifleCell", PulseRiflePath, new Color32(60, 170, 255, 255)),
                ("SniperCellHigh", "고출력 저격 셀", "SniperCell", Nebula7Path, new Color32(255, 70, 70, 255)),
                ("PlasmaCapsuleHigh", "고출력 플라즈마 캡슐", "PlasmaCapsule", PlasmaScatterPath, new Color32(80, 230, 110, 255)),
            };

            foreach (var a in ammo)
            {
                ItemData baseItem = LoadItem(a.baseAmmo);
                bool created = LoadItem(a.file) == null;
                ItemData item = EnsureItem(a.file, a.name, ItemCategory.Ammo, baseItem.Color, baseItem.Weight,
                    Mathf.RoundToInt(baseItem.Value * 1.6f), baseItem.MaxStack, baseItem.DefaultCount);
                if (created)
                {
                    var so = new SerializedObject(item);
                    so.FindProperty("_width").intValue = baseItem.Width;
                    so.FindProperty("_height").intValue = baseItem.Height;
                    so.FindProperty("_ammoDamageMultiplier").floatValue = 1.25f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                SetReferenceIfEmpty(item, "_icon", EnsurePointArt($"{AttachmentArtFolder}/{a.file}.png", HighCellArt, a.accent));
                SetReferenceIfEmpty(AssetDatabase.LoadAssetAtPath<WeaponData>(a.weaponPath), "_highAmmoItem", item);
            }

            // 장거리 탈출 포드 부품 (기획서 14.5). 크고 무거워서 레이드에서 들고 나오기 어렵다. 팔 수 없다.
            var partColor = new Color(0.75f, 0.55f, 0.3f);
            (string file, string name, int width, int height, float weight, int value, string icon, string[] art)[] parts =
            {
                ("HullPanel", "선체 패널", 2, 2, 4f, 150, null, HullPanelArt),
                ("PowerCell", "동력 셀", 1, 2, 3f, 300, "PowerCell", null),
                ("Thruster", "추진 노즐", 2, 2, 6f, 500, null, ThrusterArt),
                ("OxygenRecycler", "산소 재생기", 2, 2, 3f, 400, "OxygenRecycler", null),
                ("NavCore", "항법 코어", 1, 1, 1f, 500, "NavCore", null),
                ("SecurityKey", "보안 인증 키", 1, 1, 0.1f, 300, "SecurityKey", null),
                ("PlasmaIgniter", "플라즈마 점화기", 1, 2, 2f, 200, "PlasmaIgniter", null),
            };

            foreach (var p in parts)
            {
                bool created = LoadItem(p.file) == null;
                ItemData item = EnsureItem(p.file, p.name, ItemCategory.PodPart, partColor, p.weight, p.value, 1, 1);
                if (created)
                {
                    var so = new SerializedObject(item);
                    so.FindProperty("_width").intValue = p.width;
                    so.FindProperty("_height").intValue = p.height;
                    so.FindProperty("_cannotSell").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                Sprite sprite = p.icon != null
                    ? EnsurePixelSprite($"{PixelItemFolder}/{p.icon}.png")
                    : EnsurePointArt($"{AttachmentArtFolder}/{p.file}.png", p.art, new Color32(255, 160, 40, 255));
                SetReferenceIfEmpty(item, "_icon", sprite);
            }

            EnsureItemDatabase();

            // 동력 구역의 연구 컨테이너: 연구 컨테이너 표에 동력 셀(약 5%)을 더한 것 (기획서 14.5).
            if (LoadLoot("PowerResearchLoot") == null)
            {
                EnsureLootTable("PowerResearchLoot", false, 2, 3,
                    ("QuantumCore", 22f), ("SniperCell", 15f), ("MediKit", 15f), ("OxygenCan", 12f), ("Helmet2", 10f),
                    ("Vest1", 6f), ("Vest2", 8f), ("EnergyCrystal", 14f), ("NanoFiber", 14f), ("PowerCell", 7f),
                    ("PulseRifleItem", 3f), ("PlasmaScatterItem", 3f), ("Helmet3", 2f), ("Vest3", 1f));
            }

            // 제작법: 처음 만들 때만 덧붙인다.
            MerchantData workshop = LoadMerchant("Workshop");
            if (workshop != null && !HasRecipe(workshop, "HullPanel"))
            {
                var so = new SerializedObject(workshop);
                AddRecipe(so, "PistolCellHigh", 0, "Q13", ("PistolCell", 12), ("NanoFiber", 1));
                AddRecipe(so, "RifleCellHigh", 0, "Q13", ("RifleCell", 30), ("NanoFiber", 1));
                AddRecipe(so, "SniperCellHigh", 0, "Q13", ("SniperCell", 5), ("NanoFiber", 1));
                AddRecipe(so, "PlasmaCapsuleHigh", 0, "Q13", ("PlasmaCapsule", 6), ("NanoFiber", 1));
                AddRecipe(so, "HullPanel", 0, "Q05", ("Alloy", 4), ("CircuitPart", 1));
                AddRecipe(so, "Thruster", 0, "Q06", ("PlasmaIgniter", 1), ("Alloy", 6), ("NanoFiber", 4));
                AddRecipe(so, "PowerCell", 0, "Q13", ("EnergyCrystal", 3), ("QuantumCore", 1));
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static bool HasRecipe(MerchantData merchant, string outputName)
        {
            foreach (MerchantData.Recipe recipe in merchant.Recipes)
            {
                if (recipe.Output != null && recipe.Output.name == outputName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
