using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public sealed partial class RunGameSession
    {
        public bool HasOpeningRoute { get; private set; }

        // Only the new-start flow records this command. Old journals keep their original map.
        public bool TryEnableOpeningRoute()
        {
            if (IsPractice || HasOpeningRoute || StartingApproach == CombatApproach.None
                || journal == null || journal.Count != 1 || journal[0].Kind != RunCommandKind.ChooseApproach
                || Phase != RunPhase.MapSelection || Map.CompletedNodeCount != 0 || Map.ActiveNodeIndex >= 0) return false;
            CharacterDefinition character = null;
            foreach (var candidate in PrototypeCharacterCatalog.All)
                if (candidate.Id == journalCharacter) character = candidate;
            if (character == null) return false;
            Map = new RunMapProgress(OpeningRoute.Create(Map.Definition, character));
            HasOpeningRoute = true;
            return Record(true, RunCommandKind.OpeningRoute);
        }

        public string OpeningStepHint
        {
            get
            {
                if (!HasOpeningRoute) return "";
                int layer = Map.ActiveNodeIndex >= 0 ? Map.Definition.GetNode(Map.ActiveNodeIndex).Layer : Map.CompletedNodeCount;
                if(HasExpeditionSupplies && layer==0)return "도입 · 가까운 적부터 작도에 걸어 보세요. 전리품에서 필요한 보상을 받으세요.";
                if(HasExpeditionSupplies && layer==7)return "첫 관문 · 익힌 기술과 작도를 함께 사용하세요. 보스 너머로 다음 층이 이어집니다.";
                if (HasEconomy && layer == 4) return "보상 · 은화로 상점에서 준비하거나 유물·사건을 선택하세요.";
                return OpeningRoute.Hint(layer);
            }
        }
    }

    public static class OpeningRoute
    {
        private static readonly RoomStory Preparation = new RoomStory("첫 정비 · 다음 작도 준비",
            "첫 전투에서 얻은 성장점을 G 성좌에서 사용하거나, 핵심 능력을 시험해 보세요.\n여기서는 다음 전투에 가져갈 도움을 하나 고릅니다.",
            new RoomChoice("몸을 돌본다", "최대 체력의 25% 회복", RoomEffect.Heal, 25),
            new RoomChoice("보호 부적을 챙긴다", "다음 전투 시작 보호막 +8", RoomEffect.Supply, 8),
            new RoomChoice("박자를 가다듬는다", "공명 +3", RoomEffect.Resonance, 3));
        private static readonly RoomStory OpeningWorkshop = new RoomStory("첫 공방 · 공격 준비",
            "도구를 벼리거나 다음 작도에 쓸 카드를 고릅니다.\n성좌 변경과 핵심 능력 체험은 방을 나온 뒤 G에서 할 수 있습니다.",
            new RoomChoice("도구를 벼린다", "원정 그래프 피해 +1 (최대 +3) · 무료", RoomEffect.Training, 1),
            new RoomChoice("새 파편을 고른다", "카드 하나 선택", RoomEffect.CardReward),
            new RoomChoice("기록을 정리한다", "덱에서 카드 1장 제거", RoomEffect.RemoveCard));

        public static string Hint(int layer)
        {
            switch (layer)
            {
                case 0: return "도입 · 가까운 적부터 작도에 걸어 보세요. 승리 성장점 +2.";
                case 1: return "정비 · G에서 성장점을 투자하고 다음 전투를 준비하세요.";
                case 2: return "활용 · 움직이는 적이나 포수에게 기술로 적중 위치를 고쳐 보세요.";
                case 3: return "돌파 · 약한 무리를 함께 공격하거나 정예에 도전하세요.";
                case 4: return "보상 · 유물, 훈련, 보급 중 다음 전투에 필요한 것을 고르세요.";
                case 5: return "응용 · 적의 장치와 공격을 함께 살피며 기술을 조합하세요.";
                case 6: return "보스 준비 · 회복이나 훈련 후 G에서 빌드를 점검하세요.";
                case 7: return "첫 관문 · 익힌 기술과 작도를 함께 사용하세요. 승리 성장점 +3.";
                default: return "";
            }
        }

        public static string MapLabel(RunMapNodeDefinition node)
        {
            switch (node.Layer)
            {
                case 0: return node.Battle.GetEnemy(1).Y > .5 ? "위쪽 잔영" : "가까운 잔영";
                case 1: return node.Kind == RunNodeKind.Workshop ? "공격 준비" : "회복 · 보호";
                case 2: return node.Battle.GetEnemy(1).Behavior.Kind == EnemyBehaviorKind.LineGunner ? "포수 대응" : "기동 대응";
                case 3: return node.Kind == RunNodeKind.Elite ? "정예 도전" : "무리 돌파";
                case 6: return node.Kind == RunNodeKind.Workshop ? "보스 전 공방" : "보스 전 휴식";
                default: return "";
            }
        }

        public static RunMapDefinition Create(RunMapDefinition original, CharacterDefinition character)
        {
            var nodes = new RunMapNodeDefinition[original.NodeCount];
            int option = 0, previousLayer = -1;
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = original.GetNode(i);
                if (node.Layer != previousLayer) { previousLayer = node.Layer; option = 0; }
                int branch = option++;
                if (node.Layer >= 8) { nodes[i] = node; continue; }
                RunNodeKind kind = node.Kind;
                string name = node.DisplayName;
                RoomStory story = node.Story;
                BattleDefinition battle = node.Battle;
                int depth = node.Layer;
                if (depth == 0 || depth == 2 || depth == 3)
                {
                    kind = depth == 3 && branch == 1 ? RunNodeKind.Elite : RunNodeKind.Battle;
                    name = depth == 0 ? (branch == 1 ? "첫 작도 · 위쪽 잔영" : "첫 작도 · 가까운 잔영")
                        : depth == 2 ? (branch == 1 ? "활용 · 포수의 사선" : "활용 · 엇갈린 잔영")
                        : kind == RunNodeKind.Elite ? "도전 · 굳센 선봉" : "돌파 · 모여든 파편";
                    story = null;
                    battle = Encounter(depth, branch, character, original.PlayerMaxHealth);
                }
                else if (depth == 1)
                {
                    kind = branch == 1 ? RunNodeKind.Workshop : RunNodeKind.Rest;
                    story = branch == 1 ? OpeningWorkshop : Preparation;
                    name = story.Title; battle = null;
                }
                else if (depth == 6)
                {
                    kind = branch == 1 ? RunNodeKind.Workshop : RunNodeKind.Rest;
                    story = branch == 1 ? RoomStoryCatalog.Workshop : RoomStoryCatalog.Rest;
                    name = "보스 준비 · " + story.Title; battle = null;
                }
                var edges = new int[node.NextNodeCount];
                for (int j = 0; j < edges.Length; j++) edges[j] = node.GetNextNodeIndex(j);
                // Every incoming path offers both rest/workshop or ordinary/elite, not two copies of one choice.
                if (depth == 0 || depth == 1 || depth == 2 || depth == 5)
                {
                    int first = i + 1;
                    while (original.GetNode(first).Layer == depth) first++;
                    int count = 0;
                    while (first + count < original.NodeCount && original.GetNode(first + count).Layer == depth + 1) count++;
                    edges = new[] { first + (count == 3 && branch % 2 == 1 ? 2 : 0), first + 1 };
                }
                nodes[i] = new RunMapNodeDefinition(node.Id, name, depth, node.Lane, kind, battle, edges, story, original.PlayerMaxHealth);
            }
            return new RunMapDefinition(nodes, original.RoomsPerFloor);
        }

        private static BattleDefinition Encounter(int depth, int branch, CharacterDefinition c, int health)
        {
            EnemyDefinition[] enemies;
            if (depth == 0)
                enemies = new[] {
                    new EnemyDefinition("opening.front", "가까운 잔영", 6.7, -2, 20, 3),
                    new EnemyDefinition("opening.flank", "흩어진 잔영", 4, branch == 1 ? 1.0 : .2, 20, 3)
                };
            else if (depth == 2)
                enemies = new[] {
                    new EnemyDefinition("opening.front", "엇갈린 잔영", 6.8, -1.3, 27, 4,
                        EnemyBehaviorDefinition.AlternatingPosition(6.8, 1.3)),
                    new EnemyDefinition("opening.flank", branch == 1 ? "선형 포수" : "기다리는 잔영", 4, 1.0, 29, 4,
                        branch == 1 ? EnemyBehaviorDefinition.LineGunner() : EnemyBehaviorDefinition.SteadyAttack())
                };
            else
            {
                bool elite = branch == 1;
                enemies = new[] {
                    new EnemyDefinition("opening.wave.a", "파편 선봉", 6.5, -2, elite ? 36 : 12, elite ? 5 : 3),
                    new EnemyDefinition("opening.wave.b", "파편 잔영", 6, -.6, elite ? 30 : 12, elite ? 5 : 3),
                    new EnemyDefinition("opening.wave.c", "파편 잔영", 4.5, .3, elite ? 30 : 12, elite ? 5 : 3)
                };
            }
            return new BattleDefinition(health, c.MaxEnergy, enemies, c.Archetype, fragments: true);
        }
    }
}
