using UnityEditor;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // 스토리·퀘스트·엔딩 (기획서 14장, 2026-11-18 사용자 결정). 이름·대사·보상은 Claude가 정한 초기값이다.
    // 에셋은 처음 한 번만 만든다. 이후에는 Assets/Data/QuestDatabase.asset의 Inspector에서 고친다.
    public static partial class ProjectSetup
    {
        private const string QuestDatabasePath = "Assets/Data/QuestDatabase.asset";

        // 2026-11-19 사용자 결정: 행성의 우주선 무덤에 불시착한 이야기로 바꿨다 (기획서 19.1).
        private const string Prologue =
            "행성 케르-7. 궤도에 다가온 배는 하나도 돌아가지 못했다.\n협곡 바닥에는 떨어진 우주선들이 무덤처럼 쌓여 있다.\n\n" +
            "회수 업자인 당신은 잔해를 노리고 궤도에 들어섰다.\n그 순간, 궤도의 자동 방어망 '센티널'이 회수선을 향해 포문을 열었다.\n\n" +
            "불타는 회수선은 협곡에 처박혔다.\n살아남은 것은 당신과 정비사 오토, 그리고 다른 배에서 버티던 생존자 넷.\n\n" +
            "도약 엔진만 고치면 이 행성을 떠날 수 있다.\n출격 해치로 무덤의 배들에 들어가 부품을 모으고, 살아서 돌아와라.";

        private const string Ending =
            "마지막 부품이 맞물리자, 도약 엔진이 낮게 울리기 시작했다.\n\n" +
            "회수선이 협곡의 먼지를 털고 떠올랐다.\n센티널이 포문을 돌렸지만, 해제 코드가 그 명령을 덮어썼다.\n\n" +
            "볼트, 한, 세라, 오토, 미라. 그리고 당신.\n여섯 명을 태운 회수선이 케르-7의 궤도를 벗어났다.\n\n" +
            "멀어지는 행성을 보며 누군가 말했다.\n\"다음엔 무덤 말고, 진짜 집으로 가자.\"";

        private const string Credits =
            "ESCAPE from Space\n\n" +
            "그림  Kenney · knekko · Hyptosis · Jannax · ansimuz (CC0)\n" +
            "소리  Kenney (CC0)\n" +
            "음악  Fupi, Jason Shaw (CC0)\n\n" +
            "플레이해 주셔서 고맙습니다.\n엔딩 뒤에도 대기실에서 계속할 수 있습니다.";

        private static readonly string[] PodPortArt =
        {
            "......kkkk......",
            "....kkllllkk....",
            "...kllmmmmllk...",
            "..klmmaaaammlk..",
            "..klmaaaaaamlk..",
            ".klmmaaaaaammlk.",
            ".klmmmmmmmmmmlk.",
            ".kmmmddddddmmmk.",
            ".kmmddkkkkddmmk.",
            ".kmmdkrrrrkdmmk.",
            ".kmmdkrrrrkdmmk.",
            ".kmmddkkkkddmmk.",
            "kkmmmmmmmmmmmmkk",
            "kddkmmmmmmmmkddk",
            "kddk.kddddk.kddk",
            "kkkk.kkkkkk.kkkk",
        };

        private struct QuestSpec
        {
            public string Id, Npc, Title, Story, Done, Requires, Robot, Named, Zone, Note;
            public int Chapter, Count, Credits;
            public QuestKind Kind;
            public (string item, int count)[] Deliver;
            public (string item, int count)[] Reward;
        }

        private static QuestSpec[] QuestSpecs => new[]
        {
            new QuestSpec { Id = "Q01", Npc = "Workshop", Chapter = 1, Title = "첫 회수", Kind = QuestKind.Escape, Count = 1, Credits = 100,
                Reward = new[] { ("MediGel", 2) },
                Story = "불시착하고 고생 많았지. 난 오토, 이 배 정비를 맡고 있어.\n\n도약 엔진을 고치려면 무덤에 널린 배들에서 물자를 가져와야 해. 출격 해치로 나가서, 일단 살아서 돌아와 봐.",
                Done = "돌아왔구나! 그거면 충분해. 이걸로 시작하자." },
            new QuestSpec { Id = "Q02", Npc = "Weapons", Chapter = 1, Title = "보안 로봇 실태", Kind = QuestKind.KillAny, Count = 5, Credits = 200,
                Note = "무기상: 펄스 라이플, 라이플 셀",
                Story = "센티널 로봇은 우리 순찰선을 떨어뜨린 놈들이야. 배마다 아직 돌아다니지.\n\n몇 기만 부숴 봐. 감을 잡으면 더 좋은 총을 내줄게.",
                Done = "손이 좀 맞는군. 이제 펄스 라이플을 팔아 주지." },
            new QuestSpec { Id = "Q03", Npc = "Medical", Chapter = 1, Title = "응급 처치", Kind = QuestKind.Deliver,
                Deliver = new[] { ("MediGel", 3) }, Note = "의료상: 산소 캔",
                Story = "병원선에서 챙겨 나온 약이 거의 바닥났어요.\n\n메디젤 세 개만 구해다 주세요. 산소 캔을 나눠 드릴게요.",
                Done = "고마워요. 이제 산소 캔도 드릴 수 있어요." },
            new QuestSpec { Id = "Q04", Npc = "Armor", Chapter = 1, Title = "우주복 수선", Kind = QuestKind.Deliver,
                Deliver = new[] { ("CircuitPart", 2) }, Note = "방어구상: 1단계 헬멧",
                Story = "헬멧을 만들려는데 회로 부품이 모자라.\n\n두 개만 구해 오면 헬멧을 만들어 팔게.",
                Done = "좋아, 이걸로 헬멧을 만들 수 있겠어." },
            new QuestSpec { Id = "Q05", Npc = "Workshop", Chapter = 2, Title = "전원 추적", Kind = QuestKind.Visit, Zone = "Engine",
                Note = "제작소: 선체 패널",
                Story = "도약 엔진에 동력을 넣으려면 채굴선의 동력 배선을 봐야 해.\n\n동쪽 채굴선에 들어갔다가 살아서 나와 줘. 네가 본 것만으로도 지도가 생겨.",
                Done = "채굴선 배선이 아직 살아 있군. 이제 선체 패널을 만들어 줄 수 있어." },
            new QuestSpec { Id = "Q06", Npc = "Workshop", Chapter = 2, Title = "녹슨 용접공", Kind = QuestKind.KillNamed, Named = "E5",
                Reward = new[] { ("PlasmaIgniter", 1) }, Note = "제작소: 추진 노즐",
                Story = "채굴선에 '용접공 E-5'라는 놈이 버티고 있어. 원래 수리 로봇이었는데, 센티널이 무기를 달아 놨지.\n\n그놈을 쓰러뜨리면 플라즈마 점화기를 구할 수 있어. 추진 노즐에 꼭 필요해.",
                Done = "점화기다! 이걸로 추진 노즐을 만들 수 있어." },
            new QuestSpec { Id = "Q07", Npc = "Weapons", Chapter = 2, Title = "과부하", Kind = QuestKind.KillNamed, Named = "PX", Requires = "Q05",
                Reward = new[] { ("PowerCell", 1) }, Note = "무기상: 부착물 1단계",
                Story = "남동쪽 연료 수송선의 '과부하 P-X'가 동력 장치를 붙들고 있어.\n\n그놈을 쓰러뜨리면 동력 셀을 하나 건질 수 있을 거야. 부착물도 풀어 주지.",
                Done = "동력 셀이군. 오토가 좋아하겠어. 부착물 1단계를 팔아 주지." },
            new QuestSpec { Id = "Q08", Npc = "Weapons", Chapter = 2, Title = "연사 대응", Kind = QuestKind.KillRobot, Robot = "RifleRobot", Count = 8,
                Note = "무기상: 플라즈마 스캐터, 플라즈마 캡슐",
                Story = "AR 로봇은 연사하고 나면 한참 쉬어. 그게 틈이야.\n\n여덟 기를 부수고 와. 샷건을 내줄 만하다고 인정하지.",
                Done = "좋아. 플라즈마 스캐터를 팔아 주지." },
            new QuestSpec { Id = "Q09", Npc = "Armor", Chapter = 2, Title = "두꺼운 장갑", Kind = QuestKind.Deliver,
                Deliver = new[] { ("Alloy", 10) }, Note = "방어구상: 2단계 방탄복·헬멧",
                Story = "2단계 방탄복엔 강화 합금이 많이 들어가.\n\n열 개만 모아 줘.",
                Done = "튼튼한 걸로 만들어 놨어. 2단계 장비를 팔게." },
            new QuestSpec { Id = "Q10", Npc = "BlackMarket", Chapter = 2, Title = "짐꾼", Kind = QuestKind.KillNamed, Named = "K7", Credits = 500, Requires = "Q02",
                Story = "화물선의 '짐꾼 K-7' 알아? 내 물건을 실은 컨테이너를 그놈이 지키고 있어.\n\n치워 주면 넉넉히 쳐 줄게.",
                Done = "깔끔하네. 약속한 돈이야." },
            new QuestSpec { Id = "Q11", Npc = "Medical", Chapter = 3, Title = "생존자 기록", Kind = QuestKind.KillNamed, Named = "N3", Requires = "Q05",
                Note = "의료상: 메디킷",
                Story = "서쪽 이민선에 남은 사람들의 기록을 찾고 싶어요. 그런데 '야간 경비 N-3'가 지키고 있어요.\n\n센티널이 왜 다가오는 배를 모두 떨어뜨리는지, 기록에 남아 있을지도 몰라요.",
                Done = "기록에 적혀 있었어요… 센티널은 연구선이 싣고 온 무언가를 이 행성에 '격리'하려 했대요. 이제 메디킷을 드릴게요." },
            new QuestSpec { Id = "Q12", Npc = "Medical", Chapter = 3, Title = "소독", Kind = QuestKind.KillNamed, Named = "M1",
                Reward = new[] { ("OxygenRecycler", 1) },
                Story = "병원선의 '소독기 M-1'이 아직 소독 명령을 따르고 있어요. 다가가는 건 뭐든 쏴 버려요.\n\n그 안쪽 창고에 산소 재생기가 있어요. 도약 엔진에 꼭 필요해요.",
                Done = "산소 재생기예요. 오토에게 가져가 주세요." },
            new QuestSpec { Id = "Q13", Npc = "Workshop", Chapter = 3, Title = "연구 샘플", Kind = QuestKind.Deliver, Requires = "Q06",
                Deliver = new[] { ("QuantumCore", 3) }, Note = "제작소: 고출력 탄약, 동력 셀",
                Story = "연구선 사람들이 쓰던 퀀텀 코어로 고출력 탄약과 동력 셀을 만들 수 있어.\n\n세 개만 모아 와 줘.",
                Done = "이제 고출력 탄약과 동력 셀을 만들 수 있어." },
            new QuestSpec { Id = "Q14", Npc = "Weapons", Chapter = 3, Title = "감시자", Kind = QuestKind.KillNamed, Named = "R9", Requires = "Q13",
                Reward = new[] { ("NavCore", 1) },
                Story = "연구선의 '감시자 R-9'가 항법 코어를 품고 있어. 센티널이 격리하려던 연구 데이터와 같이 말이야.\n\n항법 코어 없이 우리 배는 어디로도 못 가.",
                Done = "항법 코어다. 데이터는… 지워져 있군. 센티널이 지운 거야." },
            new QuestSpec { Id = "Q15", Npc = "Weapons", Chapter = 3, Title = "정밀 사격", Kind = QuestKind.KillRobot, Robot = "SniperRobot", Count = 6,
                Note = "무기상: 네뷸라-7, 저격 셀, 부착물 2단계",
                Story = "저격 로봇 여섯 기. 조준선을 보고 구르는 법을 익히면 할 수 있어.\n\n해내면 네뷸라-7과 고급 부착물을 내주지.",
                Done = "훌륭해. 네뷸라-7과 부착물 2단계를 팔아 주지." },
            new QuestSpec { Id = "Q16", Npc = "Armor", Chapter = 3, Title = "최고급 우주복", Kind = QuestKind.Deliver,
                Deliver = new[] { ("NanoFiber", 8), ("EnergyCrystal", 2) }, Note = "방어구상: 3단계 방탄복·헬멧",
                Story = "최고급 우주복엔 나노 섬유와 에너지 결정이 들어가.\n\n섬유 여덟, 결정 둘이면 돼.",
                Done = "완성이야. 3단계 장비를 팔게." },
            new QuestSpec { Id = "Q17", Npc = "Weapons", Chapter = 4, Title = "방어망 해제", Kind = QuestKind.KillNamed, Named = "S0", Requires = "Q14",
                Reward = new[] { ("SecurityKey", 1) },
                Story = "궤도의 센티널 방어망을 뚫으려면 해제 코드가 있어야 해. 북동쪽 군 순찰선의 '감시탑 S-0'가 그 코드를 쥐고 있지.\n\n이게 마지막이야. 조심해.",
                Done = "해제 코드다. 이제 센티널도 우리를 못 떨어뜨려." },
            new QuestSpec { Id = "Q18", Npc = "BlackMarket", Chapter = 4, Title = "무덤 청소", Kind = QuestKind.AllNamed, Credits = 1500, Requires = "Q17",
                Story = "네임드 일곱 기를 전부 한 번씩 쓰러뜨려 봐.\n\n이 무덤에서 제일 위험한 놈들이지. 해내면 크게 쳐 줄게.",
                Done = "정말 다 해치웠네. 약속대로야." },
            new QuestSpec { Id = "Q19", Npc = "BlackMarket", Chapter = 4, Title = "귀중품", Kind = QuestKind.Deliver,
                Deliver = new[] { ("QuantumCore", 5) }, Reward = new[] { ("EnergyCrystal", 5) },
                Story = "퀀텀 코어 다섯 개. 떠나기 전에 한몫 챙겨야지.\n\n대신 에너지 결정을 넉넉히 줄게.",
                Done = "거래 성립. 결정 받아." },
            new QuestSpec { Id = "Q20", Npc = "Workshop", Chapter = 4, Title = "귀환", Kind = QuestKind.RepairPod, Requires = "Q17",
                Note = "엔딩",
                Story = "부품만 다 모이면 우리 배는 날 수 있어.\n\n선체 패널 여섯, 동력 셀 셋, 추진 노즐, 산소 재생기, 항법 코어, 방어망 해제 코드. 도약 엔진에 설치하고 출발하자.",
                Done = "" },
        };

        private static void EnsureQuestDatabase()
        {
            if (AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestDatabasePath) != null)
            {
                return;
            }

            var database = ScriptableObject.CreateInstance<QuestDatabase>();
            AssetDatabase.CreateAsset(database, QuestDatabasePath);
            var so = new SerializedObject(database);
            SerializedProperty list = so.FindProperty("_quests");
            QuestSpec[] specs = QuestSpecs;
            list.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                QuestSpec spec = specs[i];
                SerializedProperty quest = list.GetArrayElementAtIndex(i);
                quest.FindPropertyRelative("_id").stringValue = spec.Id;
                quest.FindPropertyRelative("_title").stringValue = spec.Title;
                quest.FindPropertyRelative("_npc").stringValue = spec.Npc;
                quest.FindPropertyRelative("_chapter").intValue = spec.Chapter;
                quest.FindPropertyRelative("_story").stringValue = spec.Story;
                quest.FindPropertyRelative("_doneLine").stringValue = spec.Done ?? string.Empty;
                quest.FindPropertyRelative("_requires").stringValue = spec.Requires ?? string.Empty;
                quest.FindPropertyRelative("_kind").enumValueIndex = (int)spec.Kind;
                quest.FindPropertyRelative("_count").intValue = spec.Count > 0 ? spec.Count : 1;
                quest.FindPropertyRelative("_robot").objectReferenceValue =
                    spec.Robot != null ? AssetDatabase.LoadAssetAtPath<RobotData>(RobotDataPath(spec.Robot)) : null;
                quest.FindPropertyRelative("_namedId").stringValue = spec.Named ?? string.Empty;
                quest.FindPropertyRelative("_zoneId").stringValue = spec.Zone ?? string.Empty;
                quest.FindPropertyRelative("_credits").intValue = spec.Credits;
                quest.FindPropertyRelative("_rewardNote").stringValue = spec.Note ?? string.Empty;
                FillCosts(quest.FindPropertyRelative("_deliver"), spec.Deliver);
                FillCosts(quest.FindPropertyRelative("_rewardItems"), spec.Reward);
            }

            FillCosts(so.FindProperty("_podParts"), new[]
            {
                ("HullPanel", 6), ("PowerCell", 3), ("Thruster", 1), ("OxygenRecycler", 1), ("NavCore", 1), ("SecurityKey", 1),
            });

            SerializedProperty named = so.FindProperty("_allNamed");
            named.arraySize = NamedRobots.Length;
            for (int i = 0; i < NamedRobots.Length; i++)
            {
                named.GetArrayElementAtIndex(i).stringValue = NamedRobots[i].Id;
            }

            so.FindProperty("_prologue").stringValue = Prologue;
            so.FindProperty("_ending").stringValue = Ending;
            so.FindProperty("_credits").stringValue = Credits;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 도약 엔진은 상인 창의 "수리" 탭으로 연다 (상인과 같은 방식으로 말을 건다).
            EnsureMerchant("LongPod", LongPodName, "수리", Color.white, LongPodGreeting, _ => { });
        }

        private static void FillCosts(SerializedProperty list, (string item, int count)[] costs)
        {
            costs = costs ?? new (string, int)[0];
            list.arraySize = costs.Length;
            for (int i = 0; i < costs.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("_item").objectReferenceValue = LoadItem(costs[i].item);
                list.GetArrayElementAtIndex(i).FindPropertyRelative("_count").intValue = costs[i].count;
            }
        }

        // ---------- 대기실 ----------

        private static void EnsureLobbyQuests(GameObject player, GameObject canvasObject, int wallLayer)
        {
            GameObject pod = GameObject.Find("LongRangePod");
            if (pod == null)
            {
                pod = new GameObject("LongRangePod");
                pod.transform.position = LongPodPosition;
                pod.layer = wallLayer;
                var renderer = pod.AddComponent<SpriteRenderer>();
                renderer.sprite = EnsurePointArt($"{MapArtFolder}/LongRangePod.png", PodPortArt, new Color32(80, 220, 255, 255));
                renderer.sortingOrder = -2;
                pod.transform.localScale = new Vector3(3.2f, 3.2f, 1f);
                pod.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.55f);
                var npc = pod.AddComponent<Npc>();
                var npcSo = new SerializedObject(npc);
                npcSo.FindProperty("_merchant").objectReferenceValue = LoadMerchant("LongPod");
                npcSo.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject questObject = GameObject.Find("Quests");
            if (questObject == null)
            {
                questObject = new GameObject("Quests");
            }

            EnsureComponent(questObject, out QuestLog log);
            SetReferenceIfEmpty(log, "_database", AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestDatabasePath));

            EnsureComponent(canvasObject, out StoryScreenUI story);
            EnsureComponent(canvasObject, out QuestUI questUi);
            SetReferenceIfEmpty(questUi, "_merchant", canvasObject.GetComponent<MerchantUI>());
            SetReferenceIfEmpty(questUi, "_log", log);
            SetReferenceIfEmpty(questUi, "_story", story);
        }

        // ---------- 레이드 ----------

        private static void EnsureRaidQuests(GameObject player, GameObject canvasObject)
        {
            GameObject questObject = GameObject.Find("Quests");
            if (questObject == null)
            {
                questObject = new GameObject("Quests");
            }

            EnsureComponent(questObject, out QuestLog log);
            SetReferenceIfEmpty(log, "_database", AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestDatabasePath));
            SetReferenceIfEmpty(log, "_player", player.transform);
            EnsureComponent(canvasObject, out QuestTrackerUI _);
        }
    }
}
