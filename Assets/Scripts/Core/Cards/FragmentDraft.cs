using System;
using System.Collections.Generic;
using Graphaclysm.Core.Decks;

namespace Graphaclysm.Core.Cards
{
    // A draft's colour changes the distribution, not the card definitions or a guaranteed minimum.
    public static class FragmentDraft
    {
        private static readonly int[,] Weights = { {50,28,18,4}, {25,35,30,10}, {10,20,45,25}, {0,10,40,50} };
        private static readonly string[] Names = { "흩어진 파편", "빛나는 파편", "공명하는 파편", "완성에 가까운 파편" };
        private static readonly string[] Odds = { "회색 50% · 청록 28%\n보라 18% · 금색 4%", "회색 25% · 청록 35%\n보라 30% · 금색 10%", "회색 10% · 청록 20%\n보라 45% · 금색 25%", "회색 0% · 청록 10%\n보라 40% · 금색 50%" };
        private static readonly string[] Descriptions = BuildDescriptions();
        public static string Name(int grade) => Names[grade];
        public static string Chances(int grade) => Odds[grade];
        public static string Description(int grade) => Descriptions[grade];
        public static int Price(int grade) => 8 + grade * 6;
        public static int Weight(int grade, int colour) => Weights[grade,colour];
        private static string[] BuildDescriptions()
        {
            var values=new string[4];
            for(int i=0;i<4;i++) values[i]="지금 카드 3장 중 1장 선택\n\n"+Odds[i]+"\n각 후보의 추첨 확률";
            return values;
        }
        public static int RollGrade(IRandomSource random, int danger, bool improved)
        {
            int roll=random.Next(100);
            int grade=danger>=2 ? (roll<15?1:roll<70?2:3)
                : danger==1 ? (roll<15?0:roll<50?1:roll<90?2:3)
                : (roll<55?0:roll<85?1:roll<98?2:3);
            return improved?Math.Min(3,grade+1):grade;
        }
        public static CardDefinition Draw(IRandomSource random,IReadOnlyList<CardDefinition> pool,int grade,CardDefinition[] excluded,int n)
        {
            if(grade<0 || grade>3)throw new ArgumentOutOfRangeException(nameof(grade));
            int total=0;
            for(int t=0;t<4;t++)if(Count(pool,t,excluded,n)>0)total+=Weights[grade,t];
            if(total==0)throw new InvalidOperationException("No eligible draft cards.");
            int roll=random.Next(total);
            for(int t=0;t<4;t++)
            {
                int count=Count(pool,t,excluded,n);if(count==0)continue;
                roll-=Weights[grade,t];if(roll>=0)continue;
                int pick=random.Next(count);
                foreach(var card in pool)if((int)card.Rarity==t && Eligible(card,excluded,n) && pick--==0)return card;
            }
            throw new InvalidOperationException("Draft failed.");
        }
        private static int Count(IReadOnlyList<CardDefinition> pool,int t,CardDefinition[] excluded,int n)
        { int count=0;foreach(var card in pool)if((int)card.Rarity==t && Eligible(card,excluded,n))count++;return count; }
        private static bool Eligible(CardDefinition card,CardDefinition[] excluded,int n)
        {for(int i=0;i<n;i++)if(excluded[i]?.Id==card.Id)return false;return true;}
    }
}
