# 구현 계획 (11/13 + 11/14 작업)

기준 문서는 `docs/GDD.md`다. 범위는 GDD 10장 일정의 11/13과 11/14 작업이다.

- 11/13: 프로젝트 세팅, 회색 박스 맵, 플레이어 이동·조준·구르기
- 11/14: 레이캐스트 사격, 피해·방어력 공식, HP, 무기 데이터 틀, 피격 번쩍임·혈흔·불꽃

반동·탄퍼짐·재장전·탄창, 나머지 무기 4종은 11/15 작업이므로 여기서 만들지 않는다.

## 결정 사항

2026-11-15에 사용자가 확정했다.

| # | 항목 | 결정 |
| --- | --- | --- |
| 1 | 입력 | Input System 패키지 사용. Active Input Handling은 "Input System Package"만 |
| 2 | 에셋 생성 | Editor 스크립트로 자동 생성 |
| 3 | 플레이어 이동 속도 | 5칸/초 (PlayerData SO 값, Inspector에서 조정) |
| 4 | 구르기 | 3칸을 0.4초 동안 이동 (무적 시간과 동일) |
| 5 | 멈춘 상태에서 Space | 조준 방향으로 구르기 (이동 중이면 이동 방향) |
| 6 | 구르기 중 사격 | 불가 |
| 7 | 경직 0.2초 | 이동·사격·구르기를 막음. 조준 회전은 가능 |
| 8 | 사격 입력 | 모든 무기가 좌클릭을 누르고 있으면 연사 속도로 계속 발사 |
| 9 | 사격 궤적 | 궤적선 없음. 명중점에만 작은 동그라미 표시 (레드 도트 사이트와 레이저 궤적은 나중에 부착물로 추가) |
| 10 | 테스트 표적 | AI 없이 HP·불꽃만 있는 TestTarget 프리팹 (HP 60). 11/16에 로봇으로 대체 |
| 11 | 카메라 | 플레이어 고정 추적, Orthographic Size 8 (카메라 Inspector 값) |
| 12 | 맵 | 60×40 전체 구역을 벽만으로 배치 (컨테이너·로봇·포드는 해당 일정에) |
| 13 | 혈흔 | 판 끝까지 유지. 최대 100개를 넘으면 오래된 것부터 제거 (개수는 PlayerData SO) |
| 14 | 무기 | WeaponData 틀 + 레이저 피스톨 에셋 1개를 Player 프리팹에 임시 연결. 나머지 4종은 11/15 |
| 15 | 플레이어 HP 0 | 조작만 멈춤 (사망·결과 처리는 11/18) |
| 16 | 피격 효과 | 플레이어 = 번쩍임 + 혈흔 + 경직, 로봇 = 불꽃만 |

공통 규칙: 1칸 = Unity 1유닛. 나중에 Inspector에서 바꿀 값은 ScriptableObject나 Inspector 필드에 둔다.

## 폴더 구조

```text
Assets/
  Art/Placeholder/      Square.png, Circle.png (단색 도형용 흰색 스프라이트)
  Data/                 PlayerData.asset, CombatConfig.asset
  Data/Weapons/         LaserPistol.asset
  Input/                GameInput.inputactions
  Prefabs/              Player, TestTarget, HitMarker, BloodDecal, Spark
  Scenes/               Station.unity
  Tiles/                WallTile.asset, FloorTile.asset
  Scripts/
    Camera/             CameraFollow
    Combat/             Health, DamageCalculator, HitFlash, BloodSplatter, SparkOnHit, TestTarget, TimedDestroy
    Data/               PlayerData, WeaponData, CombatConfig
    Player/             PlayerInputReader, PlayerController, PlayerRoll, PlayerShooter
    Editor/             ProjectSetup, StationLayout
```

## 에셋 생성 방식

- Unity 메뉴 `EscapeFromSpace > 에셋 생성·갱신`이 스프라이트, 타일, SO 에셋, 프리팹, 씬을 만든다.
- 이미 있는 에셋은 덮어쓰지 않는다. 없는 에셋, 없는 컴포넌트, 비어 있는 참조만 채운다. Inspector에서 바꾼 값과 손으로 고친 타일맵은 그대로 남는다.
- 기획서 초기값은 에셋을 처음 만들 때만 넣는다.

## 작업 순서

한 번에 한 단계씩 한다. 단계가 끝나면 컴파일을 확인하고, 사용자가 Unity에서 확인한 뒤 OK하면 커밋한다.

| 단계 | 내용 | Unity에서 확인할 것 |
| --- | --- | --- |
| 1 | 프로젝트 세팅, 회색 박스 맵, 이동·조준, 카메라 추적 | WASD 이동, 벽 충돌, 마우스 쪽으로 총구 회전, 카메라 추적, 맵 구역 |
| 2 | 구르기 | Space로 3칸 구르기, 방향 규칙, 쿨타임 3초, 벽에서 멈춤 |
| 3 | 레이캐스트 사격, 무기 데이터 틀, 레이저 피스톨, 명중점 표시, TestTarget | 좌클릭 유지 연사, 명중점 동그라미, 벽 비관통, 사거리, 구르기 중 사격 불가 |
| 4 | HP, 피해·방어력 공식, 플레이어 HP 0 처리 | 표적이 4발에 파괴, 방어력 값에 따른 피해 감소, HP 0에서 조작 정지 |
| 5 | 피격 효과 (번쩍임·혈흔·경직·불꽃) | 플레이어 번쩍임 0.1초, 혈흔 누적과 상한, 경직 0.2초, 표적 불꽃 |

이번 범위에는 플레이어를 공격하는 적이 없다. 4~5단계에서 플레이어 피격은 Health 컴포넌트의 Inspector 우클릭 메뉴(테스트용 피해 주기)로 확인한다.
