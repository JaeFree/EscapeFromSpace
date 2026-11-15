# 진행 기록

다음 세션은 이 파일과 `docs/PLAN.md`를 읽고 시작한다. 대화 기록은 PC 사이에 이어지지 않는다.

## 지금 상태 (2026-11-15 기준)

- GDD 일정의 11/13·11/14 작업(`docs/PLAN.md` 1부, 1~5단계)을 모두 끝내고 커밋·push했다.
- 다음은 11/15 작업(`docs/PLAN.md` 2부)이다. 계획과 질문 17개를 정리해 두었고, **사용자가 질문에 답하기 전에는 구현하지 않는다.**

## 다음에 할 일

1. 사용자에게 `docs/PLAN.md` 2부의 질문 1~17에 대한 답을 받는다. 답을 받으면 2부에 "결정 사항" 표로 정리한다.
2. 2부 작업 순서대로 6단계(탄창·재장전)부터 구현한다. 한 단계씩 하고, 단계마다 컴파일·테스트 → 사용자 확인 → OK 후 한국어 커밋. push는 사용자가 시킬 때만 한다.

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

- 템플릿 기본 파일 `Assets/InputSystem_Actions.inputactions`와 `Assets/Scenes/SampleScene.unity`는 쓰지 않지만 지우지 않았다.
- `ProjectSettings`의 productName이 아직 `TempProject`다.
- `docs/PROMPT_DAY1.md`는 사용자의 파일이라 커밋하지 않았다 (추적되지 않는 상태).
- 이동 중 구르기는 걷기보다 1.5배 빨라질 뿐이라 눈에 잘 안 띈다. 사용자가 나중에 구르기 애니메이션을 넣을 예정이라 표시는 추가하지 않기로 했다.
- `Health`의 Armor 칸과 우클릭 메뉴 "테스트: 공격 피해 20 받기"는 방어구(11/17)와 로봇(11/16)이 생기면 정리한다.
