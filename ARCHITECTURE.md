# GRAPHACLYSM Architecture

## v15 자유 이동과 기술 성좌 소유권

`RunGrowthState`는 캐릭터별 18노드 정의, 부모와 8개 배타 그룹, 해금 마스크를 소유한다. `BattleSkillLoadout`은 선택한 3종 전투 기술/3종 궁극기 형태와 부가 노드 마스크를 새 전투에 전달한다. `BattleSession`은 캐릭터별 직선·다중선·착지 파동·왕복·연쇄 적중과 안전한 기술 착지, 주 대상 보장 판정을 소유한다.

`TacticalCombatState`는 반경 1.8 자유 좌표 이동 가능 여부와 취소 원점을 소유한다. `BattleSession.TryResolveMoveDestination`은 경계 제한과 적·기둥 주변의 고정 후보 탐색을 수행하고, `RunGameSession`은 0.01 단위 좌표를 `MoveTo` 명령 하나에 압축해 기록·재생한다. Presentation은 포인터를 필드 좌표로 바꾸고 Core가 반환한 실제 도착점만 미리 그린다. 저장은 형식 2/규칙 15다. [전투 조작·기술 성좌 v15](Docs/COMBAT_CONTROL_GROWTH_V15.md)를 따른다.

## v14 정보 UI와 성장 그래프 소유권

`RunGrowthState`는 12비트 해금 마스크와 각 노드의 부모·배타 그룹을 소유한다. `CanPurchase`는 성장점·레벨·선행 노드·같은 갈림길을 함께 검사하고 `BattleSkillLoadout`이 마스크를 새 전투에 전달한다. `BattleSession`과 `TacticalCombatState`는 오중 궤적, 처치 공명, 궁극기 공명 환급과 캐릭터별 하위 효과를 계산한다.

Presentation은 성장 노드의 고정 좌표와 부모선을 그릴 뿐 구매 가능 여부를 다시 판단하지 않는다. 카드 keyword는 원본 카드·확대본·source·bridge·tooltip Rect를 보존하고, 상태 칩은 Core의 magnitude/duration을 함께 읽는다. 저장은 형식 2/규칙 14이며 v13 이하는 재생하지 않는다. [전투 정보·성좌 성장 v14](Docs/COMBAT_READABILITY_V14.md)를 따른다.

## v13 전투 흐름 소유권

`FragmentEquation.TryAppend`는 빈 prefix의 첫 카드에 한해 `BattleSession`이 전달한 플레이어 좌표를 `origin[0]`에 고정한다. 네 신규 변환도 기존 `(Capacity+1)×1536` x/y prefix 버퍼 안에서 계산하며 보수 주파수 상한을 공유한다.

`BattleSession`은 전투 기술 대상 선택, 선분-적 거리 판정, 피해·모듈 처리, 3턴 쿨타임과 마지막 연출용 좌표/적중 요약을 소유한다. `TacticalCombatState.SkillDashTo`는 좌표와 이동 소비를 적용하되 되돌리기 표식을 만들지 않는다. 이 절의 6비트 성장은 위 v14의 12비트 의존 성좌로 확장되었다. `RunGameSession`은 대상 인덱스를 명령에 기록하고 기술 처치 승리도 공통 완료 경로로 보낸다.

Presentation은 고정 `Rect` 두 개로 키워드 툴팁과 포인터 이동 통로를 기억하고, 마지막 기술의 숫자/좌표만 View 필드에 복사해 한 줄 또는 세 줄을 그린다. 수학·충돌 루프에 새 컬렉션 할당은 없다. 원정 저장은 형식 2/규칙 13이며 v12 이하 명령은 재생하지 않는다. [전투 흐름·분기 성장 v13](Docs/COMBAT_FLOW_V13.md)을 따른다.

## v12 전투·성장 소유권

`CombatStatusState`는 12종 상태의 수치·수명을 고정 배열로 소유하고 `BattleSession`이 가시 반격, 추진·파열 소모, 요새화 방어와 6종 신규 유물 발동 순서를 조정한다. 이 절의 자유 전환형 기술 설명은 v13의 배타 분기로 대체되었다.

`LegacyProgression`은 원정 밖의 잔광·여섯 영구 기록 랭크·중복 보상 방지용 최근 시드를 소유한다. `LegacyProgressionStore`는 `legacy.save`를 SHA-256과 같은 폴더 임시 파일 교체로 저장한다. 새 원정은 그 시점의 `LegacyBenefits`만 복사하므로 이후 메인 화면 구매가 진행 중 원정을 바꾸지 않는다. v12 당시 원정 규칙은 12였으며 최신 규칙은 위 v15 절의 15다.

신규 유물 Texture 6장은 `Awake`에서 한 번 Resources로 읽어 View 수명 동안 재사용한다. OnGUI는 카탈로그 ID로 캐시를 조회하며 디스크 로드나 새 Texture를 만들지 않는다. 상세 계약은 [전투와 성장 v12](Docs/COMBAT_GROWTH_V12.md)다.

## v11 캐릭터 선택 초상

선택 전용 이안·루나의 눈 감음/눈 뜸 RGBA Texture 네 장을 Resources에서 한 번 불러온다. `PortraitMedallion.shader`는 블러·채도 조작 없이 원형 알파 마스크만 적용하여 512×512 표시본 네 장을 `Awake`에서 굽는다. View는 뒤에 불투명 원을 그리지 않는다. `characterEyeOpen` 두 float만 호버에 따라 0/1로 이동시키며 닫힘/열림 표시본의 alpha를 교차한다. 기본 표시 지름은 540 논리 픽셀이며 위치·크기·테두리는 상태와 무관하게 고정한다. 생성 Texture와 Material은 `OnDestroy`에서 해제한다.

Presentation만 바뀌므로 Core/Application 규칙과 `RunSaveData.RulesVersion=10`은 유지한다. 자세한 자산 계약은 [캐릭터 선택 v11](Docs/CHARACTER_SELECTION_V11.md)이다.

## v10 전장·카드 UI와 지형

`BattleTerrainDefinition`은 Core의 불변 ID·종류·좌표·반경을 가진다. `BattleDefinition`이 배열을 복사해 소유하고 `DungeonGenerator`가 원정 RNG로 일반·정예 2개, 보스 3개를 만든다. 초기 플레이어·적·다른 지형과 간격을 검증한다. `BattleSession`은 기둥과의 원 충돌로 플레이어/적 이동을 거부하고, `EquationAnalyzer`의 같은 선 샘플로 프리즘 교차를 판정해 적중 피해 +2를 적용한다. Presentation은 공개 정의와 `PrismCharged`를 읽기만 한다.

전투의 수학 좌표는 `x=0~10`, `y=-4~4` 그대로이며 `FieldUnit`을 118로 높여 1180×944 활성 필드를 사용한다. `GraphaclysmBattlefieldView`가 전체 화면 바탕, 지형, 카드/상태 키워드와 가장자리 HUD 보조를 담당한다. 손패는 기존 카드 캐시를 회전해 그리며 호버 카드만 마지막에 다시 그린다. 확대 카드의 키워드 문자열 배열은 `BuildVisuals`에서 한 번 생성한다.

v10 당시 캐릭터 선택은 기존 이안 v2·루나 v6 Texture에서 선명본·블러본을 구웠다. 이 경로는 v11의 선택 전용 옆모습 네 장과 고정 크기 눈 전환으로 대체했다.

생성/전투 규칙 변경으로 `RunSaveData.RulesVersion=10`이다. v9 원정은 v10으로 재생하지 않으며 설정 저장 형식은 유지한다. 상세 내용은 [전장·카드 UI v10](Docs/BATTLEFIELD_V10.md)이다.

## v9 저장·설정·일시정지

`RunGameSession`은 성공한 Application 명령의 `List<RunCommand>`를 소유한다. 256칸에서 필요할 때 증가하고 최대 65,536개(구조체 데이터 약 512 KiB)다. 초과 상태는 bool로 기록해 저장을 거부하고 마지막 파일을 유지한다. 예측·렌더링·충돌 루프에서는 기록하지 않는다. legacy/custom constructor는 기록을 켜지 않고 `PrototypeRunFactory.Create`만 캐릭터 ID와 함께 활성화한다.

`CaptureSave`는 저장 명령 때만 명령 배열을 복사한다. 버전/시드/캐릭터/명령의 바이너리 payload와 SHA-256을 `RunSaveStore`가 읽고 쓴다. 파일 읽기 상한 1 MiB, 최대 명령 데이터 약 320 KiB다. 배열·MemoryStream·해시 버퍼는 저장/불러오기 동안만 소유한다. 파일 교체는 같은 볼륨의 `.tmp`를 flush한 뒤 `File.Replace`하며 이전 파일을 `.bak`에 남긴다. 패배·완주는 백업도 종료 기록으로 바꾼다. 역직렬화는 임의 타입을 생성하지 않고 명령 ID·길이·버전·해시를 검증한다.

복원은 새 `RunGameSession`에 성공 명령을 재생한다. RNG, 덱 영역, 수식 prefix, 이동 취소 이력, 상태 수명, 응축/드로우 소비를 기존 규칙으로 다시 만든다. 전부 성공한 후보만 `PrototypeGameFlow.TryContinueRun`에 연결한다. 콘텐츠 변경에는 `RulesVersion` 변경 또는 마이그레이션이 필수다. 연출의 진행 프레임은 저장하지 않으며 전투 phase로 판정 전/후를 구분해 다시 연출한다.

`GraphaclysmSystemView`는 저장 어댑터/설정/현재 저장 참조와 문자열 캐시를 View 수명 동안 소유한다. `Refresh`의 성공 명령 이후 revision이 바뀔 때 저장한다. 일시정지는 로컬 `viewTime` 증가와 명령 입력을 막고 전역 Time.timeScale은 변경하지 않는다. 메뉴·도움말·설정·인벤토리가 동일하게 멈춘다. 설정 파일에는 고정 크기 값만 보관하고 범위를 제한한다. 음량 문자열 101개·안내 5페이지는 정적 공유, 인벤토리 페이지/상세 문자열은 선택 시에만 갱신한다.

`GameAudio`는 AudioSource 1개와 짧은 mono 44.1kHz AudioClip 6개를 Awake에서 만들고 OnDestroy에서 해제한다. 초기 합성 float 배열의 최대 길이는 17,640이며 생성 후 해제 대상이다. 재생은 기존 소스의 PlayOneShot을 사용하고 프레임별 오브젝트를 만들지 않는다. 전체 음량은 AudioListener, 효과음은 해당 소스에 적용하며 View 종료 때 이전 listener 음량을 복원한다. 이는 새 Profiler 측정 결과가 아니다.

검증 절차와 저장 위치는 [기본 기능 v9](Docs/BASICS_V9.md)를 따른다. 아래 구조 확장 순서의 저장 DTO 항목은 v9에서 이 방식으로 구현했다.

## 목표

게임 규칙을 Unity 오브젝트와 분리해 테스트 가능하게 유지한다. 화면, 애니메이션, 입력 방식이 바뀌어도 수식 계산과 전투 결과가 달라지지 않아야 한다.

## 의존성 방향

```text
Presentation / Unity adapters
            ↓
       Application
            ↓
          Core
```

- `Core`는 Unity API를 참조하지 않는다.
- `Runtime`은 Core의 공개 명령만 호출한다.
- 화면 코드는 체력, 에너지, 적 상태를 직접 수정하지 않는다.
- 콘텐츠 정의와 플레이 중 상태를 같은 객체에 저장하지 않는다.
- 전역 변경 가능한 싱글턴을 만들지 않는다. 조립은 Bootstrap에서만 한다.

## 현재 모듈

### Core/Cards

- 변경 불가능한 카드 정의
- 카드 식별자, 비용, 수식 연산 종류
- 실제 런의 FragmentCardCatalog: 궤적 변형 하나와 적중/드로우 능력 1~2개를 묶은 파편 31종. Calculator/Skill 카탈로그는 legacy 회귀용. `CardAbility`는 불변 값이며 CardDefinition이 배열을 복사하여 소유
- `PrototypeCardCatalog`의 기존 완성 그래프는 수학 보관함이며 시작 덱·보상에서 제외

### Core/Characters

- 변경 불가능하고 생성 시 검증되는 캐릭터 정의
- 최대 체력, 에너지, 손패 크기, ID 기반 시작 덱 구성과 `CombatArchetype`
- 외부에서 원본 시작 덱 배열을 수정할 수 없도록 복사본만 제공

### Core/Equations

- 입력과 연산 순서를 보관하는 `EquationState`. 조립 입력은 중복 연산을 허용하며 legacy 필드에는 기존 가역 변환만 허용
- `GraphSegmentClipper`: 화면 경계와 중앙 방사 공개를 위한 무할당 선분 자르기. 현재 파편은 표시·적/자신 충돌 모두 1,536개 선분, legacy 계산기 768개/곡선 160개로 근사
- 모든 곡선을 `[0,1]` 매개변수의 점으로 샘플링해 직교·극좌표·매개변수·폴리라인을 통합
- 복소평면에서 `z^5-1`의 뉴턴 수렴을 계산하는 무할당 `PolynomiographEvaluator`
- 곡선 샘플과 영역 샘플을 명시적으로 분리해 잘못된 표현 호출을 즉시 거부
- 충돌과 기울기 피해를 계산하는 `EquationAnalyzer`
- 샘플 선분과 적 원의 거리로 닫힌 곡선과 수직 구간도 동일하게 판정
- 반복 계산 경로에서 컬렉션이나 임시 노드를 생성하지 않음

### Core/Combat

- 적의 변경 불가능한 정의와 전투별 상태 분리
- 변경 불가능한 행동 패턴 정의가 턴 번호로 다음 의도를 결정
- 전투별 적 상태가 현재 좌표와 공개 의도를 소유하고 해결
- 플레이 계획, 작도, 적 턴, 승리, 패배 상태 머신
- 카드 사용과 피해 적용의 단일 진실 공급원인 `BattleSession`
- `TacticalCombatState`는 플레이어 좌표·이동 한도·공명·궁극기 준비를 소유. 내부 변경 메서드는 BattleSession만 사용
- `CombatStatusState`는 전투 참가자마다 크기 8의 수치 배열과 수명 배열을 소유. 최대 수치 24, 전투 수명 동안 재사용
- v5는 이전 궤적을 받는 파편 최대 8개. 응축은 식 이력을 유지하고 방출/해체 뒤에만 초기화한다. legacy 각인 회귀를 위해 이력 버퍼는 최대 11개를 유지. 이동 비용은 별도 기록하여 수식 취소로 환급하지 않음

### Core/Decks

- 시작 덱, 손패, 드로우 더미, 버린 더미의 소유권 관리
- 고정 배열을 재사용하는 카드 이동과 Fisher-Yates 셔플
- 저장 가능한 시드를 사용하는 결정론적 난수
- 기존 base 카드 보장은 legacy 덱에만 적용. Fragment 카드 덱은 어느 카드든 시작 가능. 손패를 유지하고 사용한 카드는 예약 영역에 보관하여 재셔플을 막음

### Core/Runs

- 전투 사이에 유지되는 런 덱 소유
- 최대 64장 제한과 안전한 카드 추가·인덱스 제거. 최소 손패 크기 및 비용은 Application에서 검사
- 변경 불가능하고 검증된 방향성 맵 정의
- 선택 가능·진행 중·완료 노드를 소유하는 맵 진행 상태
- 모든 경로는 더 높은 층만 가리켜 순환을 구조적으로 차단
- 고정 용량 유물 컬렉션과 식별자 중복 방지, 효과 합산

### Core/Relics

- 변경 불가능한 유물 정의와 효과 종류
- 현재 FragmentRelicCatalog 20종 (PrototypeRelicCatalog 6종은 legacy)
- 유물은 런 상태를 직접 수정하지 않고 전투 생성 시 보정값으로 전달

### Application

- 여러 Core 상태를 함께 바꾸는 유스케이스 조정
- 타이틀, 캐릭터 선택, 런을 전이하는 `PrototypeGameFlow`
- 선택 캐릭터와 현재 런의 생성·교체·해제를 한곳에서 소유
- 카드 적용과 손패 제거를 하나의 명령으로 처리
- 카드 선택 취소 시 수식·손패·예약 영역을 하나의 역트랜잭션으로 복구. legacy에서는 에너지/버린 더미를 복구
- 적 턴 종료와 다음 손패 드로우를 하나의 전이로 처리
- 전투, 방, 덱 정리, 카드/유물 보상, 런 완주, 패배 상태 전이
- 정예 유물 보상과 이후 전투의 드로우·시작 손패·피해·회복 보정. 에너지 보정은 legacy용
- 전투 사이의 플레이어 체력·공명·획득 카드 유지
- 직접 피해 승리와 지속 피해 승리는 공통 `CompleteVictory`로 노드·보상·영속 상태를 처리
- 맵 노드 선택과 해당 조우 생성

### Runtime · v7 기본 화면과 규칙

- 기본 조립은 `GraphaclysmModernView`. Battle/Screens/FragmentView/QuietView/AtlasView partial이 전투·지도·파편 조립을 분리한다. CalculatorView는 현재 방/덱 정리와 legacy 계산기 메서드를 가진다. 기본 런은 계산기 화면으로 진입하지 않는다. 이전 PrototypeView/TacticalView는 명시적 legacy Editor 진단에서만 생성한다.
- `AstralUi`가 3개 Font와 10개 GUIStyle을 View 수명 동안 재사용한다. 논리 캔버스 1920×1080, 화면별 균일 배율·레터박스. 선은 기존 GUI 행렬에 로컬 TRS를 곱해 축소 화면의 회전 피벗 오류를 피한다.
- `SkillVisual[49]` (현재 파편 31종 + legacy 계산기 11종 + 기술 13종)은 Awake에서 구성한다. 파편 문양 Vector2[65], legacy 그림 Vector2[41], 비용·능력·상세·위력 배지 문자열을 View 수명 동안 재사용한다.
- 새 전투가 바인딩될 때 적 수만큼 체력·피해·의도·상태 상세·상태 요약·번호·피격 숫자 string 배열 7개, int 피해 배열 1개, bool 피격 배열 1개를 만들고 재사용한다. 현재 조우는 최대 적 3명이다. 손패 용량만큼 float 호버·string 불가 이유 배열을 만든다. 명령 후 문자열 캐시만 갱신한다.
- `DescribeStatuses`의 StringBuilder는 상태 변경 후 Refresh에서만 만들며 최대 상태 12종이다. OnGUI는 캐시된 문자열을 읽는다.
- 원형 마커용 Texture2D 64×64 1개와 임시 Color[4096]은 Awake에서 만든다. Apply 후 CPU 픽셀 복사본은 버리고 Texture는 OnDestroy에서 해제한다.
- `AstralSpellRenderer`는 Material 1개를 View 수명 동안 소유한다. Resources/AstralInk.shader가 빌드에 포함된다. Core의 같은 1,536개 파편/768개 legacy 계산기/160개 legacy 곡선 선분(legacy 필드 최대 8,192)을 읽으며 별도 프레임 버퍼나 입자 GameObject를 만들지 않는다. Repaint당 GL.Begin/End 한 쌍으로 예측선·발광 가장자리·백색 중심선을 쌓는다.
- 한 일반 곡선 선분은 최대 4개 사각형(예측+빛 3층), 공개 끝점 반짝임은 최대 추가 2개다. 최악 상한은 선분당 정점 24개지만 일반 끝점 장식은 공개 경계에서만 나온다. 실제 비용은 새 화면에서 별도 측정해야 한다.
- CastElapsed 1.08초에 Run.ResolvePlot을 한 번 실행하고 1.85초에 적 행동을 실행한다. 적중 여부·피해 숫자는 작도 시작 때 고정 배열에 보존한다. 결과가 보상으로 바뀌어도 잔광 동안 이전 전투 참조를 유지한다.
- 기본 배경과 초상 3개 Texture, 폰트 3개는 Resources에서 로드한다. 라이선스는 StreamingAssets/FontLicenses에 포함된다.
- `FragmentsV5Smoke`는 Editor 전용, 명시적인 batch 호출에만 동작한다. 고정 손패·HP·공명을 설치하고 화면 전이를 검증한다. Runtime 진단 호버·시간 속성은 UNITY_EDITOR에서만 존재한다.

### Legacy Runtime · 비교 기록

- Unity 실행 시 객체를 조립하는 Bootstrap
- Core 상태를 읽고 입력을 명령으로 변환하는 임시 IMGUI 화면
- 화면용 문자열은 상태가 바뀔 때만 다시 계산
- `GraphaclysmTacticalView` partial은 이동·궁극기·상태·플레이어·툴팁 표시를 담당
- 툴팁 문자열 배열은 손패 최대 용량으로 View가 소유. 상태 문자열의 StringBuilder는 명령 후 캐시 재생성 때만 사용하며 상태는 8개로 제한
- 메뉴·카드 GUIStyle은 View 초기화 때 만들고 재사용. 런타임은 매 프레임 문자열·컬렉션·이펙트 오브젝트를 만들지 않음

- 폴리노미오그래프는 수렴 필드에서 추출한 고정 용량 선분 버퍼를 수식이 바뀐 때만 재생성
- `PolynomiographLineRenderer`는 같은 Core 선분을 화면 두께가 있는 사각형으로 바꾸고, Repaint에서 한 번의 `GL.Begin/End`로 제출한다. 완성 예측선을 유지하고 중심 (5,0)에서 확장되는 반경으로 선분을 잘라 작도를 공개한다. 좌표 자체는 변경하지 않는다.
- 렌더러는 View가 Awake에서 생성하고 OnDestroy에서 해제한다. Material 1개를 View 수명 동안 재사용하며 별도 선분 배열·메시·렌더 텍스처를 만들지 않는다. 한 패스에 최대 8,192개 선분에서 최대 32,768개 정점을 제출한다. 작도 중에는 예측선·발광선 두 패스를 사용한다.
- 전용 셰이더는 `Assets/Resources/PolynomiographLines.shader`에 있어 Player 빌드에도 포함된다. 장식 다음, 적 표시 이전에 출력하고 GL 행렬을 복구한다. 현재 최상위 IMGUI 좌표를 전제로 한다.
- Editor 진단 도구는 기존 IMGUI 출력과 GL 출력을 같은 Full HD Game 뷰에서 비교한다. 기존 출력 경로와 진단 속성은 `UNITY_EDITOR`로 Player에서 제외한다. 측정 결과와 제한은 [PERFORMANCE.md](PERFORMANCE.md)를 참고한다.

## 메모리 규칙

1. `Update`, 그래프 샘플링, 충돌 판정에서는 LINQ와 클로저를 사용하지 않는다.
2. 매 프레임 `Instantiate`, `Destroy`, 문자열 조합을 하지 않는다.
3. 수식 변환은 최대 용량이 정해진 재사용 배열에 저장한다.
4. 카드 정의는 변경 불가능하게 만들고 모든 전투에서 공유한다.
5. 전투 상태는 한 세션이 소유하며 외부에서는 임의로 수정하지 못한다.
6. 반복 생성되는 카드, 피해 숫자, 이펙트는 정식 UI 단계에서 풀링한다.
7. 이벤트를 추가할 때는 구독 해제를 같은 생명주기에 배치한다.
8. 최적화 판단은 Unity Profiler의 GC Alloc과 프레임 시간 측정으로 검증한다.

## 코드 규칙

- 클래스 하나에는 변경 이유를 하나만 둔다.
- 상태 변경은 `Try...` 명령 또는 명시적인 전이 메서드로만 수행한다.
- 잘못된 제작 데이터는 생성 시 즉시 예외로 거부한다.
- 플레이 중 가능한 실패는 예외 대신 결과 값으로 반환한다.
- 프레젠테이션 문자열과 색상은 게임 규칙에 넣지 않는다.
- Core 변경에는 해당 규칙을 고정하는 EditMode 테스트를 추가한다.

## 다음 구조 확장 순서

1. 생성 던전·식 조립의 플레이 검증과 보스 전용 행동
2. ScriptableObject 콘텐츠 어댑터와 검증기
3. uGUI 기반 화면 및 오브젝트 풀
4. 저장 및 불러오기용 버전이 있는 데이터 전송 객체

현재 직교함수, 매개변수·극좌표 곡선, 폴리라인은 `EquationState.Sample`의 공통 점 샘플 규약으로 통합되어 있다. 폴리노미오그래프는 `SampleField`로 수렴값을 계산한 후 `PolynomiographContourSet`이 경계·등고선을 선분으로 추출한다. 따라서 렌더링과 전투 판정이 같은 선 기하를 사용한다.

아직 구현하지 않은 기능을 미리 추상화하지 않는다. 실제 두 번째 구현이 생길 때 인터페이스를 추출한다.

## V2 검증 도구

`CombatV2Smoke.RunBatch`는 명시적으로 호출한 batch Editor에서만 실행되는 진단 fixture다. 실제 캐릭터 선택·Run 명령·View를 사용하되 재현용 적 체력·손패·궁극기 게이지를 설치하고 Full HD 캡처를 남긴다. 사용자 Editor의 런에 자동 주입하지 않는다. 캡처는 밸런스 플레이 결과와 구분한다.

## 이전 v4 소유권과 할당 범위 (legacy 계산기와 공통 던전)

- `ExpressionProgram`은 외부 eval/reflection 없이 중위식 문자열을 고정 64개 Node 배열로 컴파일한다. double[64] 평가 버퍼를 재사용하며 샘플링 중 할당하지 않는다. 각 식 최대 128자·연산 24개·재귀 깊이 16이다. 0 나눗셈 근처·비정상 크기는 NaN으로 제외한다. 단일 전투 스레드에서 평가한다.
- `CalculatorEquation`은 활성 X/Y와 임시 X/Y의 ExpressionProgram 4개를 소유한다. 유효성 확인은 임시 두 프로그램에 수행하고 둘 다 성공하면 활성 배열에 복사한다. 잘못된 Y로 활성 X만 바뀌지 않는다. 컴파일의 문자열·오류 객체는 편집 명령 때만 생성하며 프레임 평가에 들어가지 않는다.
- `EquationState`는 계산기를 첫 활성화 때 만들고 int[8]로 각 카드의 대상 축을 기록한다. 같은 Sample을 렌더링·충돌·잉크 계산이 사용한다. TraceLength는 입력/변형/취소 시 더러움 표시 후 한 번 계산하여 캐시한다. 새 선분 컬렉션을 만들지 않는다.
- `BattleSession`은 유료 이동에 소비한 EN, TacticalCombatState는 이동 전 좌표와 경쾌 수치·수명을 보관한다. TryUndoMove는 계획 단계에서만 한 번 복구하고 스냅샷을 비운다. 카드 이력과 독립된다.
- `RunGameSession`은 기본 식 두 문자열(각 128자 이하)을 런 수명 동안 보유한다. 유효한 편집 뒤 갱신하고 다음 전투에 설치한다. 턴 전이에서는 카드 레이어만 지운다. 수식 변경·카드 적용·이동은 Application 명령을 통한다.
- `DungeonGenerator`는 새 런에만 할당한다. 층 시작/폭 int[8] 각각, 최대 22개 노드·레인·간선 List(노드당 최대 3), 불변 정의의 복제된 간선 배열을 만든다. 전투당 최대 적 3명과 기존 행동 정의를 구성한다. XorShift 시드로 지도/방/배치가 재현된다. 런을 교체하면 이전 그래프는 해제 대상이다.
- `RoomStoryCatalog`는 이벤트 6개·휴식 1개·유물 1개, 각 1~3개 불변 선택지를 정적으로 공유한다. RunMapNodeDefinition은 전투 또는 RoomStory 중 맞는 조우와 캐릭터 최대 HP 메타데이터를 가진다. 비전투 노드의 Battle은 null이다.
- `RunRooms`는 방의 비용과 결과를 적용하고 맵 노드를 한 번 완료한다. 제거 비용은 int 하나로 보류하여 실제 삭제 명령에서만 차감한다. 취소는 비용 없이 끝난다. 유물 보상 배열은 기존 3칸을 재사용하고 미보유 수가 적으면 나머지를 null로 둔다.
- UI는 기본 식 초안 두 문자열(각 128자), 오류·잉크·시드·방 자원·덱 페이지 문자열을 편집/전이 때 갱신한다. 입력란의 변화가 없으면 파서를 호출하지 않는다. 고정 계산기 키 20개·층 표시 8개·방 이름 6개는 정적 배열이다. 덱 정리는 페이지당 12장을 기존 카드 그림 버퍼로 표시한다.
- 리소스는 배경 v3, 이안 v2, 루나 v6의 Texture 세 개를 로드한다. 이전 루나 PNG는 보관하되 기본 화면에서 로드하지 않는다. 신규 초상에도 별도 프레임 텍스처 생성은 없다.
- `FragmentsV5Smoke`는 Editor 전용이고 명시적인 batch 호출에만 fixture를 설치한다. 생성 지도 두 시드와 고정 방/전투를 분리한다. V2/V3 고정 맵 fixture는 역사적 기록이며 현재 자동 확인의 기준이 아니다.

검증: EditMode 85개, 생성 경로 100개 시드, 실제 화면 23장. 렌더링은 균일 샘플링의 근사이며 고주파/불연속에 대한 적응형 판정은 미구현이다. 수학·전이 검사와 메모리 소유권 기록을 새로운 Profiler 측정으로 오인하지 않는다.

## v5 파편·응축 기반 (아래 v7 확장 적용)

- FragmentEquation은 한 전투에 하나다. x/y double 배열 각각 `(8+1)×1536`개, FragmentKind[8], 주파수 int[9]를 소유한다. 좌표 배열 합계 221,184바이트(216 KiB)다. 0층은 회전 기준, 다음 층은 카드가 이전 층 전체를 감싼 결과다. 추가/취소/초기화 때 고정 버퍼를 재사용한다.
- 역회전·고속 반복은 이전 층의 모듈러 인덱스를 읽는다. 각 추가 카드는 1,536회 계산하며 중첩으로 재귀 평가 횟수가 지수적으로 증가하지 않는다. 매 프레임 Sample은 최종 층에서 두 점 보간만 한다. 새 컬렉션·노드·문자열 할당이 없다.
- EquationState.EnableFragments가 모드를 활성화하고 FragmentEquation을 재사용한다. 빈 상태의 HasBase는 false이며 카드 추가 후 true다. TryRemoveLastStep은 마지막 층을 제거하고 마지막 카드였다면 HasBase를 비운다. legacy mode의 이력과 분리한다.
- BattleDefinition.UsesFragments는 계산기 모드와 동시 활성화할 수 없다. BattleSession은 CondenseCount, SealedCardCount, 유지 여부를 소유한다. 계획 → 응축 → 적 턴 → 계획에서는 파편 유지, 방출/해체 → 적 턴 → 계획에서는 초기화한다. 드로우 보너스는 확정 인덱스 이후 새 파편만 합산한다.
- BattleSession의 에너지 필드는 legacy 모드용으로 남기되 현재 카드는 Cost 0, 이동 지출 0이다. 실제 런과 UI에서는 사용하지 않는다. 조립 피해 보너스는 파편 수에서 계산하고 실제 적중한 적에게만 적용한다.
- DeckSession은 retained 모드에서 기존 손패 배열(최대 8)과 예약 배열(CardDefinition[덱 크기], 최대 64)을 소유한다. DrawNewHand는 첫 손패 5 또는 시작 손패 유물 적용 크기까지 받는다. DrawRetained는 남은 손패를 버리지 않고 빈 자리만 보충한다.
- ReserveFromHand는 손패에서 예약으로 옮긴다. ReleaseReserved는 방출/해체에만 버린 더미로 이동한다. 예약 카드는 재셔플 대상이 아니다. 취소는 예약의 마지막 카드와 이전 손패 인덱스를 검증한 뒤 원상복구한다. 손패+드로우+버림+예약의 총합을 테스트한다.
- BattleGameSession은 다음 보충 보너스 int 하나를 턴 확정 시 저장한다. 응축 시 카드 인덱스 이력을 유지하고 방출/해체 뒤 비운다. 확정 파편 취소는 Core를 호출하기 전에 거부하여 두 객체의 부분 복구를 방지한다. 이번 작업 초기 테스트에서 잡힌 해당 예외를 수정하고 회귀를 통과했다.
- RunGameSession은 FragmentRelicCatalog의 BonusDraw를 전투별 기본 보충량에 전달한다. 시작 손패 보정과 보존 손패 한도는 분리한다. 새 전투는 빈 조립대로 시작하며 v4 계산기 문자열 지속은 legacy 모드에서만 사용한다.
- Runtime은 파편 조립대와 손패를 기존 IMGUI/GL로 출력한다. 새 float 하나로 마지막 파편의 짧은 진입 이동을 계산한다. 슬롯당 GameObject를 만들지 않는다. 조립/보충/피해 요약 문자열은 Refresh에서만 만든다.
- SkillVisual은 최초 구성 때 줄바꿈 능력 요약 CompactAbilities를 추가로 만든다. 실제 파편의 그림은 완성 그래프 대신 기호와 괄호로 그린다. 6~8장 손패는 큰 축약 기호와 두 줄 능력, 호버 시 185×228 확대 카드를 기존 정의로 다시 출력한다. 프레임별 문자열 가공은 하지 않는다.
- FragmentsV5Smoke는 Editor 전용 명시적 batch fixture다. 24장의 Full HD/720p를 남기고 응축 유지·방출 초기화·손패 한도·응축 한도·방/보상/완주 전이를 단언한다. v4 이전 fixture는 역사적 기록으로 보관한다.

v5 검증은 EditMode 97개와 실제 화면 fixture 24장이다. v6는 Core/Application 변경 없이 화면 fixture 17장을 다시 확인했다. GC/프레임 시간의 신규 Profiler 측정은 하지 않았으며 기존 성능 보고를 v5 측정으로 표현하지 않는다. 사용자가 열어 둔 Editor와 격리된 검증 사본을 사용한다.

## v6 정보 공개와 인물 표시 기반

`GraphaclysmQuietView.cs`는 ModernView의 partial로 기본 파편 전투 UI를 담당한다. 규칙이나 도메인 상태를 소유하지 않는다. `showBattleDetails`와 `showSecondaryActions`는 View 수명의 bool이며 전투 바인딩에서 초기화한다. `EnemyRow`를 적 목록과 호버 판정에서 공유한다. 손패를 그린 다음 적 패널을 그려 같은 프레임의 카드 호버 상세를 표시한다. 전투 외 카드/보상은 능력 요약을 유지한다.

조립/응축 문자열과 마지막 피드백은 View에 캐시하고 Refresh에서만 갱신한다. 피드백 종료 시간은 float 하나이며 새 메시지를 3초 노출한다. 프레임별 새 컬렉션이나 Texture를 만들지 않는다. 메인 화면의 작은 천체 장식은 기존 코드 도형으로 그린다. 새 루나 PNG는 Resources에서 View 수명으로 로드하고 선택/지도/필드에 재사용한다. 필드 얼굴 UV와 선택 화면 크기만 새 구도에 맞췄다.

`QuietV6Smoke`는 Editor 전용 명시적 batch runner이며 17개의 Full HD/720p 화면과 Runtime errors 0을 기록했다. 소스/검증 사본 일치 및 Core/Application 보존 기록은 `Logs/quiet-v6-source-verification.txt`다. 이번 UI 변경의 신규 Profiler 수치는 측정하지 않았다.

## v7 기준점·응축·층·유물 소유권

- FragmentEquation이 기준점 prefix double[9] 두 개를 추가 소유한다. 기존 F(t)의 x/y 층별 double 버퍼와 같은 Count로 원자적으로 취소·유지·초기화한다. HomeAnchor 좌표는 BattleSession이 카드 사용 순간 전달한다. 이후 변형은 로컬 F만 바꾸며 Sample에서 O를 더한다. 동적 객체나 새로운 프레임 배열은 없다.
- GraphSegmentClipper.ClipReveal은 선택적 centerX/Y를 받고 가장 먼 필드 모서리까지의 반경으로 공개한다. legacy 기본값은 (5,0). AstralSpellRenderer와 작도 집광/필드 기준점 표식이 같은 O를 사용한다. 실제 판정은 같은 Equation.Sample이며 연출 반경으로 충돌하지 않는다.
- BattleSession이 CondenseHealthCost와 위력을 소유한다. 응축은 체력 2/4를 선불 소비하고 비용 이하 체력은 거부한다. BattleGameSession은 condensing bool과 다음 드로우 합계를 보관하고 응축의 기본 1장/보너스 최대 1장을 적용한다. 방출/해체 보충은 turnDraw를 쓴다. 기존 예약 카드·확정 취소 금지는 그대로다.
- 유물 collection을 BattleSession 생성 시 읽고 시작 보호막·자가 보호막·첫 방출·짧은/긴 조립·이동 후 방출 보정의 int 6개로 복사한다. startingResonance에 시작 공명 보정을 더한다. mutable collection 참조를 전투에 저장하지 않는다. resolvedPlots int로 첫 방출을 구분한다. 이동 보호막은 실제 방출 때 처리하여 취소 복제를 막는다. 승리 공명은 RunGameSession.CompleteVictory에서 한 번 처리한다.
- RunMapDefinition은 RoomsPerFloor 메타데이터를 가진다. 기본 0 인수는 기존 한 층 지도로 해석하여 legacy 테스트를 유지한다. 생성기는 3×8의 24개 깊이, 전체 45~66노드/최대 3레인으로 구성한다. 원정 생성 때만 노드·간선 배열을 만든다. 매 층 마지막 깊이의 보스를 다음 층 첫 방으로 연결하고 마지막 보스만 끝점이다.
- RunGameSession.CurrentFloor는 active/lastCompleted layer에서 계산한다. Map이 전체 원정을 소유하므로 덱·유물·체력·공명의 별도 복사 전이가 없다. 중간 보스 승리 시 회복 8 후 유물 보상을 제공한다. UI는 현재 층 8개 열만 그린다. 새 전투마다 새 BattleSession/DeckSession을 생성한다.

## v7 카드 사전과 UI 수명

AtlasView는 ModernView partial이다. codexOpen/Relics bool, 필터/페이지/선택 인덱스와 int[23] 인덱스 버퍼를 소유한다. 사전용 FragmentEquation 하나와 Vector2[129] 미리보기 버퍼를 View 수명 동안 재사용한다. 해당 FragmentEquation도 기존 층별 double 버퍼를 가진다. 필터/선택 명령 때만 첫 파편 예시와 페이지 문자열을 갱신하고 OnGUI는 저장된 점을 읽는다. 미리보기는 동일 축척 35px/단위, 128선분으로 표시하며 전투 판정에는 참여하지 않는다. 전체 사전은 실제 Catalog를 읽고 별도 중복 카드 정의가 없다.

카드의 상징 문양은 파편마다 65점으로 Awake에서 생성한다. 문양은 수학 출력으로 약속하지 않으며 실제 곡선 예시는 사전 패널에서 따로 표시한다. 루나/이안/배경 Texture는 기존 자산을 그대로 재사용한다. 새 이미지 생성·로드는 없다. 이동 패널은 왼쪽 하단, 두 취소는 같은 행, 방출/응축은 오른쪽 고정 폭이다.

2026-09-06 Unity EditMode 111개 통과, ExpeditionV7Smoke 23장 Full HD/720p, Runtime errors 0. Editor fixture 외에는 진단 상태 설치가 없다. 화면 fixture의 지도 빨리 넘기기/8장 보충은 실제 플레이 밸런스 검증으로 사용하지 않는다. 최종 소스 비교는 Logs/expedition-v7-source-verification.txt. 새 Profiler 측정은 하지 않았다.

## v8 파편 확장과 넓은 전투 화면

리사주·에피트로코이드·리마송·전단 변환·위상 차·복소 거듭제곱을 조사하여 파편 6종을 추가했다. 실제 카드는 총 23종이며 기존 14종 유물과 3층 원정을 유지한다. [조사와 정확한 합성 규칙](Docs/CARD_RESEARCH_V8.md)을 따른다. 이안 시작 덱은 중복 잔향 한 장을 비껴쓴 선으로, 루나는 윤무 한 장을 쌍성직조로 교체했다.

필드 사각형은 (423,108)에서 950×760, 두 축 모두 95 논리 px/단위다. 이전보다 가로·세로 25%, 면적 56.25% 확대했다. 전장 좌표 범위와 이동/명중 규칙은 유지한다. 왼쪽 자원·조립·이동, 오른쪽 적 의도·호버·행동, 필드 아래 손패로 정렬했다. 상세/수식 버튼과 피드백은 필드 위쪽이다. 최대 8장 손패는 폭을 나누어 배치하고 호버를 확대하며, 카드 설명과 배지 사이 간격을 확보한다. 새 카드 문양도 기존 코드 선을 사용한다.

Unity EditMode 122/122 통과: `Logs/editmode-wide-v8.xml`. 새 수식이 첫 카드·이전 비원형 궤적에 적용되는 실제 좌표와 기준점/취소/주파수 경계를 확인한다. 화면 검증은 명시적 `Graphaclysm.Editor.WideV8Smoke.RunBatch`로 실행하며 결과는 PROTOTYPE의 최신 검증 항목을 따른다. 전체 런 밸런스·새 Profiler 측정을 대신하지 않는다.

FragmentEquation의 새 6개 연산도 기존 prefix x/y 버퍼에 계산한다. 시간 재샘플링은 1,536 표본의 정수 인덱스, 위상 이동은 정확히 384칸 이동이다. 최대 주파수의 보수 상한은 쌍성직조/삼중봉인 ×3, 월식 ×4, 편월 +1, 전단/위상 유지다. 새 샘플링 배열이나 프레임별 할당은 없다. Catalog의 불변 카드 6개와 그 능력 배열은 앱 수명이다. View는 Catalog 크기에 맞춘 SkillVisual 47개와 파편 문양 65점씩을 Awake에서 만들며 기존 폴링/출력에 재사용한다. 사전 인덱스는 int[23]이다. Field와 FieldUnit을 화면/표식/실제 반경 변환이 공유한다. 검증용 새 테스트와 WideV8Smoke의 진단 상태 설치는 Editor에서만 동작한다.

V8 최종 화면은 `Logs/WideV8Captures` 27장, Runtime errors 0이다. `Logs/wide-v8-source-verification.txt`에 검증 사본과의 일치를 기록했다. 변경 전 소스는 `Logs/BeforeWideV8-*`, 문서는 `Docs/Archive/BeforeWideV8`에 보관했다.
