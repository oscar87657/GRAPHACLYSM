using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;
namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private bool showBattleDetails, showSecondaryActions;
        private string quietWeave = "", quietCondense = "", lastFeedback = "", condenseHint = "";
        private float feedbackUntil;
        private Rect EnemyRow(int index) => battle.UsesFragments ? new Rect(1580,128+index*82,308,74) : new Rect(1490,187+index*143,335,130);
        private void DrawQuietPlayer()
        {
            Fill(new Rect(28,92,314,218),new Color(.055f,.05f,.095f,.88f));
            Border(new Rect(28,92,314,218),new Color(Gold.r,Gold.g,Gold.b,.48f),14);
            Label(new Rect(48,105,180,38),flow.CurrentCharacter.DisplayName,ui.HeadingLight);
            Label(new Rect(225,108,98,34),hpText,ui.SmallLight,true);
            Fill(new Rect(48,151,274,3),new Color(1,1,1,.13f));
            Fill(new Rect(48,151,274f*battle.PlayerHealth/battle.PlayerMaxHealth,3),Violet);
            DrawStatusChips(battle.Tactics.Statuses,new Rect(48,164,274,38),true,4);
            Rect ultimate = new Rect(48,210,274,43);
            if(ui.Button(ultimate,ultimateText,battle.Tactics.UltimateArmed,!battle.InExtraAssembly && !castActive && !combatSkillTargeting && battle.Tactics.Resonance>=6)) {run.TryToggleUltimate();Refresh();}
            for(int i=0;i<6;i++) Fill(new Rect(49+i*46,259,39,3),i<battle.Tactics.Resonance?Violet:new Color(.32f,.30f,.39f));
            bool observation = battle.Approach == CombatApproach.Observation;
            bool twoSkills = battle.Approach == CombatApproach.Tuning || observation;
            Rect skill = new Rect(48,270,twoSkills ? 133 : 274,31);
            string skillLabel = combatSkillTargeting ? "대상 선택 취소" : combatSkillText;
            if (battle.Approach == CombatApproach.Execution && !combatSkillTargeting) skillLabel = battle.CanUseCombatSkill ? "K · 집행 / 파열 +6" : "집행 · 대기 중";
            if (battle.UsesDiagramAbility && !combatSkillTargeting) skillLabel = battle.HasRecording ? "다음 방출에 기록 공격"
                : battle.RecordingArmed ? "기록 예약 완료" : battle.Approach == CombatApproach.Recording ? "K · 기록 위치 선택" : "K · 기준점 / 방향 조정";
            if (twoSkills) skillLabel = combatSkillTargeting && !lunaPullTargeting ? "조준 취소" : "K · 조율";
            if (observation) skillLabel = combatSkillTargeting ? "조준 취소" : "K · 위성";
            if(ui.Button(skill,skillLabel,combatSkillTargeting,!castActive && (battle.CanUseCombatSkill || battle.CanUseDiagramAbility || battle.CanPlaceSatellite || combatSkillTargeting)))
                ToggleCombatSkillTargeting();
            if (observation)
            {
                Rect origin = new Rect(189,270,133,31);
                if (ui.Button(origin,battle.SatelliteOriginSelected ? "J · 원격" : "J · 몸",battle.SatelliteOriginSelected,
                    !castActive && !combatSkillTargeting && battle.CanToggleSatelliteOrigin)) ToggleSatelliteOrigin();
                RegisterHoverHint(skill,"위성 설치","몸에서 3 이내에 위성 하나를 설치하거나 옮깁니다. 턴당 한 번.\n위성이 작도에 닿으면 루나에게 자가 강화가 전달됩니다. 몸과 동시에 닿아도 한 번만 적용합니다.");
                RegisterHoverHint(origin,"다음 식의 출발점","조립 전에 J로 몸 / 위성을 선택합니다. 위성을 옮겨도 이미 만든 식은 움직이지 않습니다.\n위성은 적 공격을 대신 받지 않으며 루나의 몸은 직접 피해야 합니다.");
            }
            else if (twoSkills)
            {
                Rect pull = new Rect(189,270,133,31);
                if (ui.Button(pull,combatSkillTargeting && lunaPullTargeting ? "견인 취소" : "J · 견인",
                    combatSkillTargeting && lunaPullTargeting,!castActive && (battle.CanUseLunaPull || combatSkillTargeting && lunaPullTargeting))) ToggleLunaPull();
                RegisterHoverHint(pull,"성운 견인","몸에서 3 이내 지점 선택 → 반경 2.2의 적을 최대 1.5 끌어당깁니다.\n고정된 적은 이동하지 않습니다. 피해 없음 · 도안 조율과 합쳐 턴당 한 번.");
                RegisterHoverHint(skill,"도안 조율","조립 후 기준점을 옮기고 Q/E로 도안을 회전합니다.\n성운 견인과 합쳐 턴당 한 번 · 우클릭으로 취소.");
            }
            if (battle.Approach == CombatApproach.None) RegisterHoverHint(skill,GrowthSkillName(),combatSkillDescription);
            if(ultimate.Contains(Event.current.mousePosition) || showBattleDetails)
                RegisterHoverHint(ultimate,ultimateText,ultimateDescription);
            DrawQuietLoom();
            if(battle.Tactics.CanUndoMove && !castActive && !combatSkillTargeting)
                if(ui.Button(new Rect(28,744,145,40),"이동 취소")) UndoMove();
            if (showBattleDetails) DrawMovementDock();
            else Label(new Rect(28,802,314,42), "필드 클릭으로 이동 · 방향키 지원", ui.SmallLight, true);
            if(showBattleDetails)
            {
                Fill(new Rect(28,930,314,96),new Color(.055f,.05f,.095f,.86f));
                Label(new Rect(45,936,280,31),drawStatus,ui.SmallLight);
                Label(new Rect(45,969,280,48),selfStateText.Length>0?selfStateText:"적용 중인 상태 없음",ui.SmallLight);
            }
        }
        private void DrawQuietLoom()
        {
            Fill(new Rect(28,326,314,398),new Color(.055f,.05f,.095f,.82f));
            Border(new Rect(28,326,314,398),new Color(Gold.r,Gold.g,Gold.b,.34f),14);
            Label(new Rect(48,338,274,35),quietWeave,ui.Light);
            if(battle.PlayedCardCount==0)
            {
                Diamond(new Vector2(185,514),32,new Color(Violet.r,Violet.g,Violet.b,.62f));
                Label(new Rect(48,566,274,38),"첫 파편을 놓으세요",ui.SmallLight,true);
            }
            for(int i=0;i<battle.PlayedCardCount;i++)
            {
                var card=battle.GetPlayedCard(i);float y=383+i*40;
                float enter=!preferences.ReduceMotion && i==battle.PlayedCardCount-1 ? Mathf.Clamp01(1-(ViewTime-fragmentPlacedAt)*5)*10:0;
                Color color=i<battle.SealedCardCount?Muted:Violet;
                Line(new Vector2(48,y+enter),new Vector2(48,y+27+enter),color,1.4f);
                Label(new Rect(63,y-2+enter,168,31),card.DisplayName,ui.SmallLight);
                if(showBattleDetails) Label(new Rect(230,y+enter,91,30),CompactMark(card.Fragment),ui.SmallLight,true);
                else Diamond(new Vector2(315,y+14+enter),3,Rarity(card.DiagramRarity));
            }
            if(battle.PlayedCardCount>battle.UndoFloor && !castActive && !combatSkillTargeting)
                if(ui.Button(new Rect(190,744,152,40),"파편 취소")) Undo();
        }

        private string GrowthSkillName()
        {
            int variant = run.Growth.ActiveVariant;
            if (flow.CurrentCharacter.Archetype == CombatArchetype.Ian)
                return variant == 1 ? "삼중 유리길   K" : variant == 2 ? "집행의 직선   K"
                    : variant == 3 ? "거울 교환   K" : "유리 쇄도   K";
            return variant == 1 ? "만월 착지   K" : variant == 2 ? "초승달 회귀   K"
                : variant == 3 ? "별무리 전이   K" : "월광 도약   K";
        }

        private string GrowthSkillDescription()
        {
            if(battle.HasSpecialization) return SpecializationSkillHint()+StyleCapstoneText(false);
            int variant = run.Growth.ActiveVariant;
            string form;
            if (flow.CurrentCharacter.Archetype == CombatArchetype.Ian)
            {
                form = variant == 1 ? (run.Growth.IsUnlocked(3) ? "다섯 갈래 관통 · 피해 5" : "세 갈래 관통 · 피해 6")
                    + (run.Growth.IsUnlocked(4) ? " · 적중마다 보호막" : "")
                    : variant == 2 ? "처형선 · 피해 10 · 처치 시 즉시 재사용"
                        + (run.Growth.IsUnlocked(5) ? "·공명 +1" : "") + (run.Growth.IsUnlocked(6) ? " · 파열 3" : "")
                    : variant == 3 ? "대상 뒤 전이 · 주변 피해 7 · 반경 " + (run.Growth.IsUnlocked(7) ? "1.85" : "1.15")
                        + (run.Growth.IsUnlocked(8) ? " · 보호막 6/요새화 3" : "")
                    : "대상을 반드시 가르는 직선 이동 · 피해 7";
                return "대기 3턴 · " + form;
            }
            form = variant == 1 ? "착지 파동 · 피해 5 · 반경 " + (run.Growth.IsUnlocked(3) ? "2.20" : "1.70")
                    + (run.Growth.IsUnlocked(4) ? " · 정화/보호막/요새화" : "")
                : variant == 2 ? "왕복 참격 · 피해 9 · 원위치"
                    + (run.Growth.IsUnlocked(5) ? " · 노출 3" : "") + (run.Growth.IsUnlocked(6) ? " · 회복/추진" : "")
                : variant == 3 ? "연쇄 전이 · 피해 5 · 연결 " + (run.Growth.IsUnlocked(7) ? "3.60" : "2.40")
                    + (run.Growth.IsUnlocked(8) ? " · 처치 시 즉시 재사용/공명" : "")
                : "대상 곁으로 도약 · 착지 주변 피해 6";
            return "대기 3턴 · " + form;
        }

        private string UltimateSkillDescription()
        {
            if(battle.HasApproachUltimates) return (battle.HasSpecialization ? SpecializationUltimateHint()+StyleCapstoneText(true) : ApproachUltimateDescriptions[(int)battle.Approach])+"\n공명 6 · 방출할 때 소비\n충전: 적 2명 이상 +1 · 자신과 적 동시 +2";
            int variant = run.Growth.UltimateVariant;
            bool resonanceRefund = run.Growth.IsUnlocked(12);
            string charge = "\n충전: 적 2명 이상 +1 · 자신과 적 동시 +2" + (resonanceRefund ? " · 궁극 다중 적중 환급 +1" : "");
            if (flow.CurrentCharacter.Archetype == CombatArchetype.Ian)
                return variant == 1 ? "공명 6 · 피해 +" + (run.Growth.IsUnlocked(13) ? 14 : 10) + " · 파열 3" + charge
                    : variant == 2 ? "공명 6 · 피해 +6 · 고정 " + (run.Growth.IsUnlocked(14) ? 3 : 2)
                        + " · 자신 적중 시 회복 " + (run.Growth.IsUnlocked(15) ? 8 : 4) + charge
                    : variant == 3 ? "공명 6 · 피해 +8 · 강화 제거 · 약화 " + (run.Growth.IsUnlocked(17) ? 3 : 2)
                        + (run.Growth.IsUnlocked(16) ? " · 자가 적중 정화/보호막 6" : "") + charge
                    : "공명 6 · 피해 +6 · 적중한 적 이동 봉쇄" + charge;
            return variant == 1 ? "공명 6 · 자가 반경 " + (run.Growth.IsUnlocked(13) ? "1.50" : "1.25")
                    + " · 정화 · 보호막 " + (run.Growth.IsUnlocked(13) ? 16 : 12) + " · 회복 " + (run.Growth.IsUnlocked(13) ? 9 : 7) + charge
                : variant == 2 ? "공명 6 · 피해 +" + (run.Growth.IsUnlocked(14) ? 8 : 4) + " · 정화 · 보호막 "
                    + (run.Growth.IsUnlocked(15) ? 10 : 6) + " · 회복 " + (run.Growth.IsUnlocked(15) ? 7 : 3) + charge
                : variant == 3 ? "공명 6 · 피해 +2 · 고정 2" + (run.Growth.IsUnlocked(16) ? " · 기술 대기 초기화" : "")
                    + (run.Growth.IsUnlocked(17) ? " · 약화 3" : "") + charge
                : "공명 6 · 자가 반경 확대 · 정화 · 보호막 8 · 회복 5" + charge;
        }
        private void DrawQuietEnemyPanel()
        {
            float intentHeight = 42 + battle.Enemies.Count * 82;
            Fill(new Rect(1564,92,340,intentHeight),new Color(.055f,.05f,.095f,.88f));
            Label(new Rect(1580,101,308,32),"다음 행동",ui.Light);
            for(int i=0;i<battle.Enemies.Count;i++)
            {
                var enemy=battle.Enemies[i];Rect r=EnemyRow(i);
                if(hoveredEnemy==i) Fill(r,new Color(1,1,1,.10f));
                Line(new Vector2(r.x,r.yMax),new Vector2(r.xMax,r.yMax),new Color(Gold.r,Gold.g,Gold.b,.25f));
                Label(new Rect(r.x,r.y+2,34,30),enemyBadges[i],ui.SmallLight);
                Label(new Rect(r.x+39,r.y+2,184,30),enemy.Definition.DisplayName,ui.Light);
                Label(new Rect(r.x+224,r.y+3,84,27),enemyHealth[i],ui.SmallLight,true);
                Label(new Rect(r.x+39,r.y+35,150,29),enemy.IsAlive?enemyIntent[i]:"소멸",ui.SmallLight);
                DrawStatusChips(enemy.Statuses,new Rect(r.x+172,r.y+34,136,34),true,2);
                Rect enemyNameArea = new Rect(r.x,r.y+2,220,30);
                if (enemy.IsAlive && enemyNameArea.Contains(Event.current.mousePosition)) RegisterKeyword(enemyNameArea, enemy.Definition.DisplayName,
                    (enemy.Definition.Behavior.Kind>=EnemyBehaviorKind.RotatingGun ? EnemyArchive.Find(enemy.Definition.Behavior.Kind).Pattern+"\n"+EnemyArchive.Find(enemy.Definition.Behavior.Kind).Counter+"\n"
                    : enemy.IsLineGunner ? (battle.HasStatusRules ? "직선 포격: 견인하면 발사 위치가 이동하고 방향은 유지됩니다. 체력 피격 시 새 보호막 획득 −3, 2턴.\n" : "직선 포격: 조준 방향을 전장 끝까지 공격합니다. 뒤로 물러서기보다 옆으로 피하세요.\n")
                    : enemy.IsRingSentinel ? (battle.HasStatusRules ? "고리 공격: 적 주위 두 원 사이를 공격합니다. 견인하면 고리도 함께 이동합니다. 매 턴 보호막 3.\n" : "고리 공격: 두 원 사이를 공격합니다. 작은 원 안이나 큰 원 밖으로 피하세요. 예고 뒤 적을 옮겨도 고리는 그대로입니다.\n")
                    : enemy.IsCrossGunner ? (battle.HasStatusRules ? "십자 포격: 대각선으로 두 띠를 피하세요. 체력 피격 시 새 보호막 획득 −2, 3턴. 견인해도 지면 조준은 유지됩니다.\n" : "십자 포격: 턴 시작 위치를 지나는 가로·세로 띠를 공격합니다. 대각선으로 이동해 두 띠를 모두 벗어나세요.\n")
                    : enemy.IsTwinMortar ? (battle.HasStatusRules ? "쌍점 폭격: 두 원 밖으로 피하세요. 중복 피해 없음. 체력 피격 시 잔불2, 3턴. 견인해도 지면 조준은 유지됩니다.\n" : "쌍점 폭격: 턴 시작 위치와 전장 중심 반대편을 동시에 공격합니다. 두 원이 겹쳐도 피해는 한 번입니다.\n")
                    : enemy.Definition.Behavior.Kind == EnemyBehaviorKind.SealScribe ? "홀수 턴 봉인 예고, 다음 턴 이동·프리즘 봉쇄. 중앙 표식을 그래프로 맞히면 취소/해제합니다. 짝수 턴은 일반 공격.\n"
                    : enemy.Definition.Behavior.Kind == EnemyBehaviorKind.LinkGuardian ? "연결 아군의 그래프 피해 −4. 수호자와 중앙 매듭을 같은 그래프로 맞히면 해제됩니다. 매 턴 다시 연결합니다. 기술·잔불·장치 피해는 줄이지 않습니다.\n"
                    : enemy.Definition.Behavior.Kind == EnemyBehaviorKind.EscalatingAttack ? "격앙: 두 턴마다 공격 +1, 최대 +4. 먼저 처리하거나 약화로 억제하세요.\n"
                    : enemy.Definition.Behavior.Kind == EnemyBehaviorKind.Skirmisher ? "기동: 재배치 뒤 두 번 공격합니다. 고정으로 이동을 막을 수 있습니다.\n" : "공격은 미리 표시된 조준 영역에만 적중합니다.\n")
                    + "작도 피해 " + battle.PreviewDamage(enemy) + " · 결정 추가 " + battle.PreviewDeviceDamage(enemy)
                    + "\n맥동 피해: 방출 후 " + battle.PreviewPulseDamage(enemy.X, enemy.Y, BattleSession.EnemyHitRadius, true)
                    + " / 응축 시 " + battle.PreviewPulseDamage(enemy.X, enemy.Y, BattleSession.EnemyHitRadius, false));
            }
            if(hoveredEnemy>=0 || showBattleDetails)
            {
                int index=hoveredEnemy>=0?hoveredEnemy:0;
                Fill(new Rect(1564,526,340,174),new Color(.055f,.05f,.095f,.82f));
                Label(new Rect(1580,539,308,32),battle.Enemies[index].Definition.DisplayName,ui.Light);
                Label(new Rect(1580,578,308,108),enemyStatuses[index].Length>0?enemyStatuses[index]:"적용 중인 상태 없음",ui.SmallLight);
            }
            Fill(new Rect(1564,718,340,300),new Color(.055f,.05f,.095f,.88f));
            Label(new Rect(1580,734,308,31),castActive?"선을 새기는 중":outcomeText,ui.SmallLight,true);
            if(ui.Button(new Rect(1580,777,308,64),castActive?lastPlotName:"방출   ↵",true,!castActive && !combatSkillTargeting && battle.CanPlot))StartCast();
            if(ui.Button(new Rect(1580,855,308,48),quietCondense,false,!castActive && !combatSkillTargeting && battle.CanCondense))Condense();
            if(battle.InExtraAssembly)
            {
                if(ui.Button(new Rect(1580,921,308,42),"추가 조립 포기 · 적 행동",false,!castActive && !combatSkillTargeting))Unravel();
                return;
            }
            if(ui.Button(new Rect(1798,921,90,42),"해체",showSecondaryActions,!castActive && !combatSkillTargeting))showSecondaryActions=!showSecondaryActions;
            if(showSecondaryActions)
            {
                if(ui.Button(new Rect(1580,921,202,42),"해체하고 넘기기",false,!castActive && !combatSkillTargeting)) {showSecondaryActions=false;Unravel();}
            }
            else RegisterHoverHint(new Rect(1580,855,308,48), "응축", condenseHint + "\n현재 위치 맥동 피해 "
                + battle.PreviewPulseDamage(battle.Tactics.X,battle.Tactics.Y,TacticalCombatState.PlayerRadius,false));
        }
    }
}
