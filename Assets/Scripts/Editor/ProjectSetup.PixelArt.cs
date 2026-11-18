using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 아이템 그림을 CC0 픽셀아트로 바꾼다 (2026-11-18 사용자 결정: 퀀텀 코어, 회로 부품, 헬멧, 총이 너무 단순해 보임).
    // 아직 처음 넣은 Kenney 그림을 쓰는 것만 바꾼다. Inspector에서 다른 그림을 넣었으면 그대로 둔다.
    public static partial class ProjectSetup
    {
        private const string PixelItemFolder = "Assets/Art/Pixel/Items";

        private static void EnsurePixelItemArt()
        {
            ReplaceItemIcon("LaserPistolItem", "gun01", "LaserPistol_Icon");
            ReplaceItemIcon("PulseRifleItem", "gun04", "PulseRifle_Icon");
            ReplaceItemIcon("PlasmaScatterItem", "gun05", "PlasmaScatter_Icon");
            ReplaceItemIcon("Nebula7Item", "gun08", "Nebula7_Icon");
            ReplaceItemIcon("Helmet1", "ufoBlue", "Helmet1");
            ReplaceItemIcon("Helmet2", "ufoGreen", "Helmet2");
            ReplaceItemIcon("Helmet3", "ufoRed", "Helmet3");
            ReplaceItemIcon("Vest1", "shield_bronze", "Vest1");
            ReplaceItemIcon("Vest2", "shield_silver", "Vest2");
            ReplaceItemIcon("Vest3", "shield_gold", "Vest3");
            ReplaceItemIcon("QuantumCore", "star_gold", "QuantumCore");
            ReplaceItemIcon("CircuitPart", "things_silver", "CircuitPart");

            ReplaceWeaponSprite(LaserPistolPath, "gun01", "LaserPistol_Hand");
            ReplaceWeaponSprite(PulseRiflePath, "gun04", "PulseRifle_Hand");
            ReplaceWeaponSprite(PlasmaScatterPath, "gun05", "PlasmaScatter_Hand");
            ReplaceWeaponSprite(Nebula7Path, "gun08", "Nebula7_Hand");
        }

        private static void ReplaceItemIcon(string itemName, string oldSprite, string newFile)
        {
            ItemData item = LoadItem(itemName);
            if (item != null)
            {
                ReplaceSpriteField(item, "_icon", oldSprite, newFile);
            }
        }

        private static void ReplaceWeaponSprite(string weaponPath, string oldSprite, string newFile)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            if (weapon != null)
            {
                ReplaceSpriteField(weapon, "_sprite", oldSprite, newFile);
            }
        }

        private static void ReplaceSpriteField(Object target, string field, string oldSprite, string newFile)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            Object current = property.objectReferenceValue;
            string currentPath = current != null ? AssetDatabase.GetAssetPath(current) : string.Empty;
            if (current != null && !currentPath.EndsWith("/" + oldSprite + ".png"))
            {
                return;
            }

            property.objectReferenceValue = EnsurePixelSprite($"{PixelItemFolder}/{newFile}.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 픽셀아트는 번지지 않게 점 필터로 읽는다. 긴 변이 1칸이 되게 해서 바닥에 버린 아이템이 너무 커지지 않게 한다.
        private static Sprite EnsurePixelSprite(string path)
        {
            const string marker = "EscapeFromSpace:pixel";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.userData != marker)
            {
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Mathf.Max(width, height);
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.userData = marker;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
