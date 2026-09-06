# GRAPHACLYSM Prototype · v0.9

2026-09-06 · 수식 파편·보존 손패·응축과 방출 v5.

## 실행

Unity 6000.3.23f1에서 `Assets/Scenes/SampleScene.unity`를 열고 Play한다. 기존 Play가 실행 중이면 종료 후 다시 시작한다. 사용자가 열어 둔 Editor는 강제 종료하지 않았다. 기본 화면은 GraphaclysmModernView다.

기록 시작 → 이안/루나 → 여정 시작 → 생성 지도 노드 선택. v4의 8층 던전·이벤트·유물·휴식·덱 제거와 루나 새 디자인은 유지된다.

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
| 해체 · 넘기기 | 조립 파편을 버리고 적 행동·기본 보충. 공격과 파편 드로우 보너스 없음 |
| 방향키 / 화면 화살표 | 턴당 한 번 무료 이동 |
| 이동 취소 / Backspace | 작도 전 위치·이동 기회 복구 |
| 궁극기 | 공명 6으로 준비/취소, 실제 방출에만 소비 |
| 수식 보기 | 각 단계가 이전 궤적 F를 감싸는 정확한 규칙 |
| ? / Esc | 조작 안내 |

계산기·문자 입력·x/y 선택·에너지 비용은 현재 전투에 없다. 카드들은 공통 회전 F₀를 바탕으로 작도하므로 시작 카드가 필요하지 않다. 조립대가 비어 있으면 작도할 수 없다. 잔향과 윤무는 순서를 바꾸면 다른 무늬가 나온다. 개화·유리정원·성운으로 반경·복소 제곱·추가 회전을 중첩할 수 있다.

드로우는 카드를 클릭하자마자 지급하지 않는다. 다음 응축/방출 뒤 보충에 한 번 적용한다. 응축한 파편은 되돌릴 수 없고 예약 카드는 드로우 더미에 섞이지 않는다. 손패 한도 때문에 받지 못한 드로우는 저장하지 않는다. “턴 드로우”는 한도가 허용할 때 받을 보충량이다.

## 검증과 한계

- **Unity EditMode 97/97 통과:** [최종 XML](Logs/editmode-fragments-v5-final.xml). 기존 회귀 85개와 새 파편 검사 12개.
- 실제 역회전 좌표/명중, 순서에 따른 다른 곡선과 취소, 슬롯·주파수 제한, 무료 사용·이동, 예약 카드 보존, 드로우 단일 적용, 확정 후 취소 거부, 두 번 응축 제한, 방출/해체, 적·자신 동시 적중, 실제 런의 드로우 유물을 검사한다.
- [Unity 캡처 24장](Logs/FragmentsV5Captures), [runner 결과](Logs/FragmentsV5Captures/smoke-result.txt), [최종 로그](Logs/fragments-v5-smoke-final.log). 실제 생성 지도·초기 손패, 파편 중첩·응축·방출, 이동 취소, 8장 손패·720p, 방·덱 제거·새 유물·카드 보상·완주를 확인한다.
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
$smokeArgs = '-batchmode -projectPath "' + $verification + '" -executeMethod Graphaclysm.Editor.FragmentsV5Smoke.RunBatch -logFile "' + $PWD + '\Logs\fragments-v5-smoke-final.log"'
Start-Process -FilePath $unity -ArgumentList $smokeArgs -WindowStyle Hidden -Wait
```

스크린샷 실행에는 `-nographics`나 `-quit`을 붙이지 않는다. runner가 종료한다. V2/V3/V4 진단은 고정 맵 시절의 역사적 fixture로, 현재 생성 런의 확인은 FragmentsV5Smoke를 사용한다.

작업 전 사본은 `Logs/BeforeFragmentsV5-*`, 문서 보관은 [Docs/Archive/BeforeFragmentsV5](Docs/Archive/BeforeFragmentsV5)에 있다. 새 이미지 생성은 없으며 이안·루나·배경은 이전 자산을 유지한다. 현재 규칙은 [COMBAT_REDESIGN.md](COMBAT_REDESIGN.md)를 따른다.
