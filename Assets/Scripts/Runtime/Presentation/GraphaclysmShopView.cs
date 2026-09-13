using Graphaclysm.Application;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private ShopOffer shopEffectOwner;
        private string shopEffectText;
        private GUIStyle shopEffectStyle;
        private int inspectedShopItem = -1;
        private RunGameSession inspectedShopOwner;
        private void DrawShop()
        {
            if (run.HasMarketBalance) { DrawExpandedShop(); return; }
            Header("THE TRAVELLING MERCHANT", "원정 상점");
            Label(new Rect(150,145,1450,80),run.CurrentRoom.Title,ui.PageTitle);
            Label(new Rect(150,244,1600,40),"은화 " + run.Coins + " · 연구권 " + run.ResearchTickets
                + " · 상품은 각 1회 구매 · 살펴본 뒤 아래 구매 버튼으로 확정하세요.",ui.Body);
            for (int i=0;i<run.ShopOfferCount;i++)
            {
                var offer=run.GetShopOffer(i);
                Rect purchase;
                if (i<2)
                {
                    var rect=new Rect(170+i*440,318,380,400);
                    if (offer.Card != null)
                    {
                        var visual=Visual(offer.Card);
                        DrawFragmentCard(rect,visual,rect.Contains(Event.current.mousePosition),true);
                        if(shopEffectOwner!=offer)
                        { shopEffectOwner=offer; shopEffectText=string.Join("\n",visual.KeywordDetails); }
                        if(shopEffectStyle==null) { shopEffectStyle=new GUIStyle(ui.Small); shopEffectStyle.normal.textColor=Ink; }
                        Fill(new Rect(rect.x,799,rect.width,132),new Color(Paper.r,Paper.g,Paper.b,.96f));
                        DrawExplainedText(new Rect(rect.x+14,807,rect.width-28,116),shopEffectText,shopEffectStyle);
                    }
                    else
                    {
                        Fill(rect,new Color(1,1,1,.68f)); Border(rect,Gold);
                        DrawRelicArt(new Rect(rect.x+40,rect.y+15,300,225),offer.Relic);
                        Label(new Rect(rect.x+20,rect.y+237,340,56),offer.Title,ui.Heading,true);
                        DrawExplainedText(new Rect(rect.x+24,rect.y+300,332,94),offer.Description,ui.Body,true);
                    }
                    if (run.ShopSold(i))
                    {
                        Fill(rect,new Color(.1f,.09f,.15f,.62f));
                        Label(new Rect(rect.x,rect.center.y-30,rect.width,60),"구매 완료",ui.HeadingLight,true);
                    }
                    purchase=new Rect(rect.x,738,rect.width,48);
                }
                else
                {
                    var rect=new Rect(1090,318+(i-2)*150,650,134);
                    Fill(rect,new Color(1,1,1,.65f)); Border(rect,run.ShopSold(i)?Muted:Gold);
                    Label(new Rect(rect.x+20,rect.y+10,610,38),offer.Title,ui.Heading);
                    DrawExplainedText(new Rect(rect.x+20,rect.y+50,610,32),offer.Description,ui.Small);
                    purchase=new Rect(rect.x+20,rect.y+87,610,36);
                }
                string blocker=run.ShopBlocker(i);
                string action=string.IsNullOrEmpty(blocker)?"은화 "+offer.Price+" · "+(offer.Kind==ShopItemKind.RemoveCard?"카드 선택":"구매")
                    :blocker+(run.ShopSold(i)?"":" · "+offer.Price);
                if (ui.Button(purchase,action,false,string.IsNullOrEmpty(blocker)))
                { run.TryBuyShop(i); Refresh(); return; }
            }
            Label(new Rect(150,935,1260,38),run.RoomResult,ui.Small);
            if(ui.Button(new Rect(1440,969,310,54),"상점 나가기",true)) { run.TryLeaveRoom(); Refresh(); }
        }

        private void DrawExpandedShop()
        {
            if (!ReferenceEquals(inspectedShopOwner,run)) { inspectedShopOwner=run; inspectedShopItem=-1; }
            Header("THE TRAVELLING MERCHANT", "원정 상점");
            Label(new Rect(100,128,1300,65),run.CurrentRoom.Title,ui.PageTitle);
            Label(new Rect(100,205,1700,40),"은화 " + run.Coins + " · 카드 5장 / 유물 3개 · 상품 클릭: 상세 보기 · 구매는 아래 버튼",ui.Body);
            if (inspectedShopItem >= 0 && inspectedShopItem < run.ShopOfferCount)
            {
                DrawShopInspection(inspectedShopItem); return;
            }
            int cardNumber = 0, relicNumber = 0, serviceNumber = 0;
            for (int i = 0; i < run.ShopOfferCount; i++)
            {
                var offer = run.GetShopOffer(i); Rect purchase;
                if (offer.Kind == ShopItemKind.Card)
                {
                    var rect = new Rect(100 + cardNumber++ * 249,280,229,310);
                    DrawFragmentCard(rect,Visual(offer.Card),rect.Contains(Event.current.mousePosition),false);
                    DrawShopSold(rect,i);
                    if (GUI.Button(rect,GUIContent.none,GUIStyle.none)) { inspectedShopItem=i; return; }
                    purchase = new Rect(rect.x,605,rect.width,43);
                }
                else if (offer.Kind == ShopItemKind.Relic)
                {
                    var rect = new Rect(100 + relicNumber++ * 415,684,395,212);
                    Fill(rect,new Color(1,1,1,.7f)); Border(rect,Gold);
                    DrawRelicArt(new Rect(rect.x+14,rect.y+12,125,125),offer.Relic);
                    Label(new Rect(rect.x+146,rect.y+18,232,54),offer.Title,ui.Heading);
                    DrawExplainedText(new Rect(rect.x+146,rect.y+80,232,121),offer.Description,ui.Small);
                    DrawShopSold(rect,i);
                    if (GUI.Button(rect,GUIContent.none,GUIStyle.none)) { inspectedShopItem=i; return; }
                    purchase = new Rect(rect.x,905,rect.width,43);
                }
                else
                {
                    var rect = new Rect(1390,280+serviceNumber++*133,430,122);
                    Fill(rect,new Color(1,1,1,.65f)); Border(rect,Muted);
                    Label(new Rect(rect.x+14,rect.y+6,400,32),offer.Title,ui.Heading);
                    if(offer.Kind==ShopItemKind.Draft)
                    {
                        Border(rect,Rarity((Graphaclysm.Core.Cards.CardRarity)offer.DraftGrade));
                        Label(new Rect(rect.x+14,rect.y+43,400,28),"3장 중 1장 · 눌러서 확률 확인",ui.Small);
                        if(GUI.Button(new Rect(rect.x,rect.y,rect.width,75),GUIContent.none,GUIStyle.none)){inspectedShopItem=i;return;}
                    }
                    else DrawExplainedText(new Rect(rect.x+14,rect.y+43,400,28),offer.Description,ui.Small);
                    purchase = new Rect(rect.x+14,rect.y+79,400,35);
                }
                if (ShopPurchaseButton(purchase,i)) return;
            }
            Label(new Rect(100,977,1260,38),run.RoomResult,ui.Small);
            if(ui.Button(new Rect(1490,969,330,54),"상점 나가기",true)) { inspectedShopItem=-1; run.TryLeaveRoom(); Refresh(); }
        }
        private void DrawShopSold(Rect rect,int index)
        {
            if (!run.ShopSold(index)) return;
            Fill(rect,new Color(.1f,.09f,.15f,.62f));
            Label(new Rect(rect.x,rect.center.y-28,rect.width,56),"구매 완료",ui.HeadingLight,true);
        }
        private bool ShopPurchaseButton(Rect rect,int index)
        {
            var offer=run.GetShopOffer(index); string blocker=run.ShopBlocker(index);
            string action=string.IsNullOrEmpty(blocker)?"은화 "+offer.Price+" · 구매":blocker+(run.ShopSold(index)?"":" · "+offer.Price);
            if (!ui.Button(rect,action,false,string.IsNullOrEmpty(blocker))) return false;
            run.TryBuyShop(index); inspectedShopItem=-1; Refresh(); return true;
        }
        private void DrawShopInspection(int index)
        {
            var offer=run.GetShopOffer(index);
            Fill(new Rect(240,285,1440,610),new Color(Paper.r,Paper.g,Paper.b,.98f));
            if (offer.Card != null)
            {
                var visual=Visual(offer.Card);
                DrawFragmentCard(new Rect(300,325,400,410),visual,false,true);
                DrawReadableCardDetails(new Rect(780,318,800,414),visual,true,true);
            }
            else if(offer.Kind==ShopItemKind.Draft)
            {
                Fill(new Rect(320,345,350,330),new Color(1,1,1,.75f));
                Border(new Rect(320,345,350,330),Rarity((Graphaclysm.Core.Cards.CardRarity)offer.DraftGrade));
                DrawLootEmblem(new Vector2(495,490),LootKind.Cards);
                Label(new Rect(780,330,800,70),offer.Title,ui.PageTitle);
                Label(new Rect(780,425,800,260),offer.Description,ui.Body);
                Label(new Rect(780,695,800,40),"구매 즉시 선택 · 넘기면 은화는 반환되지 않습니다",ui.Small);
            }
            else
            {
                DrawRelicArt(new Rect(320,345,350,330),offer.Relic);
                Label(new Rect(780,330,800,70),offer.Title,ui.PageTitle);
                DrawExplainedText(new Rect(780,440,800,270),offer.Description,ui.Body);
            }
            if(ShopPurchaseButton(new Rect(780,753,800,55),index)) return;
            if(ui.Button(new Rect(300,823,1280,50),"진열대로 돌아가기")) inspectedShopItem=-1;
        }
    }
}
