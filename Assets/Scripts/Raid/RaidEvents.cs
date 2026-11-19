using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    public enum RaidEventKind
    {
        None,
        Blackout,
        SecurityAlert,
        AlienInvasion,
    }

    // 이번 판의 우주선 이변 (기획서 19.4). 대기실의 출격 창에서 뽑아 보여 주고, 레이드 씬이 이어받는다.
    public sealed class RaidEventPlan
    {
        public RaidEventKind Kind;
        public readonly List<string> ShipIds = new List<string>();
        public readonly List<string> ShipNames = new List<string>();

        public bool Affects(string zoneId) => Kind != RaidEventKind.None && zoneId != null && ShipIds.Contains(zoneId);

        public string KindName
        {
            get
            {
                switch (Kind)
                {
                    case RaidEventKind.Blackout: return "정전";
                    case RaidEventKind.SecurityAlert: return "보안 경계";
                    case RaidEventKind.AlienInvasion: return "에일리언 습격";
                    default: return string.Empty;
                }
            }
        }

        // 출격 창에 쓰는 한 줄
        public string Describe(float lootBonus)
        {
            if (Kind == RaidEventKind.None || ShipNames.Count == 0)
            {
                return "이번 판: 이변 없음";
            }

            return $"이번 판: {string.Join("·", ShipNames)} {KindName} (아이템 +{lootBonus * 100f:0}%)";
        }
    }

    public static class RaidEvents
    {
        // 대기실이 뽑아 둔 다음 판의 이변. 레이드 씬이 가져가면 비운다.
        public static RaidEventPlan Next { get; set; }

        // 지금 레이드의 이변. 로봇과 컨테이너가 읽는다.
        public static RaidEventPlan Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Next = null;
            Current = null;
        }

        public static RaidEventPlan Roll(RaidConfig config)
        {
            var plan = new RaidEventPlan();
            if (config == null)
            {
                return plan;
            }

            float total = config.NoEventWeight + config.BlackoutWeight + config.SecurityAlertWeight + config.AlienInvasionWeight;
            float roll = Random.value * total;
            if ((roll -= config.NoEventWeight) < 0f)
            {
                return plan;
            }

            plan.Kind = (roll -= config.BlackoutWeight) < 0f ? RaidEventKind.Blackout
                : (roll -= config.SecurityAlertWeight) < 0f ? RaidEventKind.SecurityAlert
                : RaidEventKind.AlienInvasion;

            var candidates = new List<int>();
            for (int i = 0; i < config.EventShipIds.Length; i++)
            {
                bool alienShip = System.Array.IndexOf(config.AlienShipIds, config.EventShipIds[i]) >= 0;
                if (plan.Kind != RaidEventKind.AlienInvasion || !alienShip)
                {
                    candidates.Add(i);
                }
            }

            for (int n = 0; n < config.EventShipCount && candidates.Count > 0; n++)
            {
                int pick = Random.Range(0, candidates.Count);
                int index = candidates[pick];
                candidates.RemoveAt(pick);
                plan.ShipIds.Add(config.EventShipIds[index]);
                plan.ShipNames.Add(index < config.EventShipNames.Length ? config.EventShipNames[index] : config.EventShipIds[index]);
            }

            if (plan.ShipIds.Count == 0)
            {
                plan.Kind = RaidEventKind.None;
            }

            return plan;
        }

        public static bool IsBlackout(string zoneId) => Current != null && Current.Kind == RaidEventKind.Blackout && Current.Affects(zoneId);

        public static bool IsAlert(string zoneId) => Current != null && Current.Kind == RaidEventKind.SecurityAlert && Current.Affects(zoneId);
    }
}
