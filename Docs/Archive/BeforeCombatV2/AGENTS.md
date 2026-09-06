> 이전 버전 보관본. 현재 구현과 방향은 루트의 같은 이름 문서를 따른다.

# GRAPHACLYSM 에이전트 작업 지침

이 문서는 프로젝트 전체에 적용된다. 다음 작업을 시작하기 전에 이 문서와 루트의 기획·구조 문서를 먼저 읽어라.

## 1. 프로젝트 개요

- 프로젝트명: `GRAPHACLYSM` / 그래파클리즘(가칭)
- 장르: 수식 조합형 덱 빌딩 로그라이트
- 핵심: 기초 그래프와 변환 카드를 이어 마법진을 만들고, 작도된 선과 적의 교차로 공격한다.
- Unity 버전: `6000.3.23f1`
- 주 개발 환경: Windows PowerShell
- 현재 Git 저장소가 아니다. Git 명령이 성공할 것이라고 가정하지 말라.
- 사용자는 최종 아트를 직접 도트로 제작할 예정이다. 현재 생성 이미지는 배치 검증용 임시 자산이므로, 요청 없이 아트 작업을 확장하지 말라.
- 사용자와는 한국어로 소통하고, 작업 결과를 먼저 알린다.

## 2. 정식 문서

서로 충돌하면 현재 코드와 최근 사용자 지시를 우선하고 문서를 같이 수정한다.

- `GAME_DESIGN_DOCUMENT.md`: 전체 기획, 확정·방향·제안·미정 구분
- `ARCHITECTURE.md`: 의존성, 메모리, 코드 규칙
- `PROTOTYPE.md`: 실행·조작법과 현재 검증 범위
- `ART_DIRECTION.md`: 중세 판타지 시각 방향과 임시 자산 상태
- `PERFORMANCE.md`: 작도 선 출력 비교, Game 뷰 검증과 재현 방법

기능이나 규칙을 바꾸면 관련 문서를 같은 작업에서 갱신한다. 구현된 내용을 제안으로, 아직 없는 내용을 확정으로 적지 말라.

## 3. 의존성과 소유권

```text
Runtime / Presentation
          ↓
     Application
          ↓
         Core
```

- `Assets/Scripts/Core`: Unity API를 참조하지 않는 순수 C# 규칙과 상태
- `Assets/Scripts/Application`: 여러 Core 상태를 원자적으로 바꾸는 유스케이스
- `Assets/Scripts/Runtime`: Bootstrap, Unity 입력, IMGUI 표현
- 화면 코드는 체력·에너지·적 상태를 직접 수정하지 않는다.
- 콘텐츠 정의는 불변으로 두고, 전투·런 상태와 분리한다.
- 전역 변경 가능 싱글턴을 추가하지 말라. 객체 조립은 Bootstrap에서만 한다.
- 상태 변경은 `Try...` 명령이나 명시적 전이 메서드를 통한다.
- Core 규칙을 변경하면 EditMode 테스트를 추가하거나 갱신한다.

## 4. 메모리와 성능 규칙

- `Update`, `OnGUI`, 그래프 샘플링, 충돌 판정 경로에서 LINQ·클로저·임시 컬렉션을 만들지 말라.
- 매 프레임 `Instantiate`, `Destroy`, 문자열 조합을 금지한다.
- 고정 용량 배열과 재사용 버퍼를 우선한다.
- 새로운 할당을 설계적으로 추가할 때는 소유 주체, 생명주기, 최대 용량을 명시한다.
- 최적화는 추측하지 말고 Unity Profiler의 GC Alloc과 프레임 시간으로 검증한다.

## 5. 현재 시스템 상태

### 전체 흐름

- 타이틀 → 캐릭터 선택 → 3층 분기 맵 → 전투 → 카드/유물 보상 → 완주 또는 패배
- 캐릭터: 아르카, 노아. 능력치·시작 덱이 다르다.
- 손패는 중앙 부채꼴로 배치되고, 호버 시 위로 올라오며 확대·정렬된다.
- 우클릭 또는 상단 수식 체인의 마지막 카드 재클릭으로 최근 선택을 취소한다.
- 적은 정상 공격, 충전 후 맹공, 좌표 이동 교대 패턴과 사전 공개 의도를 갖는다.
- 카드 보상은 서로 다른 3장, 정예 보상은 서로 다른 유물 3개 중 하나를 선택하거나 건너뛴 수 있다.

### 그래프와 전투

- 기초 그래프 18종, 변환 4종이다.
- 직교함수, 극좌표, 매개변수, 폴리라인 곡선은 `EquationState.Sample([0,1])`로 통합되어 있다.
- 원, 장미곡선, 리사주, 나선, T-폴리모노, 3차·4차, 하이포트로코이드, 마우러 장미, 하모노그래프, 슈퍼포뮬러가 구현되어 있다.
- 그래프 선분과 적 원의 최단 거리로 명중을 판정하고, 접선 기울기로 피해 보너스를 계산한다.

### 폴리노미오그래프 중요 규칙

- `폴리오미노(polyomino)`와 `폴리노미오그래프(polynomiograph)`는 서로 다르다.
- 사용자가 원한 것은 이미지 텍스처가 아니라 **수식에서 생성된 실제 선**이다.
- 현재 5근·8근·12근 카드는 `p(z)=z^n-1`, `N(z)=z-p(z)/p'(z)`를 사용한다.
- `PolynomiographEvaluator`가 수렴 근과 반복 횟수를 무할당으로 계산한다.
- `PolynomiographContourSet`이 64×48 필드에서 근 경계와 반복 횟수 `4, 6, 8, 11, 15, 20`의 등고선을 마칭 스퀘어로 추출한다.
- 추출 결과는 최대 8,192개의 고정 용량 선분으로 보관한다. 현재 5·8·12근 모두 용량 내에서 생성된다.
- Runtime은 이 선분을 순서대로 GL 배치 출력해 작도 애니메이션을 만들고, `EquationAnalyzer`도 같은 선분으로 명중을 판정한다.
- 이전의 색상 텍스처 렌더링은 제거됐다. 폴리노미오그래프를 다시 텍스처 기반 영역 공격으로 바꾸지 말라.
- 참고 이미지는 36차식 작품이지만 공개 기사에 정확한 계수와 렌더링 설정은 없다. 동일한 작품을 복제했다고 표현하지 말라.

### 카드 등급

- `CardRarity`: `Common`, `Uncommon`, `Rare`, `Legendary`
- 한국어 UI: 일반, 고급, 희귀, 전설
- 카드별 보상 가중치: 일반 100, 고급 45, 희귀 15, 전설 3
- 손패와 보상 UI에 등급명과 등급 색을 표시한다.
- 현재 등급 분포:
  - 일반: 직선, 포물선, 파동, 절댓값, 상승, 하강
  - 고급: 증폭, 반전, 환, 이중 나선, T-폴리오미노, 삼차 왜곡, 기요셰 톱니환
  - 희귀: 오엽 장미, 조화 매듭, 사차 성배, 5근·8근 수렴문, 마우러 장미망, 감쇠 조화진
  - 전설: 십이각 대성식, 초월 성형식

### 유물

- 현재 6종이다.
- 효과: 최대 에너지, 손패 크기, 그래프 적중 피해, 전투 승리 후 회복
- 같은 ID는 중복 획득할 수 없고, 같은 효과의 다른 유물은 합산한다.
- 정예 전투 승리 후 3개 보상이 나오며 다음 전투부터 적용한다.

## 6. 주요 파일

- `Assets/Scripts/Core/Equations/EquationState.cs`: 그래프 종류, 수식 변환, 곡선·필드 샘플
- `Assets/Scripts/Core/Equations/EquationAnalyzer.cs`: 선분-적 교차와 피해
- `Assets/Scripts/Core/Equations/PolynomiographEvaluator.cs`: 복소수 뉴턴 반복
- `Assets/Scripts/Core/Equations/PolynomiographContourSet.cs`: 수렴 경계·등고선 추출
- `Assets/Scripts/Core/Cards/CardDefinition.cs`: 카드 타입과 등급
- `Assets/Scripts/Core/Cards/PrototypeCardCatalog.cs`: 현재 카드 전체
- `Assets/Scripts/Core/Relics`: 유물 정의와 카탈로그
- `Assets/Scripts/Core/Combat`: 전투·적·의도 규칙
- `Assets/Scripts/Core/Runs`: 런 덱, 맵, 유물 보유 상태
- `Assets/Scripts/Application/RunGameSession.cs`: 맵·전투·보상·영속 상태의 조정
- `Assets/Scripts/Runtime/Presentation/GraphaclysmPrototypeView.cs`: 현재 IMGUI 화면과 애니메이션
- `Assets/Scripts/Runtime/Presentation/PolynomiographLineRenderer.cs`: 폴리노미오그래프 Repaint 전용 GL 배치 출력
- `Assets/Resources/PolynomiographLines.shader`: 선 두께·색·알파를 표현하는 Player 포함 셰이더
- `Assets/Editor/PolynomiographProfiling.cs`: Full HD A/B 측정, 캡처, 작도 전환 검증
- `Assets/Tests/EditMode`: Core와 Application 회귀 테스트

## 7. 카탈로그 주의사항

- `PrototypeCharacterCatalog` 시작 덱은 현재 `PrototypeCardCatalog.All` 인덱스를 참조한다.
- 기존 카드 사이에 새 카드를 삽입하면 시작 덱이 조용히 바뀐 수 있다. 일단 끝에 추가하거나 시작 덱을 ID 기반 참조로 마이그레이션한다.
- 위치를 가정하는 테스트를 만들지 말고 ID로 찾아 검증한다.
- 카드 보상은 `RunGameSession.SelectWeightedRewardCandidate`에서 카드별 가중 추첨하고 3장의 ID 중복을 거부한다.

## 8. 검증 명령

소스를 바꿀 때마다 프로젝트 루트에서 다음을 순서대로 실행한다.

```powershell
dotnet build Temp/GraphaclysmVerification/GraphaclysmVerification.csproj --nologo
dotnet run --project Temp/GraphaclysmEditModeHarness/GraphaclysmEditModeHarness.csproj --nologo
```

- 2026-09-03 기준: 빌드 경고 0, 오류 0, EditMode 테스트 44/44 통과
- 2026-09-06: 위 Temp 보조 프로젝트가 정리된 상태라 Unity Test Runner를 사용했다. Unity 컴파일 및 EditMode 44/44 통과. GL 출력의 Full HD Game 뷰 검증 결과는 `PERFORMANCE.md`에 있다.
- `Temp` 하위 검증 프로젝트는 Unity 없이 빠른 회귀 검증을 하기 위한 보조 수단이다.
- Unity가 `Temp` 폴더를 정리하면 위 보조 프로젝트가 없어질 수 있다. 그런 경우 영구 소스를 `Temp`에 복구하지 말고 Unity Test Runner로 EditMode 테스트를 실행한다.
- 최종 확인은 Unity Editor에서 `Assets/Scenes/SampleScene`을 열고 Play Mode로 진행한다.
- 시각 변경은 16:9 Game 뷰에서 확인한다.

## 9. 현재 제약과 다음 우선순위

1. 폴리노미오그래프 GL 배치 출력과 Full HD Game 뷰 비교는 완료했다. 같은 Core 선분·순차 작도·적 표시 순서를 유지한다. 출력 구간의 정상 상태 할당은 0 B였지만 기존 IMGUI와 Editor 전체 프레임에는 할당이 남아 있다. Player의 GPU 시간과 다른 그래픽 API는 아직 측정하지 않았다.
2. 카드 UI는 등급명과 색을 표시하지만 정식 프레임·아이콘·상세 툴팁은 없다.
3. 등급 보상 가중치는 프로토타입 값이다. 출현 분포 테스트와 플레이 데이터로 조정해야 한다.
4. 현재 콘텐츠 카탈로그는 코드에 하드코딩되어 있다. ScriptableObject 어댑터와 제작 데이터 검증기가 다음 구조 확장 후보다.
5. 현재 그래프 변환은 상하 이동·세로 증폭·반전만 있다. 좌우 이동, 가로 압축, 회전, 구간 제한, 다중 곡선이 다음 게임플레이 확장 후보다.
6. 폴리오미노는 현재 T자 외곽 회로만 있다. 칸 점유·회전·배치 규칙은 아직 미구현이다.

다음 작업에서는 사용자의 새 요청을 우선한다. 작도 선 배치 렌더링은 완료했으므로, 새 지시가 없다면 2번의 카드 상세 정보·툴팁 가독성 개선을 먼저 진행한다.

## 10. 조사 메모

- 폴리노미오그래프 참고 기사: <https://www.dongascience.com/ko/news/10038>
- Bahman Kalantari의 폴리노미오그래피 정의: <https://www.sciencedirect.com/science/article/abs/pii/S0097849304000354>
- 하이포트로코이드 수식: <https://encyclopediaofmath.org/wiki/Epitrochoid>
- Peter M. Maurer, `A Rose is a Rose...`: <https://www.jstor.org/stable/2322215>
- 하모노그래프 수학 모델: <https://revistas.ug.edu.ec/index.php/iti/en/article/view/1268>
- Johan Gielis의 슈퍼포뮬러 원문: <https://pubmed.ncbi.nlm.nih.gov/21659124/>
