# 진행 기록

다음 세션은 이 파일과 `docs/PLAN.md`를 읽고 시작한다. 대화 기록은 PC 사이에 이어지지 않는다.

## 지금 상태 (2026-11-16 기준)

- GDD 일정의 11/13·11/14 작업(`docs/PLAN.md` 1부, 1~5단계)을 모두 끝내고 커밋·push했다.
- 11/15 작업(`docs/PLAN.md` 2부)의 질문 17개를 사용자가 확정했다 (6·8·14·15번만 추천안과 다름). 결정은 `PLAN.md` 2부 "결정 사항 (2부)"에 있다. 코드는 아직 건드리지 않았다.
- 6단계(탄창·재장전)는 사용자 확인을 거쳐 커밋했다. 감속 30%(배율 0.7)와 구르기 후 자동 재시작도 사용자가 확인했다.
- 7단계(탄퍼짐과 반동)는 사용자 확인을 거쳐 커밋했다.
- 8단계(무기 4종 에셋, 샷건 펠릿, 궤적 선, 표적 재생성)는 사용자 확인을 거쳐 커밋했다.
- 궤적 선을 총구 쪽부터 흐려지게 바꿨다 (사용자 요청, `BulletTracer`). 사용자 확인을 거쳐 커밋했다. 선은 점이 있는 곳에서만 색을 읽으므로 21개 점으로 나눠 그린다.
- 9단계(저격총 우클릭 조준)와 조준 원은 사용자 확인을 거쳐 커밋했다.
- 10단계(광선검 범위 판정과 부채꼴 궤적)는 사용자 확인을 거쳐 커밋했다.
- 11/15 작업(2부, 6~10단계)이 끝났다. GDD 4.4 처치 타수 표는 무기 에셋 값으로 계산해 맞는 것을 확인했다.
- 11/16 작업(3부, 로봇)의 질문 17개는 추천안대로 확정했고, 추격 실패 시 원래 자리로 복귀하는 규칙이 추가됐다.
- 11~15단계를 사용자 요청으로 한 번에 구현했고, 사용자 확인을 거쳐 커밋했다. 11/16 작업(3부)이 끝났다.
- 11/17 작업(4부, 칸 가방)의 계획과 질문 21개를 `PLAN.md` 4부에 정리했다. 3부 15번에서 "로봇 드롭은 GDD에 없다"고 한 것은 틀렸다 (GDD 6.2에 잔해 루팅이 있다). 4부 19번으로 다시 물었다.
- 11/17 작업(4부)의 질문은 5개(6·14·16·18·21번)만 추천안과 다르게 확정했다. 16~20단계를 사용자 요청으로 한 번에 구현했고, 사용자 확인("플레이에는 이상 없음")을 거쳐 커밋했다. 확인 뒤 요청에 따라 가방 화면을 키우고 로봇을 13기로 늘렸다. 11/17 작업(4부)이 끝났다.
- 11/18 작업(5부)의 계획과 질문 16개를 `PLAN.md` 5부에 정리했다. 대기실 씬의 구성도 1번으로 물었다.
- **단계를 시작하기 전에 사용자에게 한 번 묻는다.**

## 다음에 할 일

1. 사용자에게 `docs/PLAN.md` 5부의 질문 1~16에 대한 답을 받는다. 답을 받으면 5부에 "결정 사항" 표로 정리한다. **답을 받기 전에는 구현하지 않는다.**
2. 5부 작업 순서대로 21단계(무게)부터 구현한다. push는 사용자가 시킬 때만 한다.

## 2026-11-16에 한 일

- 2부 질문 17개 확정, `PLAN.md`에 결정 사항 표 정리.
- 6단계 구현:
  - `PlayerShooter`: 탄창, 임시 예비탄(Reserve Ammo 칸), 재장전. 탄창이 비면 자동, 탄이 남았으면 R키. 구르기 취소는 `PlayerRoll`을 고치지 않고 `PlayerShooter`가 `IsRolling`을 보고 처리한다.
  - `PlayerController`: 재장전 중 이동 속도에 `PlayerData.ReloadMoveSpeedMultiplier`(0.7)를 곱한다. `_shooter` 참조와 `IsDead` 추가.
  - `GameInput.inputactions`, `PlayerInputReader`: Reload(R) 추가.
  - `ProjectSetup`: 새 필드가 0일 때만 초기값을 채운다 (`SetFloatIfZero`, 예비탄은 탄창 2개분).
- 테스트로 확인한 것: 12발 뒤 자동 재장전, 재장전 1.20초, 재장전 중 속도 3.5칸/초, 수동 재장전 시 남은 탄 유지, 구르기 취소, 빈 탄창은 구르기 후 재시작, 예비탄 0이면 장전·발사 안 됨.

## 막힌 점

- 없음

## 2026-11-15에 한 일

| 단계 | 커밋 | 내용 |
| --- | --- | --- |
| 1 | `360d241` | 프로젝트 세팅, Input System, 60×40 벽 맵(`Station.unity`), 이동·조준, 카메라 추적 |
| 2 | `91ccb71` | 구르기 (`PlayerRoll`) |
| 3 | `e785904` | 레이캐스트 사격, `WeaponData`, 레이저 피스톨, 명중점 표시, `TestTarget` 3개 |
| 4 | `d4862f0` | `Health`, 피해·방어력 공식(`CombatConfig`, `DamageCalculator`), 구르기 무적, HP 0에서 조작 정지 |
| 5 | `57c2a14` | 피격 효과: 번쩍임, 혈흔, 경직(플레이어), 불꽃(표적) |

- 7단계 구현:
  - `PlayerShooter`: 발마다 ±(기본 퍼짐 + 쌓인 반동) 무작위 각, 이동 입력이 있으면 2배. 반동은 쏜 뒤에 쌓이고 항상 회복한다. Current Recoil 칸으로 확인.
  - `WeaponData`에 반동 회복 속도(4°/초)와 상한(10°), `CombatConfig`에 흔들림 계수(0.03칸/°)와 시간(0.1초) 추가.
  - `CameraShake`(새 파일)가 더할 값만 계산하고, 카메라 위치는 `CameraFollow`만 쓴다.
- 7단계에서 알게 된 것:
  - 레이저 피스톨은 Fire Rate 2에서 발 사이(0.5초)에 2°가 회복되어 반동 1.5°가 쌓이지 않는다. 누적은 펄스 라이플 수치(8발/초, 0.8°)로 확인했다 (3초 연사에 7.2°).
  - 피스톨의 화면 흔들림은 0.045칸으로 작다. 약하면 `CombatConfig`의 계수를 올린다.
  - 배치 모드 테스트에서 `WaitForEndOfFrame`은 쓸 수 없다. LateUpdate 뒤의 값은 실행 순서를 늦춘 보조 컴포넌트로 읽는다.

- 8단계 구현:
  - 사용자 요청으로 사격 궤적 선 추가 (`BulletTracer`, `Prefabs/BulletTracer`). GDD 4.1과 1부 결정 9번의 "궤적 선 없음"을 바꿨고 두 문서에 적었다.
  - 무기 에셋 4개(`LightSaber`, `PulseRifle`, `Nebula7`, `PlasmaScatter`). `ProjectSetup.EnsureWeapons`가 GDD 4.2 초기값으로 만든다.
  - 샷건은 펠릿 8개를 따로 판정한다. 퍼짐 값 18°는 부채꼴 전체 각이고, 반동은 부채꼴 중심을 흔든다.
  - Play 중에 `PlayerShooter`의 Weapon 칸을 바꾸면 탄창이 가득 차고 예비탄이 탄창 2개분이 된다 (임시).
  - 광선검은 10단계 전까지 좌클릭해도 공격하지 않는다.
  - `TestTarget`은 부서지고 3초 뒤 같은 자리에 다시 생긴다 (임시).

- 9단계 구현:
  - 입력에 AimHold(우클릭) 추가. `WeaponData`에 조준 시 퍼짐(`_aimSpreadAngle`, 네뷸라-7은 0.5°) 추가.
  - `PlayerShooter.IsAiming`: 저격총이고 우클릭 중일 때. 기본 퍼짐만 바뀌고 반동 누적과 이동 중 2배는 그대로다.
  - 사용자 요청으로 조준 원 추가 (`AimReticle`, Player 프리팹의 `AimReticle` 자식). 저격총을 조준하는 동안 마우스 위치에 빈 노란 원을 그린다. 반지름은 총구에서 마우스까지 거리 × tan(현재 퍼짐)이고 최소 0.08칸이다. GDD에 없는 표시다.

- 10단계 구현:
  - `PlayerShooter.Swing`: 플레이어 위치 기준 앞쪽 부채꼴(무기의 퍼짐 값 120°가 전체 각, 사거리 1.8칸) 안의 대상을 모두 한 번씩 맞힌다. 대상의 가장 가까운 점이나 중심이 부채꼴 안에 있고 그 사이에 벽이 없으면 맞는다. 탄을 쓰지 않는다.
  - `SwingArc`(새 파일, `Prefabs/SwingArc`): 사용자 요청으로 사격 궤적처럼 휘두르기 시작한 쪽(조준 방향의 왼쪽)부터 흐려지는 부채꼴. 플레이어의 자식이라 따라 움직인다. 표시 시간은 궤적 선과 같은 0.12초 (결정 15번의 0.1초에서 바꿈, 프리팹에서 조정).
  - 테스트에서 알게 된 것: 조준 방향을 바꾸는 것과 클릭을 같은 프레임에 넣으면 실행 순서 때문에 이전 방향으로 나갈 수 있다. 테스트에서는 조준을 먼저 돌리고 한 프레임 뒤에 누른다.

- 11~15단계 구현:
  - `RobotData`와 에셋 5개(`Data/Robots`). GDD 5.3 초기값. 플레이어 무기 에셋과 따로 둔다.
  - `WallGrid`: 벽 타일맵을 읽는 칸 단위 A*. 대각선은 양옆이 비었을 때만, 곧게 갈 수 있는 구간은 건너뛴다.
  - `RobotMotor`(이동·회전, 속도를 쓰는 유일한 곳), `Robot`(상태 머신: Patrol → Alert → Attack / Chase → Return), `RobotAttack`(사격 패턴과 판정), `AttackTelegraph`(저격 조준선, 샷건 충전 범위).
  - `RobotZone`: 씬의 `Robots/CargoZone`(근접 1·권총 2), `Robots/ResearchZone`(근접 1·권총 1·AR 1·샷건 1·저격 1). 로봇은 Play를 시작할 때 생긴다. Scene 뷰에 시작 위치와 순찰 경로를 그린다.
  - `GunNoise`: 플레이어가 총을 쏘면 12칸 안의 로봇이 그 위치로 추격한다. 광선검은 소리가 없다.
  - 테스트 표적(`TestTarget` 스크립트·프리팹·씬 오브젝트)과 `Health`의 테스트용 우클릭 메뉴를 지웠다.
- 테스트로 확인한 값:
  - 순찰: 웨이포인트 3곳을 순서대로 돌고 벽을 침범하지 않는다. 저격 로봇은 제자리에서 ±45°를 훑는다.
  - 감지: 등 뒤와 벽 너머는 못 본다. 정면에서 보면 경계 0.60초 뒤 사격. 놓치면 마지막 본 곳까지 가고 5.00초 뒤 복귀, 떠난 자리(오차 0.2칸)로 돌아와 순찰. 12칸 안의 총소리와 피격에 추격.
  - 사격 주기: 권총 8×2발(0.33초 간격) 1.67초, AR 6×5발(0.125초 간격) 2.13초, 근접 20을 1.2초마다, 샷건 충전 0.5초 뒤 1.5초마다(4칸에서 펠릿 5~7개 명중), 저격 조준 1.5초 뒤 3초마다 40. GDD 5.4의 초당 피해와 맞는다.
  - 저격 조준선이 고정된 뒤 옆으로 비키면 안 맞는다. 플레이어 HP가 0이면 사격을 멈추고 순찰로 돌아간다.
- 로봇에서 정한 것 (GDD에 없는 세부):
  - 사격 상태에서는 시야각을 보지 않고 거리와 벽만 본다. 이미 알아챈 뒤라 몸을 돌려 따라가기 때문이다. 처음 발견할 때와 추격 중에는 시야각까지 본다.
  - 사거리의 80% 안으로 들어오면 멈춘다. 사거리 끝에 서면 플레이어가 한 걸음만 물러나도 다시 걸어야 해서다.
  - 쉬는 시간은 사격 상태에서만 흐른다.

- 16~20단계 구현:
  - `ItemData`와 에셋 20개(`Data/Items`: 탄약 4, 의약품 2, 산소 캔, 전리품 2, 무기 아이템 5, 방탄복 3, 헬멧 3). 모두 1×1.
  - `LootTable`과 에셋 7개(`Data/Loot`: 일반·연구 컨테이너, 로봇 5종). `WeaponData`에 탄약 아이템, `RobotData`에 루팅 표를 이었다.
  - `InventoryGrid`(칸 배치 판정), `ItemStack`(수량·회전·탄창 잔탄), `PlayerInventory`(장비 칸 3개, 가방 3×4 + 방탄복 추가 줄, 보관함, 탄약 합계와 소모, 방어력 반영).
  - `InventoryUI`(씬의 `UI` 캔버스): Tab으로 열고, 좌클릭 드래그, R 회전, 초록/빨강 표시. 칸과 아이템은 실행 중에 코드로 그린다. 글꼴은 Windows의 맑은 고딕을 실행 중에 불러 쓴다.
  - `Lootable`(컨테이너, 로봇 잔해, 버린 아이템)과 `PlayerInteractor`(E 1초, 맞으면 취소, 범위 1.5칸, 버리기).
  - `PlayerShooter`는 무기 슬롯의 아이템을 쓰고, 예비탄은 가방과 보관함의 탄약 합계다. 임시 Weapon 칸과 임시 예비탄 칸을 없앴다. Ammo In Magazine과 Reserve Ammo 칸은 확인용으로만 남겼다.
  - 컨테이너 10개(`Containers`, 일반 8·연구 2)를 벽 레이어에 놓았다. `WallGrid`가 벽 레이어의 충돌체도 막힌 칸으로 읽는다.
  - 로봇이 부서지면 그 자리에 잔해(`RobotWreck`, 3×1)가 남는다.
- 테스트로 확인한 값:
  - 칸 배치: 겹침, 밖으로 나감, 회전(1×2 임시 아이템), 줄 수 줄이기 제한.
  - 루팅 표: 일반 컨테이너 2~3개(방탄복 없음), 저격 로봇 잔해는 탄약 항상·무기 약 10%·3단계 헬멧 약 5%.
  - 가방: 재장전하면 가방 탄약이 줄고, 가방을 연 동안 사격이 막히고, 무기 맞바꾸기, 종류가 다른 칸 거부, 보관함 탄약도 예비탄, 탄약 합치기(60발 + 나머지 7발), 방탄복 2단계로 방어력 4·가방 6줄, 추가 줄에 아이템이 있으면 못 벗음, 맨손이면 못 쏨.
  - 루팅: E 0.5초에서 맞으면 취소, 다시 1.00초 뒤 열림, 가져오기와 다시 넣기, 범위를 벗어나면 닫힘, 다시 열면 바로 열리고 내용 유지.
  - 버리기: 화면 밖에 놓으면 바닥에 남고, E로 바로 열어 다시 줍고, 시간이 지나면 사라진다.
- 16~20단계에서 알게 된 것:
  - 서로 다른 오브젝트의 `Awake`는 순서를 알 수 없다. `InventoryUI`가 `Awake`에서 가방을 읽으면 아직 없을 수 있어서 `Start`에서 잇는다.
  - 게임 코드에 직접 닿아야 하는 로직 테스트는 asmdef 없는 `Assets/<임시폴더>/Editor/`에 정적 메서드로 두고 `-executeMethod`로 돌리면 리플렉션 없이 쓸 수 있다 (끝나면 폴더 삭제).
  - 화면에 바로 그리는 캔버스(Overlay)는 카메라 렌더링 캡처에 안 찍힌다. 캡처할 때만 캔버스를 Screen Space - Camera로 바꾼다. 그래서 `InventoryUI`의 좌표 계산은 캔버스 방식에 상관없이 맞게 해 두었다.

## 코드 구조 요약

- **플레이어**: `PlayerInputReader`(입력) → `PlayerController`(이동·조준·경직, 속도를 쓰는 유일한 곳) / `PlayerRoll`(구르기·무적) / `PlayerShooter`(사격). `PlayerController.CanAct`가 false면 이동·사격·구르기가 막힌다.
- **전투**: `Health`가 `Damaged`, `Died` 이벤트를 내고 `HitFlash`, `BloodSplatter`, `SparkOnHit`, `PlayerController`가 구독한다.
- **데이터**: `Assets/Data`의 `PlayerData`, `CombatConfig`, `Weapons/LaserPistol`. 기획서 수치는 모두 여기에 있다.
- **로봇**: `RobotZone`이 `Robot` 프리팹을 만들고 `RobotData`, 웨이포인트, `WallGrid`, 플레이어 `Health`를 넘긴다. `Robot`이 상태를 정하고 `RobotMotor`와 `RobotAttack`에 시킨다.
- **가방과 루팅**: `PlayerInventory`가 가진 것 전부를 들고, `InventoryUI`가 그리고 옮긴다. `PlayerInteractor`가 E로 `Lootable`을 열면 `InventoryUI`가 그것을 보고 루팅 칸을 띄운다. `PlayerController.IsBagOpen`이 true면 사격·구르기·재장전 시작이 막힌다.
- **에셋 생성**: Unity 메뉴 `EscapeFromSpace > 에셋 생성·갱신`(`Scripts/Editor/ProjectSetup.cs`). 없는 에셋·컴포넌트·빈 참조만 채우고, 이미 있는 값은 건드리지 않는다. 새 컴포넌트나 프리팹을 추가할 때는 이 스크립트에 "없으면 만들기"를 더한다.

## 사용자가 조정한 값 (되돌리지 말 것)

- `LaserPistol.asset`의 Fire Rate: 3 → 2
- 로봇 수: 8기 → 13기 (2026-11-16, 씬의 `Robots` 아래 `RobotZone` 목록. 화물칸 5, 연구 구역 8)
- 가방 화면 크기: `UI`의 `InventoryUI` Cell Size 108, 글자 19 / 22 / 22 (2026-11-16, 스크립트 기본값)
- `PlayerData.asset`의 Hit Stun Duration: 0.2 → 0 (2026-11-16, 피격 경직을 없앰. 코드는 그대로라 값을 올리면 다시 생긴다)
- `Data/Robots`의 로봇 5종 Hearing Radius: 12 → 8 (2026-11-16, 총소리 반응 거리)

## 작업 방식에서 알게 된 것

- **사용자에게 확인을 부탁하기 전에 직접 돌려서 확인한다.** 1단계에서 컴파일만 확인하고 넘겼다가 맵이 안 보이고 벽을 통과하는 문제가 사용자에게 넘어갔다. 그 뒤로는 Unity 배치 모드에서 임시 플레이 모드 테스트를 돌린다.
- **배치 모드는 Unity 에디터가 닫혀 있어야 돈다.** 열려 있으면 사용자에게 닫아 달라고 한다. 닫기 전에 Inspector에서 바꾼 값은 `Ctrl+S`로 저장해야 파일에 반영된다.
- **컴파일과 에셋 생성**:
  `Unity.exe -batchmode -quit -projectPath <저장소> -executeMethod EscapeFromSpace.EditorTools.ProjectSetup.Run -logFile <로그>`
  로그에서 `error CS`와 `Exception`을 찾는다. Unity 설치 경로는 PC마다 다르므로 저장소에 적지 않는다.
- **임시 플레이 모드 테스트**: `Assets/TempTests/`에 asmdef와 테스트 파일을 두고
  `Unity.exe -batchmode -projectPath <저장소> -runTests -testPlatform PlayMode -testResults <xml> -logFile <로그>`로 돌린 뒤 폴더를 지운다 (커밋하지 않는다).
  - asmdef: `references`에 `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Unity.InputSystem`, `precompiledReferences`에 `nunit.framework.dll`, `overrideReferences` true, `defineConstraints`에 `UNITY_INCLUDE_TESTS`.
  - 게임 코드에 asmdef가 없어 테스트는 게임 타입을 직접 참조하지 못한다. `GameObject.Find`, `GetComponent("Health")`, 리플렉션, `SendMessage`를 쓴다.
  - 입력은 `InputSystem.QueueStateEvent`로 `KeyboardState`, `MouseState`를 넣는다. 배치 모드에서는 `InputSystem.settings.backgroundBehavior = IgnoreFocus`와 `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView`를 먼저 설정해야 입력이 들어간다.
  - 화면 확인은 `Camera.main`을 RenderTexture로 렌더링해 PNG로 저장한다. 캡처는 프레임을 길게 멈추므로 시간 측정 구간에 넣지 않는다.
  - 컴파일 에러가 있으면 `-runTests`가 끝나지 않으므로 타임아웃을 건다.
- **배치 모드에서 씬을 열면 그 전에 잡아 둔 에셋 참조가 무효가 된다.** 씬을 연 뒤에 `AssetDatabase.LoadAssetAtPath`로 다시 불러온다.
- **배치 모드에서 타일을 칠한 뒤에는 벽 충돌체를 직접 만들어야 한다** (`ProjectSetup.EnsureWallColliders`). 안 하면 합쳐진 충돌체가 빈 채로 저장되어 벽을 통과한다.
- **속도는 한 스크립트에서만 쓴다.** 두 스크립트가 각자 `linearVelocity`를 쓰면 실행 순서에 따라 한 틱이 덮인다 (구르기가 2.85칸만 가던 원인).
- **플레이어 충돌체에는 마찰 0 재질을 쓴다** (`Assets/Physics/NoFriction`). 기본 마찰이면 벽에 비스듬히 붙어 움직일 때 느려진다.

## 남아 있는 것

- 플레이어의 총 막대(`AimPivot/Gun`)는 고정된 임시 도형이라 무기를 바꿔도(광선검 포함) 모양이 그대로다. `WeaponData`에 무기 그림 칸이 없고, 무기별 모양을 바꾸는 작업은 계획에 없다. 사용자가 물어봤으니 무기 그림을 넣을 때 정한다.

- 프리팹의 Reserve Ammo를 0으로 저장하고 `에셋 생성·갱신`을 다시 돌리면 탄창 2개분으로 다시 채워진다 (새 칸과 직접 넣은 0을 구분하지 못함). 11/17에 가방 탄약으로 바꾸면 사라진다.

- 템플릿 기본 파일 `Assets/InputSystem_Actions.inputactions`와 `Assets/Scenes/SampleScene.unity`는 쓰지 않지만 지우지 않았다.
- `ProjectSettings`의 productName이 아직 `TempProject`다.
- `docs/PROMPT_DAY1.md`는 사용자의 파일이라 커밋하지 않았다 (추적되지 않는 상태).
- 이동 중 구르기는 걷기보다 1.5배 빨라질 뿐이라 눈에 잘 안 띈다. 사용자가 나중에 구르기 애니메이션을 넣을 예정이라 표시는 추가하지 않기로 했다.
- 헬멧의 산소 추가, 무게, 의약품·산소 캔 사용, 사망 시 보관함만 남기기는 아직 없다 (11/18~19 작업). 데이터는 `ItemData`에 들어 있다.
- 시작 장비는 `PlayerInventory`의 임시 칸(Starting Weapon, Starting Items)이다. 대기실 씬을 만들면 없앤다.
- 사용자가 씬에서 `Robots/CargoZone/MeleeRobot 1` 경로를 (0.1, 9)만큼 옮겼다. 그대로 둔다.
- 로봇의 총 사격과 플레이어의 실제 클릭 사격(총소리 발생)은 임시 테스트에서 따로 확인하지 않았다. 총소리는 `GunNoise.Emit`을 직접 불러 확인했다.
