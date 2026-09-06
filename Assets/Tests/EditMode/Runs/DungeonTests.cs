using System.Collections.Generic;
using System.Text;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Runs
{
    public sealed class DungeonTests
    {
        private static string Fingerprint(RunMapDefinition map)
        {
            var s = new StringBuilder();
            for (int i=0;i<map.NodeCount;i++)
            {
                var n=map.GetNode(i); s.Append(n.Layer).Append(':').Append(n.Lane).Append(':').Append(n.Kind).Append(n.DisplayName);
                for(int e=0;e<n.NextNodeCount;e++) s.Append('/').Append(n.GetNextNodeIndex(e));
                if(n.Battle!=null) for(int e=0;e<n.Battle.EnemyCount;e++) { var enemy=n.Battle.GetEnemy(e); s.Append(enemy.X).Append(',').Append(enemy.Y).Append(',').Append(enemy.MaxHealth); }
            }
            return s.ToString();
        }
        private static RunGameSession RoomRun(RoomStory story, RunNodeKind kind = RunNodeKind.Event)
        {
            var c=PrototypeCharacterCatalog.All[0];
            var battle=new BattleDefinition(42,4,new[]{new EnemyDefinition("enemy","Enemy",8,0,100,0)},CombatArchetype.Ian,true);
            return new RunGameSession(new RunMapDefinition(new[]{
                new RunMapNodeDefinition("room","Room",0,0,kind,null,new[]{1},story,42),
                new RunMapNodeDefinition("boss","Boss",1,0,RunNodeKind.Boss,battle,new int[0])
            }),c.CreateStartingDeckCopy(),CalculatorCardCatalog.All,5,73,PrototypeRelicCatalog.All);
        }

        [Test]
        public void ASeedReproducesRoomsEdgesAndEncountersWhileOtherSeedsVary()
        {
            var c=PrototypeCharacterCatalog.All[0];
            Assert.That(Fingerprint(DungeonGenerator.Generate(55,c)),Is.EqualTo(Fingerprint(DungeonGenerator.Generate(55,c))));
            var unique=new HashSet<string>();
            for(uint seed=1;seed<=40;seed++) unique.Add(Fingerprint(DungeonGenerator.Generate(seed,c)));
            Assert.That(unique.Count,Is.EqualTo(40));
        }
        [Test]
        public void EncounterPoolIncludesDistantEdgesAndTightCentralFormations()
        {
            bool edge=false, center=false;
            for(uint seed=1;seed<=30;seed++)
            {
                var map=DungeonGenerator.Generate(seed,PrototypeCharacterCatalog.All[0]);
                for(int i=0;i<map.NodeCount;i++)
                {
                    var b=map.GetNode(i).Battle; if(b==null)continue;
                    for(int e=0;e<b.EnemyCount;e++)
                    {
                        var enemy=b.GetEnemy(e);double r2=(enemy.X-5)*(enemy.X-5)+enemy.Y*enemy.Y;
                        edge|=r2>16;center|=r2<2.25;
                        Assert.That(enemy.X,Is.InRange(0.48,9.52));Assert.That(enemy.Y,Is.InRange(-3.52,3.52));
                    }
                }
            }
            Assert.That(edge&&center,Is.True);
        }
        [Test]
        public void GeneratedMapsRemainReachableAndEveryPathPassesNonCombatRooms()
        {
            for(uint seed=1;seed<=100;seed++)
            {
                var map=DungeonGenerator.Generate(seed,PrototypeCharacterCatalog.All[0]);
                Assert.That(map.LayerCount,Is.EqualTo(24)); Assert.That(map.FloorCount,Is.EqualTo(3)); Assert.That(map.NodeCount,Is.InRange(45,66));
                var reachable=new bool[map.NodeCount];
                for(int i=0;i<map.StartNodeCount;i++) reachable[map.GetStartNodeIndex(i)]=true;
                for(int i=0;i<map.NodeCount;i++)
                {
                    var n=map.GetNode(i); Assert.That(reachable[i],Is.True);
                    if(n.Layer%8==2) Assert.That(n.Kind==RunNodeKind.Event||n.Kind==RunNodeKind.Rest,Is.True);
                    if(n.Layer%8==4) Assert.That(n.Kind,Is.EqualTo(RunNodeKind.Treasure));
                    if(n.Layer%8==7) Assert.That(n.Kind,Is.EqualTo(RunNodeKind.Boss));
                    if(n.Layer==23) Assert.That(n.NextNodeCount,Is.Zero);
                    else Assert.That(n.NextNodeCount,Is.GreaterThan(0));
                    for(int e=0;e<n.NextNodeCount;e++) { int next=n.GetNextNodeIndex(e); Assert.That(map.GetNode(next).Layer,Is.EqualTo(n.Layer+1)); reachable[next]=true; }
                }
            }
        }
        [Test]
        public void EventCostsAndResonanceApplyOnceAndPersistIntoCombat()
        {
            var run=RoomRun(RoomStoryCatalog.Event(0)); Assert.That(run.TrySelectMapNode(0),Is.True);
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Room)); Assert.That(run.CurrentBattle,Is.Null);
            Assert.That(run.TryChooseRoomOption(0),Is.True); Assert.That(run.PlayerHealth,Is.EqualTo(37)); Assert.That(run.Resonance,Is.EqualTo(4));
            Assert.That(run.TryChooseRoomOption(0),Is.False); Assert.That(run.TrySelectMapNode(0),Is.False);
            run.TrySelectMapNode(1); Assert.That(run.CurrentBattle.Battle.PlayerHealth,Is.EqualTo(37)); Assert.That(run.CurrentBattle.Battle.Tactics.Resonance,Is.EqualTo(4));
        }
        [Test]
        public void TreasureOffersOnlyRemainingRelicsAndCannotLoopWhenPoolIsExhausted()
        {
            var run=RoomRun(RoomStoryCatalog.Treasure,RunNodeKind.Treasure);
            for(int i=0;i<4;i++) run.Relics.TryAdd(PrototypeRelicCatalog.All[i]);
            run.TrySelectMapNode(0); Assert.That(run.TryChooseRoomOption(0),Is.True);
            Assert.That(run.Phase,Is.EqualTo(RunPhase.RelicReward));
            Assert.That(run.RelicRewardOptions[0],Is.Not.Null); Assert.That(run.RelicRewardOptions[1],Is.Not.Null); Assert.That(run.RelicRewardOptions[2],Is.Null);
            Assert.That(run.TrySelectRelicReward(2),Is.False); Assert.That(run.TrySelectRelicReward(0),Is.True); Assert.That(run.Relics.Count,Is.EqualTo(5));
            var empty=RoomRun(RoomStoryCatalog.Treasure,RunNodeKind.Treasure);
            foreach(var relic in PrototypeRelicCatalog.All) empty.Relics.TryAdd(relic);
            empty.TrySelectMapNode(0); Assert.That(empty.CanChooseRoomOption(0),Is.False); Assert.That(empty.TryChooseRoomOption(1),Is.True);
            Assert.That(empty.Phase,Is.EqualTo(RunPhase.MapSelection));
        }
        [Test]
        public void CardRemovalChangesThePersistentDeckAndCancellationDoesNotChargeHealth()
        {
            var run=RoomRun(RoomStoryCatalog.Event(4)); run.TrySelectMapNode(0); int count=run.Deck.Count;
            Assert.That(run.TryChooseRoomOption(1),Is.True); Assert.That(run.PlayerHealth,Is.EqualTo(42));
            Assert.That(run.TryRemoveDeckCard(0),Is.True); Assert.That(run.Deck.Count,Is.EqualTo(count-1)); Assert.That(run.PlayerHealth,Is.EqualTo(40));
            Assert.That(run.TryRemoveDeckCard(0),Is.False);
            var cancel=RoomRun(RoomStoryCatalog.Event(4)); cancel.TrySelectMapNode(0); cancel.TryChooseRoomOption(1);
            Assert.That(cancel.TrySkipRefinement(),Is.True); Assert.That(cancel.PlayerHealth,Is.EqualTo(42)); Assert.That(cancel.Deck.Count,Is.EqualTo(12));
        }
        [Test]
        public void RoomChoicesCannotKillThePlayerOrRepeatRewards()
        {
            var story=new RoomStory("Risk","Risk",new RoomChoice("Risk","42 health",RoomEffect.CardReward,0,42),new RoomChoice("Leave","No change",RoomEffect.Leave));
            var run=RoomRun(story); run.TrySelectMapNode(0);
            Assert.That(run.CanChooseRoomOption(0),Is.False); Assert.That(run.TryChooseRoomOption(0),Is.False);
            Assert.That(run.PlayerHealth,Is.EqualTo(42)); Assert.That(run.TryChooseRoomOption(1),Is.True);
            Assert.That(run.TryChooseRoomOption(1),Is.False);
        }
        [Test]
        public void AnUnaffordableRoomCanAlwaysBeLeftWithoutACharge()
        {
            var story=new RoomStory("Risk","Risk",new RoomChoice("Risk","42 health",RoomEffect.CardReward,0,42));
            var run=RoomRun(story); run.TrySelectMapNode(0);
            Assert.That(run.TryLeaveRoom(),Is.True); Assert.That(run.PlayerHealth,Is.EqualTo(42));
            Assert.That(run.Phase,Is.EqualTo(RunPhase.MapSelection)); Assert.That(run.TryLeaveRoom(),Is.False);
        }
    }
}
