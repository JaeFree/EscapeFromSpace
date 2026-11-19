# ESCAPE from Space

> 챙길수록 무거워지고, 산소는 줄어든다.

방어망에 맞아 행성 케르-7의 "우주선 무덤"에 불시착한 회수 업자가 되어, 협곡에 떨어진 우주선들을 뒤져 물자를 챙기고, 보안 로봇과 에일리언을 피하거나 파괴하며, 산소가 떨어지기 전에 귀환 비콘까지 살아 나가는 1인용 탑다운 SF 익스트랙션 슈터입니다.

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6 (6000.0.85f1), Universal 2D (2D URP), C# |
| 플랫폼 | Windows, 키보드 + 마우스 |
| 개발 기간 | 2026.11.13 ~ (1인 개발, AI 협업) |

## 게임 루프

1. **대기실**: 창고에서 장비를 골라 가방에 넣고, 상인에게 사고팔고, 테크 트리를 강화하고, 퀘스트를 받습니다.
2. **레이드**: 출격 해치로 우주선 무덤에 나갑니다. 대기가 독성이라 산소는 8분입니다.
   - 8개 구역의 컨테이너와 로봇 잔해를 뒤집니다.
   - 칸 배치 가방에 챙길수록 무거워져 느려집니다.
3. **탈출**: 레이드 시작 1분 뒤 열리는 귀환 비콘 3곳 중 하나에서 X를 5초 누릅니다.
   - 성공하면 가방째 창고로 돌아갑니다.
   - 죽으면 보관함 1칸 말고는 모두 잃습니다.
4. **목표**: 퀘스트로 부품을 모아 회수선의 도약 엔진을 고치면 엔딩입니다.

## 주요 기능

- 무기 5종 (광선검, 레이저 피스톨, 펄스 라이플, 저격총, 샷건): 히트스캔 사격, 탄퍼짐·반동, 탄창·재장전, 부착물 5종
- 보안 로봇 5종, 에일리언 2종, 네임드 7기: 순찰·의심·경계·공격·추격·복귀 상태 머신, 시야·총소리·발소리 감지, A* 길찾기, 충전·조준선 예고, 날아가는 산성 침
- 은신: 발견 게이지, 살금 걷기(Ctrl), 엄폐(엄폐물이 하얗게 빛남), 뒤에서 베기, 소음기, 홀로그램 미끼, EMP 수류탄, 은신 테크 트리
- 판마다 달라지는 판: 로봇·컨테이너 무작위 배치, 우주선 이변(정전, 보안 경계, 에일리언 습격)
- 칸 배치 가방 (회전, 묶음 나누기, 더블클릭 옮기기), 무게에 따른 감속, 방어구, 퀘스트로 커지는 보관함, 따라다니는 로봇 개(가방 3칸)
- 산소와 질식, 귀환 비콘, 사망 시 손실, 가 본 곳을 기억하는 전체 맵 (M)
- 돈·상인 5명·제작소, 데이터 칩으로 여는 설계도, 레벨과 테크 트리, 퀘스트 21개, 프롤로그·엔딩 장면
- 시작 화면, 설정 메뉴 (음량, 도움말), 저장 (JSON)

## 실행

- **빌드**: Unity에서 메뉴 `EscapeFromSpace > Windows 빌드`를 누르면 `Builds/` 아래에 exe가 생깁니다. `Builds/`는 저장소에 올리지 않습니다.
- **에디터에서 Play**: `Assets/Scenes/Title` 씬에서 시작합니다.
  - 대기실부터 보려면 `Lobby` 씬에서 Play합니다.
  - `Station` 씬에서 바로 Play하면 테스트용 장비로 레이드를 시작합니다.
- **에셋 다시 만들기**: 메뉴 `EscapeFromSpace > 에셋 생성·갱신`. 이미 있는 에셋과 Inspector에서 바꾼 값은 건드리지 않습니다.

## 조작

| 조작 | 키 |
| --- | --- |
| 이동 / 구르기 | WASD / Space |
| 살금 걷기 | Ctrl (누르는 동안) |
| 조준 / 사격 | 마우스 / 좌클릭 (저격총은 우클릭으로 조준) |
| 재장전 | R |
| 컨테이너 열기 | E (길게) |
| 상인·단말기·엔진 수리 | E |
| 도구칸 (메디젤·메디킷·산소 캔·홀로그램 미끼·EMP) | 1~5 |
| 탈출 (귀환 비콘 안에서) | X (길게) |
| 가방 / 아이템 회전 | Tab / 가방을 연 상태에서 R |
| 아이템 옮기기 | 드래그, 더블클릭 |
| 전체 맵 | M |
| 설정 / 창 닫기 | Esc |

## 문서

| 문서 | 내용 |
| --- | --- |
| [`docs/GDD.md`](docs/GDD.md) | 기획서. 구현의 기준이고, 수치와 결정 기록이 있습니다 |
| [`docs/COMBAT.md`](docs/COMBAT.md) | 전투 시스템 설명 (사격, 방어력, 로봇 AI, 밸런스 조정) |
| [`docs/PLAN.md`](docs/PLAN.md) | 단계별 작업 계획과 세부 결정 |
| [`docs/PROGRESS.md`](docs/PROGRESS.md) | 진행 기록 |

## 프로젝트 구조

```
Assets/
  Scripts/    Player, Enemy, Combat, Inventory, Raid, Lobby, UI, Save, Audio, Data, Editor
  Data/       무기·로봇·아이템·퀘스트·테크 트리 등 수치 (ScriptableObject, Inspector에서 밸런싱)
  Scenes/     Title, Lobby, Station
  Art/, Audio/  아래 출처의 CC0 그림과 소리
```

- 수치는 코드에 넣지 않고 ScriptableObject 에셋으로 분리했습니다. Inspector에서 플레이하며 조정합니다.
- 씬과 에셋은 에디터 스크립트(`Assets/Scripts/Editor/ProjectSetup*.cs`)가 만듭니다. 없을 때만 만들고, 사용자가 바꾼 값은 그대로 둡니다.

## 에셋 출처

모든 그림, 효과음, 음악은 CC0 (퍼블릭 도메인, https://creativecommons.org/publicdomain/zero/1.0/)입니다. 표시 의무는 없지만 감사의 뜻으로 적습니다. 폴더마다 원본 라이선스 파일이 있습니다.

| 쓰는 곳 | 팩 (제작자) | 출처 |
| --- | --- | --- |
| 플레이어, 로봇, 무기, 효과 | Space Shooter Redux, Space Shooter Extension, Topdown Shooter (Kenney) | https://kenney.nl |
| 효과음 | Sci-Fi Sounds, Digital Audio, Impact Sounds, Interface Sounds (Kenney) | https://kenney.nl |
| 총, 헬멧, 방탄복, 퀀텀 코어 | Futuristic Guns, Weapons and Items (knekko) | https://opengameart.org/content/futuristic-guns-weapons-and-items |
| 회로 부품 등 재료 아이콘 | Cybericons (knekko) | https://opengameart.org/content/cybericons |
| 맵 바닥·벽·소품 | Space Sci-Fi RPG Tiles 48x48 (Hyptosis) | https://opengameart.org/content/space-scifi-rpg-tiles-48x48 |
| 맵 소품 | Sci-fi Object Set (Jannax) | https://opengameart.org/content/sci-fi-object-set |
| 서버 랙, 사물함 | Warped Top-Down Tech Lab 2 (ansimuz) | https://opengameart.org/content/warped-top-down-tech-lab-2 |
| 대기실 음악 | Bird in Hand Night (Fupi, 원곡 Jason Shaw "Bird in Hand") | https://opengameart.org/content/bird-in-hand-night |

침대·진료 침대 소품과 프롤로그·엔딩 장면은 에디터 스크립트와 코드로 직접 그렸습니다.

## 개발 방식

Claude Code(AI)와 협업해 만들었습니다. 사람이 기획서(`docs/GDD.md`)의 방향과 결정을 정하고, AI가 구현·테스트·문서화를 맡았습니다.

- 기획서에 없는 기능은 넣지 않고, 비어 있거나 충돌하는 부분은 먼저 물어 정했습니다 (`CLAUDE.md`).
- 단계마다 배치 모드 컴파일, 임시 플레이 모드 테스트, 화면 캡처로 확인했습니다.
- 밸런스는 자동 플레이어를 실제 엔진에서 돌려 측정한 결과로 조정했습니다 (기획서 11.3).
