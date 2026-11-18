using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 총기 부착물 5부위 × 2단계 (기획서 17장, 2026-11-18 사용자 결정). 다른 에셋과 같은 규칙: 없는 것만 만든다.
    public static partial class ProjectSetup
    {
        private const string AttachmentArtFolder = "Assets/Art/Pixel/Attachments";

        // 그림은 다른 아이템(knekko의 픽셀아트)과 같은 결로 16×16 점 그림을 2배로 키운다. 무료 팩에 부착물 그림이 없어서다.
        // k 테두리, d 어두운 금속, m 중간, l 밝은 면, a 단계 색(1단계 청록, 2단계 주황), r 레이저 빛, . 투명
        private static readonly string[] StockArt =
        {
            "................",
            "................",
            "................",
            "kkkkkkkkk.......",
            "kmmmmmmmlkkkkkk.",
            "kmddddddmmmmmmmk",
            "kmdaaaadddddddmk",
            "kmddddddkkkkkkk.",
            "kmdddddk........",
            "kmmddddk........",
            "kkmmmddk........",
            ".kkmmmmk........",
            "..kkkkkk........",
        };

        private static readonly string[] GripArt =
        {
            "................",
            "................",
            "..kkkkkkkkkkkk..",
            "..kllllllllllk..",
            "..kmmmmmmmmmmk..",
            "..kkkkmmmmkkkk..",
            ".....kmddmk.....",
            ".....kmddmk.....",
            ".....kmaamk.....",
            ".....kmddmk.....",
            ".....kmddmk.....",
            ".....kmaamk.....",
            ".....kmddmk.....",
            ".....kmmmmk.....",
            ".....kkkkkk.....",
        };

        private static readonly string[] ScopeArt =
        {
            "................",
            "................",
            "................",
            ".kkk........kkk.",
            "kllk........kllk",
            "kmmkkkkkkkkkkmmk",
            "kmmmlllllllllmmk",
            "kaammmmmmmmmmaak",
            "kmmddddddddddmmk",
            "kmmkkkkkkkkkkmmk",
            "kddk...kk...kddk",
            ".kkk..kddk..kkk.",
            "......kkkk......",
        };

        private static readonly string[] MuzzleArt =
        {
            "................",
            "................",
            "................",
            "................",
            ".kkkkkkkkkkkkk..",
            ".kllkllkllklllk.",
            ".kmmkmmkmmkmmmkk",
            ".kaakaakaakdddmk",
            ".kmmkmmkmmkmmmkk",
            ".kddkddkddkdddk.",
            ".kkkkkkkkkkkkk..",
        };

        private static readonly string[] LaserArt =
        {
            "................",
            "................",
            "................",
            "................",
            "..kkkkkkkk......",
            "..kllllllkk.....",
            "..kmmmmmmmkkk...",
            "..kmaammmmkrkrrr",
            "..kmmmmmmmkkk...",
            "..kddddddkk.....",
            "..kkkkkkkk......",
            "....kddk........",
            "....kkkk........",
        };

        private static void EnsureAttachments()
        {
            Directory.CreateDirectory(AttachmentArtFolder);
            bool isNew = LoadItem("Stock1") == null;

            var tier1 = new Color32(40, 230, 235, 255);
            var tier2 = new Color32(255, 160, 40, 255);
            var color = new Color(0.45f, 0.48f, 0.56f);

            // (파일, 이름, 부위, 그림, 무게, 가격, 반동 배율, 퍼짐 배율, 이동 중 배율, 조준 퍼짐 배율, 레이저, 해금 퀘스트)
            var list = new (string file, string name, AttachmentSlot slot, string[] art, Color32 accent, float weight, int price,
                float recoil, float spread, float move, float aim, bool laser, string quest)[]
            {
                ("Stock1", "개머리판 I", AttachmentSlot.Stock, StockArt, tier1, 0.5f, 200, 0.8f, 1f, 0f, 1f, false, "Q07"),
                ("Stock2", "개머리판 II", AttachmentSlot.Stock, StockArt, tier2, 0.5f, 500, 0.65f, 1f, 0f, 1f, false, "Q15"),
                ("Grip1", "손잡이 I", AttachmentSlot.Grip, GripArt, tier1, 0.3f, 150, 0.9f, 1f, 1.7f, 1f, false, "Q07"),
                ("Grip2", "손잡이 II", AttachmentSlot.Grip, GripArt, tier2, 0.3f, 400, 0.85f, 1f, 1.4f, 1f, false, "Q15"),
                ("Scope1", "조준경 I", AttachmentSlot.Scope, ScopeArt, tier1, 0.4f, 250, 1f, 0.85f, 0f, 0.8f, false, "Q07"),
                ("Scope2", "조준경 II", AttachmentSlot.Scope, ScopeArt, tier2, 0.4f, 600, 1f, 0.75f, 0f, 0.6f, false, "Q15"),
                ("Muzzle1", "총구 보정기 I", AttachmentSlot.Muzzle, MuzzleArt, tier1, 0.3f, 200, 0.9f, 0.9f, 0f, 1f, false, "Q07"),
                ("Muzzle2", "총구 보정기 II", AttachmentSlot.Muzzle, MuzzleArt, tier2, 0.3f, 500, 0.85f, 0.85f, 0f, 1f, false, "Q15"),
                ("Laser1", "레이저 사이트 I", AttachmentSlot.Laser, LaserArt, tier1, 0.2f, 150, 1f, 0.9f, 0f, 1f, true, "Q07"),
                ("Laser2", "레이저 사이트 II", AttachmentSlot.Laser, LaserArt, tier2, 0.2f, 400, 1f, 0.8f, 0f, 1f, true, "Q15"),
            };

            foreach (var a in list)
            {
                bool created = LoadItem(a.file) == null;
                ItemData item = EnsureItem(a.file, a.name, ItemCategory.Attachment, color, a.weight, a.price / 2, 1, 1);
                if (created)
                {
                    var so = new SerializedObject(item);
                    so.FindProperty("_attachmentSlot").enumValueIndex = (int)a.slot;
                    so.FindProperty("_recoilMultiplier").floatValue = a.recoil;
                    so.FindProperty("_spreadMultiplier").floatValue = a.spread;
                    so.FindProperty("_moveSpreadScale").floatValue = a.move;
                    so.FindProperty("_aimSpreadMultiplier").floatValue = a.aim;
                    so.FindProperty("_laserSight").boolValue = a.laser;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                SetReferenceIfEmpty(item, "_icon", EnsurePointArt($"{AttachmentArtFolder}/{a.file}.png", a.art, a.accent));
                EnsureOffer("Weapons", a.file, a.price, a.quest);
            }

            EnsureItemDatabase();

            if (isNew)
            {
                // 기획서 17장: AR, 샷건, 저격총만 단다.
                foreach (string path in new[] { PulseRiflePath, PlasmaScatterPath, Nebula7Path })
                {
                    var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<WeaponData>(path));
                    so.FindProperty("_acceptsAttachments").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // 연구 컨테이너와 AR·저격·샷건 로봇 잔해에서 가끔(모두 합쳐 약 5%) 나온다.
                foreach (var a in list)
                {
                    AddLootEntry("ResearchContainerLoot", a.file, 0.6f);
                    AddLootEntry("RifleRobotLoot", a.file, 0.5f);
                    AddLootEntry("SniperRobotLoot", a.file, 0.5f);
                    AddLootEntry("ShotgunRobotLoot", a.file, 0.5f);
                }
            }

            // 조준 레이저
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            EnsureComponent(root, out LaserSight laser);
            var shooter = root.GetComponent<PlayerShooter>();
            SetReferenceIfEmpty(laser, "_shooter", shooter);
            SetReferenceIfEmpty(laser, "_controller", root.GetComponent<PlayerController>());
            SetReferenceIfEmpty(laser, "_roll", root.GetComponent<PlayerRoll>());
            SetReferenceIfEmpty(laser, "_material", AssetDatabase.LoadAssetAtPath<Material>(BeamMaterialPath));
            var laserSo = new SerializedObject(laser);
            if (laserSo.FindProperty("_hitMask").intValue == 0)
            {
                laserSo.FindProperty("_hitMask").intValue = new SerializedObject(shooter).FindProperty("_hitMask").intValue;
                laserSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 상인의 목록에 그 아이템이 없을 때만 덧붙인다 (Inspector에서 고친 값은 두려고).
        private static void EnsureOffer(string merchantId, string itemName, int price, string quest)
        {
            MerchantData merchant = LoadMerchant(merchantId);
            ItemData item = LoadItem(itemName);
            if (merchant == null || item == null)
            {
                return;
            }

            foreach (MerchantData.Offer offer in merchant.Offers)
            {
                if (offer.Item == item)
                {
                    return;
                }
            }

            var so = new SerializedObject(merchant);
            AddOffer(so, itemName, 0, price, quest);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 글자로 적은 점 그림을 2배 크기 PNG로 만든다. 맨 윗줄이 그림의 위다.
        private static Sprite EnsurePointArt(string path, string[] rows, Color32 accent)
        {
            if (!File.Exists(path))
            {
                const int cells = 16;
                const int scale = 2;
                var texture = new Texture2D(cells * scale, cells * scale, TextureFormat.RGBA32, false);
                var pixels = new Color32[cells * scale * cells * scale];
                int top = (cells - rows.Length) / 2;
                for (int row = 0; row < rows.Length; row++)
                {
                    string line = rows[row].PadRight(cells, '.');
                    for (int column = 0; column < cells; column++)
                    {
                        Color32 color = PointColor(line[column], accent);
                        int y = cells - 1 - (row + top);
                        for (int sy = 0; sy < scale; sy++)
                        {
                            for (int sx = 0; sx < scale; sx++)
                            {
                                pixels[(y * scale + sy) * cells * scale + column * scale + sx] = color;
                            }
                        }
                    }
                }

                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            return EnsurePixelSprite(path);
        }

        private static Color32 PointColor(char c, Color32 accent)
        {
            switch (c)
            {
                case 'k': return new Color32(20, 20, 26, 255);
                case 'd': return new Color32(52, 55, 63, 255);
                case 'm': return new Color32(88, 93, 104, 255);
                case 'l': return new Color32(140, 147, 158, 255);
                case 'a': return accent;
                case 'r': return new Color32(255, 50, 40, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }
    }
}
