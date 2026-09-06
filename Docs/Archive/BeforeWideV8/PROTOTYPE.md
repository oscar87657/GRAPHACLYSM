# GRAPHACLYSM Prototype · v0.11

2026-09-06 · 카드 사전·기준점 이동·3층 원정 v7.

## 실행

Unity 6000.3.23f1에서 Assets/Scenes/SampleScene.unity를 열고 Play한다. 실행 중이었다면 Play를 종료하고 다시 시작한다. 사용자 Editor를 강제 종료하지 않았고 별도 검증 복사본에서 검사했다.

메인 → 카드 사전에서 17종 카드와 14종 유물, 역할 필터·페이지·상세 효과·첫 파편 실제 곡선 예시를 확인한다. 카드 사전은 덱이나 전투 상태를 바꾸지 않는다. 메인 → 기록 시작 → 이안/루나 → 여정 시작으로 3층 원정에 들어간다.

## 조작

| 조작 | 동작 |
|---|---|
| 손패 클릭 / 숫자 1~8 | 이전 식 전체를 감싸는 파편 추가 |
| 카드 호버 | 카드의 수식·대상·효과·수명 확인. 많은 손패에서는 카드 확대 |
| 우클릭 / 파편 되돌리기 | 이번 응축 이후 새로 붙인 파편만 역순 취소 |
| 방출 / Enter | 현재 궤적·효과 발동. 이후 적 행동과 기본 2장+보너스 보충 |
| 응축 / Space | 식·손패 유지, 체력 첫 2/두 번째 4 소비, 적 행동, 기본 1장+카드 보너스 최대 1장 |
| 해체 → 해체하고 넘기기 | 조립 버리기, 공격/카드 드로우 보너스 없이 적 행동과 기본 보충 |
| 방향키 / 왼쪽 하단 이동 버튼 | 턴당 한 번 무료 이동 |
| 이동 취소 / Backspace | 작도 전에 위치와 이동 기회 복구 |
| 궁극기 | 공명 6으로 준비/취소, 실제 방출에 소비 |
| 상세 → 수식 보기 | 현재 O+F(t)의 기준점과 각 단계 규칙 |
| 적 호버 / 상세 | 상태·예상 피해 |
| ? / Esc | 조작 안내. 사전에서 Esc는 사전 닫기 |

카드는 이름·문양·수식·짧은 동작/역할·위력 또는 드로우 배지로 구분한다. 문양은 카드의 상징이고 조합 후 출력 곡선은 필드에서 미리 볼 수 있다. 보상·덱 제거에서는 부가 능력 요약도 표시한다.

## 새 카드와 응축

귀환점은 사용 당시 플레이어 위치로 기준점을 옮긴다. 서쪽의 문은 기준점 왼쪽 2, 승천은 위로 1.5다. 그래프와 중심에서 퍼지는 작도 이펙트가 함께 이동한다. 이후 파편은 이동한 기준점에서 F를 변형한다. 여백·낙화의 F 내부 평행 이동은 이후 회전에 영향을 받는 다른 연산이다.

속삭임은 작은 1/4 역회전, 오엽성은 5갈래 반경, 격류는 1.7배 확대, 긴 황혼은 가로 1.4·세로 0.65다. 카드마다 위력과 능력 수치를 구분했다. 조립 위력은 카드별 0~3, 합계 최대 10이다.

응축 비용은 보호막과 무관하며 체력이 비용 이하이면 쓸 수 없다. 기본 보충은 1장, 파편의 추가 드로우는 합계 최대 1장만 적용한다. 넘친 보너스는 소비하고 이후 방출에서 복원하지 않는다. 턴 드로우 유물은 응축에 적용하지 않는다. 방출/해체 후에는 기본 2장+일반 턴 보충 유물이 적용된다. 예약·손패 각각 최대 8장, 응축은 방출/해체 사이 두 번이다.

## 원정

총 3층, 층마다 보스를 포함해 8개 방을 방문한다. 생성 노드는 전체 45~66개, 한 경로는 24개 방이다. 현재 층만 지도에 표시한다. 1·2층 보스 뒤 체력 8 회복·유물 보상, 3층 보스 뒤 완주한다. 체력·공명·덱·유물은 층 사이에 유지한다. 후반 층 적 체력·공격력이 증가한다.

유물은 14종이다. 추가 8종은 시작 보호막/공명, 자가 적중 보호막, 첫 방출 피해, 3개 이하/6개 이상 조립 피해, 이동 후 방출 보호막, 승리 공명을 제공한다. [전투 수치와 전체 목록](COMBAT_REDESIGN.md)을 따른다.

## 검증

- **Unity EditMode 111/111 통과:** [XML](Logs/editmode-expedition-v7.xml), [로그](Logs/editmode-expedition-v7.log). 기존 회귀와 V7 추가 14개. 새 기준점의 실제 좌표·적중·취소·초기화·공개 반경, 계수 변화, 응축 비용과 보너스 제한, 조건부 유물, 층 보스의 보상/진행을 검사했다.
- 생성 지도 100개 시드의 모든 노드 접근성, 각 층 비전투/유물/보스 보장도 검사한다.
- **실제 화면 23장:** [캡처](Logs/ExpeditionV7Captures), [결과](Logs/ExpeditionV7Captures/smoke-result.txt), [최종 로그](Logs/expedition-v7-smoke-final.log). Full HD/720p, 사전 필터·페이지·유물, 세 층 지도, 새 카드 문양, 이동 기준점/작도, 응축 비용, 이동 취소, 8장 손패, 방/덱 제거. Runtime errors 0.
- 화면 fixture는 재현용 손패·적 HP·지도 진행을 설치하고 8장 손패를 직접 채운다. 전체 런을 실전 난이도로 완주한 결과가 아니다. 재미·최종 밸런스·새 Profiler 수치를 보증하지 않는다.
- 최종 소스와 검증 사본 비교: Logs/expedition-v7-source-verification.txt. Unity 시작 시 SearchDatabase 내부 예외와 게임 Runtime 오류를 구분했다.

## 진단 재실행

검증 사본에 최신 Assets/Packages/ProjectSettings를 반영하고 순차 실행한다. 기존 Unity 프로세스가 끝난 뒤 다음 명령을 실행한다.

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$verifyPath = (Resolve-Path 'Logs\CombatV2VerificationProject').Path
$testArgs = '-batchmode -projectPath "' + $verifyPath + '" -runTests -testPlatform EditMode -testResults "' + $PWD + '\Logs\editmode-expedition-v7.xml" -logFile "' + $PWD + '\Logs\editmode-expedition-v7.log"'
Start-Process -FilePath $unityExe -ArgumentList $testArgs -WindowStyle Hidden -Wait
$smokeArgs = '-batchmode -projectPath "' + $verifyPath + '" -executeMethod Graphaclysm.Editor.ExpeditionV7Smoke.RunBatch -logFile "' + $PWD + '\Logs\expedition-v7-smoke-final.log"'
Start-Process -FilePath $unityExe -ArgumentList $smokeArgs -WindowStyle Hidden -Wait
```

화면 검증에는 -nographics/-quit을 붙이지 않는다. runner가 종료한다. V2~V6 fixture는 과거 규칙 기록이다. 현재 기본 검증은 ExpeditionV7Smoke다. 작업 전 소스는 Logs/BeforeExpeditionV7-20260906-140614, 문서는 Docs/Archive/BeforeExpeditionV7에 있다.

아트는 이안 v2·루나 v6·배경 v3를 유지했고 새 bitmap 생성은 없다. 저장·상점·강화·단계형 튜토리얼·신규 음향은 미구현이다. 수학은 1,536선분/최대 주파수 96의 근사이며 임의 수식·무제한 프랙탈은 지원하지 않는다.
