# GRAPHACLYSM Prototype · v0.10

2026-09-06 · 수식 파편 전투 v5 / 정보 정리·루나 v6.

## 실행

Unity 6000.3.23f1에서 `Assets/Scenes/SampleScene.unity`를 열고 Play한다. 기존 Play가 실행 중이면 종료 후 다시 시작한다. 사용자가 열어 둔 Editor는 강제 종료하지 않았다. 기본 화면은 GraphaclysmModernView다.

기록 시작 → 이안/루나 → 여정 시작 → 생성 지도 노드 선택. 8층 던전·이벤트·유물·휴식·덱 제거는 유지한다. 메인 화면의 인물을 제거하고 루나는 긴 흰머리와 밤 보라색 고스풍 스탠딩으로 교체했다.

## 전투 조작

1. 어느 손패든 선택한다. 기호의 빈칸에 앞선 궤적 전체가 들어가며 왼쪽 조립대에 파편이 쌓인다.
2. 바로 방출하거나, 응축으로 적의 턴을 넘기고 식·손패를 보존하면서 카드를 더 받는다.
3. 응축은 최대 두 번, 손패와 조립대는 각각 최대 여덟 장이다. 기본 턴 보충은 두 장이며 드로우 파편과 유물로 늘어난다.
4. 방출하면 쌓은 수식·능력이 한 번에 발동한다. 첫 장 이후 카드마다 조립 피해 +2. 남은 손패는 유지하고 식은 다음 턴에 비운다.

| 조작 | 기능 |
|---|---|
| 카드 클릭 / 숫자 1~8 | 다음 파편을 바깥에 붙임 |
| 카드 호버 | 정확한 변형·대상·수명·드로우 확인. 많은 손패에서는 해당 카드 확대 |
| 우클릭 / 마지막 파편 되돌리기 | 이번 응축 이후 놓은 파편을 역순으로 취소 |
| 응축 · 넘기기 / Space | 식·손패 보존, 파편 확정, 적 행동과 보충. 최대 두 번 |
| 방출 / Enter | 현재 조립 발동. 비용 없음 |
| `···` → 해체하고 넘기기 | 조립 파편을 버리고 적 행동·기본 보충. 공격과 파편 드로우 보너스 없음 |
| 방향키 / 화면 화살표 | 턴당 한 번 무료 이동 |
| 이동 취소 / Backspace | 작도 전 위치·이동 기회 복구 |
| 궁극기 | 공명 6으로 준비/취소, 실제 방출에만 소비 |
| 상세 → 수식 보기 | 각 단계가 이전 궤적 F를 감싸는 정확한 규칙 |
| 상세 / 적 호버 | 상태·좌표·예상 피해 등 추가 정보 |
| ? / Esc | 조작 안내 |

계산기·문자 입력·x/y 선택·에너지 비용은 현재 전투에 없다. 카드들은 공통 회전 F₀를 바탕으로 작도하므로 시작 카드가 필요하지 않다. 조립대가 비어 있으면 작도할 수 없다. 잔향과 윤무는 순서를 바꾸면 다른 무늬가 나온다. 개화·유리정원·성운으로 반경·복소 제곱·추가 회전을 중첩할 수 있다.

드로우는 카드를 클릭하자마자 지급하지 않는다. 다음 응축/방출 뒤 보충에 한 번 적용한다. 응축한 파편은 되돌릴 수 없고 예약 카드는 드로우 더미에 섞이지 않는다. 손패 한도 때문에 받지 못한 드로우는 저장하지 않는다. “턴 드로우”는 한도가 허용할 때 받을 보충량이다.

## 검증과 한계

- **v5 Unity EditMode 97/97 통과 기록:** [최종 XML](Logs/editmode-fragments-v5-final.xml). 기존 회귀 85개와 새 파편 검사 12개.
- 실제 역회전 좌표/명중, 순서에 따른 다른 곡선과 취소, 슬롯·주파수 제한, 무료 사용·이동, 예약 카드 보존, 드로우 단일 적용, 확정 후 취소 거부, 두 번 응축 제한, 방출/해체, 적·자신 동시 적중, 실제 런의 드로우 유물을 검사한다.
- v5 [Unity 캡처 24장](Logs/FragmentsV5Captures), [runner 결과](Logs/FragmentsV5Captures/smoke-result.txt), [최종 로그](Logs/fragments-v5-smoke-final.log). 실제 생성 지도·초기 손패, 파편 중첩·응축·방출, 이동 취소, 8장 손패·720p, 방·덱 제거·새 유물·카드 보상·완주를 확인한다.
- 재현용 손패·적 HP·방 연결을 설치한 Editor fixture이며 전체 런을 실제 난이도로 완주한 결과가 아니다. 자동 검사는 재미·밸런스를 보증하지 않는다.
- 수식은 1,536개 선분 근사, 최대 주파수 96이다. 한도를 넘기는 파편은 거부한다. 적응형 샘플링·임의 수식·전체 프랙탈 문법은 지원하지 않는다. 기존 잉크 64 제한은 현재 모드에서 쓰지 않는다.
- 응축 횟수·보충량·조립 피해·적 수치는 플레이 피드백으로 조정할 초기 값이다. 저장·상점·강화·단계형 튜토리얼·새 음향은 미구현이다.
- 격리된 `Logs/CombatV2VerificationProject`에서 검사했다. Unity 시작 단계 SearchDatabase 내부 예외와 게임 runner 오류를 구분한다. 이전 PERFORMANCE 측정치를 v5 전체 성능으로 재사용하지 않는다.

## 진단 재실행

열려 있지 않은 검증 복사본에 최신 Assets/Packages/ProjectSettings를 반영한 다음 아래를 순차 실행한다. PowerShell에서 GUI 프로세스가 끝나기 전에 다음 Unity를 시작하지 않도록 기다린다.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$verification = (Resolve-Path 'Logs\CombatV2VerificationProject').Path
$testArgs = '-batchmode -projectPath "' + $verification + '" -runTests -testPlatform EditMode -testResults "' + $PWD + '\Logs\editmode-fragments-v5-final.xml" -logFile "' + $PWD + '\Logs\editmode-fragments-v5-final.log"'
Start-Process -FilePath $unity -ArgumentList $testArgs -WindowStyle Hidden -Wait
$smokeArgs = '-batchmode -projectPath "' + $verification + '" -executeMethod Graphaclysm.Editor.QuietV6Smoke.RunBatch -logFile "' + $PWD + '\Logs\quiet-v6-smoke-final.log"'
Start-Process -FilePath $unity -ArgumentList $smokeArgs -WindowStyle Hidden -Wait
```

스크린샷 실행에는 `-nographics`나 `-quit`을 붙이지 않는다. runner가 종료한다. V2/V3/V4 진단은 고정 맵 시절의 역사적 fixture로, 전체 생성 런 흐름 fixture는 FragmentsV5Smoke, 최신 표시 fixture는 QuietV6Smoke를 사용한다.

작업 전 사본은 `Logs/BeforeQuietV6-20260906-133854`, 문서 보관은 [Docs/Archive/BeforeQuietV6](Docs/Archive/BeforeQuietV6)에 있다. 루나 v6는 image_gen 참조 편집으로 제작했다. [전체 생성 기록](Docs/ART_PROMPTS_V6.md)과 최종 투명 PNG를 보관한다. 현재 규칙은 [COMBAT_REDESIGN.md](COMBAT_REDESIGN.md)를 따른다.

## v6 화면 검증

[최종 17장](Logs/QuietV6Captures), [runner 결과](Logs/QuietV6Captures/smoke-result.txt), [최종 실행 로그](Logs/quiet-v6-smoke-final.log). Full HD/720p에서 타이틀 인물 제거, 서로 반대인 스탠딩, 간결한 전투, 카드 호버, 상세 열기, 응축/방출/이동 취소, 8장 손패, 보조 메뉴, 방과 덱 제거를 확인했다. Runtime errors 0. 위 97개 EditMode는 v5 실행 기록이며 이번 표시 작업에서 Core/Application 변경 없이 UI fixture를 재실행했다. 소스 비교는 `Logs/quiet-v6-source-verification.txt`에 기록한다.

전투 기본 카드에는 이름·기호·드로우 배지만 표시한다. 마우스를 올리면 상세 능력이 열린다. 궁극기 설명도 호버/상세에서 읽고, 새 행동 피드백은 3초간 보인다. 전투 밖 보상과 덱 제거 카드는 능력 요약을 계속 표시한다.
