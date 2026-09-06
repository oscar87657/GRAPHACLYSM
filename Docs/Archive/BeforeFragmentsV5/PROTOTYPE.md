# GRAPHACLYSM Prototype · v0.8

2026-09-06 · 생성 던전, 계산기 전투, 이동 취소, 루나 새 일러스트.

## 실행과 조작

Unity 6000.3.23f1에서 프로젝트를 열고 Play한다. 기본 씬은 `Assets/Scenes/SampleScene.unity`이며 Bootstrap이 `GraphaclysmModernView`를 생성한다. 기존 Play가 켜져 있으면 종료 후 다시 시작한다. Full HD 권장, 1280×720도 확인했다.

1. 기록 시작 → 이안/루나 → 이 여정 시작 → 연결된 지도 노드 선택.
2. 전투 왼쪽 두 입력칸으로 x(t), y(t)를 편집한다. 처음에는 두 식 모두 `t-3`이다. 손패의 시작 카드가 필요하지 않다.
3. x(t)/y(t) 버튼으로 카드가 바꿀 좌표를 선택하고 기술을 사용한다. 선의 모양과 적중 예상이 갱신된다.
4. 이동으로 적의 조준을 피하거나 자신의 선에 들어간다. 작도 전에는 이동 취소로 위치·비용을 복원할 수 있다.
5. 작도 → 적 행동 → 다음 손패. 기본 식은 유지되고 이번 턴의 카드 변형은 지워진다.
6. 전투 보상과 이벤트·유물·휴식 선택으로 덱과 자원을 바꾸며 보스로 간다.

| 조작 | 기능 |
|---|---|
| 두 식 입력칸 / 계산기 버튼 | 숫자·사칙연산·괄호·삼각함수 입력 |
| x(t) / y(t) 버튼 | 이후 카드가 변형할 좌표 선택 |
| 지우기 / 한 글자 / 원래 식 | 선택 칸 비우기 / 끝 글자 삭제 / 마지막 유효 기본 식 복원 |
| 카드 클릭 / 숫자 1~9 | 해당 손패 사용. 수식 입력칸에 커서가 있으면 숫자는 글자로 입력 |
| 우클릭 / 한 장 되돌리기 | 마지막 카드의 변형·비용·손패·능력을 함께 취소 |
| 방향키 / 화면 화살표 | 상하좌우 1.5 이동. 입력칸에 커서가 있으면 방향키는 편집에 사용 |
| 이동 취소 / Backspace | 작도 전 이동과 실제 사용 비용·경쾌 복구. 입력 중 Backspace는 글자 삭제 |
| 궁극기 버튼 | 공명 6으로 준비, 다시 누르면 취소 |
| Enter / 작도 | 현재 유효 식 실행. 수식 입력 중에는 작도 버튼 사용 |
| 수식 보기 | 카드 변형까지 합친 최종 두 식 확인 |
| 카드·적 호버 / ?·Esc | 상세 / 도움말 |

직접 입력 가능한 문법은 `t`, `pi`, 숫자 0~12, 소수, `+ - * /`, 괄호, `sin(...)`, `cos(...)`, `abs(...)`, `sqr(...)`다. 곱셈 기호는 생략하지 않는다. 각 칸은 128자, 연산 24개, 괄호 깊이 16까지다. 잘못된 입력 중에는 마지막 유효 예측선을 남기고 카드 사용·작도를 잠근다. 잉크는 필드 안의 선 길이이며 0.05~64에서만 작도한다.

스피로그래프 실험: 위 칸 `2*cos(3*t)+cos(7*t)`, 아래 칸 `2*sin(3*t)-sin(7*t)`. 두 회전을 합친 실제 곡선이며 필드 x에 5가 더해진다. 숫자를 바꾸어 크기와 반복 수를 조정할 수 있다. 예제의 선 길이는 약 52.5다. 적 배치에 따라 이 식이 빗나갈 수 있다.

## 구현 범위와 한계

8층·전체 15~22개 방의 시드 생성 지도, 방 6종, 이벤트 6종, 적 배치 8종, 유물 6종, 기술 카드 11종, 각 12장 덱이 연결된다. 기본 손패 5장, 이안 HP 42/EN 4, 루나 HP 36/EN 5. 상태 8종과 회복·정화, 두 궁극기, 카드 제거, 완주·패배를 포함한다. 새 여정과 재시작은 다른 시드를 사용한다.

수식은 턴·전투 간 유지하고 카드 변형은 턴마다 초기화한다. 이동 취소는 실제 소비 EN만 돌려준다. 무료 이동이었다면 소비한 경쾌를 돌려주며 EN이 생기지 않는다. 카드 취소와 이동 취소는 별개다. 이벤트 비용은 비치명적이며 카드 제거를 취소하면 비용을 내지 않는다.

계산기 곡선은 768개 선분 근사다. 극단적인 반복·불연속 구간에는 근사 오차가 있고 적응형 샘플링은 미구현이다. 잉크·수치·덱은 플레이 피드백으로 조정할 초기 값이다. 저장·상점·강화·단계형 튜토리얼·새 음향·영상 컷인은 미구현이다. 완료 fixture는 실제 난이도로 8층을 플레이한 밸런스 결과가 아니다.

## 검증

- **Unity EditMode 85/85 통과**: [최종 XML](Logs/editmode-roguelike-v4-final.xml). 기존 69개와 계산기·이동 취소 8개, 던전·방 진행 8개.
- 수식 우선순위·실제 스피로그래프 좌표/명중·무효 입력의 원자성·잉크 한도·선택 좌표별 카드/취소·턴 유지·유료/무료 이동 취소를 검사한다.
- 100개 시드의 접근성과 보스 경로, 시드 재현·변화, 중앙/가장자리 적 배치, 이벤트 비용의 단일 적용, 유물 소진, 카드 제거와 취소, 지나가기를 검사한다.
- **실제 Unity Play Mode 23장, runner 게임 오류 0**: [캡처](Logs/RoguelikeV4Captures), [runner 결과](Logs/RoguelikeV4Captures/smoke-result.txt), [로그](Logs/roguelike-v4-smoke.log).
- 두 실제 생성 지도, 루나, 계산기, 수식 변경, 이동/복구, 무효 입력, y축 카드, 중심 작도, 다음 턴 식 유지, 이벤트·카드 제거·휴식·유물·전투 보상·완주, Full HD/720p를 확인한다. 방/보상/완주 전이와 게임 오류 수를 runner에서 단언한다.
- 고정 손패·적 HP·공명·방 연결을 설치하는 Editor fixture다. 사용자 Editor를 종료하지 않고 `Logs/CombatV2VerificationProject`에서 실행했다. 복사본은 영구 소스의 기준이 아니다.
- Unity 시작 단계의 SearchDatabase 내부 예외와 게임 runner 오류를 구분한다. 기존 `PERFORMANCE.md`의 이전 화면 측정치를 v4 성능으로 재사용하지 않는다.

## 진단 재실행

열려 있지 않은 검증 복사본에 최신 Assets/Packages/ProjectSettings를 반영한 다음 아래를 순차 실행한다. PowerShell에서 GUI 프로세스가 끝나기 전에 다음 Unity를 시작하지 않도록 기다린다.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$verification = (Resolve-Path 'Logs\CombatV2VerificationProject').Path
$testArgs = '-batchmode -projectPath "' + $verification + '" -runTests -testPlatform EditMode -testResults "' + $PWD + '\Logs\editmode-roguelike-v4-final.xml" -logFile "' + $PWD + '\Logs\editmode-roguelike-v4-final.log"'
Start-Process -FilePath $unity -ArgumentList $testArgs -WindowStyle Hidden -Wait
$smokeArgs = '-batchmode -projectPath "' + $verification + '" -executeMethod Graphaclysm.Editor.RoguelikeV4Smoke.RunBatch -logFile "' + $PWD + '\Logs\roguelike-v4-smoke.log"'
Start-Process -FilePath $unity -ArgumentList $smokeArgs -WindowStyle Hidden -Wait
```

스크린샷 실행에는 `-nographics`나 `-quit`을 붙이지 않는다. runner가 종료한다. V2/V3 진단은 고정 맵 시절의 역사적 fixture로, 현재 생성 런의 확인은 RoguelikeV4Smoke를 사용한다.

작업 전 사본은 `Logs/BeforeRoguelikeV4-*`, 이전 문서는 [Docs/Archive/BeforeRoguelikeV4](Docs/Archive/BeforeRoguelikeV4)에 있다. 이안 원본과 기존 초상은 보존했다. 루나 새 투명 PNG 및 생성 기록은 [아트 방향](ART_DIRECTION.md), [프롬프트](Docs/ART_PROMPTS_V4.md)를 참고한다.
