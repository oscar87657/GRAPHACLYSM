# GRAPHACLYSM Prototype · v0.7

2026-09-06 · 기술 카드와 전체 화면 v3. [게임 기획](GAME_DESIGN_DOCUMENT.md), [전투 규칙](COMBAT_REDESIGN.md), [아트 방향](ART_DIRECTION.md).

## 실행과 조작

Unity 6000.3.23f1에서 프로젝트를 열고 Play한다. Bootstrap이 `GraphaclysmModernView`를 생성한다. 기본 확인 화면은 `Assets/Scenes/SampleScene.unity`, 권장 Game 뷰 1920×1080이며 1280×720도 확인했다. 이전 화면이 이미 실행 중이면 Play를 종료했다가 다시 시작한다.

1. `기록 시작` → 이안 또는 루나 → `이 여정 시작` → 빛나는 맵 노드 선택.
2. 손패 5장에서 `초점` 또는 `월륜`으로 선을 시작한다. 다른 기술을 이어 선을 바꾼다. 카드 한 장의 부가 능력은 함께 붙는다.
3. 예측선과 적 피해·자신 강화 표시를 확인한다. 카드에 마우스를 올리면 효과 대상·수식·지속시간과 사용 불가 이유를 읽을 수 있다.
4. 방향키 또는 화살표로 한 번 이동한다. 공명 6이 모이면 왼쪽 궁극기를 준비할 수 있다.
5. `작도` 또는 Enter. 중심에서 선이 새겨지고 적 행동과 다음 손패가 이어진다.

| 조작 | 기능 |
|---|---|
| 카드 클릭 / 숫자 1~9 | 해당 손패 선택 |
| 우클릭 / 한 장 되돌리기 | 마지막 카드의 수식·비용·손패·향후 능력을 함께 취소 |
| 방향키 / 화살표 | 상하좌우 1.5 이동, 턴당 한 번, 1 EN |
| 궁극기 버튼 | 공명 6으로 준비, 다시 누르면 취소 |
| Enter / 작도 | 현재 수식 실행 |
| 수식 보기 | 완성 수식 펼치기·접기 |
| 적 / 오른쪽 의도 패널 호버 | 해당 적의 조준·이동 목적지·상태 상세 |
| ? / Esc | 안내 열기·닫기 |

예: `초점 → 낙화`는 y=x−6으로 (4,−2)의 자신을 맞혀 보호막 7을 준다. `초점 → 잔향 → 낙화`는 y=sin(x−5)−1이다. 다른 카드의 능력도 **그 선에 닿은 대상에만** 적용된다.

## 구현 범위와 한계

실제 카드 풀 13종, 시작 덱 각 12장, 기본 손패 5장. 이안 HP 42 / EN 4, 루나 HP 36 / EN 5. 8종 상태·회복·정화, 공명과 두 궁극기, 3층 분기 맵·카드 보상·유물 6종·완주·패배가 연결된다.

상아색 배경·먹빛 정면 좌표판, Orbit/Pretendard/Cormorant 글꼴, 등급 테두리와 간결한 카드, 중심 작도·적중 파편·궁극기 이름 띠를 적용했다. 초상은 기존 v2, 배경은 v3다. 새 음향·전용 전투 포즈·영상 컷인·저장·상점·단계형 튜토리얼은 미구현이다.

완성 그래프와 분리형 각인 카드는 legacy 수학·회귀 코드에 남아 있고 실제 덱·보상에서는 쓰지 않는다. 뉴턴 수렴 등 모든 고급 수학을 새 카드 조합으로 만들 수 있는 것은 아니다. 극단적 반복 연산은 160선분 근사의 한계가 있다. 수치·덱은 플레이 피드백으로 조정할 초기 값이다.

## 검증

- **Unity EditMode 69/69 통과**: [결과 XML](Logs/editmode-visual-v3-final.xml). 기존 63개와 새로운 기술 카드 검사 6개.
- 추가 검사는 수식·능력의 결합, 실제 적중 전 효과 미적용, 적/자신 대상 분리와 빗나감, 원자적 취소·에너지 복원, 여러 능력의 합산, 새 노출의 적용 순서, 정의 배열의 불변성과 잘못된 능력 거부를 확인한다.
- **실제 Unity Play Mode 캡처 21장**, 게임 runner 오류 0: [캡처](Logs/VisualV3Captures), [결과](Logs/VisualV3Captures/smoke-result.txt), [최종 로그](Logs/visual-v3-smoke-final.log).
- 타이틀·두 캐릭터·맵·실제 시작 손패·카드 상세·동시 적중·안쪽/바깥쪽 공개·적중·다음 턴·두 궁극기·카드 상세 보상·유물 보상·완주·패배·도움말을 확인했다. 보상/완주/패배 단계는 runner가 상태를 단언한다.
- 1920×1080과 1280×720에서 배치와 선 변환을 확인했다. 720p의 GL 작도도 별도 캡처했다. 축소 화면의 선 장식 위치 오류와 어두운 파편 위 글 대비 문제를 발견해 수정했다.
- 진단은 재현용 손패·적 체력·공명·체력을 설치하는 fixture이며 전체 런 밸런스 통과를 의미하지 않는다.
- 사용자가 열어 둔 Editor를 종료하지 않고 `Logs/CombatV2VerificationProject` 복사본에서 실행했다. 원본이 영구 소스의 기준이다.
- Editor 시작 단계의 `UnityEditor.Search.SearchDatabase` 내부 예외가 로그에 있다. 최종 화면 runner에서 발생한 게임 오류는 0이며 이 두 범위를 구분한다. 기존 `PERFORMANCE.md` 수치를 새 UI 전체 성능으로 재사용하지 않는다.

## 진단 재실행

열려 있지 않은 격리 복사본에 최신 Assets/Packages/ProjectSettings를 복사한 다음 실행한다. 실행 중인 사용자 프로젝트에 별도 Unity 프로세스를 겹쳐 띄우지 않는다.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$verification = (Resolve-Path 'Logs\CombatV2VerificationProject').Path
& $unity -batchmode -projectPath $verification -runTests -testPlatform EditMode -testResults "$PWD\Logs\editmode-visual-v3-final.xml" -logFile "$PWD\Logs\editmode-visual-v3-final.log"
& $unity -batchmode -projectPath $verification -executeMethod Graphaclysm.Editor.VisualV3Smoke.RunBatch -logFile "$PWD\Logs\visual-v3-smoke-final.log"
```

위 두 실행은 순차적으로 끝난 뒤 다음을 실행한다. 캡처에는 `-nographics`나 `-quit`을 붙이지 않는다. `VisualV3Smoke`가 종료를 처리한다. legacy 진단의 `CombatV2Smoke`와 `PolynomiographProfiling`은 이전 화면을 선택하는 Editor 경로를 유지한다.

작업 전 사본은 `Logs/BeforeVisualV3-20260906-032551`, 문서 보관은 `Docs/Archive/BeforeVisualV3`다. 사용자 원본·이전 초상·기존 사용자 임포트 설정을 덮어쓰지 않았다.
