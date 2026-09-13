using System;
using System.Collections.Generic;

namespace Graphaclysm.Core.Cards
{
    // Same 105 diagrams, colors and prices; selected riders introduce spatial/status combinations.
    public static class StatusCardRules
    {
        public static CardDefinition[] Build(IReadOnlyList<CardDefinition> previous)
        {
            var result = new CardDefinition[previous.Count];
            for (int i=0; i<result.Length; i++)
            {
                var card=previous[i]; var abilities=new CardAbility[card.AbilityCount];
                for(int a=0;a<abilities.Length;a++)
                {
                    var old=card.GetAbility(a);
                    abilities[a]=old.Kind==CardAbilityKind.Momentum
                        ? new CardAbility(CardAbilityKind.Focus,old.Target,old.Magnitude,old.Duration) : old;
                }
                var unchanged=abilities;
                // Families carry a consistent role, instead of random debuffs on every card.
                if(card.Id.StartsWith("frag.weave.2.")) abilities=Pair(CardAbilityKind.Wound,3,CardAbilityKind.Pierce,4);
                else if(card.Id.StartsWith("frag.weave.3.")) abilities=Pair(CardAbilityKind.Guidance,1,CardAbilityKind.Haste,1);
                else if(card.Id.StartsWith("frag.weave.6.")) abilities=Pair(CardAbilityKind.Pull,1,CardAbilityKind.Ward,1);
                else if(card.Id.StartsWith("frag.weave.7.")) abilities=Pair(CardAbilityKind.FrailShield,4,CardAbilityKind.Fortify,4);
                else if(card.Id.StartsWith("frag.weave.10.")) abilities=Pair(CardAbilityKind.Wound,4,CardAbilityKind.Regeneration,3);
                else if(card.Id.StartsWith("frag.weave.13.")) abilities=Pair(CardAbilityKind.Pull,1,CardAbilityKind.Focus,3);
                else if(card.Id=="frag.contract") abilities=new[]{new CardAbility(CardAbilityKind.Pull,AbilityTarget.Enemy,1,2)};
                else if(card.Id=="frag.shear") abilities=Pair(CardAbilityKind.Guidance,1,CardAbilityKind.Shield,3);
                else if(card.Id=="frag.guardian") abilities=new[]{Self(CardAbilityKind.Thorns,3),Self(CardAbilityKind.Ward,1)};
                if(!ReferenceEquals(abilities,unchanged))
                    for(int a=0;a<abilities.Length;a++)
                    {
                        var rider=abilities[a];
                        bool binary=rider.Kind==CardAbilityKind.Ward || rider.Kind==CardAbilityKind.Haste
                            || rider.Kind==CardAbilityKind.Pull || rider.Kind==CardAbilityKind.Guidance;
                        int magnitude=binary ? rider.Magnitude : Math.Max(1,(rider.Magnitude*(3+(int)card.Rarity)+3)/4);
                        int duration=card.Rarity==CardRarity.Legendary && !binary && rider.Kind!=CardAbilityKind.Shield ? 3 : rider.Duration;
                        abilities[a]=new CardAbility(rider.Kind,rider.Target,magnitude,duration);
                    }
                result[i]=CardDefinition.Balance(card,card.Rarity,abilities,card.WeavePower);
            }
            return result;
        }
        private static CardAbility Self(CardAbilityKind kind,int amount) => new CardAbility(kind,AbilityTarget.Player,amount,2);
        private static CardAbility[] Pair(CardAbilityKind enemy,int amount,CardAbilityKind self,int value)
            => new[]{new CardAbility(enemy,AbilityTarget.Enemy,amount,2),Self(self,value)};
    }
}
