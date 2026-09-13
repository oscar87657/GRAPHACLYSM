using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private BattleSession damageObservedBattle;
        private int damageObservedSerial;
        private float playerImpactAt = -100;
        private string playerImpactText = "";
        private bool playerImpactHealth;
        private void ObservePlayerImpact()
        {
            if (battle == null) return;
            if (damageObservedBattle != battle)
            { damageObservedBattle = battle; damageObservedSerial = battle.IncomingEventSerial; playerImpactAt = -100; return; }
            if (damageObservedSerial == battle.IncomingEventSerial) return;
            damageObservedSerial = battle.IncomingEventSerial;
            int health = battle.LastPlayerHealthDamage, shield = battle.LastPlayerShieldDamage;
            if (health == 0 && shield == 0) return;
            playerImpactAt = ViewTime;
            playerImpactHealth = health > 0;
            playerImpactText = health > 0 ? "체력 −" + health : "방어 성공";
            if (shield > 0) playerImpactText += "\n보호막 −" + shield;
        }

        private void DrawPlayerImpact()
        {
            float elapsed = ViewTime - playerImpactAt;
            if (elapsed < 0 || elapsed > 1.6f || battle == null) return;
            Vector2 center = FieldPoint(battle.Tactics.X,battle.Tactics.Y);
            Color color = playerImpactHealth ? new Color(1,.35f,.43f) : new Color(.4f,.95f,.9f);
            float fade = 1-Mathf.Clamp01(elapsed/1.6f);
            color.a = fade;
            if (!preferences.ReduceMotion)
            {
                float wave = Mathf.Clamp01(elapsed/.6f);
                Ring(center,45+wave*60,new Color(color.r,color.g,color.b,(1-wave)*.9f),3);
                for (int i=0;i<10;i++)
                {
                    float angle=i*Mathf.PI*.2f;
                    Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    Line(center+direction*(35+wave*40),center+direction*(45+wave*70),new Color(color.r,color.g,color.b,(1-wave)*.8f),2);
                }
            }
            float lift = preferences.ReduceMotion ? 0 : elapsed*20;
            Rect text = new Rect(center.x-100,center.y-110-lift,200,68);
            Fill(text,new Color(.08f,.035f,.06f,fade*.94f));
            Color saved=GUI.color; GUI.color=new Color(1,1,1,fade);
            Label(text,playerImpactText,ui.Light,true); GUI.color=saved;
            float edgeAlpha=preferences.ReduceMotion ? 0 : Mathf.Max(0,.5f-elapsed)*.6f;
            if (playerImpactHealth)
            {
                Color edge=new Color(color.r,color.g,color.b,edgeAlpha);
                Fill(new Rect(0,72,12,1008),edge); Fill(new Rect(1908,72,12,1008),edge);
                Fill(new Rect(0,1068,1920,12),edge);
            }
        }
    }
}
