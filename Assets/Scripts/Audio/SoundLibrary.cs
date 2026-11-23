using UnityEngine;

namespace EscapeFromSpace
{
    // 코드에서 효과음을 가리키는 이름. 어떤 파일이 나는지는 SoundLibrary 에셋에서 정한다.
    public enum SfxId
    {
        Footstep,
        Roll,
        ReloadStart,
        ReloadEnd,
        PlayerHurt,
        PlayerDeath,
        ItemUseStart,
        HealDone,
        OxygenCan,
        OxygenWarning,
        WallImpact,
        RobotHit,
        RobotDestroyed,
        RobotAlert,
        RobotCharge,
        RobotAimLock,
        LootSearch,
        LootOpen,
        BagOpen,
        BagClose,
        ItemPick,
        ItemPlace,
        ItemRefuse,
        ItemDropToWorld,
        PodOpen,
        EscapeSuccess,
        UiPrompt,
        UiConfirm,
        UiCancel,
        MapOpen,
        MapClose,
        PortalLaunch,
        LevelUp,

        // 기획서 20장 (2026-11-23)
        Heartbeat,
        KillConfirm,
        SentinelAlarm,
    }

    // 역할별 효과음 목록 (2026-11-17 사용자 결정: 역할에 맞는 사운드를 넣는다).
    // 무기와 로봇의 발사음은 종류마다 달라서 WeaponData와 RobotData에 따로 둔다.
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "EscapeFromSpace/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("전체")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.8f;
        [SerializeField, Tooltip("칸. 이 거리까지는 원래 크기로 들린다")] private float _fullVolumeDistance = 4f;
        [SerializeField, Tooltip("칸. 이보다 먼 소리는 들리지 않는다")] private float _hearingDistance = 22f;

        [Header("플레이어")]
        [SerializeField] private SoundCue _footstep = new SoundCue();
        [SerializeField] private SoundCue _roll = new SoundCue();
        [SerializeField] private SoundCue _reloadStart = new SoundCue();
        [SerializeField] private SoundCue _reloadEnd = new SoundCue();
        [SerializeField] private SoundCue _playerHurt = new SoundCue();
        [SerializeField] private SoundCue _playerDeath = new SoundCue();
        [SerializeField, Tooltip("메디젤·메디킷을 쓰기 시작할 때")] private SoundCue _itemUseStart = new SoundCue();
        [SerializeField, Tooltip("회복이 끝났을 때")] private SoundCue _healDone = new SoundCue();
        [SerializeField] private SoundCue _oxygenCan = new SoundCue();
        [SerializeField, Tooltip("산소가 얼마 안 남았을 때 되풀이되는 경고음")] private SoundCue _oxygenWarning = new SoundCue();

        [Header("전투")]
        [SerializeField, Tooltip("탄이 벽이나 컨테이너에 맞았을 때")] private SoundCue _wallImpact = new SoundCue();
        [SerializeField] private SoundCue _robotHit = new SoundCue();
        [SerializeField] private SoundCue _robotDestroyed = new SoundCue();
        [SerializeField, Tooltip("로봇이 플레이어를 발견했을 때")] private SoundCue _robotAlert = new SoundCue();
        [SerializeField, Tooltip("샷건 충전과 저격 조준을 시작할 때")] private SoundCue _robotCharge = new SoundCue();
        [SerializeField, Tooltip("저격 조준선이 고정될 때. 이 소리 뒤에 피해야 한다")] private SoundCue _robotAimLock = new SoundCue();

        [Header("가방과 루팅")]
        [SerializeField, Tooltip("E를 누르고 뒤지는 동안 되풀이된다")] private SoundCue _lootSearch = new SoundCue();
        [SerializeField] private SoundCue _lootOpen = new SoundCue();
        [SerializeField] private SoundCue _bagOpen = new SoundCue();
        [SerializeField] private SoundCue _bagClose = new SoundCue();
        [SerializeField] private SoundCue _itemPick = new SoundCue();
        [SerializeField] private SoundCue _itemPlace = new SoundCue();
        [SerializeField, Tooltip("놓을 수 없는 자리에 놓았을 때")] private SoundCue _itemRefuse = new SoundCue();
        [SerializeField] private SoundCue _itemDropToWorld = new SoundCue();

        [Header("레이드와 화면")]
        [SerializeField] private SoundCue _podOpen = new SoundCue();
        [SerializeField] private SoundCue _escapeSuccess = new SoundCue();
        [SerializeField, Tooltip("출격 확인 창이 뜰 때")] private SoundCue _uiPrompt = new SoundCue();
        [SerializeField] private SoundCue _uiConfirm = new SoundCue();
        [SerializeField] private SoundCue _uiCancel = new SoundCue();
        [SerializeField] private SoundCue _mapOpen = new SoundCue();
        [SerializeField] private SoundCue _mapClose = new SoundCue();
        [SerializeField] private SoundCue _portalLaunch = new SoundCue();
        [SerializeField] private SoundCue _levelUp = new SoundCue();

        [Header("긴장과 손맛 (기획서 20장)")]
        [SerializeField, Tooltip("HP가 낮을 때 되풀이되는 심장 소리")] private SoundCue _heartbeat = new SoundCue();
        [SerializeField, Tooltip("플레이어가 로봇을 부순 순간의 확인음 (폭발음과 따로)")] private SoundCue _killConfirm = new SoundCue();
        [SerializeField, Tooltip("센티널 수색이 시작되거나 증원이 내려올 때")] private SoundCue _sentinelAlarm = new SoundCue();

        public float MasterVolume => _masterVolume;
        public float FullVolumeDistance => _fullVolumeDistance;
        public float HearingDistance => _hearingDistance;

        public SoundCue Get(SfxId id)
        {
            switch (id)
            {
                case SfxId.Footstep: return _footstep;
                case SfxId.Roll: return _roll;
                case SfxId.ReloadStart: return _reloadStart;
                case SfxId.ReloadEnd: return _reloadEnd;
                case SfxId.PlayerHurt: return _playerHurt;
                case SfxId.PlayerDeath: return _playerDeath;
                case SfxId.ItemUseStart: return _itemUseStart;
                case SfxId.HealDone: return _healDone;
                case SfxId.OxygenCan: return _oxygenCan;
                case SfxId.OxygenWarning: return _oxygenWarning;
                case SfxId.WallImpact: return _wallImpact;
                case SfxId.RobotHit: return _robotHit;
                case SfxId.RobotDestroyed: return _robotDestroyed;
                case SfxId.RobotAlert: return _robotAlert;
                case SfxId.RobotCharge: return _robotCharge;
                case SfxId.RobotAimLock: return _robotAimLock;
                case SfxId.LootSearch: return _lootSearch;
                case SfxId.LootOpen: return _lootOpen;
                case SfxId.BagOpen: return _bagOpen;
                case SfxId.BagClose: return _bagClose;
                case SfxId.ItemPick: return _itemPick;
                case SfxId.ItemPlace: return _itemPlace;
                case SfxId.ItemRefuse: return _itemRefuse;
                case SfxId.ItemDropToWorld: return _itemDropToWorld;
                case SfxId.PodOpen: return _podOpen;
                case SfxId.EscapeSuccess: return _escapeSuccess;
                case SfxId.UiPrompt: return _uiPrompt;
                case SfxId.UiConfirm: return _uiConfirm;
                case SfxId.UiCancel: return _uiCancel;
                case SfxId.MapOpen: return _mapOpen;
                case SfxId.MapClose: return _mapClose;
                case SfxId.PortalLaunch: return _portalLaunch;
                case SfxId.LevelUp: return _levelUp;
                case SfxId.Heartbeat: return _heartbeat;
                case SfxId.KillConfirm: return _killConfirm;
                case SfxId.SentinelAlarm: return _sentinelAlarm;
                default: return null;
            }
        }
    }
}
