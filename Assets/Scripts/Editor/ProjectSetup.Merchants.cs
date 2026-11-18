using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 상인 5명 (기획서 14.2, 16장, 2026-11-18 사용자 결정). 다른 에셋과 같은 규칙: 없는 것만 만들고 이미 있는 값은 두다.
    public static partial class ProjectSetup
    {
        private const string MerchantFolder = "Assets/Data/Merchants";

        private static string MerchantPath(string id) => $"{MerchantFolder}/{id}.asset";

        private static MerchantData LoadMerchant(string id) => AssetDatabase.LoadAssetAtPath<MerchantData>(MerchantPath(id));

        private static void EnsureMerchants()
        {
            Directory.CreateDirectory(MerchantFolder);

            EnsureMerchant("Weapons", "볼트", "무기상", new Color(1f, 0.72f, 0.45f),
                "정거장 보안 책임자였지. 지금은 내가 만든 로봇들한테 쫓기는 신세고.\n\n" +
                "녀석들은 재장전할 때와 연사 사이가 비어. 그 틈을 노려.\n\n살 게 있으면 골라 봐. 외상은 없어.",
                so =>
                {
                    AddOffer(so, "LaserPistolItem", 0, 100, "");
                    AddOffer(so, "LightSaberItem", 0, 150, "");
                    AddOffer(so, "PistolCell", 0, AmmoPrice("PistolCell"), "");
                    AddOffer(so, "PulseRifleItem", 0, 600, "Q02");
                    AddOffer(so, "RifleCell", 0, AmmoPrice("RifleCell"), "Q02");
                    AddOffer(so, "PlasmaScatterItem", 0, 700, "Q08");
                    AddOffer(so, "PlasmaCapsule", 0, AmmoPrice("PlasmaCapsule"), "Q08");
                    AddOffer(so, "Nebula7Item", 0, 900, "Q15");
                    AddOffer(so, "SniperCell", 0, AmmoPrice("SniperCell"), "Q15");
                });

            EnsureMerchant("Armor", "한", "방어구상", new Color(0.6f, 0.85f, 1f),
                "화물칸에서 일하던 기술자야. 우주복이라면 눈 감고도 고쳐.\n\n" +
                "구멍 난 걸 입고 나가면 산소부터 새. 제대로 된 걸 사 가.",
                so =>
                {
                    AddOffer(so, "Vest1", 0, 200, "");
                    AddOffer(so, "Helmet1", 0, 150, "Q04");
                    AddOffer(so, "Vest2", 0, 500, "Q09");
                    AddOffer(so, "Helmet2", 0, 400, "Q09");
                    AddOffer(so, "Vest3", 0, 1000, "Q16");
                    AddOffer(so, "Helmet3", 0, 800, "Q16");
                });

            EnsureMerchant("Medical", "세라", "의료상", new Color(0.55f, 1f, 0.7f),
                "의무실이 봉쇄되기 직전에 빠져나왔어요. 약품은 그때 챙긴 게 전부예요.\n\n" +
                "다치면 아끼지 말고 쓰세요. 살아 돌아오는 게 먼저니까요.",
                so =>
                {
                    AddOffer(so, "MediGel", 0, 40, "");
                    AddOffer(so, "OxygenCan", 0, 70, "Q03");
                    AddOffer(so, "MediKit", 0, 120, "Q11");
                });

            EnsureMerchant("Workshop", "오토", "제작소", new Color(1f, 0.9f, 0.4f),
                "격납고의 장거리 탈출 포드, 봤지? 저걸 고치면 여기서 나갈 수 있어.\n\n" +
                "재료만 가져오면 무기든 부품이든 내가 만들어 줄게.",
                so =>
                {
                    AddRecipe(so, "LaserPistolItem", 0, "", ("CircuitPart", 1));
                    AddRecipe(so, "LightSaberItem", 0, "", ("CircuitPart", 2));
                    AddRecipe(so, "PulseRifleItem", 0, "", ("CircuitPart", 4));
                    AddRecipe(so, "PlasmaScatterItem", 0, "", ("CircuitPart", 3), ("QuantumCore", 1));
                    AddRecipe(so, "Nebula7Item", 0, "", ("CircuitPart", 2), ("QuantumCore", 2));
                });

            EnsureMerchant("BlackMarket", "미라", "암시장", new Color(0.95f, 0.55f, 1f),
                "출처는 안 물어. 값만 맞으면 돼.\n\n" +
                "무기랑 방어구는 상점가의 절반, 나머지는 제값 쳐 줄게.",
                so =>
                {
                    so.FindProperty("_buysItems").boolValue = true;
                    SerializedProperty sources = so.FindProperty("_priceSources");
                    string[] ids = { "Weapons", "Armor" };
                    sources.arraySize = ids.Length;
                    for (int i = 0; i < ids.Length; i++)
                    {
                        sources.GetArrayElementAtIndex(i).objectReferenceValue = LoadMerchant(ids[i]);
                    }
                });
        }

        // 기획서 8장: 탄약은 무기 상점에서 아이템 가치의 2배에 판다.
        private static int AmmoPrice(string itemName)
        {
            ItemData item = LoadItem(itemName);
            return item != null ? item.Value * 2 : 0;
        }

        private static void EnsureMerchant(string id, string displayName, string title, Color tint, string greeting,
            System.Action<SerializedObject> fill)
        {
            if (LoadMerchant(id) != null)
            {
                return;
            }

            var merchant = ScriptableObject.CreateInstance<MerchantData>();
            AssetDatabase.CreateAsset(merchant, MerchantPath(id));
            var so = new SerializedObject(merchant);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_title").stringValue = title;
            so.FindProperty("_greeting").stringValue = greeting;
            so.FindProperty("_tint").colorValue = tint;
            fill(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddOffer(SerializedObject so, string itemName, int count, int price, string quest)
        {
            SerializedProperty list = so.FindProperty("_offers");
            list.arraySize++;
            SerializedProperty entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            entry.FindPropertyRelative("_item").objectReferenceValue = LoadItem(itemName);
            entry.FindPropertyRelative("_count").intValue = count;
            entry.FindPropertyRelative("_price").intValue = price;
            entry.FindPropertyRelative("_unlockQuest").stringValue = quest;
        }

        private static void AddRecipe(SerializedObject so, string outputName, int count, string quest, params (string item, int count)[] inputs)
        {
            SerializedProperty list = so.FindProperty("_recipes");
            list.arraySize++;
            SerializedProperty entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            entry.FindPropertyRelative("_output").objectReferenceValue = LoadItem(outputName);
            entry.FindPropertyRelative("_count").intValue = count;
            entry.FindPropertyRelative("_unlockQuest").stringValue = quest;
            SerializedProperty costs = entry.FindPropertyRelative("_inputs");
            costs.arraySize = inputs.Length;
            for (int i = 0; i < inputs.Length; i++)
            {
                costs.GetArrayElementAtIndex(i).FindPropertyRelative("_item").objectReferenceValue = LoadItem(inputs[i].item);
                costs.GetArrayElementAtIndex(i).FindPropertyRelative("_count").intValue = inputs[i].count;
            }
        }

        // ---------- 대기실 씬 ----------

        // 상인을 처음 한 번만 세운다. 이후에는 Scene 뷰에서 옮긴다. 이름 앞의 숫자는 화면의 이름표 순서다.
        private static void EnsureLobbyNpcs(GameObject player, GameObject canvasObject, LobbyManager lobby, int wallLayer)
        {
            GameObject parent = GameObject.Find("Npcs");
            if (parent == null)
            {
                parent = new GameObject("Npcs");
            }

            (string id, Vector2 position)[] npcs =
            {
                ("Weapons", new Vector2(17.5f, 10f)),
                ("Armor", new Vector2(20.5f, 10f)),
                ("Medical", new Vector2(17.5f, 7f)),
                ("Workshop", new Vector2(20.5f, 7f)),
                ("BlackMarket", new Vector2(20.5f, 3.5f)),
            };

            Sprite body = EnsureArtSprite($"{AstronautFolder}/astronaut_stand.png");
            for (int i = 0; i < npcs.Length; i++)
            {
                string objectName = $"Npc{i + 1}_{npcs[i].id}";
                if (parent.transform.Find(objectName) != null)
                {
                    continue;
                }

                var npcObject = new GameObject(objectName);
                npcObject.transform.SetParent(parent.transform);
                npcObject.transform.position = npcs[i].position;

                // 우주인 그림은 오른쪽을 본다. 플레이어가 다가오는 아래쪽을 보게 눕힌다.
                npcObject.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
                npcObject.layer = wallLayer;
                var renderer = npcObject.AddComponent<SpriteRenderer>();
                renderer.sprite = body;
                renderer.sortingOrder = 2;
                npcObject.AddComponent<CircleCollider2D>().radius = 0.32f;

                var npc = npcObject.AddComponent<Npc>();
                var so = new SerializedObject(npc);
                so.FindProperty("_merchant").objectReferenceValue = LoadMerchant(npcs[i].id);
                so.FindProperty("_renderer").objectReferenceValue = renderer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureComponent(canvasObject, out MerchantUI ui);
            SetReferenceIfEmpty(ui, "_inventory", player.GetComponent<PlayerInventory>());
            SetReferenceIfEmpty(ui, "_input", player.GetComponent<PlayerInputReader>());
            SetReferenceIfEmpty(ui, "_controller", player.GetComponent<PlayerController>());
            SetReferenceIfEmpty(ui, "_lobby", lobby);
        }
    }
}
