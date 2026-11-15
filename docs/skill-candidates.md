# 스킬 후보 목록

- 조사일: 2026-11-15
- 상태: **후보 정리만 함. 아무것도 설치하지 않음.** 사용자가 고른 항목만 설치한다.
- 기준: `docs/GDD.md` (Unity 6 2D URP 탑다운 익스트랙션 슈터, 8일 프로토타입)

## 선택 결과 (2026-11-15)

사용자가 "GDD를 보고 알아서 판단해 추가"하라고 위임해서, 아래 8개만 프로젝트 `.claude/skills/`에 설치했다. 나머지 후보는 설치하지 않았다.

| 설치한 스킬 | GDD에서 쓰이는 곳 | 출처 커밋 |
|---|---|---|
| unity-scriptableobjects | 무기·로봇·아이템·방어구·맵 수치를 SO로 분리 (CLAUDE.md 규칙) | gamedev-skills `c6ca6c4` |
| unity-physics | 4.1 히트스캔 Raycast2D, 벽 비관통, 5.2 시야 Raycast2D | gamedev-skills `c6ca6c4` |
| unity-input-system | 9.3 조작 (WASD, 마우스 조준, R, Space, E 길게 누르기, Tab, M, 1~3) | gamedev-skills `c6ca6c4` |
| unity-tilemap-2d | 9.1 타일맵 맵 1개 (60×40), 격벽 충돌 | gamedev-skills `c6ca6c4` |
| unity-csharp-scripting | 전체 C# 스크립트, `[SerializeField]`, `.meta` 관리 | gamedev-skills `c6ca6c4` |
| game-ai | 5장 로봇 상태 머신(순찰·경계·사격·추격), 시야 판정, 총성 감지 | gamedev-skills `c6ca6c4` |
| game-feel | 4.1 반동 카메라 흔들림, 피격 번쩍임 0.1초·경직 0.2초, 불꽃 | gamedev-skills `c6ca6c4` |
| scope-check | "기획서에 없는 기능 추가 금지" 점검, 10장 범위·일정 대비 확인 | Claude-Code-Game-Studios `abc0de7` |

- `scope-check`는 기준 문서 경로가 `design/gdd/`로 되어 있어서 `docs/GDD.md`를 보도록 한 줄을 추가했다.
- `rigor` 설정: 저장소 루트 `project.yaml`에 `modes.rigor: minimal`.
- 라이선스 사본: `.claude/licenses/` (MIT, Apache-2.0 + NOTICE).
- 훅은 하나도 설치하지 않았다. 자동 실행되는 것은 없다.
- 스크립트가 있는 것은 `unity-csharp-scripting/scripts/check_unity_project.py` 하나다. 표준 라이브러리만 쓰고 파일을 읽기만 하는 검사 도구이며, 스킬을 쓸 때만 실행된다.

설치하지 않은 이유:

- Donchitos 규칙(ai-code, ui-code 등): 현지화·게임패드·색약 모드·집단 AI 대형·네트워크 검증을 "필수"로 요구해서 "기획서에 없는 기능 추가 금지"와 충돌한다. gameplay-code의 데이터 분리 원칙은 CLAUDE.md에 이미 있다.
- Donchitos의 다른 스킬(balance-check 등): `design/gdd/`, `assets/data/*.json`, `yaml-helper.sh` 같은 Donchitos 폴더 구조를 전제로 해서 SO `.asset`을 쓰는 이 프로젝트와 맞지 않는다.
- Donchitos 에이전트·훅: 서브에이전트와 자동 실행 스크립트는 1주 프로젝트에 무겁다.
- camera-systems, fps-shooter, game-jam: GDD에 카메라 요구사항이 따로 없고, fps-shooter는 1인칭 기준이며, game-jam은 scope-check와 겹친다.
- save-systems: 예제가 Godot·Python 코드다. 7장의 JSON 저장은 Unity `JsonUtility`로 충분하다.
- Unity MCP 서버: 로컬 서버를 계속 띄워야 하고 두 PC에 모두 설정해야 한다. 필요하면 따로 요청.

## 설치 전에 알아둘 점

1. **Donchitos/Claude-Code-Game-Studios는 "템플릿 저장소" 형태다.** 원래 설치 방법은 저장소 전체를 복제해서 새 프로젝트로 쓰는 것이다. 이 프로젝트에는 이미 Unity 프로젝트가 있으므로, 고른 항목만 `.claude/` 아래로 복사하는 방식이 된다.
2. **Donchitos 스킬 대부분은 `.claude/hooks/yaml-helper.sh`와 저장소 루트의 `project.yaml`을 읽는다.** 스킬 하나만 골라도 이 두 파일은 같이 가져와야 하며, `project.yaml`에 `modes.rigor: minimal`을 넣는다 (CLAUDE.md 규칙 4). 실행에는 Bash(Git Bash)가 필요하다.
3. Donchitos의 `/code-review`는 Claude Code 내장 `/code-review`와 이름이 같다. 고르면 이름이 겹친다.
4. Donchitos 규칙(rules)은 경로로 적용된다. 예: `gameplay-code`는 `Assets/**/Gameplay/**/*.cs`에만 걸린다. 고르면 스크립트 폴더 이름을 거기에 맞춰야 한다.
5. gamedev-skills 저장소의 Unity 스킬은 **Unity 6.3 LTS** 기준으로 쓰였다. 이 프로젝트는 6000.0.85f1이라 일부 API 설명이 다를 수 있다.
6. 프로젝트에 이미 들어 있는 패키지: Input System 1.20.0, Test Framework 1.6.0, Tilemap 모듈.

## A. Donchitos/Claude-Code-Game-Studios — 스킬

출처: <https://github.com/Donchitos/Claude-Code-Game-Studios> (MIT, 스킬 75개 중 관련 있는 것만)

| 이름 | 출처 URL | 한 줄 용도 | 라이선스 | 이 프로젝트에 필요한 이유 | 훅/스크립트 실행 여부 |
|---|---|---|---|---|---|
| balance-check | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/balance-check | 수식·데이터에서 밸런스 이상치, 지배 전략, 경제 불균형을 찾음 | MIT | 무기 5종 DPS, 방어력 공식, 무게·산소 수치를 SO 초기값으로 넣은 뒤 점검 | `yaml-helper.sh` 실행 |
| scope-check | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/scope-check | 원래 계획 대비 범위가 늘었는지 점검, 줄일 것 제안 | MIT | "기획서에 없는 기능 추가 금지" 규칙을 확인하는 데 맞음. 8일 일정 관리 | 없음 (`git log`만 읽음) |
| cut | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/cut | 기획서와 구현 상태를 보고 자를 기능·폴리시 목록 제안, 사람이 하나씩 고름 | MIT | 마감(11/20)이 빠듯할 때 무엇을 뺄지 결정 | `yaml-helper.sh`, `cut-read.sh`, `cut-apply.sh`, `config-set.sh`, `one-shot-*.sh` 실행 |
| quick-design | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/quick-design | 작은 수치 조정·소규모 메커닉용 가벼운 명세 작성 | MIT | 열린 질문 답을 받은 뒤 GDD를 조금 고칠 때 | `yaml-helper.sh` 실행 |
| bug-report | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/bug-report | 재현 단계·심각도가 있는 버그 리포트 작성, 코드에서 버그 후보 분석 | MIT | 플레이 테스트 중 발견한 문제 정리 | `yaml-helper.sh` 실행 |
| playtest-report | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/playtest-report | 플레이테스트 메모를 구조화된 피드백으로 정리 | MIT | Inspector 밸런싱을 직접 플레이로 하므로 기록 정리에 사용 | `yaml-helper.sh` 실행 |
| code-review (Donchitos판) | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/code-review | 코딩 표준·SOLID·테스트 용이성·성능 관점 아키텍처 리뷰 | MIT | C# 코드 규칙 준수 확인. **내장 `/code-review`와 이름 충돌** | `yaml-helper.sh` 실행, 서브에이전트 호출 |
| test-setup | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/test-setup | 테스트 폴더·엔진 테스트 러너·GitHub Actions CI 구성 | MIT | 데미지 공식·DPS 계산 등에 EditMode 테스트를 붙일 경우 | `yaml-helper.sh`, `test-scaffold.sh` 실행 |
| smoke-check | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/smoke-check | 자동 테스트를 돌려 핵심 경로가 깨지지 않았는지 확인 | MIT | 빌드 제출 전 확인. 단, Godot 명령 위주라 Unity 지원 범위 확인 필요 | `yaml-helper.sh`, `project-coherence.sh`, `test-run.sh` 실행 |
| sprint-plan | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/sprint-plan | 마일스톤·완료 작업·가용 시간으로 스프린트 계획 작성 | MIT | 8일 일정을 날짜별로 나눌 때 | `yaml-helper.sh` 실행, 서브에이전트 호출 |
| next-step | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/next-step | 지금 해야 할 다음 한 단계를 알려줌 | MIT | 학교/집 PC를 오가며 작업 재개할 때 | `yaml-helper.sh`, `story-status.sh`, `artifact-check.sh` 실행 |
| project-settings | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/project-settings | `project.yaml` 설정값 조회·변경 | MIT | `rigor: minimal` 설정 확인·변경용. Donchitos 스킬을 하나라도 쓰면 필요 | `yaml-helper.sh`, `config-set.sh` 실행 |
| team-combat | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/team-combat | 전투 관련 에이전트 6개를 묶어 설계→구현→검증 진행 | MIT | 전투 시스템이 핵심이지만, 서브에이전트를 많이 띄워 1주 프로젝트엔 무거움 | `yaml-helper.sh` 실행, 서브에이전트 다수 호출 |
| perf-profile | https://github.com/Donchitos/Claude-Code-Game-Studios/tree/main/.claude/skills/perf-profile | 병목 찾기, 예산 대비 측정, 최적화 우선순위 제안 | MIT | 로봇·탄·이펙트가 많아질 때만 필요. 우선순위 낮음 | `yaml-helper.sh` 실행 |

## B. Donchitos/Claude-Code-Game-Studios — 에이전트

에이전트는 `.claude/agents/*.md` 파일 하나씩이며 자체적으로 스크립트를 실행하지 않는다 (단, 에이전트가 쓰는 도구 권한은 각 파일에 정의됨).

| 이름 | 출처 URL | 한 줄 용도 | 라이선스 | 이 프로젝트에 필요한 이유 | 훅/스크립트 실행 여부 |
|---|---|---|---|---|---|
| unity-specialist | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/unity-specialist.md | Unity 패턴·API 전문 (MonoBehaviour, Input System 등) | MIT | Unity 6 전용 API 판단 | 없음 |
| gameplay-programmer | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/gameplay-programmer.md | 플레이어·전투·상호작용 메커닉 구현 | MIT | 사격·구르기·재장전·루팅 구현 | 없음 |
| ai-programmer | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/ai-programmer.md | 상태 머신·길찾기·인지 등 적 AI 구현 | MIT | 보안 로봇 AI | 없음 |
| systems-designer | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/systems-designer.md | 전투 공식·성장 곡선 등 수치 모델링 | MIT | 방어력 공식, DPS, 무게 패널티 검토 | 없음 |
| ui-programmer | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/ui-programmer.md | HUD·인벤토리 화면·메뉴 구현 | MIT | 산소/HP HUD, 가방 칸 UI, 대기실 창고 화면 | 없음 |
| unity-ui-specialist | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/unity-ui-specialist.md | UI Toolkit / UGUI 구현 | MIT | 위와 같음. ui-programmer와 둘 중 하나면 충분 | 없음 |
| technical-artist | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/technical-artist.md | 셰이더·VFX·아트 파이프라인 | MIT | 피격 번쩍임, 불꽃, 카메라 흔들림. 단색 도형 단계라 우선순위 낮음 | 없음 |
| qa-tester | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/qa-tester.md | 테스트 케이스·회귀 체크리스트 작성 | MIT | 탈출/사망 처리 등 핵심 루프 확인 목록 | 없음 |
| game-designer | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/agents/game-designer.md | 핵심 루프·전투·경제 규칙 설계 | MIT | 기획서가 이미 있어 필요성 낮음. 열린 질문 정리 시에만 | 없음 |

## C. Donchitos/Claude-Code-Game-Studios — 규칙(rules)

경로가 맞는 파일을 편집할 때 자동으로 컨텍스트에 들어가는 문서. 스크립트 실행 없음.

| 이름 | 출처 URL | 한 줄 용도 | 라이선스 | 이 프로젝트에 필요한 이유 | 훅/스크립트 실행 여부 |
|---|---|---|---|---|---|
| gameplay-code | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/rules/gameplay-code.md | 수치 데이터 분리, deltaTime 사용, UI 참조 금지 (`Assets/**/Gameplay/**/*.cs`) | MIT | "수치는 SO로 분리" 규칙과 일치 | 없음 |
| ai-code | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/rules/ai-code.md | AI 코드 성능 예산·디버그 용이성·데이터 기반 파라미터 (`Assets/**/AI/**/*.cs`) | MIT | 로봇 AI 코드 | 없음 |
| ui-code | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/rules/ui-code.md | UI는 게임 상태를 소유하지 않음 (`Assets/**/UI/**/*.cs`) | MIT | HUD·인벤토리 UI 분리 | 없음 |
| engine-code | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/rules/engine-code.md | 핫패스 할당 금지, API 안정성 (`Assets/**/Core/**/*.cs`) | MIT | 공용 시스템(데미지, 이벤트) 코드 | 없음 |
| scene-files | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/rules/scene-files.md | `.unity`, `.prefab`, `.asset` 편집 규칙 | MIT | SO 에셋(`.asset`)을 다룰 때. 사용자가 바꾼 값을 되돌리지 않는 규칙과 함께 확인 필요 | 없음 |
| test-standards | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/rules/test-standards.md | 테스트 이름·커버리지·픽스처 규칙 (`Assets/Tests/**/*.cs`) | MIT | 테스트를 쓸 경우에만 | 없음 |

## D. Donchitos/Claude-Code-Game-Studios — 훅

훅은 `.claude/settings.json`에 등록되어 **이벤트마다 자동 실행**되는 Bash 스크립트다. 고르면 매번 실행된다.

| 이름 | 출처 URL | 한 줄 용도 | 라이선스 | 이 프로젝트에 필요한 이유 | 훅/스크립트 실행 여부 |
|---|---|---|---|---|---|
| validate-commit.sh | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/hooks/validate-commit.sh | 커밋 시 하드코딩 수치·TODO 형식·JSON·기획 문서 섹션 검사 | MIT | 수치 하드코딩 방지 규칙과 맞음 | **예** — Bash/PowerShell 호출 전마다 자동 실행 |
| validate-push.sh | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/hooks/validate-push.sh | 보호 브랜치 푸시·태그·릴리스 경고 | MIT | main 직접 푸시 경고 | **예** — Bash/PowerShell 호출 전마다 자동 실행 |
| session-start.sh | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/hooks/session-start.sh | 세션 시작 시 브랜치·최근 커밋 표시 | MIT | PC를 옮겨 작업 재개할 때 맥락 파악 | **예** — 세션 시작 시 자동 실행 |
| notify.sh | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/hooks/notify.sh | 권한 요청·대기 시 Windows 토스트 알림 | MIT | 두 PC 모두 Windows | **예** — 권한 요청/약 1분 대기 시 자동 실행 |
| validate-write.sh | https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/hooks/validate-write.sh | 보호 파일 수정 등 규칙 위반 경고 (차단 안 함) | MIT | 필요성 낮음 | **예** — Write/Edit 전마다 자동 실행 |

## E. 그 밖의 Unity 2D 슈터용 스킬

출처: <https://github.com/gamedev-skills/awesome-gamedev-agent-skills> (Apache-2.0, 게임 개발 스킬 79개, Unity 6.3 LTS 기준)

| 이름 | 출처 URL | 한 줄 용도 | 라이선스 | 이 프로젝트에 필요한 이유 | 훅/스크립트 실행 여부 |
|---|---|---|---|---|---|
| unity-scriptableobjects | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/unity/unity-scriptableobjects | CreateAssetMenu 설정 에셋, 이벤트 채널, 코드로 `.asset` 생성 | Apache-2.0 | 모든 수치를 SO로 분리하는 규칙의 핵심 | 없음 (문서만) |
| unity-csharp-scripting | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/unity/unity-csharp-scripting | MonoBehaviour 수명주기, 직렬화, `.meta`/GUID 관리, 명령줄 테스트 | Apache-2.0 | 스크립트 신규·이름 변경 시 `.meta` 깨짐 방지, `[SerializeField]` 관련 | **예** — `scripts/check_unity_project.py` (Python 3.10+) 실행 단계 있음 |
| unity-physics | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/unity/unity-physics | Rigidbody2D, 레이어 충돌 매트릭스, 레이캐스트 | Apache-2.0 | 히트스캔 Raycast2D, 벽 비관통, 이동 처리 | 없음 (문서만) |
| unity-input-system | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/unity/unity-input-system | `.inputactions`, 액션 맵, PlayerInput 연결 | Apache-2.0 | WASD·마우스 조준·R·Space 입력. Input System 1.20 이미 설치됨 | 없음 (문서만) |
| unity-tilemap-2d | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/unity/unity-tilemap-2d | 타일 팔레트, Tilemap Collider 2D + Composite Collider | Apache-2.0 | 정거장 구역·벽을 타일맵으로 만들 경우 | 없음 (문서만) |
| game-ai | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/disciplines/game-ai | FSM/BT 선택, 시야 판정, A* 길찾기 (엔진 무관) | Apache-2.0 | 보안 로봇 순찰·추적·벽 너머 감지 방지 | 없음 (문서만) |
| game-feel | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/disciplines/game-feel | 화면 흔들림, 히트스톱, 넉백 등 손맛 | Apache-2.0 | 기획서의 반동 카메라 흔들림·피격 번쩍임·경직 0.2초 | 없음 (문서만) |
| camera-systems | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/disciplines/camera-systems | 2D 추적 카메라 데드존·룩어헤드, Cinemachine 3 | Apache-2.0 | 탑다운 추적 카메라. 우선순위 중간 | 없음 (문서만) |
| fps-shooter | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/genres/fps-shooter | 히트스캔 vs 투사체, TTK, 거리 감쇠, 반동 패턴 | Apache-2.0 | 1인칭 기준이지만 히트스캔·반동·TTK 설계는 그대로 적용 가능 | 없음 (문서만) |
| game-jam | https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/workflows/game-jam | 고정 마감에 맞춘 범위 조정·컷 리스트·빌드 확인 | Apache-2.0 | 8일 마감 프로젝트. Donchitos `cut`/`scope-check`와 역할 겹침 | 없음 (문서만) |

## F. Unity 에디터 연동 (MCP 서버)

스킬이 아니라 MCP 서버다. Unity 에디터 안의 씬·에셋·콘솔을 Claude가 직접 다룰 수 있게 한다. Unity 패키지 설치와 로컬 서버 실행이 필요하다.

| 이름 | 출처 URL | 한 줄 용도 | 라이선스 | 이 프로젝트에 필요한 이유 | 훅/스크립트 실행 여부 |
|---|---|---|---|---|---|
| MCP for Unity (CoplayDev) | https://github.com/CoplayDev/unity-mcp | 에디터 씬·에셋·스크립트 조작, 콘솔 로그 읽기 | MIT | 프리팹·SO 에셋 생성, 콘솔 에러 확인을 자동화. 별 약 1.6만, 활발히 유지보수 | **예** — Python 3.10+/uv로 로컬 서버 상시 실행, Unity 패키지(UPM git URL) 설치 |
| Unity-MCP (IvanMurzak) | https://github.com/IvanMurzak/Unity-MCP | 에디터 도구·스킬·CLI, 테스트 실행까지 포함 | Apache-2.0 | 위와 같은 용도. 둘 중 하나만 고르면 됨 | **예** — 로컬 서버/CLI 실행, Unity 패키지 설치 |

## 제외한 항목 (참고)

- Donchitos의 나머지 스킬: 릴리스·라이브옵스·현지화·내러티브·오디오·스프린트 스토리 관리(`create-epics`, `dev-story` 등)는 1주 프로토타입 범위 밖. `/prototype`, `/setup-engine`은 웹(브라우저) 빌드나 Godot 중심이라 제외.
- Donchitos의 Godot·Unreal·DOTS·Addressables·네트워크 관련 에이전트와 규칙: 이 프로젝트와 무관.
- 출처가 불분명하거나 라이선스가 없는 Unity 스킬 저장소(예: creator-hian/claude-code-plugins, 라이선스 표기 없음)는 제외.
