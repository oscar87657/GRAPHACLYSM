using Graphaclysm.Application;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private bool confirmLeaveLoot;
        private int lootUiRevision=-1;
        private RunGameSession lootUiOwner;
        private LootOffer lootRowOwner;
        private string lootLeaveLabel="";
        private float lootScroll, lootScrollAtPress;
        private Vector2 lootPressPoint;
        private bool lootPressed, lootDragging;
        private const float LootCardWidth=300, LootCardStep=326;
        private static readonly Rect LootViewport=new Rect(120,330,1680,540);
        private float LootScrollLimit => Mathf.Max(0,run.PendingLootCount*LootCardStep-26-LootViewport.width);
        private string lootNotice="";
        private int lootNoticeIndex=-1;

        private void HandleLootPan(Event input)
        {
            if(!GUI.enabled) { lootPressed=lootDragging=false; return; }
            bool inside=LootViewport.Contains(input.mousePosition);
            if(input.type==EventType.ScrollWheel && inside)
            {
                lootScroll=Mathf.Clamp(lootScroll+(input.delta.x+input.delta.y)*65,0,LootScrollLimit);
                input.Use();
            }
            else if(input.type==EventType.MouseDown && input.button==0 && inside)
            { lootPressed=true; lootDragging=false; lootPressPoint=input.mousePosition; lootScrollAtPress=lootScroll; }
            else if(input.type==EventType.MouseDrag && lootPressed)
            {
                if((input.mousePosition-lootPressPoint).sqrMagnitude>64)lootDragging=true;
                if(lootDragging)
                {
                    lootScroll=Mathf.Clamp(lootScrollAtPress+lootPressPoint.x-input.mousePosition.x,0,LootScrollLimit);
                    GUIUtility.hotControl=0; input.Use();
                }
            }
            else if(input.type==EventType.MouseUp && lootPressed)
            {
                if(lootDragging) { GUIUtility.hotControl=0; input.Use(); }
                lootPressed=lootDragging=false;
            }
        }

        private void DrawExpeditionLoot()
        {
            if(lootUiOwner!=run || lootRowOwner!=run.GetLoot(0))
            {
                lootUiOwner=run; lootRowOwner=run.GetLoot(0); lootScroll=0;
                lootPressed=lootDragging=false; lootUiRevision=-1; lootNotice=""; lootNoticeIndex=-1;
            }
            if(lootUiRevision!=run.Revision)
            {
                lootUiRevision=run.Revision; confirmLeaveLoot=false;
                lootScroll=Mathf.Clamp(lootScroll,0,LootScrollLimit);
                lootLeaveLabel=run.PendingLootCount==0?"계속 진행":"남은 보상 "+run.PendingLootCount+"개 두고 가기";
                if(lootNoticeIndex>=0 && run.GetLoot(lootNoticeIndex)!=null)
                {var notice=run.GetLoot(lootNoticeIndex);lootNotice=notice.Title+" · "+notice.Result;}
            }
            Header("SPOILS OF THE ASCENT", "전리품");
            Label(new Rect(120,140,1400,60),"가져갈 것만 챙기세요",ui.PageTitle);
            Label(new Rect(120,218,1650,36),run.SupplyInventoryText,ui.Heading);
            Label(new Rect(120,272,1650,36),lootNotice==""?"카드를 좌우로 드래그하거나 휠로 넘기세요. 받은 카드는 목록에서 사라집니다.":lootNotice,ui.Small);
            HandleLootPan(Event.current);

            GUI.BeginGroup(LootViewport);
            GUI.BeginGroup(new Rect(-lootScroll,0,Mathf.Max(LootViewport.width,run.PendingLootCount*LootCardStep),LootViewport.height));
            int slot=0;
            for(int i=0;i<run.LootCount;i++)
            {
                var item=run.GetLoot(i); if(item.Resolved)continue;
                Rect card=new Rect(slot++*LootCardStep,8,LootCardWidth,516);
                // The stable underlying reward index is never replaced by its visual position.
                if(card.xMax<lootScroll || card.xMin>lootScroll+LootViewport.width)continue;
                Fill(card,new Color(1,1,1,.88f));
                Color rewardColour=item.DraftGrade>=0?Rarity((Graphaclysm.Core.Cards.CardRarity)item.DraftGrade):Gold;
                Fill(new Rect(card.x,card.y,card.width,5),rewardColour);
                Fill(new Rect(card.x,card.y,2,card.height),rewardColour);Fill(new Rect(card.xMax-2,card.y,2,card.height),rewardColour);
                Fill(new Rect(card.x,card.yMax-2,card.width,2),rewardColour);
                Label(new Rect(card.x+20,card.y+24,260,58),item.Title,ui.Heading,true);
                DrawLootEmblem(new Vector2(card.center.x,card.y+151),item.Kind);
                Label(new Rect(card.x+24,card.y+223,252,157),item.Description,ui.Body);
                string blocker=run.LootBlocker(i);
                bool oldEnabled=GUI.enabled; GUI.enabled=oldEnabled && !lootDragging;
                bool changed=false;
                if(blocker!="")Label(new Rect(card.x+22,card.y+409,256,80),blocker,ui.Small,true);
                else if(item.Kind==LootKind.Chest)
                {
                    if(LootButton(new Rect(card.x+22,card.y+396,256,44),"열쇠 1개 · 확정",true,run.Keys>0))
                    { changed=run.TryOpenLootChest(i,true); lootNotice=run.LastLootText; }
                    if(!changed && LootButton(new Rect(card.x+22,card.y+453,256,44),"직접 열기 · 50%"))
                    { changed=run.TryOpenLootChest(i,false); lootNotice=run.LastLootText; }
                }
                else if(LootButton(new Rect(card.x+22,card.y+445,256,48),item.Kind==LootKind.Cards || item.Kind==LootKind.Relics?"선택해서 받기":"받기",true))
                { changed=run.TryClaimLoot(i); lootNotice=item.Title+" · "+item.Result; }
                GUI.enabled=oldEnabled;
                if(changed) { lootNoticeIndex=i; GUI.EndGroup(); GUI.EndGroup(); Refresh(); return; }
            }
            GUI.EndGroup(); GUI.EndGroup();
            if(run.PendingLootCount==0)
                Label(new Rect(400,480,1120,130),"남은 보상이 없습니다.\n준비가 끝났다면 다음 방으로 이동하세요.",ui.Heading,true);
            if(LootScrollLimit>0)
            {
                if(ui.Button(new Rect(120,882,65,43),"‹",enabled:lootScroll>0))lootScroll=Mathf.Max(0,lootScroll-LootCardStep);
                lootScroll=GUI.HorizontalScrollbar(new Rect(209,898,1502,17),lootScroll,LootViewport.width,0,LootViewport.width+LootScrollLimit);
                if(ui.Button(new Rect(1735,882,65,43),"›",enabled:lootScroll<LootScrollLimit))lootScroll=Mathf.Min(LootScrollLimit,lootScroll+LootCardStep);
            }
            if(confirmLeaveLoot)
            {
                Label(new Rect(120,960,1030,40),"받지 않은 보상은 사라집니다. 그대로 떠날까요?",ui.Body);
                if(ui.Button(new Rect(1170,955,180,50),"취소"))confirmLeaveLoot=false;
                if(ui.Button(new Rect(1370,955,430,50),"두고 떠나기",true)) { run.TryLeaveLoot(); Refresh(); }
            }
            else if(ui.Button(new Rect(1290,955,510,54),lootLeaveLabel,true))
            { if(run.PendingLootCount>0)confirmLeaveLoot=true; else {run.TryLeaveLoot();Refresh();} }
        }

        private bool LootButton(Rect rect,string text,bool primary=false,bool enabled=true)
        {
            // Matrix-rotated Border lines escape nested GUI groups at scaled resolutions.
            // Use clipped axis-aligned fills inside the horizontal reward viewport.
            bool hover=GUI.enabled && enabled && rect.Contains(Event.current.mousePosition);
            Fill(rect,primary?(hover?Violet:Ink):new Color(1,1,1,hover?.78f:.28f));
            Color edge=primary?Gold:Muted;
            Fill(new Rect(rect.x,rect.y,rect.width,1),edge);Fill(new Rect(rect.x,rect.yMax-1,rect.width,1),edge);
            Fill(new Rect(rect.x,rect.y,1,rect.height),edge);Fill(new Rect(rect.xMax-1,rect.y,1,rect.height),edge);
            Label(rect,text,primary?ui.Light:ui.Body,true);
            bool previous=GUI.enabled;GUI.enabled=previous && enabled;
            bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);GUI.enabled=previous;
            if(clicked)PlayClick();return clicked;
        }
        private void DrawLootEmblem(Vector2 p,LootKind kind)
        {
            // Code-native marks: no imported reference-game art or per-frame assets.
            Color ink=Violet;
            if(kind==LootKind.Coins)
            {
                for(int n=0;n<3;n++)
                { Rect r=new Rect(p.x-37+n*12,p.y-33+n*17,53,36);Fill(r,Gold);Fill(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),Paper); }
            }
            else if(kind==LootKind.Key)
            {
                Fill(new Rect(p.x-28,p.y-39,45,42),ink);Fill(new Rect(p.x-22,p.y-33,33,30),Paper);
                Fill(new Rect(p.x-9,p.y,8,45),ink);Fill(new Rect(p.x-1,p.y+23,20,7),ink);
            }
            else if(kind==LootKind.Chest)
            {
                Fill(new Rect(p.x-48,p.y-28,96,69),ink);Fill(new Rect(p.x-42,p.y-22,84,21),Paper);
                Fill(new Rect(p.x-7,p.y-5,14,29),Gold);
            }
            else
            {
                for(int n=0;n<2;n++)
                { Rect r=new Rect(p.x-37+n*20,p.y-43+n*15,55,73);Fill(r,ink);Fill(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),Paper); }
                Label(new Rect(p.x-14,p.y-17,50,45),kind==LootKind.Relics?"✧":kind==LootKind.Cards?"ƒ":"≡",ui.Heading,true);
            }
        }
    }
}
