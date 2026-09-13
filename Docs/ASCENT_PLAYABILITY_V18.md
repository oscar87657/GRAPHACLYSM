# 탐색과 전투 변화 v18

2026-09-12. 기존 v17 작업 위의 플레이 편의·전술 다양성 개선이다. 240개 성장 노드의 전투 효과를 전부 구현한 버전은 아니다.

## 적용

- 성장: 시작점 습득 직후 선택지는 양 캐릭터 모두 3개. 이안의 기술 진입을 기준점·파편·균열 가지로 분산했다. 6개 이하의 수평 행, 265 간격과 285 이상의 높이 간격으로 위로 확장한다. 부모 위치의 비중을 늘리고 임의 높이 흔들림을 제거했다.
- 선 표시: 다른 상위 경로가 이미 포함하는 오래된 선행 연결은 중복해서 그리지 않는다. 실제 조건은 오른쪽에서 그대로 확인한다. 연결 중복 여부는 레이아웃 생성 시 캐시한다.
- 호버: 원과 확대 상태의 이름 위에서 설명이 즉시 바뀐다. 오른쪽 설명으로 이동해도 마지막 설명이 유지된다. 가리키기만으로 습득·장착하지 않는다. 본문의 16개 핵심 용어에 밑줄과 툴팁을 연결했다.
- 전투 UI: 방향 버튼 네 개는 상세로 이동, 적 패널 높이는 실제 적 수에 맞춤, 응축 보조 설명은 호버로 이동. 적 이름 호버에 패턴·작도·결정·맥동 예상 피해를 보여 준다.
- 이동: 적 표식과 접촉·중첩 허용. 기둥·필드 경계·고정·턴당 한 번·이동 취소는 유지한다. 지형 툴팁 때문에 해당 지점의 이동 클릭까지 막히던 경로도 수정했다.
- 지도: 현재 층의 8개 방을 아래→위로 연결하고, 상시 방 설명을 종류 표식과 호버 상세로 줄였다. 기존 생성 경로·층 전이 규칙은 유지한다. 새 탑 내부 배경을 실제 화면에서 로드한다.

## 전투의 단조로움에 대한 진단과 대응

기존 코드는 적 행동 세 패턴, 기둥/피해 +2 프리즘 두 지형에 집중했다. 위치 보정 카드 비중이 높고, 같은 위치에서 식을 맞힌 뒤 방출하는 행동을 반복하기 쉬웠다. 이는 코드 구조에 근거한 진단이며 사용자 플레이 로그의 통계 분석은 아니다.

| 추가 요소 | 정확한 규칙 | 선택 |
|---|---|---|
| 맥동 균열 | 짝수 턴 적 행동 직전 원에 닿는 자신·적에게 피해 6. 실제 방출이 균열을 통과하면 해당 턴만 봉쇄 | 이동 회피 / 작도로 봉쇄 / 적이 맞도록 유지 |
| 보호 기점 | 작도로 통과하면 보호막 5. 전투당 한 번, 자가 적중 불필요 | 공격 경로를 기점에 걸어 방어 마련 |
| 파열 결정 | 작도로 통과하면 반경 2.1 + 적 반경 내의 적에게 피해 6. 전투당 한 번 | 그래프가 적을 직접 못 맞혀도 장치로 공격 |
| 격앙 적 | 두 턴마다 공격 +1, 최대 +4. 의도에 반영 | 위험이 커지기 전에 처리하거나 약화 |
| 기동 적 | 재배치 → 공격 → 공격의 세 턴 주기, 다음 주기에 원위치 | 고정으로 재배치 봉쇄 / 이동할 곳을 예상 |

장치는 드로우·카드 능력·공명을 복제하지 않는다. 맥동은 적 재배치보다 먼저 적용한다. 응축은 그래프를 화면에 남겨도 봉쇄로 취급하지 않는다. 보호막은 지형 피해를 흡수하지만 응축 비용을 막지는 않는다. 장치 사용 상태는 BattleSession이 소유하며 Reset과 저장 명령 재생에서 재구성된다.

지형 개수는 일반/정예 2개, 보스 3개를 유지한다. 무작정 표식을 늘리지 않고 생성 풀과 상호작용을 늘렸다.

## 카드와 시작 덱

카드 31 → 35종. 신규 카드는 기존 수학 변환에 서로 다른 능력 묶음을 연결한다. 새로운 수학 변환 네 종을 추가했다는 뜻은 아니다.

- 붙잡는 초승: 편월 + 적 고정/약화.
- 균열 바늘: 전단 + 적 파열/자가 추진.
- 달의 정박지: 타원 + 자가 요새화/적 약화.
- 재를 걷는 바람: 파쇄 궤적 + 적/자가 정화.

12장 덱과 시작 손패 5는 유지한다. 이안은 서쪽의 문·거울밤을 붙잡는 초승·균열 바늘로 바꾸어 후속 공격 준비를 늘렸다. 루나는 승천·성운·유리정원을 붙잡는 초승·달의 정박지·유성 보폭으로 바꾸었다. 루나의 시작 전설 카드와 이안과 공유하던 희귀 핵심을 줄이고, 방어·기동 조합으로 정체성을 분리한다. 최종 밸런스 확정이 아닌 첫 조정이다.

## 참고 조사

《Into the Breach》 공식 소개는 모든 적 공격의 예고와 대응 계획을 핵심으로 설명한다. 개발자 Justin Ma 인터뷰는 공개된 공격과 확률 없는 행동이 전투를 퍼즐처럼 만든다고 설명한다. 이 원리를 위험 지대의 활성 턴·방출 봉쇄·같은 Core 판정의 미리보기에 적용했다. [공식 소개](https://www.subsetgames.com/itb.html), [개발자 인터뷰](https://www.gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i-).

《Slay the Spire》 리뷰에서 높게 평가한 것은 적 의도에 맞춘 손패 계획, 카드 간 시너지, 선택의 누적이다. 이를 단순 공격 수치 추가 대신 고정/파열/요새화가 들어간 시작 덱과 일회용 장치 활용에 참고했다. 다른 게임에서의 호평이 우리 게임의 재미를 보장하지는 않는다. [Nintendo Life 리뷰](https://www.nintendolife.com/reviews/switch-eshop/slay_the_spire), [PC Gamer 리뷰](https://www.pcgamer.com/slay-the-spire-review/).

## 아트

내장 imagegen으로 생성. CLI나 키는 사용하지 않았다. 최종 자산: `Assets/Resources/Art/Generated/tower-ascent-v18.png`. 원본은 Codex generated_images에 보존했다. 생성 결과를 확인한 뒤 중앙 어두운 여백에 지도 노드를 배치했다.

생성 프롬프트:

> Create a premium game background bitmap for GRAPHACLYSM's ascending tower room-selection map. Landscape 16:9 composition, painterly dark fantasy astral architecture, ink black and deep indigo atmosphere, ivory stone, subtle lavender light, tiny restrained antique gold accents. One enormous ancient glass-and-stone astronomical tower viewed from within, climbing from a dim broad base at bottom to distant luminous observatory at top. Vertical ribs, suspended staircases on the far left and right, broken circular celestial mechanisms, drifting fine star dust. The central 65 percent must remain low-contrast dark negative space for readable UI room nodes arranged from bottom to top: architecture subtly frames rather than obscures these nodes. Rich crafted brush texture, restrained and elegant, no characters, NO text, NO letters, NO numbers, NO UI, NO node icons, NO watermark. Entire composition is background artwork only.

## 저장·검증·남은 범위

저장 형식 2 / 규칙 18. v17 원정은 재생하지 않는다. 기존 저장 파일을 테스트로 지우거나 덮어쓰지 않았으며 새 원정에서 확인해야 한다. 영구 기록과 설정은 유지한다.

EditMode 169/169 통과: `Logs/editmode-ascent-v18.xml`. 지형 예고/봉쇄/일회용/리셋, 정화 후 결정 피해 예측, 적 접촉과 취소, 새 행동 주기, 시작 선택 3개와 기존 생성/저장 재생을 검사했다.

Full HD/720p 화면 fixture 40장, 게임 Runtime errors 0: `Logs/ascent-v18-smoke.log`, `Logs/CombatV2VerificationProject/Logs/BasicsV9Captures/smoke-result.txt`. 양 캐릭터 연결 방향·노드 겹침·팬/줌, 진단용 호버의 설명 변경/포인트 불변, 키워드 표시와 지도·저장 이어하기를 검사했다. 진단용 호버 캡처는 모든 실제 마우스 이동 경로를 수동 테스트했다는 뜻이 아니다. 시작 시 Unity SearchDatabase 내부 예외는 게임 진입 전 Editor 서비스 오류로 별도 관찰되었다.

대표 화면: `Docs/Screenshots/tower-map-v18.png`, `growth-root-v18.png`, `growth-keyword-v18.png`, `battle-v18.png`.

대부분의 240개 성좌 효과는 여전히 설계 등록 상태다. 이번에 UI나 설명을 정리했다고 해당 효과까지 적용되지는 않는다. 전체 3층 수동 플레이, 체감 난이도, 카드 채택률, Profiler와 Windows 빌드는 미검증이다.
