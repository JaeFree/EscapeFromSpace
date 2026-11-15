# ESCAPE from Space 제작 지침

## 목표

`docs/GDD.md` (ESCAPE from Space 프로토타입 기획서)를 기준으로 Unity 6 (2D URP, C#) 게임을 제작한다.

- 기획서에 없는 기능은 추가하지 않는다.
- 기획서 안에서 충돌하거나 비어 있는 부분은 임의로 정하지 말고 먼저 질문한다. `docs/GDD.md`의 "열린 질문" 항목도 답을 받기 전에는 구현하지 않는다.

## 프로젝트 정보

- Unity 버전: 6000.0.85f1, 템플릿은 Universal 2D (2D URP)
- 시점: 탑다운 2D, 마우스 방향 조준
- 작업 환경: 학교 PC와 집 PC를 오가며 작업한다. PC에 묶이는 절대 경로나 로컬 설정은 저장소에 넣지 않는다.

## 코드 규칙

- C# 표준(Microsoft/Unity 관례)을 따른다.
  - 클래스, 메서드, 프로퍼티, enum: `PascalCase`
  - 지역변수, 매개변수: `camelCase`
  - private 필드: `_camelCase`
  - 인터페이스: `I` 접두사
- 인스펙터 노출은 `public` 필드 대신 `[SerializeField] private`를 쓴다.
- 들여쓰기는 스페이스 4칸, 중괄호는 새 줄(Allman)로 하고 `.editorconfig`로 고정한다.
- 주석은 한국어로, "왜 이렇게 했는지"만 쓴다.
- 기획서 표의 수치는 코드에 넣지 말고 ScriptableObject 데이터로 분리한다.

## 밸런싱 규칙

- 기획서의 수치는 모두 초기값이다. ScriptableObject 에셋에 초기값을 넣어 두고, 최종 값은 사용자가 Unity Inspector에서 직접 플레이하며 조정한다.
- 사용자가 Inspector에서 바꾼 에셋 값을 코드나 기획서 값으로 되돌리지 않는다.

## 작업 기록 규칙

- 작업을 마칠 때마다 `docs/PROGRESS.md`에 오늘 한 일, 다음에 할 일, 막힌 점을 갱신한다. 학교 PC와 집 PC에서 대화 기록이 이어지지 않으므로, 다음 세션은 이 파일과 `docs/PLAN.md`를 읽고 시작한다.

## 스킬 사용 규칙

1. <https://github.com/Donchitos/Claude-Code-Game-Studios> 의 스킬·에이전트·규칙을 읽는다. GitHub 등에서 Unity 2D 슈터 제작에 필요하거나 도움이 될 스킬도 추가로 찾는다.
2. 후보를 `docs/skill-candidates.md`에 표로 정리한다.

   | 이름 | 출처 URL | 한 줄 용도 | 라이선스 | 이 프로젝트에 필요한 이유 | 훅/스크립트 실행 여부 |
   |---|---|---|---|---|---|

3. 목록을 나에게 보여주고 기다린다. **내가 고른 스킬만** 설치하고 사용하며, 승인 전에는 아무것도 설치하지 않는다.
4. 1주짜리 프로젝트이므로 저장소의 `rigor` 설정은 `minimal`로 둔다.
