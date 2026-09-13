# GRAPHACLYSM 업로드 소스 확인 근거

기준: 대화에 첨부된 `GRAPHACLYSM-main.zip`의 v15 소스. 아래는 압축을 풀어 읽은 실제 파일의 발췌다. 새 스킬 설계나 실행 검증 결과가 아니다. 저장소 문서에 기록된 테스트 통과 수는 이번 검토에서 재실행하지 않았다.

## `README.md` · 원본 1–46행

```text
0001  # GRAPHACLYSM
0002  
0003  수식 파편 카드를 순서대로 조립하여 그래프를 만들고, 같은 전장의 적에게 피해를 주거나 자신에게 강화를 주는 Unity 덱 빌딩 로그라이트 프로토타입입니다.
0004  
0005  ![전체 화면 전장과 부채꼴 손패](Docs/Screenshots/battlefield-v10.png)
0006  
0007  ## 실행
0008  
0009  1. 저장소를 복제합니다.
0010     ```sh
0011     git clone https://github.com/oscar87657/GRAPHACLYSM.git
0012     ```
0013  2. Unity Hub에서 복제한 프로젝트 폴더를 추가하고 **Unity 6000.3.23f1**로 엽니다.
0014  3. 패키지 설치와 에셋 임포트가 끝나면 `Assets/Scenes/SampleScene.unity`를 열고 Play합니다.
0015  4. 메인의 카드 사전에서 파편을 살펴보거나, 새 기록 시작 → 이안/루나 선택 → 여정 시작으로 플레이합니다. 저장한 원정은 이어하기를 선택합니다.
0016  
0017  ## 현재 구현 · v15
0018  
0019  - 이전 궤적 전체를 감싸는 수식 파편 31종, 카드별 문양·등급·부가 능력
0020  - 에너지 비용 없이 시작 손패 5장, 보존 손패·조립대 각각 최대 8장
0021  - 방출·체력을 사용하는 응축·해체, 이동과 이동 취소
0022  - 첫 파편이 현재 플레이어 위치에서 시작하는 작도, 적 적중 피해와 자기 적중 강화
0023  - 이벤트·휴식·유물 방과 층별 보스를 포함한 3층 생성 던전, 유물 20종
0024  - 가시·추진·요새화·파열을 포함한 상태 12종과 현재 수치·남은 턴이 보이는 상태 칩
0025  - 3턴 쿨타임 이동 궤적 기술과 처치 초기화·3/5갈래 공격 연출
0026  - 이안/루나가 서로 다른 3종 전투 기술·3종 궁극기를 교체하고 부가 효과를 고르는 캐릭터별 18노드 성좌
0027  - 필드에서 반경 1.8 안의 좌표를 직접 고르는 이동, 적·기둥과 겹친 클릭의 가까운 빈 자리 보정
0028  - 패배·완주 잔광으로 올리는 메인 화면 영구 기록과 새 원정 초기화 분리
0029  - 중앙 관문 양쪽의 큰 투명 옆모습 초상, 기본 눈 감음·호버 눈 뜸 연출
0030  - 전체 화면 전장 배경, 1180×944 활성 좌표 필드, 가장자리 오버레이 HUD
0031  - 책장형 카드와 부채꼴 손패, 상세 패널까지 포인터를 옮겨도 유지되는 키워드 설명
0032  - 이동을 막는 기록 기둥과 작도 적중 피해를 높이는 굴절 프리즘
0033  - 행동 후 자동 저장과 이어하기, 이전 저장 백업과 손상 복구
0034  - Esc 일시정지, 저장 후 타이틀·종료, 새 원정의 저장 교체 확인
0035  - 전체/효과음 음량, 창·전체 화면과 해상도, 연출 줄이기, 창 전환 시 일시정지 설정
0036  - 기본 효과음 6종, 5단계 조작 안내, 원정 중 보유 덱·유물 열람
0037  
0038  카드 클릭/숫자 1~8로 조립하고 Enter로 방출합니다. Space는 응축, 필드 클릭/방향키는 이동, Backspace는 이동 취소, 우클릭은 파편 취소입니다. K는 가리킨 적을 대상으로 하는 캐릭터별 쿨타임 기술, G는 이번 원정의 배타적 18노드 성장 화면입니다. 상세 설명은 카드 호버와 사전, 전체 조작은 게임의 `?`와 [실행 문서](PROTOTYPE.md)에서 확인할 수 있습니다.
0039  
0040  **Esc**로 일시정지하고 **D**로 보유 덱·유물을 확인합니다. **F1 / ?**는 조작 안내입니다. 도움말·설정·덱 목록을 열어도 전투 진행이 멈춥니다. 이어하기 슬롯은 하나이며 손패·조립·이동·응축·보상 선택 상태까지 복원합니다. 패배·완주한 원정은 종료 기록으로 저장합니다. 화면 모드와 해상도는 실행 빌드에서 적용됩니다.
0041  
0042  저장 위치와 복구 방식은 [기본 기능 v9](Docs/BASICS_V9.md), 새 전장과 규칙 버전 10은 [전장·카드 UI 개편](Docs/BATTLEFIELD_V10.md), 유물·두 성장 축은 [전투와 성장 v12](Docs/COMBAT_GROWTH_V12.md), 카드·상태 정보는 [전투 정보·성좌 성장 v14](Docs/COMBAT_READABILITY_V14.md), 최신 자유 이동과 기술 성좌는 [전투 조작·기술 성좌 v15](Docs/COMBAT_CONTROL_GROWTH_V15.md)에 정리했습니다.
0043  
0044  ## 개발과 검증
0045  
0046  구조는 `Runtime/Presentation → Application → Core`입니다. Unity에서 Test Runner의 EditMode 테스트를 실행할 수 있습니다. 최신 검증은 **154개 테스트 통과**, Full HD/720p 화면 fixture 33장, 게임 Runtime 오류 0건입니다. 자유 좌표 이동·저장 재생, 충돌 지점 보정, 선택 캐릭터 눈 유지, 18노드 의존 분기와 캐릭터별 전투 변형을 함께 확인했습니다. 이는 Windows 빌드 수동 플레이, 전체 런 밸런스 검증이나 성능 측정 결과를 의미하지 않습니다.
```

## `Docs/COMBAT_CONTROL_GROWTH_V15.md` · 원본 1–38행

```text
0001  # 전투 조작·기술 성좌 v15
0002  
0003  2026-09-10. 선택한 기록의 눈 상태, 조립 목록의 누적 들여쓰기, 경직된 사방 이동과 전투 기술의 경계 빗나감, 적고 비슷한 성장 선택지를 함께 개편했다.
0004  
0005  ## 시각 오류 정리
0006  
0007  - 캐릭터 선택에서 호버한 인물뿐 아니라 현재 선택된 기록도 눈을 뜬 채 유지한다. 다른 기록을 선택하면 이전 인물은 다시 눈을 감는다.
0008  - 전투 왼쪽 조립 목록은 항목마다 3px씩 늘던 들여쓰기를 제거해 모든 파편의 기준선을 같은 x 좌표에 둔다.
0009  - 성좌 화면은 불투명한 먹빛 바탕과 밝은 전용 제목 스타일을 사용해 뒤의 지도 글자가 비치거나 제목이 사라지지 않는다.
0010  
0011  ## 18노드 기술 성좌
0012  
0013  [Last Epoch 공식 기술 전문화 안내](https://support.lastepoch.com/hc/en-us/articles/46363203944859-Skill-Specialization)는 기술 하나에 별도 전문화 트리를 두는 구조를 설명한다. [Hades 공식 Nighty Night 업데이트](https://www.supergiantgames.com/blog/hades-the-nighty-night-update-patch-notes/)는 같은 슬롯의 양면 재능 중 하나만 활성화하는 선택을 소개한다. 여기서는 전자의 ‘기술 사용법을 바꾸는 전용 트리’와 후자의 ‘서로 배타적인 선택’을 현재 작은 전투 규모에 맞게 결합했다. 화면 구성·명칭·수치는 GRAPHACLYSM 고유 구현이다.
0014  
0015  각 캐릭터는 전투 기술 교체 3개와 궁극기 교체 3개를 가진다. 교체 형태를 하나 고른 뒤 그 형태에 연결된 부가 노드 두 개 중 하나를 고른다. 총 18노드이며 동일 단계의 반대 갈래는 해당 원정에서 잠긴다.
0016  
0017  ### 이안
0018  
0019  - 기본 `유리 쇄도`: 선택한 적을 반드시 가르는 직선 이동 피해.
0020  - `삼중 유리길`: 세 갈래 관통. 오중 굴절 또는 적중 보호막으로 분기.
0021  - `집행의 직선`: 높은 단일 피해와 처치 즉시 초기화. 공명 획득 또는 파열로 분기.
0022  - `거울 교환`: 대상 뒤로 전이하고 대상 주변 폭발. 넓은 잔상 또는 방어 장막으로 분기.
0023  - 궁극기 `성좌 붕괴 / 불멸의 기록 / 흑경 반전`: 각각 파열 공격, 고정·자가 회복, 강화 제거·약화에 특화한다.
0024  
0025  ### 루나
0026  
0027  - 기본 `월광 도약`: 대상 곁으로 이동해 착지 파동 피해.
0028  - `만월 착지`: 넓은 착지 파동. 반경 확대 또는 정화·보호막으로 분기.
0029  - `초승달 회귀`: 왕복 참격 후 원위치. 노출 또는 회복·추진으로 분기.
0030  - `별무리 전이`: 대상 주변 적으로 이어지는 연쇄 피해. 연결 범위 또는 처치 초기화로 분기.
0031  - 궁극기 `만월의 포옹 / 그믐의 칼날 / 월식 정지`: 각각 자가 강화, 공격, 고정·기술 대기 초기화에 특화한다.
0032  
0033  ## 자유 이동과 기술 적중
0034  
0035  일반 이동은 턴마다 한 번, 현재 위치 중심 반경 1.8 안의 임의 지점을 필드에서 클릭한다. 사거리 밖을 누르면 같은 방향의 원 경계로 제한한다. 적이나 기록 기둥과 겹치는 지점은 입력을 버리지 않고 요청 지점에서 가장 가까운 유효 후보를 고정 순서로 탐색해 미끄러진다. 이동 전에는 실제 도착 마름모와 연결선, 전체 사거리 원을 표시한다. 방향키는 같은 반경의 빠른 입력으로 남기고 Backspace 이동 취소도 유지한다.
0036  
0037  전투 기술은 선택한 살아 있는 적을 명시적인 주 대상으로 판정한다. 안전한 착지점을 찾느라 캐릭터가 충돌 반경 앞에서 멈춰도 주 대상 피해가 빠지지 않는다. 다중선·착지 파동·연쇄 전이는 각자의 실제 범위로 추가 적을 판정하며 마지막 기술 스타일을 Presentation에 전달해 직선, 다중선, 폭발, 왕복, 연쇄 잔광을 구분한다.
0038  
```

## `Assets/Scripts/Core/Runs/RunGrowthState.cs` · 원본 6–82행

```text
0006      public sealed class RunGrowthNode
0007      {
0008          public RunGrowthNode(string name, string description, int cost, int requiredLevel, int group, int parentIndex = -1)
0009          {
0010              Name = name; Description = description; Cost = cost; RequiredLevel = requiredLevel;
0011              Group = group; ParentIndex = parentIndex;
0012          }
0013          public string Name { get; }
0014          public string Description { get; }
0015          public int Cost { get; }
0016          public int RequiredLevel { get; }
0017          public int Group { get; }
0018          public int ParentIndex { get; }
0019      }
0020  
0021      /// <summary>Run-scoped progression. It is rebuilt by command replay and intentionally resets with a new run.</summary>
0022      public sealed class RunGrowthState
0023      {
0024          public const int NodeCount = 18;
0025          private readonly RunGrowthNode[] nodes;
0026          private int unlockedMask;
0027  
0028          public RunGrowthState(CombatArchetype archetype)
0029          {
0030              Archetype = archetype;
0031              nodes = archetype == CombatArchetype.Luna ? LunaNodes() : IanNodes();
0032              Level = 1;
0033          }
0034  
0035          public CombatArchetype Archetype { get; }
0036          public int Level { get; private set; }
0037          public int Experience { get; private set; }
0038          public int Points { get; private set; }
0039          public int ActiveVariant => IsUnlocked(0) ? 1 : IsUnlocked(1) ? 2 : IsUnlocked(2) ? 3 : 0;
0040          public int ModuleVariant => IsUnlocked(3) ? 1 : IsUnlocked(4) ? 2 : IsUnlocked(5) ? 3
0041              : IsUnlocked(6) ? 4 : IsUnlocked(7) ? 5 : IsUnlocked(8) ? 6 : 0;
0042          public int UltimateVariant => IsUnlocked(9) ? 1 : IsUnlocked(10) ? 2 : IsUnlocked(11) ? 3 : 0;
0043          public int UnlockedMask => unlockedMask;
0044          public int ExperienceToNext => 3 + (Level - 1) / 3;
0045          public RunGrowthNode GetNode(int index)
0046          {
0047              if (index < 0 || index >= NodeCount) throw new ArgumentOutOfRangeException(nameof(index));
0048              return nodes[index];
0049          }
0050  
0051          public bool IsUnlocked(int index) => index >= 0 && index < NodeCount && (unlockedMask & (1 << index)) != 0;
0052  
0053          public bool CanPurchase(int index)
0054          {
0055              if (index < 0 || index >= NodeCount || IsUnlocked(index)) return false;
0056              RunGrowthNode node = nodes[index];
0057              if (Points < node.Cost || Level < node.RequiredLevel) return false;
0058              if (node.ParentIndex >= 0 && !IsUnlocked(node.ParentIndex)) return false;
0059              for (int i = 0; i < NodeCount; i++)
0060                  if (i != index && nodes[i].Group == node.Group && IsUnlocked(i)) return false;
0061              return true;
0062          }
0063  
0064          public bool TryPurchase(int index)
0065          {
0066              if (!CanPurchase(index)) return false;
0067              RunGrowthNode node = nodes[index];
0068              Points -= node.Cost;
0069              unlockedMask |= 1 << index;
0070              return true;
0071          }
0072  
0073          public bool TrySelect(int index)
0074          {
0075              return false;
0076          }
0077  
0078          public void AddExperience(int amount)
0079          {
0080              if (amount <= 0) return;
0081              Experience += amount;
0082              while (Experience >= ExperienceToNext)
```

## `Assets/Scripts/Core/Runs/RunGrowthState.cs` · 원본 86–137행

```text
0086                  Points++;
0087              }
0088          }
0089  
0090          public BattleSkillLoadout CreateLoadout()
0091              => new BattleSkillLoadout(ActiveVariant, ModuleVariant, UltimateVariant, unlockedMask);
0092  
0093          private static RunGrowthNode[] IanNodes() => new[]
0094          {
0095              new RunGrowthNode("삼중 유리길", "유리 쇄도를 피해 6의 넓은 세 갈래 관통으로 교체합니다.", 1, 2, 0),
0096              new RunGrowthNode("집행의 직선", "유리 쇄도를 피해 10의 단일 처형선으로 교체합니다. 처치 시 즉시 재사용합니다.", 1, 2, 0),
0097              new RunGrowthNode("거울 교환", "유리 쇄도를 대상 뒤로 전이해 주변을 피해 7로 폭발시키는 기술로 교체합니다.", 1, 2, 0),
0098              new RunGrowthNode("오중 굴절", "삼중 유리길을 피해 5의 다섯 갈래로 바꿉니다.", 1, 3, 1, 0),
0099              new RunGrowthNode("반사 장막", "삼중 유리길 적중마다 보호막 2, 기본 보호막 3을 얻습니다.", 1, 3, 1, 0),
0100              new RunGrowthNode("연쇄 처형", "집행의 직선으로 처치하면 공명 1을 얻습니다.", 1, 3, 2, 1),
0101              new RunGrowthNode("파열 흔적", "집행의 직선에 맞은 적에게 파열 3을 남깁니다.", 1, 3, 2, 1),
0102              new RunGrowthNode("잔상 폭발", "거울 교환의 폭발 반경이 1.15에서 1.85로 넓어집니다.", 1, 3, 3, 2),
0103              new RunGrowthNode("위상 장막", "거울 교환 뒤 보호막 6과 요새화 3을 얻습니다.", 1, 3, 3, 2),
0104              new RunGrowthNode("성좌 붕괴", "궁극기를 피해 +10과 파열 3을 주는 공격 형태로 교체합니다.", 1, 3, 4),
0105              new RunGrowthNode("불멸의 기록", "궁극기를 피해 +6, 고정 2, 자신 적중 시 회복 4 형태로 교체합니다.", 1, 3, 4),
0106              new RunGrowthNode("흑경 반전", "궁극기를 피해 +8, 적의 강화 제거와 약화 2를 주는 반전 형태로 교체합니다.", 1, 3, 4),
0107              new RunGrowthNode("붕괴 반향", "성좌 붕괴로 적 2명 이상 적중하면 공명 1을 돌려받습니다.", 1, 4, 5, 9),
0108              new RunGrowthNode("날카로운 잔해", "성좌 붕괴의 피해가 추가로 4 증가합니다.", 1, 4, 5, 9),
0109              new RunGrowthNode("정지된 장", "불멸의 기록이 남기는 고정의 지속시간이 1회 늘어납니다.", 1, 4, 6, 10),
0110              new RunGrowthNode("불멸의 여백", "자신 적중 시 보호막 4와 회복 4를 추가로 얻습니다.", 1, 4, 6, 10),
0111              new RunGrowthNode("흡광 장막", "흑경 반전으로 자신을 맞히면 해로운 상태를 지우고 보호막 6을 얻습니다.", 1, 4, 7, 11),
0112              new RunGrowthNode("역상 파열", "흑경 반전이 약화 3과 파열 2를 함께 남깁니다.", 1, 4, 7, 11)
0113          };
0114  
0115          private static RunGrowthNode[] LunaNodes() => new[]
0116          {
0117              new RunGrowthNode("만월 착지", "월광 도약을 넓은 착지 파동으로 교체합니다. 주변 적에게 피해 5를 줍니다.", 1, 2, 0),
0118              new RunGrowthNode("초승달 회귀", "월광 도약을 피해 9의 왕복 참격으로 교체합니다. 사용 후 원래 자리로 돌아옵니다.", 1, 2, 0),
0119              new RunGrowthNode("별무리 전이", "월광 도약을 대상에서 주변 적으로 이어지는 피해 5의 연쇄 전이로 교체합니다.", 1, 2, 0),
0120              new RunGrowthNode("넘치는 만월", "만월 착지의 파동 반경이 1.70에서 2.20으로 넓어집니다.", 1, 3, 1, 0),
0121              new RunGrowthNode("은하 피난처", "만월 착지 때 해로운 상태를 지우고 보호막 4·요새화 3을 얻습니다.", 1, 3, 1, 0),
0122              new RunGrowthNode("월식 흉터", "초승달 회귀에 맞은 적에게 노출 3을 남깁니다.", 1, 3, 2, 1),
0123              new RunGrowthNode("유성 호흡", "초승달 회귀 적중 시 체력 3을 회복하고 추진 3을 얻습니다.", 1, 3, 2, 1),
0124              new RunGrowthNode("별자리 확장", "별무리 전이의 연결 반경이 2.40에서 3.60으로 넓어집니다.", 1, 3, 3, 2),
0125              new RunGrowthNode("낙성 추격", "별무리 전이로 적을 처치하면 즉시 재사용하고 공명 1을 얻습니다.", 1, 3, 3, 2),
0126              new RunGrowthNode("만월의 포옹", "궁극기를 큰 자가 적중 반경과 정화·보호막·회복 형태로 교체합니다.", 1, 3, 4),
0127              new RunGrowthNode("그믐의 칼날", "궁극기를 피해 +4와 작은 보호막·회복을 갖는 공격 형태로 교체합니다.", 1, 3, 4),
0128              new RunGrowthNode("월식 정지", "궁극기를 피해 +2와 적 고정 2를 주는 제어 형태로 교체합니다.", 1, 3, 4),
0129              new RunGrowthNode("월광 회수", "만월의 포옹으로 적 2명 이상 적중하면 공명 1을 돌려받습니다.", 1, 4, 5, 9),
0130              new RunGrowthNode("차오른 만월", "자가 적중 반경이 1.50이 되고 보호막 4·회복 2가 추가됩니다.", 1, 4, 5, 9),
0131              new RunGrowthNode("긴 밤의 칼날", "그믐의 칼날이 주는 피해가 추가로 4 증가합니다.", 1, 4, 6, 10),
0132              new RunGrowthNode("새벽의 숨", "자신 적중 시 보호막 4와 회복 4를 추가로 얻습니다.", 1, 4, 6, 10),
0133              new RunGrowthNode("시간 회수", "월식 정지를 방출하면 전투 기술의 남은 대기시간을 없앱니다.", 1, 4, 7, 11),
0134              new RunGrowthNode("정지 파동", "월식 정지가 적에게 약화 3을 함께 남깁니다.", 1, 4, 7, 11)
0135          };
0136      }
0137  }
```

## `Assets/Scripts/Core/Combat/BattleSkillLoadout.cs` · 원본 1–19행

```text
0001  namespace Graphaclysm.Core.Combat
0002  {
0003      public readonly struct BattleSkillLoadout
0004      {
0005          public BattleSkillLoadout(int activeVariant, int moduleVariant, int ultimateVariant, int traitMask = 0)
0006          {
0007              ActiveVariant = activeVariant;
0008              ModuleVariant = moduleVariant;
0009              UltimateVariant = ultimateVariant;
0010              TraitMask = traitMask;
0011          }
0012  
0013          public int ActiveVariant { get; }
0014          public int ModuleVariant { get; }
0015          public int UltimateVariant { get; }
0016          public int TraitMask { get; }
0017          public bool HasTrait(int nodeIndex) => nodeIndex >= 0 && nodeIndex < 31 && (TraitMask & (1 << nodeIndex)) != 0;
0018      }
0019  }
```

## `Assets/Scripts/Core/Combat/TacticalCombatState.cs` · 원본 10–48행

```text
0010      public sealed class TacticalCombatState
0011      {
0012          public const int UltimateCost = 6;
0013          public const double MoveDistance = 1.8;
0014          public const double PlayerRadius = 0.48;
0015          public CombatArchetype Archetype { get; }
0016          public int UltimateVariant { get; }
0017          public int SkillTraitMask { get; }
0018          public CombatStatusState Statuses { get; } = new CombatStatusState();
0019          public double X { get; private set; }
0020          public double Y { get; private set; }
0021          public int Resonance { get; private set; }
0022          public bool UltimateArmed { get; private set; }
0023          public bool HasMoved { get; private set; }
0024          public bool CanUndoMove { get; private set; }
0025          private double moveOriginX, moveOriginY;
0026          private int savedHaste, savedHasteDuration, savedMomentum, savedMomentumDuration;
0027          public double HitRadius => UltimateArmed && Archetype == CombatArchetype.Luna
0028              ? (UltimateVariant == 1 ? (HasTrait(13) ? 1.5 : 1.25) : 1.0) : PlayerRadius;
0029          public int MoveCost => Statuses.Get(CombatStatusKind.Haste) > 0 ? 0 : 1;
0030          public int AttackBonus => Statuses.Get(CombatStatusKind.Focus)
0031              - Statuses.Get(CombatStatusKind.Weaken)
0032              + Statuses.Get(CombatStatusKind.Momentum)
0033              + (UltimateArmed && Archetype == CombatArchetype.Ian
0034                  ? UltimateVariant == 1 ? 10 + (HasTrait(13) ? 4 : 0) : UltimateVariant == 3 ? 8 : 6 : 0)
0035              + (UltimateArmed && Archetype == CombatArchetype.Luna
0036                  ? UltimateVariant == 2 ? 4 + (HasTrait(14) ? 4 : 0) : UltimateVariant == 3 ? 2 : 0 : 0);
0037  
0038          internal TacticalCombatState(CombatArchetype archetype, int resonance, int ultimateVariant = 0, int skillTraitMask = 0)
0039          {
0040              Archetype = archetype;
0041              UltimateVariant = Math.Max(0, Math.Min(3, ultimateVariant));
0042              SkillTraitMask = skillTraitMask;
0043              Reset(resonance);
0044          }
0045  
0046          private bool HasTrait(int nodeIndex) => (SkillTraitMask & (1 << nodeIndex)) != 0;
0047  
0048          internal void Reset(int resonance)
```

## `Assets/Scripts/Core/Combat/TacticalCombatState.cs` · 원본 99–168행

```text
0099              if (!UltimateArmed && Resonance < UltimateCost) return false;
0100              UltimateArmed = !UltimateArmed;
0101              return true;
0102          }
0103  
0104          internal bool IsHit(EquationState equation)
0105              => equation.HasBase && EquationAnalyzer.IntersectsCircle(equation, X, Y, HitRadius, equation.CurveSegmentCount);
0106  
0107          internal int BeginSelfHit()
0108          {
0109              Statuses.Add(CombatStatusKind.Shield, 3, 1);
0110              if (UltimateArmed && Archetype == CombatArchetype.Ian && UltimateVariant == 3 && HasTrait(16))
0111              { Statuses.Cleanse(true); Statuses.Add(CombatStatusKind.Shield, 6, 1); }
0112              if (!UltimateArmed || Archetype != CombatArchetype.Luna) return 0;
0113              Statuses.Cleanse(true);
0114              int shield = UltimateVariant == 1 ? 12 : UltimateVariant == 2 ? 6 : 8;
0115              int healing = UltimateVariant == 1 ? 7 : UltimateVariant == 2 ? 3 : 5;
0116              if (Archetype == CombatArchetype.Luna && HasTrait(13)) { shield += 4; healing += 2; }
0117              if (Archetype == CombatArchetype.Luna && HasTrait(15)) { shield += 4; healing += 4; }
0118              Statuses.Add(CombatStatusKind.Shield, shield, 1);
0119              return healing;
0120          }
0121  
0122          internal int ApplySelfInscription(InscriptionKind inscription)
0123          {
0124              switch (inscription)
0125              {
0126                  case InscriptionKind.Ward: Statuses.Add(CombatStatusKind.Shield, 6, 1); break;
0127                  case InscriptionKind.Ember: Statuses.Add(CombatStatusKind.Focus, 2, 2); break;
0128                  case InscriptionKind.Exposure: Statuses.Add(CombatStatusKind.Regeneration, 2, 2); break;
0129                  case InscriptionKind.Mend: return 3;
0130                  case InscriptionKind.Cleanse: Statuses.Cleanse(true); break;
0131                  case InscriptionKind.Phase: Statuses.Add(CombatStatusKind.Haste, 1, 2); break;
0132              }
0133              return 0;
0134          }
0135  
0136          internal static void ApplyEnemyInscription(EnemyState enemy, InscriptionKind inscription)
0137          {
0138              switch (inscription)
0139              {
0140                  case InscriptionKind.Ward: enemy.Statuses.Add(CombatStatusKind.Weaken, 2, 2); break;
0141                  case InscriptionKind.Ember: enemy.Statuses.Add(CombatStatusKind.Burn, 3, 2); break;
0142                  case InscriptionKind.Exposure: enemy.Statuses.Add(CombatStatusKind.Exposure, 3, 2); break;
0143                  case InscriptionKind.Mend: enemy.Statuses.Add(CombatStatusKind.Anchor, 1, 1); break;
0144                  case InscriptionKind.Cleanse: enemy.Statuses.Cleanse(false); break;
0145                  case InscriptionKind.Phase: enemy.Statuses.Add(CombatStatusKind.Anchor, 1, 2); break;
0146              }
0147          }
0148  
0149          internal void CompletePlot(bool hitSelf, int enemiesHit)
0150          {
0151              bool usedUltimate = UltimateArmed;
0152              if (usedUltimate) Resonance -= UltimateCost;
0153              int earned = enemiesHit >= 2 ? 1 : 0;
0154              if (hitSelf && enemiesHit > 0) earned += 2;
0155              if (usedUltimate && enemiesHit >= 2 && HasTrait(12)) earned++;
0156              Resonance = Math.Min(UltimateCost, Resonance + earned);
0157              UltimateArmed = false;
0158          }
0159  
0160          internal void GainResonance(int amount)
0161          { Resonance = Math.Max(0, Math.Min(UltimateCost, Resonance + amount)); }
0162  
0163          internal void SkillDashTo(double x, double y)
0164          {
0165              X = Math.Max(PlayerRadius, Math.Min(10 - PlayerRadius, x));
0166              Y = Math.Max(-4 + PlayerRadius, Math.Min(4 - PlayerRadius, y));
0167              HasMoved = true;
0168              CanUndoMove = false;
```

## `Assets/Scripts/Core/Combat/BattleSession.cs` · 원본 53–103행

```text
0053          public const double EnemyHitRadius = 0.48;
0054          public const double MaximumTraceLength = 64;
0055          public const int MaximumPlayedCards = EquationState.MaximumModifiers + 3;
0056          public const int PrismDamageBonus = 2;
0057          public const int CombatSkillCooldownTurns = 3;
0058  
0059          private readonly BattleDefinition definition;
0060          private readonly EnemyState[] enemies;
0061          private readonly int playerMaxEnergy;
0062          private readonly int plotDamageBonus;
0063          private readonly CardDefinition[] playedCards =
0064              new CardDefinition[MaximumPlayedCards];
0065  
0066          private int playedCardCount;
0067          private bool preserveFragments;
0068          public bool UsesFragments => definition.UsesFragments;
0069          public int CondenseCount { get; private set; }
0070          public int SealedCardCount { get; private set; }
0071          public int CondenseHealthCost => (CondenseCount + 1) * 2;
0072          public bool CanCondense => UsesFragments && Phase == BattlePhase.PlayerPlanning && CondenseCount < 2 && PlayerHealth > CondenseHealthCost;
0073          public int PendingDrawBonus { get { int n = 0; for(int i=SealedCardCount;i<playedCardCount;i++) n += playedCards[i].DrawBonus; return n; } }
0074          public int WeaveDamageBonus { get { if(!UsesFragments) return 0; int n=0; for(int i=0;i<playedCardCount;i++) n+=FragmentCardCatalog.Power(playedCards[i].Fragment); return Math.Min(10,n); } }
0075          public bool TryCondense()
0076          {
0077              if (!CanCondense) return false;
0078              PlayerHealth -= CondenseHealthCost;
0079              if (condenseShield > 0) Tactics?.Statuses.Add(CombatStatusKind.Shield, condenseShield, 1);
0080              preserveFragments = true; CondenseCount++; SealedCardCount = playedCardCount;
0081              Phase = BattlePhase.EnemyTurn; return true;
0082          }
0083          public bool TryUnravel()
0084          { if (!UsesFragments || Phase != BattlePhase.PlayerPlanning) return false; preserveFragments = false; CondenseCount = 0; ResetEquationHistory(); Phase = BattlePhase.EnemyTurn; return true; }
0085          private int movementEnergySpent;
0086          private readonly int startingResonance;
0087          private readonly int startShield, selfShield, firstPlotDamage, shortWeaveDamage, longWeaveDamage, movedPlotShield;
0088          private readonly int condenseShield, startThorns, prismDamage, moveMomentum, longWeaveRupture, startFortify;
0089          private readonly BattleSkillLoadout skillLoadout;
0090          private int resolvedPlots;
0091          public int CombatSkillCooldown { get; private set; }
0092          public double LastSkillOriginX { get; private set; }
0093          public double LastSkillOriginY { get; private set; }
0094          public double LastSkillEndX { get; private set; }
0095          public double LastSkillEndY { get; private set; }
0096          public int LastSkillHitCount { get; private set; }
0097          public int LastSkillDamage { get; private set; }
0098          public bool LastSkillWide { get; private set; }
0099          public int LastSkillLaneCount { get; private set; }
0100          public int LastSkillStyle { get; private set; }
0101          public bool LastSkillCooldownReset { get; private set; }
0102          public bool CanUseCombatSkill => Phase == BattlePhase.PlayerPlanning && Tactics != null
0103              && CombatSkillCooldown == 0 && HasLivingEnemy();
```

## `Assets/Scripts/Core/Combat/BattleSession.cs` · 원본 459–550행

```text
0459          public bool TryUseCombatSkill(int targetIndex = -1)
0460          {
0461              if (!CanUseCombatSkill) return false;
0462              if (targetIndex < 0 || targetIndex >= enemies.Length || !enemies[targetIndex].IsAlive)
0463                  targetIndex = NearestLivingEnemy();
0464              if (targetIndex < 0) return false;
0465  
0466              LastSkillOriginX = Tactics.X; LastSkillOriginY = Tactics.Y;
0467              EnemyState target = enemies[targetIndex];
0468              double dx = target.X - Tactics.X, dy = target.Y - Tactics.Y;
0469              double distance = Math.Sqrt(dx * dx + dy * dy);
0470              if (distance < .001) return false;
0471              dx /= distance; dy /= distance;
0472  
0473              int variant = skillLoadout.ActiveVariant;
0474              bool ian = Tactics.Archetype == CombatArchetype.Ian;
0475              double travel = Math.Min(2.8, Math.Max(.35,
0476                  distance - TacticalCombatState.PlayerRadius - EnemyHitRadius - .04));
0477              double desiredX = Tactics.X + dx * travel, desiredY = Tactics.Y + dy * travel;
0478              if (ian && variant == 3)
0479              { desiredX = target.X + dx * 1.08; desiredY = target.Y + dy * 1.08; }
0480              else if (!ian && variant != 2)
0481              { desiredX = target.X - dx * 1.08; desiredY = target.Y - dy * 1.08; }
0482              FindSkillLanding(desiredX, desiredY, out double endX, out double endY);
0483  
0484              LastSkillStyle = ian ? (variant == 1 ? 1 : variant == 3 ? 2 : 0)
0485                  : (variant == 2 ? 3 : variant == 3 ? 4 : 2);
0486              LastSkillLaneCount = ian && variant == 1 ? (skillLoadout.HasTrait(3) ? 5 : 3) : 1;
0487              LastSkillWide = LastSkillStyle == 1 || LastSkillStyle == 2;
0488              double width = LastSkillStyle == 1 ? (LastSkillLaneCount == 5 ? 1.65 : 1.15) : .5;
0489              double burstRadius = ian ? (variant == 3 ? (skillLoadout.HasTrait(7) ? 1.85 : 1.15) : 0)
0490                  : (variant == 1 ? (skillLoadout.HasTrait(3) ? 2.2 : 1.7) : variant == 0 ? 1.15 : 0);
0491              double chainRadius = !ian && variant == 3 ? (skillLoadout.HasTrait(7) ? 3.6 : 2.4) : 0;
0492              int damage = ian
0493                  ? (variant == 1 ? (LastSkillLaneCount == 5 ? 5 : 6) : variant == 2 ? 10 : variant == 3 ? 7 : 7)
0494                  : (variant == 1 ? 5 : variant == 2 ? 9 : variant == 3 ? 5 : 6);
0495              int hitCount = 0, dealt = 0; bool killed = false;
0496              for (int i = 0; i < enemies.Length; i++)
0497              {
0498                  EnemyState enemy = enemies[i];
0499                  if (!enemy.IsAlive) continue;
0500                  bool hit = i == targetIndex;
0501                  if (!hit && burstRadius > 0)
0502                  { double ex = enemy.X - target.X, ey = enemy.Y - target.Y; hit = ex * ex + ey * ey <= (burstRadius + EnemyHitRadius) * (burstRadius + EnemyHitRadius); }
0503                  else if (!hit && chainRadius > 0)
0504                  { double ex = enemy.X - target.X, ey = enemy.Y - target.Y; hit = ex * ex + ey * ey <= chainRadius * chainRadius; }
0505                  else if (!hit && burstRadius <= 0 && chainRadius <= 0)
0506                      hit = DistanceToSegment(enemy.X, enemy.Y, LastSkillOriginX, LastSkillOriginY, endX, endY) <= width + EnemyHitRadius;
0507                  if (!hit) continue;
0508                  int before = enemy.Health;
0509                  enemy.TakeDamage(damage);
0510                  dealt += before - enemy.Health; hitCount++;
0511                  if (enemy.Health == 0) killed = true;
0512                  if (ian && variant == 2 && skillLoadout.HasTrait(6))
0513                      enemy.Statuses.Add(CombatStatusKind.Rupture, 3, 2);
0514                  if (!ian && variant == 2 && skillLoadout.HasTrait(5))
0515                      enemy.Statuses.Add(CombatStatusKind.Exposure, 3, 2);
0516              }
0517              LastSkillEndX = endX; LastSkillEndY = endY; LastSkillHitCount = hitCount; LastSkillDamage = dealt;
0518              if (ian || variant != 2) Tactics.SkillDashTo(endX, endY);
0519              if (ian)
0520              {
0521                  if (variant == 1 && skillLoadout.HasTrait(4))
0522                      Tactics.Statuses.Add(CombatStatusKind.Shield, 3 + hitCount * 2, 2);
0523                  if (variant == 3 && skillLoadout.HasTrait(8))
0524                  { Tactics.Statuses.Add(CombatStatusKind.Shield, 6, 2); Tactics.Statuses.Add(CombatStatusKind.Fortify, 3, 2); }
0525              }
0526              else
0527              {
0528                  if (variant == 1 && skillLoadout.HasTrait(4))
0529                  { Tactics.Statuses.Cleanse(true); Tactics.Statuses.Add(CombatStatusKind.Shield, 4, 2); Tactics.Statuses.Add(CombatStatusKind.Fortify, 3, 2); }
0530                  else if (variant == 2 && skillLoadout.HasTrait(6) && hitCount > 0)
0531                  { PlayerHealth = Math.Min(PlayerMaxHealth, PlayerHealth + 3); Tactics.Statuses.Add(CombatStatusKind.Momentum, 3, 2); }
0532              }
0533              LastSkillCooldownReset = (ian && variant == 2 && killed)
0534                  || (!ian && variant == 3 && skillLoadout.HasTrait(8) && killed);
0535              if (LastSkillCooldownReset && ((ian && skillLoadout.HasTrait(5)) || !ian)) Tactics.GainResonance(1);
0536              CombatSkillCooldown = LastSkillCooldownReset ? 0 : CombatSkillCooldownTurns;
0537              movementEnergySpent = 0;
0538              if (AreAllEnemiesDefeated()) Phase = BattlePhase.Victory;
0539              return true;
0540          }
0541  
0542          public bool TryBeginPlot()
0543          {
0544              if (!CanPlot)
0545              {
0546                  return false;
0547              }
0548  
0549              preserveFragments = false; CondenseCount = 0;
0550              Phase = BattlePhase.Plotting;
```

## `Assets/Scripts/Core/Combat/BattleSession.cs` · 원본 554–652행

```text
0554          public PlotReport ResolvePlot()
0555          {
0556              if (Phase != BattlePhase.Plotting)
0557              {
0558                  throw new InvalidOperationException("Plot resolution is only valid during the Plotting phase.");
0559              }
0560  
0561              int hitCount = 0;
0562              int totalDamage = 0;
0563              bool playerHit = PreviewPlayerHit;
0564              int healing = 0;
0565              int shieldBefore = Tactics == null ? 0 : Tactics.Statuses.Get(CombatStatusKind.Shield);
0566              bool resetSkillFromUltimate = Tactics != null && Tactics.UltimateArmed
0567                  && Tactics.Archetype == CombatArchetype.Luna && Tactics.UltimateVariant == 3
0568                  && skillLoadout.HasTrait(16);
0569  
0570              for (int i = 0; i < enemies.Length; i++)
0571              {
0572                  EnemyState enemy = enemies[i];
0573                  if (!enemy.IsAlive)
0574                  {
0575                      continue;
0576                  }
0577  
0578                  int damage = PreviewDamage(enemy);
0579  
0580                  if (damage <= 0)
0581                  {
0582                      continue;
0583                  }
0584  
0585                  enemy.TakeDamage(damage);
0586                  enemy.Statuses.Remove(CombatStatusKind.Rupture);
0587                  for (int cardIndex = 0; cardIndex < playedCardCount; cardIndex++)
0588                  {
0589                      if (playedCards[cardIndex].IsInscription)
0590                          TacticalCombatState.ApplyEnemyInscription(enemy, playedCards[cardIndex].Inscription);
0591                      ApplyBundledAbilities(playedCards[cardIndex], AbilityTarget.Enemy, enemy.Statuses);
0592                  }
0593                  if (Tactics != null && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Ian)
0594                  {
0595                      if (Tactics.UltimateVariant == 1) enemy.Statuses.Add(CombatStatusKind.Rupture, 3, 2);
0596                      else if (Tactics.UltimateVariant == 2)
0597                          enemy.Statuses.Add(CombatStatusKind.Anchor, 2 + (skillLoadout.HasTrait(14) ? 1 : 0), 1);
0598                      else if (Tactics.UltimateVariant == 3)
0599                      {
0600                          enemy.Statuses.Cleanse(false);
0601                          enemy.Statuses.Add(CombatStatusKind.Weaken, skillLoadout.HasTrait(17) ? 3 : 2, 2);
0602                          if (skillLoadout.HasTrait(17)) enemy.Statuses.Add(CombatStatusKind.Rupture, 2, 2);
0603                      }
0604                      else enemy.Statuses.Add(CombatStatusKind.Anchor, 1, 1);
0605                  }
0606                  else if (Tactics != null && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Luna
0607                      && Tactics.UltimateVariant == 3)
0608                  {
0609                      enemy.Statuses.Add(CombatStatusKind.Anchor, 2, 1);
0610                      if (skillLoadout.HasTrait(17)) enemy.Statuses.Add(CombatStatusKind.Weaken, 3, 2);
0611                  }
0612                  if (UsesFragments && playedCardCount >= 6 && longWeaveRupture > 0)
0613                      enemy.Statuses.Add(CombatStatusKind.Rupture, longWeaveRupture, 2);
0614                  hitCount++;
0615                  totalDamage += damage;
0616              }
0617  
0618              if (Tactics != null)
0619              {
0620                  Tactics.Statuses.Remove(CombatStatusKind.Focus);
0621                  Tactics.Statuses.Remove(CombatStatusKind.Momentum);
0622                  if (playerHit)
0623                  {
0624                      healing = Tactics.BeginSelfHit();
0625                      if(selfShield>0) Tactics.Statuses.Add(CombatStatusKind.Shield,selfShield,1);
0626                      for (int i = 0; i < playedCardCount; i++)
0627                      {
0628                          if (playedCards[i].IsInscription)
0629                              healing += Tactics.ApplySelfInscription(playedCards[i].Inscription);
0630                          healing += ApplyBundledAbilities(playedCards[i], AbilityTarget.Player, Tactics.Statuses);
0631                      }
0632                      healing = Math.Min(healing, PlayerMaxHealth - PlayerHealth);
0633                      PlayerHealth += healing;
0634                  }
0635                  if (playerHit && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Ian
0636                      && Tactics.UltimateVariant == 2)
0637                  {
0638                      int amount = skillLoadout.HasTrait(15) ? 8 : 4;
0639                      int ultimateHealing = Math.Min(amount, PlayerMaxHealth - PlayerHealth);
0640                      PlayerHealth += ultimateHealing;
0641                      healing += ultimateHealing;
0642                      if (skillLoadout.HasTrait(15)) Tactics.Statuses.Add(CombatStatusKind.Shield, 4, 1);
0643                  }
0644                  if(Tactics.HasMoved && movedPlotShield>0) Tactics.Statuses.Add(CombatStatusKind.Shield,movedPlotShield,1);
0645                  Tactics.CompletePlot(playerHit, hitCount);
0646                  if (resetSkillFromUltimate) CombatSkillCooldown = 0;
0647              }
0648  
0649              resolvedPlots++;
0650              Phase = AreAllEnemiesDefeated() ? BattlePhase.Victory : BattlePhase.EnemyTurn;
0651              return new PlotReport(hitCount, totalDamage, playerHit, healing,
0652                  Tactics == null ? 0 : Tactics.Statuses.Get(CombatStatusKind.Shield) - shieldBefore);
```

## `Assets/Scripts/Core/Equations/FragmentEquation.cs` · 원본 6–25행

```text
0006      public enum FragmentKind { Counterpoint, Orbit, Petal, Expand, Contract, Mirror, TranslateRight, TranslateDown, Square, Overtone, HomeAnchor, WestAnchor, NorthAnchor, TwinEcho, StarPetal, Surge, Ellipse, Lissajous, Epitrochoid, Limacon, Shear, PhaseOffset, ComplexCube, CometBurst, KaleidoscopeFold, ShardFracture, NebulaRibbon }
0007  
0008      /// <summary>Each card wraps the entire preceding periodic curve. Fixed prefix tables avoid exponential evaluation.</summary>
0009      public sealed class FragmentEquation
0010      {
0011          public const int Capacity = 8, Segments = 1536, MaximumFrequency = 96;
0012          private readonly double[] x = new double[(Capacity + 1) * Segments];
0013          private readonly double[] y = new double[(Capacity + 1) * Segments];
0014          private readonly double[] originX = new double[Capacity + 1], originY = new double[Capacity + 1];
0015          public double OriginX => originX[Count];
0016          public double OriginY => originY[Count];
0017          private readonly FragmentKind[] steps = new FragmentKind[Capacity];
0018          private readonly int[] frequencies = new int[Capacity + 1];
0019          public int Count { get; private set; }
0020          public int Frequency => frequencies[Count];
0021          public FragmentEquation()
0022          {
0023              frequencies[0] = 1; originX[0] = 5;
0024              for (int i = 0; i < Segments; i++) { double t = i * Math.PI * 2 / Segments; x[i] = 1.6 * Math.Cos(t); y[i] = 1.6 * Math.Sin(t); }
0025          }
```

## `Assets/Scripts/Core/Characters/PrototypeCharacterCatalog.cs` · 원본 1–30행

```text
0001  using System.Collections.Generic;
0002  using Graphaclysm.Core.Cards;
0003  using Graphaclysm.Core.Combat;
0004  
0005  namespace Graphaclysm.Core.Characters
0006  {
0007      public static class PrototypeCharacterCatalog
0008      {
0009          private static readonly CharacterDefinition[] Characters =
0010          {
0011              new CharacterDefinition("character.ian", "이안", "흑유리의 기록자",
0012                  "파편을 겹쳐 잔불을 남기고, 모아 둔 식을 한 번에 펼칩니다.",
0013                  42, 4, 5, Deck("frag.square", "frag.home", "frag.echo", "frag.orbit",
0014                      "frag.contract", "frag.surge", "frag.right", "frag.down", "frag.west", "frag.petal",
0015                      "frag.mirror", "frag.shear"), CombatArchetype.Ian),
0016              new CharacterDefinition("character.luna", "루나", "천문 도구의 조율자",
0017                  "어긋난 회전을, 새로운 궤적으로 잇는다.",
0018                  36, 5, 5, Deck("frag.twin", "frag.home", "frag.lissajous", "frag.contract",
0019                      "frag.ellipse", "frag.right", "frag.north", "frag.down", "frag.expand", "frag.petal",
0020                      "frag.overtone", "frag.square"), CombatArchetype.Luna)
0021          };
0022          public static IReadOnlyList<CharacterDefinition> All => Characters;
0023          private static CardDefinition[] Deck(params string[] ids)
0024          {
0025              var cards = new CardDefinition[ids.Length];
0026              for (int i = 0; i < ids.Length; i++) cards[i] = FragmentCardCatalog.Find(ids[i]);
0027              return cards;
0028          }
0029      }
0030  }
```

## `Assets/Scripts/Application/RunGameSession.cs` · 원본 563–570행

```text
0563  
0564          private void AwardExploration(RunNodeKind kind)
0565          {
0566              int experience = kind == RunNodeKind.Boss ? 4 : kind == RunNodeKind.Elite ? 3
0567                  : kind == RunNodeKind.Battle ? 2 : 1;
0568              Growth.AddExperience(experience);
0569          }
0570      }
```

## `Assets/Scripts/Application/RunJournal.cs` · 원본 8–29행

```text
0008      // Stable numeric IDs are part of the run save format. Bump RulesVersion when content or rules change.
0009      public enum RunCommandKind
0010      {
0011          SelectNode, PlayCard, UndoCard, Move, UndoMove, ToggleUltimate, Condense,
0012          Unravel, BeginPlot, ResolvePlot, ResolveEnemy, SelectCardReward, SelectRelicReward,
0013          SkipReward, ChooseRoom, LeaveRoom, RemoveCard, SkipRefinement,
0014          PurchaseGrowth, SelectGrowth, UseCombatSkill, MoveTo
0015      }
0016  
0017      public readonly struct RunCommand
0018      {
0019          public RunCommand(RunCommandKind kind, int argument = 0) { Kind = kind; Argument = argument; }
0020          public RunCommandKind Kind { get; }
0021          public int Argument { get; }
0022      }
0023  
0024      public sealed class RunSaveData
0025      {
0026          public const int FormatVersion = 2;
0027          public const int RulesVersion = 15;
0028          public const int MaximumCommands = 65536;
0029          public uint Seed;
```

## `GAME_DESIGN_DOCUMENT.md` · 원본 47–84행

```text
0047  ## 핵심 경험
0048  
0049  카드의 빈칸에 앞선 궤적 전체를 넣고, 원하는 위치에서 방출하여 적에게 피해를 주고 같은 선 위의 자신에게 강화를 준다. 카드에는 이름·고유 문양·미완성 수식과 1~2개의 부가 능력을 담는다. 계산기·자유 문자 입력·시작 전용 카드·에너지 비용은 현재 런에 없다.
0050  
0051  ## 현재 전투
0052  
0053  | 항목 | 구현 |
0054  |---|---|
0055  | 카드 | 31종. 회전·반경·비율·위치가 서로 다른 연산과 효과 |
0056  | 시작 | 이안/루나 각각 12장 덱, 시작 손패 5장. 시작 덱에 귀환점 포함 |
0057  | 손패/조립 | 각각 최대 8장. 손패 보존, 조립 카드는 별도 예약 |
0058  | 방출 | 현재 궤적과 능력 발동, 적 행동 후 기본 2장 + 드로우 능력/유물 보충 |
0059  | 응축 | 식/손패 보존. 첫 체력 2, 두 번째 체력 4 소비. 기본 1장 + 파편 보너스 최대 1장, 적 행동 |
0060  | 해체 | 예약 파편을 버리고 적 행동, 기본 보충. 공격과 카드 추가 드로우 없음 |
0061  | 위력 | 파편마다 추가 피해 0~3, 합계 최대 10. 선이 닿은 적에게 대상당 한 번 |
0062  | 이동/취소 | 턴당 한 번 반경 1.8 안의 좌표 클릭 이동. 겹침 지점은 가까운 빈 자리로 보정. 작도 전 이동 취소. 응축 이후 새 파편만 취소 |
0063  | 지형 | 기록 기둥은 이동 차단. 굴절 프리즘은 작도선 교차 시 적중 피해 +2, 중첩 없음 |
0064  | 드로우 보호 | 예약 카드 재셔플 금지, 보너스는 한 번만, 넘치는 드로우/응축 보너스는 저장하지 않음 |
0065  
0066  응축의 체력 비용은 보호막으로 막지 못하며 비용보다 체력이 많아야 한다. 일반 턴 보충 유물은 응축에 적용하지 않는다. 빠른 방출과 오래 준비하는 선택을 서로 다른 유물로 지원한다. 현재 수치는 검증 가능한 플레이 시안이며 재미와 전체 런 밸런스는 실제 플레이로 조정한다.
0067  
0068  캐릭터 HP는 이안 42·루나 36. 자신이 선에 닿으면 기본 보호막 3과 카드의 자신 능력을 받는다. 공명 최대 6, 이안 궁극기는 피해 +6·적 이동 봉쇄, 루나는 자가 적중 범위 확대·정화·보호막 8·회복 5다.
0069  
0070  ## 서로 다른 무늬와 기준점
0071  
0072  공통 F₀(t)=1.6exp(it)에 파편을 중첩하며 전장 좌표는 O+F(t)다. 첫 파편의 기준점 O는 그 순간 플레이어 위치다. 귀환점은 사용 당시 플레이어 위치로 다시 옮기고, 서쪽의 문은 왼쪽 2, 승천은 위로 1.5 이동시킨다. 카드 이후 플레이어 이동으로 이미 정한 기준점이 따라 움직이지 않는다. 기준점 이동은 취소·응축·방출 이력과 함께 관리한다.
0073  
0074  여백/낙화는 F 내부에 상수를 더하는 변형이므로 뒤의 회전에 영향을 받는다. 기준점 이동 카드는 O를 바꾸므로 뒤의 회전에도 중심을 유지한다. 속삭임의 1/4 역회전, 오엽성의 5갈래 반경 변조, 격류의 1.7배 확대, 긴 황혼의 가로 1.4/세로 0.65 변형으로 기존 계수와 다른 곡선을 만든다.
0075  
0076  정확한 실제 좌표로 표시·판정하고 화면에 맞춰 정규화하지 않는다. 1,536개 선분, 최대 주파수 96, 최대 8개 파편으로 비용을 제한한다. 작도는 현재 기준점에서 바깥으로 공개한다.
0077  
0078  ## 3층 원정
0079  
0080  유리의 회랑 → 밤의 서고 → 무명의 천문대. 층마다 보스를 포함한 8개 방을 방문하고 한 경로는 총 24개 방이다. 45~66개 노드를 시드로 생성하며 화면에는 현재 층의 지도만 보여 준다. 매 층 3번째 방 비전투, 5번째 유물, 8번째 보스다. 기본 전투 방도 매 층 최소 3곳을 보장한다.
0081  
0082  1·2층 보스를 이기면 체력 8 회복과 유물 보상 뒤 다음 층으로 이어진다. 덱·체력·공명·유물은 유지하고 조립식은 전투마다 초기화한다. 최종 보스에서만 완주한다. 후반 층은 적 체력·공격력이 증가한다. 이벤트 6종, 휴식·유물·덱 제거, 여덟 적 배치는 유지한다.
0083  
0084  유물은 20종이다. 시작 보호막/공명, 첫 방출 피해, 조건부 조립 피해와 함께 응축 보호막·가시·프리즘 피해·추진·파열·요새화를 제공한다. 에너지 유물은 현재 풀에 없다.
```
