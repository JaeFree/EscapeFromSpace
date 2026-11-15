# 진행 기록

다음 세션은 이 파일과 `docs/PLAN.md`를 읽고 시작한다.

## 2026-11-15

### 오늘 한 일

- `docs/PLAN.md` 작성: 결정 사항 16개, 폴더 구조, 에셋 생성 방식, 작업 순서 5단계
- 1단계 구현
  - Active Input Handling을 "Input System Package"만으로 변경
  - `Assets/Input/GameInput.inputactions` (Move, Aim, Fire, Roll)
  - `PlayerData` SO, `PlayerInputReader`, `PlayerController`(이동·조준), `CameraFollow`
  - Editor 메뉴 `EscapeFromSpace > 에셋 생성·갱신` (`ProjectSetup`, `StationLayout`)
  - 생성된 에셋: 스프라이트 2개, 타일 2개, `PlayerData.asset`, `Player.prefab`, `Station.unity`(60×40 벽 맵)
  - Unity 배치 모드로 컴파일 에러 없음과 에셋 생성을 확인
  - 수정: 첫 생성 때 타일맵이 빈 채로 저장되어 맵이 보이지 않았다. 씬을 연 뒤에 타일 에셋을 다시 불러와 칠하도록 고쳤고, 배치 모드 렌더링으로 맵이 보이는 것을 확인했다.

  - 수정: 벽을 통과했다. 배치 모드에서 칠한 타일이 충돌체에 반영되지 않아 합쳐진 충돌체가 빈 채로 저장된 것이 원인. `ProjectSetup`이 저장 전에 충돌체를 직접 만들도록 고쳤다. 씬에 반영했다.
  - 수정: 플레이어 충돌체에 마찰 0 재질(`Assets/Physics/NoFriction`)을 붙였다. 기본 마찰 때문에 벽에 비스듬히 붙어 움직이면 속도가 40% 줄었다.
  - 검증: 임시 플레이 모드 테스트(키 입력 시뮬레이션)로 이동 5칸/초, 벽에서 정지, 벽 따라 미끄러지기 3.54칸/초를 확인했다. 테스트 파일은 지웠다.
- 1단계 커밋 완료 (`360d241`, push 안 함). 사용자가 Unity에서 이동·벽 충돌·Inspector 조정을 확인했다.
- 2단계 구르기 구현 (사용자 확인 대기 중, 아직 커밋하지 않음)
  - `PlayerRoll` 추가, `PlayerInputReader`에 Roll 입력, `PlayerController`가 구르기 속도를 적용
  - 쿨타임은 구르기를 시작한 순간부터 센다
  - 임시 플레이 모드 테스트로 확인: 정지 시 조준 방향으로 정확히 3칸, 이동 중 이동 방향, 쿨타임 중 재사용 불가, 벽에서 정지. 테스트 파일은 지웠다.

### 다음에 할 일

- 사용자가 2단계를 Unity에서 확인하고 OK하면 커밋 (push는 사용자가 시킬 때만)
- 3단계 사격 → 4단계 HP·방어력 → 5단계 피격 효과 (`docs/PLAN.md` 작업 순서)

### 막힌 점

- 없음

### 참고

- 동작 검증은 임시 PlayMode 테스트(`-runTests -testPlatform PlayMode`, Input System으로 키 입력을 큐에 넣고 Rigidbody 위치를 기록)로 한다. 게임 코드에 asmdef가 없어 테스트는 게임 타입을 참조하지 못하므로 `GameObject.Find`와 물리 값만 쓴다. 테스트 파일은 확인 후 지운다.
- 배치 모드에서 타일을 칠한 뒤에는 벽 충돌체를 직접 만들어야 한다 (`ProjectSetup.EnsureWallColliders`).
- 컴파일 확인은 Unity 에디터를 닫은 상태에서 배치 모드로 한다:
  `Unity.exe -batchmode -quit -projectPath <저장소> -executeMethod EscapeFromSpace.EditorTools.ProjectSetup.Run -logFile <로그>`
  Unity 설치 경로는 PC마다 다르므로 저장소에 적지 않는다.
- 템플릿 기본 파일 `Assets/InputSystem_Actions.inputactions`와 `Assets/Scenes/SampleScene.unity`는 쓰지 않지만 지우지 않았다.
- `ProjectSettings`의 productName이 아직 `TempProject`다.
