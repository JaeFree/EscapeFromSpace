using UnityEngine;

namespace EscapeFromSpace
{
    // 씬이 바뀌어도 이어지는 진행 상태. 대기실에서 고른 장비를 레이드 씬으로, 레이드 결과를 대기실로 넘긴다.
    public static class GameSession
    {
        public static SaveData Data { get; private set; }

        // 대기실 포탈로 들어온 레이드인지. 에디터에서 레이드 씬을 바로 Play하면 false이고, 그때는 저장하지 않는다.
        public static bool RaidFromLobby { get; private set; }

        // 에디터에서 도메인을 다시 불러오지 않는 설정이면 static 값이 앞 Play에서 넘어온다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Data = null;
            RaidFromLobby = false;
        }

        // 대기실에 들어올 때 부른다. 저장 파일이 없었으면(처음 시작) isNew가 true다.
        public static SaveData EnterLobby(out bool isNew)
        {
            RaidFromLobby = false;
            Data = SaveSystem.Load();
            isNew = Data == null;
            if (isNew)
            {
                Data = new SaveData();
            }
            else if (Data.InRaid)
            {
                // 레이드 중에 게임을 껐다. 사망과 같이 보관함만 남긴다.
                LoseCarriedItems(Data);
                Data.InRaid = false;
                SaveSystem.Save(Data);
            }

            return Data;
        }

        public static void StartRaid()
        {
            Data.InRaid = true;
            SaveSystem.Save(Data);
            RaidFromLobby = true;
        }

        // 가진 것은 부르기 전에 Data에 담아 둔다. 죽었으면 여기서 보관함만 남긴다.
        public static void EndRaid(bool escaped)
        {
            if (!escaped)
            {
                LoseCarriedItems(Data);
            }

            Data.InRaid = false;
            SaveSystem.Save(Data);
        }

        private static void LoseCarriedItems(SaveData data)
        {
            data.Equipped.Clear();
            data.Bag.Clear();
        }
    }
}
