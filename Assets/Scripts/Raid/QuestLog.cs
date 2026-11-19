using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 퀘스트 진행 (기획서 14.3). 진행 내용은 저장 파일(SaveData)에 그대로 두고 여기서는 읽고 바꾸기만 한다.
    // 대기실과 레이드 씬에 하나씩 두고, 레이드에서는 처치·방문을 세고 대기실에서는 완료와 보상을 맡는다.
    public class QuestLog : MonoBehaviour
    {
        [SerializeField] private QuestDatabase _database;
        [SerializeField, Tooltip("레이드에서 방문한 구역을 재는 대상 (플레이어)")] private Transform _player;

        private readonly HashSet<string> _visitedZones = new HashSet<string>();
        private SaveData _fallback;

        public static QuestLog Current { get; private set; }

        public QuestDatabase Database => _database;

        private SaveData State
        {
            get
            {
                if (GameSession.Data != null)
                {
                    return GameSession.Data;
                }

                if (_fallback == null)
                {
                    _fallback = new SaveData();
                }

                return _fallback;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
        }

        private void Awake()
        {
            Current = this;
            MerchantUI.QuestTitle = id =>
            {
                QuestDatabase.Quest quest = _database.Find(id);
                return quest != null ? quest.Title : id;
            };
        }

        private void OnEnable()
        {
            Robot.AnyDestroyed += OnRobotDestroyed;
        }

        private void OnDisable()
        {
            Robot.AnyDestroyed -= OnRobotDestroyed;
            if (Current == this)
            {
                Current = null;
            }
        }

        private void Update()
        {
            // 레이드 씬에만 구역이 있다. 들어간 구역을 기억했다가 탈출할 때 방문 퀘스트에 센다.
            if (_player != null && ZoneArea.Zones.Count > 0)
            {
                ZoneArea zone = ZoneArea.At(_player.position);
                if (zone != null)
                {
                    _visitedZones.Add(zone.Id);
                }
            }
        }

        // ---------- 상태 ----------

        public bool IsDone(string id)
        {
            return State.CompletedQuests.Contains(id);
        }

        // 같은 상인의 앞 퀘스트와 따로 정한 선행 퀘스트를 모두 끝냈고, 아직 끝내지 않은 것.
        public bool IsAvailable(QuestDatabase.Quest quest)
        {
            if (IsDone(quest.Id))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(quest.Requires) && !IsDone(quest.Requires))
            {
                return false;
            }

            QuestDatabase.Quest previous = null;
            foreach (QuestDatabase.Quest other in _database.Quests)
            {
                if (other == quest)
                {
                    break;
                }

                if (other.Npc == quest.Npc)
                {
                    previous = other;
                }
            }

            return previous == null || IsDone(previous.Id);
        }

        public List<QuestDatabase.Quest> Active()
        {
            var list = new List<QuestDatabase.Quest>();
            foreach (QuestDatabase.Quest quest in _database.Quests)
            {
                if (IsAvailable(quest))
                {
                    list.Add(quest);
                }
            }

            return list;
        }

        public int GetCount(string id)
        {
            int index = State.QuestIds.IndexOf(id);
            return index >= 0 ? State.QuestCounts[index] : 0;
        }

        private void AddCount(string id, int amount, int max)
        {
            SaveData state = State;
            int index = state.QuestIds.IndexOf(id);
            if (index < 0)
            {
                state.QuestIds.Add(id);
                state.QuestCounts.Add(0);
                index = state.QuestIds.Count - 1;
            }

            state.QuestCounts[index] = Mathf.Min(max, state.QuestCounts[index] + amount);
        }

        public int NamedKilledCount()
        {
            int count = 0;
            foreach (string id in _database.AllNamed)
            {
                if (State.KilledNamed.Contains(id))
                {
                    count++;
                }
            }

            return count;
        }

        // 조건을 다 채웠는지. 납품은 지금 가진 것으로 본다 (stock이 없으면 레이드 중이라 확인하지 않는다).
        public bool IsComplete(QuestDatabase.Quest quest, LobbyStock stock)
        {
            switch (quest.Kind)
            {
                case QuestKind.AllNamed:
                    return NamedKilledCount() >= _database.AllNamed.Count;
                case QuestKind.Deliver:
                    return stock != null && stock.Has(quest.Deliver);
                case QuestKind.RepairPod:
                    return false;
                default:
                    return GetCount(quest.Id) >= quest.Count;
            }
        }

        // 진행도 글 (예: "3 / 5"). 화면이 쓴다.
        public string Progress(QuestDatabase.Quest quest, LobbyStock stock)
        {
            switch (quest.Kind)
            {
                case QuestKind.AllNamed:
                    return $"{NamedKilledCount()} / {_database.AllNamed.Count}";
                case QuestKind.Deliver:
                    var text = new System.Text.StringBuilder();
                    foreach (TechTree.Cost cost in quest.Deliver)
                    {
                        if (cost.Item == null)
                        {
                            continue;
                        }

                        if (text.Length > 0)
                        {
                            text.Append("  ");
                        }

                        int owned = stock != null ? stock.Count(cost.Item) : 0;
                        text.Append($"{cost.Item.DisplayName} {owned}/{cost.Count}");
                    }

                    return text.ToString();
                case QuestKind.RepairPod:
                    return "도약 엔진에서 출발";
                case QuestKind.Visit:
                    return GetCount(quest.Id) > 0 ? "완료" : "방문 후 탈출";
                default:
                    return $"{GetCount(quest.Id)} / {quest.Count}";
            }
        }

        // ---------- 세기 ----------

        private void OnRobotDestroyed(RobotData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.IsNamed && !string.IsNullOrEmpty(data.NamedId) && !State.KilledNamed.Contains(data.NamedId))
            {
                State.KilledNamed.Add(data.NamedId);
            }

            foreach (QuestDatabase.Quest quest in Active())
            {
                bool counts = quest.Kind == QuestKind.KillAny
                    || (quest.Kind == QuestKind.KillRobot && quest.Robot == data)
                    || (quest.Kind == QuestKind.KillNamed && data.IsNamed && data.NamedId == quest.NamedId);
                if (counts)
                {
                    AddCount(quest.Id, 1, quest.Count);
                }
            }
        }

        // 탈출에 성공했을 때 RaidManager가 부른다. 저장하기 전에 불러야 한다.
        public void OnEscaped()
        {
            foreach (QuestDatabase.Quest quest in Active())
            {
                if (quest.Kind == QuestKind.Escape)
                {
                    AddCount(quest.Id, 1, quest.Count);
                }
                else if (quest.Kind == QuestKind.Visit && _visitedZones.Contains(quest.ZoneId))
                {
                    AddCount(quest.Id, 1, 1);
                }
            }
        }

        // ---------- 완료 ----------

        // 납품할 것을 빼고 보상을 창고에 넣는다. 창고에 자리가 없으면 아무것도 바꾸지 않고 false.
        public bool TryComplete(QuestDatabase.Quest quest, LobbyStock stock, out string message)
        {
            message = string.Empty;
            if (!IsAvailable(quest) || !IsComplete(quest, stock))
            {
                return false;
            }

            if (quest.Kind == QuestKind.Deliver)
            {
                stock.Consume(quest.Deliver);
            }

            foreach (TechTree.Cost reward in quest.RewardItems)
            {
                if (reward.Item != null && !stock.CanAddToStorage(reward.Item, reward.Count))
                {
                    // 납품한 것을 돌려준다.
                    if (quest.Kind == QuestKind.Deliver)
                    {
                        foreach (TechTree.Cost cost in quest.Deliver)
                        {
                            stock.AddToStorage(cost.Item, cost.Count);
                        }
                    }

                    message = "창고에 보상을 넣을 자리가 없습니다.";
                    return false;
                }
            }

            foreach (TechTree.Cost reward in quest.RewardItems)
            {
                if (reward.Item != null)
                {
                    stock.AddToStorage(reward.Item, reward.Count);
                }
            }

            Complete(quest.Id);
            State.Credits += quest.Credits;

            // 보관함 확장과 로봇 개 (기획서 19.5)
            if (quest.SecureBoxSize.x > 0 && quest.SecureBoxSize.y > 0)
            {
                stock.Inventory.ResizeSecureBox(quest.SecureBoxSize.x, quest.SecureBoxSize.y);
            }

            if (quest.UnlocksDog)
            {
                stock.Inventory.EnableDog();
            }

            message = quest.DoneLine;
            return true;
        }

        public void Complete(string id)
        {
            SaveData state = State;
            if (!state.CompletedQuests.Contains(id))
            {
                state.CompletedQuests.Add(id);
            }

            int index = state.QuestIds.IndexOf(id);
            if (index >= 0)
            {
                state.QuestIds.RemoveAt(index);
                state.QuestCounts.RemoveAt(index);
            }
        }
    }
}
