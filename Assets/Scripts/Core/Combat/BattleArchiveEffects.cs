using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Core.Combat
{
    public sealed partial class BattleSession
    {
        private RunRelicCollection archiveRelics;
        private int relicTriggerMask;
        public int LastRelicActivations { get; private set; }
        private void ResolveArchiveEnemySupport(EnemyState enemy)
        {
            if(enemy.Intent.Kind!=EnemyIntentKind.Charge) return;
            var kind=enemy.Definition.Behavior.Kind;
            if(kind==EnemyBehaviorKind.GridSentry || kind==EnemyBehaviorKind.LaneArtillerist)
            { enemy.Statuses.Add(CombatStatusKind.Shield,4,2);return; }
            if(kind==EnemyBehaviorKind.EclipseEmperor)
            { enemy.Heal(12);enemy.Statuses.Add(CombatStatusKind.Ward,1,2);return; }
            if(kind==EnemyBehaviorKind.RepairPriest || kind==EnemyBehaviorKind.ArchiveKeeper)
                foreach(var ally in enemies) ally.Heal(kind==EnemyBehaviorKind.RepairPriest?5:8);
            else if(kind==EnemyBehaviorKind.HexCaller)
            { enemy.Statuses.Add(CombatStatusKind.Shield,5,2);enemy.Statuses.Add(CombatStatusKind.Ward,1,2); }
            else if(enemy.IsBoss)
            {
                enemy.Statuses.Add(CombatStatusKind.Shield,12,2);
                if(kind==EnemyBehaviorKind.AstralSovereign) enemy.Statuses.Add(CombatStatusKind.Ward,1,2);
            }
        }
        private void SnapshotRelicTriggers()
        {
            relicTriggerMask=0;LastRelicActivations=0;
            if(archiveRelics==null || Tactics==null) return;
            int hits=0;bool burn=false,crack=false,guide=false,anchor=false,boss=false;
            foreach(var e in enemies) if(e.IsAlive && PreviewDamage(e)>0)
            {
                hits++;burn|=e.Statuses.Get(CombatStatusKind.Burn)>0;crack|=e.Statuses.Get(CombatStatusKind.Rupture)>0;
                guide|=e.Statuses.Get(CombatStatusKind.Guidance)>0;anchor|=e.Statuses.Get(CombatStatusKind.Anchor)>0;boss|=e.IsBoss;
            }
            for(int i=1;i<=19;i++)
            {
                bool active=false;
                switch((RelicTrigger)i)
                {
                    case RelicTrigger.First: active=resolvedPlots==0;break;
                    case RelicTrigger.Short: active=playedCardCount<=3;break;
                    case RelicTrigger.Long: active=playedCardCount>=6;break;
                    case RelicTrigger.Moved: active=Tactics.HasMoved;break;
                    case RelicTrigger.Still: active=!Tactics.HasMoved;break;
                    case RelicTrigger.Self: active=PreviewPlayerHit;break;
                    case RelicTrigger.Shared: active=PreviewPlayerHit && hits>0;break;
                    case RelicTrigger.Multiple: active=hits>=2;break;
                    case RelicTrigger.Solo: active=hits==1;break;
                    case RelicTrigger.Condensed: active=plotWasCondensed;break;
                    case RelicTrigger.Wounded: active=PlayerHealth*2<=PlayerMaxHealth;break;
                    case RelicTrigger.Shielded: active=Tactics.Statuses.Get(CombatStatusKind.Shield)>0;break;
                    case RelicTrigger.Burning: active=burn;break;
                    case RelicTrigger.Cracked: active=crack;break;
                    case RelicTrigger.Guided: active=guide;break;
                    case RelicTrigger.Rooted: active=anchor;break;
                    case RelicTrigger.Boss: active=boss;break;
                    case RelicTrigger.Prism: active=PrismCharged;break;
                    case RelicTrigger.Alternating: active=Turn%2==1;break;
                }
                if(active) relicTriggerMask|=1<<i;
            }
        }
        private void ResolveArchiveRelics()
        {
            if(archiveRelics==null || Tactics==null) return;
            for(int i=0;i<archiveRelics.Count;i++)
            {
                var relic=archiveRelics.GetRelic(i);
                if(relic.Trigger==RelicTrigger.None || (relicTriggerMask&(1<<(int)relic.Trigger))==0)continue;
                bool enemy=(int)relic.Reward>=8;
                var kind=RelicArchive.RewardStatus(relic.Reward);
                bool applied=false;
                if(!enemy) { Tactics.Statuses.Add(kind,relic.Magnitude,2); applied=true; }
                else for(int e=0;e<enemies.Length;e++)
                    if(enemies[e].IsAlive && threatPlotDamage[e]>0) {enemies[e].Statuses.Add(kind,relic.Magnitude,2);applied=true;}
                if(applied) LastRelicActivations++;
            }
        }
    }
}
