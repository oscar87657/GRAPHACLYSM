using System;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    // Deliberately independent of the campaign's RNG, journal and legacy benefits.
    public static class SignatureBattleFactory
    {
        public static CardDefinition[] CreateStartingDeck() => new[] {
            FragmentCardCatalog.Find("frag.ellipse"), FragmentCardCatalog.Find("frag.expand"),
            FragmentCardCatalog.Find("frag.right"), FragmentCardCatalog.Find("frag.north"),
            FragmentCardCatalog.Find("frag.contract"), FragmentCardCatalog.Find("frag.echo") };

        public static BattleDefinition CreateEncounter(CombatApproach approach, int maxHealth, bool introduction = false)
        {
            if (approach < CombatApproach.Execution || approach > CombatApproach.Observation) throw new ArgumentOutOfRangeException(nameof(approach));
            return new BattleDefinition(maxHealth, 3, new[] {
                new EnemyDefinition("signature.front", "돌격병", 6.9, -2, introduction ? 26 : 30, introduction ? 4 : 6),
                new EnemyDefinition("signature.flank", "측면병", 4, .9, introduction ? 24 : 30, introduction ? 4 : 5),
                new EnemyDefinition("signature.cannon", "동력 포수", 8, 1.8, introduction ? 36 : 46, introduction ? 8 : 12, EnemyBehaviorDefinition.LineGunner())
            }, approach >= CombatApproach.Tuning ? CombatArchetype.Luna : CombatArchetype.Ian, fragments: true, approach: approach);
        }
        public static RunGameSession Create(CombatApproach approach)
        {
            if (approach < CombatApproach.Execution || approach > CombatApproach.Observation)
                throw new ArgumentOutOfRangeException(nameof(approach));
            CombatArchetype archetype = approach >= CombatApproach.Tuning ? CombatArchetype.Luna : CombatArchetype.Ian;
            var encounter = CreateEncounter(approach, 42);
            // Six recognizable materials, all in the first hand; same shuffle for all approaches.
            var deck = CreateStartingDeck();
            var run = new RunGameSession(RunMapDefinition.CreateLinear(new[] { encounter }), deck,
                FragmentCardCatalog.All, 6, 24601, archetype: archetype) { IsPractice = true };
            run.TrySelectMapNode(0);
            return run;
        }
    }
}
