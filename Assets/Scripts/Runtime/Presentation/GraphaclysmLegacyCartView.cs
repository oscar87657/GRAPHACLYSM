using Graphaclysm.Application;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static readonly int[][] LegacyPages={new[]{0,1,2,3,4,6},new[]{7,8,9,10,5},new[]{11,12,13}};
        private static readonly string[] LegacyPageTitles={"영구 능력 강화","탐색 준비","분대 개방"};
        private int legacyPage;
        private ExpeditionSquad selectedSquad;
        private bool squadSelectionOpen;
        private readonly int[] legacyCart = new int[LegacyProgression.NodeCount];
        private readonly int[] legacyCartSnapshot = new int[LegacyProgression.NodeCount];
        private int legacyCartCurrency = -1;
        private LegacyProgression legacyCartOwner;
        private bool legacyCartConfirm;
        private string legacyCartMessage = "";
        private readonly string[] legacyRankLabels=new string[LegacyProgression.NodeCount];
        private readonly string[] legacyCostLabels=new string[LegacyProgression.NodeCount];
        private readonly string[] legacyPrerequisiteLabels=new string[LegacyProgression.NodeCount];
        private bool legacyCartTextDirty=true;
        private int legacyCartTotalCost;
        private string legacyCartTotalLabel="";

        private void ResetLegacyCart()
        {
            legacyCartOwner = legacy; legacyCartCurrency = legacy.Currency; legacyCartConfirm = false;
            for (int i = 0; i < legacyCart.Length; i++) legacyCartSnapshot[i] = legacyCart[i] = legacy.GetRank(i);
            legacyCartTextDirty=true;
        }
        private void RefreshLegacyCartText()
        {
            if(!legacyCartTextDirty)return;legacyCartTextDirty=false;
            for(int i=0;i<legacyCart.Length;i++)
            {
                var node=legacy.GetNode(i);int current=legacy.GetRank(i),target=legacyCart[i];
                legacyRankLabels[i]=current+" → "+target+" / "+node.MaxRank;
                int parent=LegacyProgression.Prerequisite(i);
                legacyPrerequisiteLabels[i]=parent<0?"바로 투자 가능":"선행: "+legacy.GetNode(parent).Name+" 1단계"+(legacyCart[parent]>0?" · 충족":"");
                int itemCost=0;for(int rank=current;rank<target;rank++)itemCost+=node.BaseCost+rank;
                legacyCostLabels[i]=itemCost>0?"담은 비용 "+itemCost:current==node.MaxRank?"완료":"다음 "+legacy.Cost(i);
            }
            legacyCartTotalCost=legacy.QuoteRanks(legacyCart);
            legacyCartTotalLabel=legacyCartTotalCost<0?"선행 항목을 구매 목록에 함께 담아 주세요.":
                "잔광 "+legacy.Currency+" − 구매 "+legacyCartTotalCost+" = "+(legacy.Currency-legacyCartTotalCost)
                +(legacyCartTotalCost>legacy.Currency?" · "+(legacyCartTotalCost-legacy.Currency)+" 부족":"");
        }
        private bool PersistLegacyCandidate(LegacyProgression candidate) => !persistenceEnabled || legacyStore.TrySave(candidate);
        private void DrawLegacyArchive() => DrawLegacyConstellation();
        private void DrawSquadSelection()
        {
            Header("EXPEDITION DOCTRINE","출발 분대");
            Label(new Rect(150,150,1600,70),"이번 원정의 분대를 선택하세요",ui.PageTitle);
            Label(new Rect(150,240,1600,70),"캐릭터·전투방식과 별개로 하나만 적용합니다. 추가 비용 없이 매 원정 바꿀 수 있습니다.",ui.Body);
            for(int i=0;i<4;i++)
            {
                var squad=(ExpeditionSquad)i;bool unlocked=legacy.IsSquadUnlocked(squad);
                Rect r=new Rect(150+i*413,360,383,395);
                Fill(r,new Color(1,1,1,.8f));Border(r,selectedSquad==squad?Violet:Gold);
                Label(new Rect(r.x+22,r.y+25,339,58),ExpeditionSquads.Names[i],ui.Heading,true);
                Label(new Rect(r.x+25,r.y+110,333,130),ExpeditionSquads.Descriptions[i],ui.Body);
                if(!unlocked)Label(new Rect(r.x+25,r.y+245,333,52),"잔광 보관소에서 영구 개방",ui.Small);
                if(ui.Button(new Rect(r.x+25,r.y+320,333,50),unlocked?(selectedSquad==squad?"선택됨":"이 분대로 준비"):"미개방",selectedSquad==squad,unlocked))
                {selectedSquad=squad;flow.PreparationUpgrades=legacy.Preparation(squad);}
            }
            if(ui.Button(new Rect(150,875,620,60),"잔광 보관소 · 분대 개방")){legacyPage=2;legacyOpen=true;}
            if(ui.Button(new Rect(1110,875,660,60),"캐릭터 선택으로",true))squadSelectionOpen=false;
        }
    }
}
