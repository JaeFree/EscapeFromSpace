using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 대기실 상인 창의 퀘스트 탭과 장거리 탈출 포드의 수리 탭 (기획서 14.3~14.5).
    // 줄은 MerchantUI가 그리고, 여기서는 줄의 내용과 버튼이 하는 일을 정한다.
    public class QuestUI : MonoBehaviour
    {
        private const string OkColor = "#7CF08F";
        private const string LackColor = "#FF7A6B";
        private const string DoneColor = "#B9AFC9";

        [SerializeField] private MerchantUI _merchant;
        [SerializeField] private QuestLog _log;
        [SerializeField] private StoryScreenUI _story;

        private QuestDatabase Database => _log.Database;
        private static SaveData Data => GameSession.Data;

        private void Start()
        {
            _merchant.QuestRows = BuildQuestRows;
            _merchant.HasQuests = npc => FindQuests(npc).Count > 0;
            _merchant.QuestGreeting = Greeting;
            _merchant.RepairRows = BuildRepairRows;

            // 처음 시작하면 프롤로그를 보여 준다 (기획서 14.1).
            if (Data != null && !Data.SeenPrologue)
            {
                _story.Show(Database.Prologue, null, Database.PrologueShots, () =>
                {
                    Data.SeenPrologue = true;
                    _merchant.Inventory.NotifyChanged();
                });
            }
        }

        private List<QuestDatabase.Quest> FindQuests(Npc npc)
        {
            var list = new List<QuestDatabase.Quest>();
            foreach (QuestDatabase.Quest quest in Database.Quests)
            {
                if (npc.Merchant != null && quest.Npc == npc.Merchant.Id)
                {
                    list.Add(quest);
                }
            }

            return list;
        }

        // 지금 진행 중인 퀘스트의 이야기. 없으면 다음 퀘스트를 기다리라는 말.
        private string Greeting(Npc npc)
        {
            foreach (QuestDatabase.Quest quest in FindQuests(npc))
            {
                if (_log.IsAvailable(quest))
                {
                    return $"<color=#FFBC4C>{quest.Chapter}장 · {quest.Title}</color>\n\n{quest.Story}";
                }
            }

            foreach (QuestDatabase.Quest quest in FindQuests(npc))
            {
                if (!_log.IsDone(quest.Id))
                {
                    return "지금은 부탁할 일이 없어. 다른 사람들 일을 먼저 도와줘.";
                }
            }

            return "덕분에 살았어. 고마워.";
        }

        private void BuildQuestRows(Npc npc, List<MerchantUI.Row> rows)
        {
            LobbyStock stock = _merchant.Stock;
            foreach (QuestDatabase.Quest quest in FindQuests(npc))
            {
                bool done = _log.IsDone(quest.Id);
                bool available = _log.IsAvailable(quest);
                if (!done && !available)
                {
                    // 아직 열리지 않은 퀘스트는 이름을 감춘다.
                    rows.Add(new MerchantUI.Row
                    {
                        Title = $"<color={DoneColor}>???</color>",
                        Detail = $"{quest.Chapter}장 · 앞의 일을 끝내면 열린다",
                        Price = string.Empty,
                        Button = string.Empty,
                    });
                    continue;
                }

                QuestDatabase.Quest captured = quest;
                bool complete = available && _log.IsComplete(quest, stock);
                string reward = Reward(quest);
                rows.Add(new MerchantUI.Row
                {
                    Title = done ? $"<color={DoneColor}>{quest.Title}</color>" : quest.Title,
                    Detail = done
                        ? $"<color={DoneColor}>완료 · {reward}</color>"
                        : $"<color={(complete ? OkColor : LackColor)}>{_log.Progress(quest, stock)}</color> · 보상 {reward}",
                    Price = string.Empty,
                    Button = done ? "완료됨" : quest.Kind == QuestKind.RepairPod ? "엔진에서" : "완료",
                    Enabled = complete,
                    OnClick = () => Complete(captured),
                });
            }
        }

        private void Complete(QuestDatabase.Quest quest)
        {
            if (_log.TryComplete(quest, _merchant.Stock, out string message))
            {
                Sfx.Play(SfxId.LevelUp);
                _merchant.ShowMessage($"<color={OkColor}>완료!</color> {message}");
            }
            else
            {
                Sfx.Play(SfxId.ItemRefuse);
                _merchant.ShowMessage($"<color={LackColor}>{message}</color>");
            }
        }

        private static string Reward(QuestDatabase.Quest quest)
        {
            var parts = new List<string>();
            if (quest.Credits > 0)
            {
                parts.Add($"{quest.Credits:N0} 크레딧");
            }

            foreach (TechTree.Cost item in quest.RewardItems)
            {
                if (item.Item != null)
                {
                    parts.Add(item.Count > 1 ? $"{item.Item.DisplayName} {item.Count}" : item.Item.DisplayName);
                }
            }

            if (!string.IsNullOrEmpty(quest.RewardNote))
            {
                parts.Add(quest.RewardNote);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }

        // ---------- 장거리 탈출 포드 수리 ----------

        public static int InstalledCount(ItemData part)
        {
            int count = 0;
            foreach (string id in Data.InstalledParts)
            {
                if (id == part.name)
                {
                    count++;
                }
            }

            return count;
        }

        public bool IsPodRepaired()
        {
            foreach (TechTree.Cost part in Database.PodParts)
            {
                if (part.Item != null && InstalledCount(part.Item) < part.Count)
                {
                    return false;
                }
            }

            return true;
        }

        private void BuildRepairRows(Npc npc, List<MerchantUI.Row> rows)
        {
            LobbyStock stock = _merchant.Stock;
            foreach (TechTree.Cost part in Database.PodParts)
            {
                if (part.Item == null)
                {
                    continue;
                }

                TechTree.Cost captured = part;
                int installed = InstalledCount(part.Item);
                int owned = stock.Count(part.Item);
                bool full = installed >= part.Count;
                rows.Add(new MerchantUI.Row
                {
                    Icon = part.Item.Icon,
                    Title = $"{part.Item.DisplayName}  <color={(full ? OkColor : LackColor)}>{installed} / {part.Count}</color>",
                    Detail = full ? "설치 완료" : $"가진 것 {owned}개 · 설치하면 되돌릴 수 없다",
                    Price = string.Empty,
                    Button = full ? "완료" : "설치",
                    Enabled = !full && owned > 0,
                    OnClick = () => Install(captured),
                });
            }

            bool repaired = IsPodRepaired();
            rows.Add(new MerchantUI.Row
            {
                Title = repaired ? "<color=#7CF08F>수리 완료</color>" : "출발",
                Detail = repaired ? "모두 태우고 케르-7을 떠난다" : "부품을 모두 설치하면 출발할 수 있다",
                Price = string.Empty,
                Button = "출발",
                Enabled = repaired,
                OnClick = Launch,
            });
        }

        private void Install(TechTree.Cost part)
        {
            int need = part.Count - InstalledCount(part.Item);
            int amount = Mathf.Min(need, _merchant.Stock.Count(part.Item));
            if (amount <= 0)
            {
                return;
            }

            _merchant.Stock.Consume(part.Item, amount);
            for (int i = 0; i < amount; i++)
            {
                Data.InstalledParts.Add(part.Item.name);
            }

            Sfx.Play(SfxId.ReloadEnd);
            _merchant.ShowMessage($"{part.Item.DisplayName} {amount}개를 설치했습니다.");
        }

        // 엔딩 (기획서 14.1): 엔딩 글과 만든 사람을 보여 준 뒤 대기실로 돌아와 계속한다.
        private void Launch()
        {
            if (!IsPodRepaired())
            {
                return;
            }

            _log.Complete("Q20");
            Data.SeenEnding = true;
            _merchant.Inventory.NotifyChanged();
            _merchant.CloseWindow();
            _story.Show(Database.Ending, Database.CreditsText, Database.EndingShots, null);
        }
    }
}
