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
- **단계를 시작하기 전에 사용자에게 한 번 묻는다.**

## 다음에 할 일

1. GDD 4.4의 처치 타수 표와 맞는지 표적 HP를 바꿔 가며 확인한다. 그러면 11/15 작업(2부)이 끝난다.
3. 그다음은 GDD 일정의 11/16 작업(로봇). 계획과 질문을 먼저 정리해 사용자에게 묻는다.

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

## 코드 구조 요약

- **플레이어**: `PlayerInputReader`(입력) → `PlayerController`(이동·조준·경직, 속도를 쓰는 유일한 곳) / `PlayerRoll`(구르기·무적) / `PlayerShooter`(사격). `PlayerController.CanAct`가 false면 이동·사격·구르기가 막힌다.
- **전투**: `Health`가 `Damaged`, `Died` 이벤트를 내고 `HitFlash`, `BloodSplatter`, `SparkOnHit`, `PlayerController`가 구독한다.
- **데이터**: `Assets/Data`의 `PlayerData`, `CombatConfig`, `Weapons/LaserPistol`. 기획서 수치는 모두 여기에 있다.
- **에셋 생성**: Unity 메뉴 `EscapeFromSpace > 에셋 생성·갱신`(`Scripts/Editor/ProjectSetup.cs`). 없는 에셋·컴포넌트·빈 참조만 채우고, 이미 있는 값은 건드리지 않는다. 새 컴포넌트나 프리팹을 추가할 때는 이 스크립트에 "없으면 만들기"를 더한다.

## 사용자가 조정한 값 (되돌리지 말 것)

- `LaserPistol.asset`의 Fire Rate: 3 → 2

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
- `Health`의 Armor 칸과 우클릭 메뉴 "테스트: 공격 피해 20 받기"는 방어구(11/17)와 로봇(11/16)이 생기면 정리한다.
