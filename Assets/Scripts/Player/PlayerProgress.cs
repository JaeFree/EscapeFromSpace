using System;
using UnityEngine;

namespace EscapeFromSpace
{
    public enum TechBlock
    {
        None,
        AlreadyUnlocked,
        Prerequisite,
        Level,
        Points,
        Materials,
    }

    // 플레이어의 레벨, 경험치, 강화한 노드 (기획서 12장). 값은 저장 내용(SaveData)에 그대로 들어 있고 여기서는 읽고 바꾸기만 한다.
    // 그래서 씬이 바뀌거나 죽어도 남는다. 레이드 씬을 바로 Play했을 때는 저장하지 않는 임시 값을 쓴다.
    public class PlayerProgress : MonoBehaviour
    {
        [SerializeField] private TechTree _tree;

        private readonly float[] _bonus = new float[Enum.GetValues(typeof(TechStat)).Length];
        private SaveData _fallback;
        private SaveData _bonusSource;
        private int _bonusCount = -1;

        // 씬마다 플레이어는 하나다. 로봇이나 화면처럼 플레이어를 직접 모르는 쪽이 여기로 찾는다.
        public static PlayerProgress Current { get; private set; }

        // 경험치, 레벨, 강화가 바뀌면 알린다.
        public event Action Changed;

        public TechTree Tree => _tree;
        public int Level => Mathf.Clamp(State.Level, 1, _tree.MaxLevel);
        public int Xp => State.Xp;
        public int XpToNext => _tree.XpToNext(Level);
        public bool IsMaxLevel => Level >= _tree.MaxLevel;
        public int Points => State.TechPoints;

        // 이 씬에 들어온 뒤 얻은 경험치. 결과 화면에 쓴다.
        public int XpGained { get; private set; }

        // 이 씬에 들어왔을 때의 레벨과 경험치. 결과 화면이 여기서부터 바를 채워 올린다.
        public int StartLevel { get; private set; } = 1;
        public int StartXp { get; private set; }

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

        // 에디터에서 도메인을 다시 불러오지 않는 설정이면 앞 판의 값이 남는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
        }

        private void Awake()
        {
            Current = this;
            StartLevel = Level;
            StartXp = Xp;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        // 강화한 노드들이 그 능력치에 더하는 값의 합
        public float Bonus(TechStat stat)
        {
            SaveData state = State;
            if (_bonusSource != state || _bonusCount != state.UnlockedTech.Count)
            {
                Array.Clear(_bonus, 0, _bonus.Length);
                foreach (string id in state.UnlockedTech)
                {
                    TechTree.Node node = _tree.Find(id);
                    if (node != null)
                    {
                        _bonus[(int)node.Stat] += node.Amount;
                    }
                }

                _bonusSource = state;
                _bonusCount = state.UnlockedTech.Count;
            }

            return _bonus[(int)stat];
        }

        // %로 줄이는 능력치(재장전, 구르기 쿨타임, 뒤지는 시간)에 곱하는 값. 아무리 올려도 0이 되지는 않게 한다.
        public float TimeMultiplier(TechStat stat)
        {
            return Mathf.Max(0.1f, 1f - Bonus(stat) / 100f);
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SaveData state = State;
            XpGained += amount;

            bool leveled = false;
            state.Level = Level;
            state.Xp += amount;
            while (state.Level < _tree.MaxLevel && state.Xp >= _tree.XpToNext(state.Level))
            {
                state.Xp -= _tree.XpToNext(state.Level);
                state.Level++;
                state.TechPoints += _tree.PointsPerLevel;
                leveled = true;
            }

            // 최고 레벨에서는 경험치가 더 쌓이지 않는다.
            if (state.Level >= _tree.MaxLevel)
            {
                state.Xp = 0;
            }

            if (leveled)
            {
                Sfx.Play(SfxId.LevelUp);
            }

            Changed?.Invoke();
        }

        public bool IsUnlocked(string id)
        {
            return State.UnlockedTech.Contains(id);
        }

        // 선행 노드를 모두 강화했는지
        public bool HasPrerequisites(TechTree.Node node)
        {
            if (node.Requires != null)
            {
                foreach (string id in node.Requires)
                {
                    if (!IsUnlocked(id))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // 강화할 수 없는 첫 번째 이유. countItem은 그 재료를 지금 몇 개 가졌는지 돌려준다.
        public TechBlock CheckUnlock(TechTree.Node node, Func<ItemData, int> countItem)
        {
            if (IsUnlocked(node.Id))
            {
                return TechBlock.AlreadyUnlocked;
            }

            if (!HasPrerequisites(node))
            {
                return TechBlock.Prerequisite;
            }

            if (Level < node.RequiredLevel)
            {
                return TechBlock.Level;
            }

            if (Points < node.PointCost)
            {
                return TechBlock.Points;
            }

            if (node.Costs != null)
            {
                foreach (TechTree.Cost cost in node.Costs)
                {
                    if (cost.Item != null && countItem(cost.Item) < cost.Count)
                    {
                        return TechBlock.Materials;
                    }
                }
            }

            return TechBlock.None;
        }

        // 포인트를 쓰고 노드를 강화한다. 재료는 부르는 쪽이 가방과 창고에서 뺀다.
        public void Unlock(TechTree.Node node)
        {
            SaveData state = State;
            state.TechPoints -= node.PointCost;
            state.UnlockedTech.Add(node.Id);
            Changed?.Invoke();
        }
    }
}
