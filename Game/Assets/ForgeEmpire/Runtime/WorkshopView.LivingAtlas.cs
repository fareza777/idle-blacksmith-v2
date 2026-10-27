using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        static readonly Vector2[] AtlasPoints={new Vector2(.22f,.76f),new Vector2(.75f,.77f),new Vector2(.23f,.40f),new Vector2(.76f,.44f),new Vector2(.48f,.18f),new Vector2(.82f,.13f)};
        void AtlasRoad(Transform parent,string name,Vector2 from,Vector2 to,Color color,float thickness=4)
        {
            var delta=to-from;var road=ForgeUI.Fill(name,parent,from.x,from.y,delta.magnitude,thickness,color);
            road.rectTransform.pivot=new Vector2(0,.5f);road.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
        float BuildLivingAtlas(Transform parent,float y)
        {
            var sim=app.Sim;float mapHeight=Mathf.Clamp(pageScroll.viewport.rect.height*.6f,340,760);
            var viewport=ForgeUI.Rect("AtlasViewport",parent,0,y,1020,mapHeight);viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var painting=ForgeUI.Rect("AtlasPainting",viewport,0,0,1020,1275);
            ForgeUI.Picture("ChronicleAtlas",painting,HandDrawnArt.Texture("worldmap-chronicles"),0,0,1020,1275);
            for(int region=1;region<6;region++)
            {
                bool earned=region<=sim.UnlockedRegion;
                AtlasRoad(painting,"AtlasRoad"+region,Vector2.Scale(AtlasPoints[region-1],new Vector2(1020,1275)),Vector2.Scale(AtlasPoints[region],new Vector2(1020,1275)),earned?new Color(.9f,.75f,.46f,.72f):new Color(.74f,.76f,.67f,.2f),earned?5:2);
            }
            int position=sim.SagaInProgress?sim.State.Saga.Run.Region:sim.UnlockedRegion;
            for(int region=0;region<6;region++)
            {
                int id=region;float x=AtlasPoints[id].x*1020,py=AtlasPoints[id].y*1275;bool cleared=sim.IsFrontierCleared(id),open=id<=sim.UnlockedRegion;
                if(id==position)ForgeUI.Light("AtlasCurrentPosition",painting,x-72,py-68,144,144).color=new Color(1,.82f,.48f,.85f);
                var pin=ForgeUI.Button("AtlasRegion"+id,painting,"",x-48,py-48,96,96,Color.clear,ForgeUI.Cream,()=>ShowChronicleRegion(id));
                ForgeUI.Glyph("AtlasPinSymbol"+id,pin.transform,cleared?"check":!open?"lock":"map",24,24,48,open?ForgeUI.Gold:ForgeUI.Muted);
                string name=GameContent.Regions[id].Name;float width=320,tx=Mathf.Clamp(x-width*.5f,0,1020-width),height=ForgeUI.Height(name,width,32,FontStyle.Bold);
                var label=ForgeUI.Label("AtlasName"+id,painting,name,tx,py+46,width,height,32,open?ForgeUI.Cream:ForgeUI.Muted,FontStyle.Bold,TextAnchor.UpperCenter);ForgeUI.TextShadow(label);
                if(sim.State.World.Projects[id]>0)ForgeUI.Glyph("AtlasEarnedTown"+id,painting,"crew",x+42,py+4,32,ForgeUI.Teal);
                if(id==sim.UnlockedRegion)
                {
                    var frontier=ForgeUI.Label("AtlasFrontier"+id,painting,"Frontier",x-100,py-90,200,ForgeUI.Height("Frontier",200,28),28,ForgeUI.Gold,FontStyle.Bold,TextAnchor.MiddleCenter);ForgeUI.TextShadow(frontier);
                }
            }
            var point=AtlasPoints[position];ForgeUI.Glyph(sim.SagaInProgress?"AtlasPartyPosition":"AtlasNextDeparture",painting,"hero",point.x*1020-82,point.y*1275-24,46,ForgeUI.Teal);
            var navigation=viewport.gameObject.AddComponent<ChronicleAtlasNavigation>();navigation.Configure(viewport,painting,pageScroll);navigation.Focus(AtlasPoints[position]);
            y+=mapHeight+16;
            y=Paragraph(parent,"AtlasGestureHint","Drag to explore · Pinch or use the zoom buttons. Gold roads are open; small locks mark future chapters.",y,ForgeUI.Small,ForgeUI.Muted)+16;
            float buttonHeight=Mathf.Max(ActionHeight,ForgeUI.Height("Zoom +",276,ForgeUI.Small,FontStyle.Bold)+24);
            ForgeUI.Button("AtlasZoomOut",parent,"Zoom −",0,y,324,buttonHeight,ForgeUI.Surface,ForgeUI.Cream,()=>navigation.ZoomBy(-.25f),ForgeUI.Small);
            ForgeUI.Button("AtlasRecenter",parent,"Party",348,y,324,buttonHeight,ForgeUI.Surface,ForgeUI.Teal,()=>{navigation.SetZoom(1);navigation.Focus(AtlasPoints[position]);},ForgeUI.Small);
            ForgeUI.Button("AtlasZoomIn",parent,"Zoom +",696,y,324,buttonHeight,ForgeUI.Surface,ForgeUI.Cream,()=>navigation.ZoomBy(.25f),ForgeUI.Small);y+=buttonHeight+16;
            string[] pans={"Pan left","Pan up","Pan down","Pan right"};
            for(int i=0;i<4;i++)
            {
                int direction=i;ForgeUI.Button("AtlasPan"+i,parent,pans[i],i*261,y,237,buttonHeight,ForgeUI.Surface,ForgeUI.Cream,()=>navigation.PanBy(direction==0?240:direction==3?-240:0,direction==1?-240:direction==2?240:0),ForgeUI.Small);
            }
            y+=buttonHeight+Gap;
            y=Paragraph(parent,"AtlasPosition",(sim.SagaInProgress?"Party travelling in "+GameContent.Regions[position].Name:"Party at the workshop · Next road: "+GameContent.Regions[position].Name)+"\nCurrent frontier: "+GameContent.Regions[sim.UnlockedRegion].Name,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            if(sim.SagaInProgress)y=BuildSavedJourney(parent,y);
            return y;
        }
        float BuildSavedJourney(Transform parent,float y)
        {
            var sim=app.Sim;var run=sim.State.Saga.Run;
            y=Paragraph(parent,"AtlasJourneyHeading","Your saved road · "+run.ClearedRooms+" / "+run.TotalRooms+" rooms cleared",y,ForgeUI.Subheading,ForgeUI.Gold,true)+16;
            float width=(1020-(run.TotalRooms-1)*12)/run.TotalRooms,row=160*ForgeUI.TextScale;
            for(int room=0;room<run.TotalRooms;room++)
            {
                float x=room*(width+12),middle=x+width*.5f;bool cleared=room<run.ClearedRooms,current=room==run.Room;
                int choice=run.RoomChoices!=null&&room<run.RoomChoices.Length?run.RoomChoices[room]:-1;
                string title=choice>=0?new[]{"Safe trail","Shelter","Rescue","Trader","Waystone","Elite"}[Mathf.Clamp(choice,0,5)]:"Uncharted";
                if(room==run.TotalRooms-1&&run.Route>=2)title="Guardian";
                else if(current&&run.PendingJourney>0)title=run.PendingJourney==3?"Trader":"Waystone";
                if(room>0)AtlasRoad(parent,"SavedRoad"+room,new Vector2(x-12, y+30),new Vector2(middle-26,y+30),cleared?ForgeUI.Teal:ForgeUI.Line,3);
                ForgeUI.Glyph("SavedRoom"+room,parent,cleared?"check":current?"hero":"map",middle-25,y+5,50,cleared?ForgeUI.Teal:current?ForgeUI.Gold:ForgeUI.Muted);
                float textHeight=ForgeUI.Height(title,width,32,FontStyle.Bold);row=Mathf.Max(row,textHeight+68);
                ForgeUI.Label("SavedRoomTitle"+room,parent,title,x,y+68,width,textHeight,32,current?ForgeUI.Gold:ForgeUI.Cream,FontStyle.Bold,TextAnchor.UpperCenter);
            }
            y+=row+16;
            string state=run.PendingJourney==3?"At a travelling trader":run.PendingJourney==4?"At a waystone":run.Phase==SagaPhase.Camp?"At a shelter":run.Phase==SagaPhase.Combat?sim.SagaEncounterName:sim.SagaNextAction;
            y=Paragraph(parent,"AtlasSavedState",state+"\n"+run.LastEvent,y,ForgeUI.Small,ForgeUI.Teal)+16;
            var rewards=sim.PreviewLivingJourneyRewards();
            y=Paragraph(parent,"AtlasJourneyRewards",(run.Phase==SagaPhase.Result?"Ready to collect: ":"On victory: ")+rewards.Gold+" gold · "+rewards.Material+" "+WorkshopSimulation.MaterialName(run.Region).ToLowerInvariant()+"\nIncludes saved branch supplies and challenge rank "+run.Modifier+". Rewards are paid once when you collect the result.",y,ForgeUI.Small,ForgeUI.Gold)+16;
            if(run.Boon>=0)y=Paragraph(parent,"AtlasSavedBoon","Active boon: "+new[]{"Might","Shelter","Clarity"}[Mathf.Clamp(run.Boon,0,2)]+" · Applies through room "+Mathf.Min(run.TotalRooms,run.BoonUntilRoom+1)+".",y,ForgeUI.Small,ForgeUI.Gold)+16;
            if(run.Phase==SagaPhase.Choice||run.PendingJourney>0)
            {
                for(int path=0;path<6;path++)if(sim.CanChooseSagaPath(path))
                    y=Paragraph(parent,"AtlasEligiblePath"+path,sim.SagaPathTitle(path)+" · "+sim.SagaPathDescription(path),y,ForgeUI.Small,ForgeUI.Muted)+12;
            }
            return AddAction(parent,"AtlasResumeSavedJourney","Continue the saved expedition",y,ShowSaga);
        }
        float AddWorldGoals(Transform parent,float y)
        {
            y=Paragraph(parent,"AtlasConnectionsHeading","A useful next creation",y,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            foreach(var goal in app.Sim.WorldGoalLinks)
            {
                var link=goal;Rule(parent,y);y+=Gap;
                y=Paragraph(parent,"WorldGoalTitle"+link.Kind,link.Title,y,ForgeUI.Subheading,null,true)+12;
                y=Paragraph(parent,"WorldGoalDetail"+link.Kind,link.Detail,y,ForgeUI.Small,ForgeUI.Muted)+16;
                if(link.Blocker.Length>0)y=Paragraph(parent,"WorldGoalBlocker"+link.Kind,link.Blocker,y,ForgeUI.Small,ForgeUI.Orange)+16;
                y=AddAction(parent,"WorldGoal"+link.Kind,link.Button,y,()=>FollowWorldGoal(link),link.Kind==WorldGoalKind.Milestone);
            }
            return y;
        }
        void FollowWorldGoal(WorldGoalLink link)
        {
            switch(link.Kind)
            {
                case WorldGoalKind.Milestone:ContinueLivingGoal();break;
                case WorldGoalKind.Contract:ShowGuildOrders();break;
                case WorldGoalKind.Recipe:ShowArtisan(link.RecipeId);break;
                case WorldGoalKind.Material:ShowSagaDeparture(link.Region,link.Route);break;
                case WorldGoalKind.Guardian:if(link.QuestId>=0)ShowQuestDetail(link.QuestId);else ShowSagaDeparture(link.Region,2);break;
                case WorldGoalKind.Restoration:ShowRestorationRegion(link.Region);break;
            }
        }
        void ContinueLivingGoal()
        {
            var sim=app.Sim;
            if(sim.SagaInProgress){ShowSaga();return;}
            if(sim.MasterworkActive){ShowArtisan(sim.State.Artisan.Project.RecipeId);return;}
            CloseModal();journeyFollowing=true;ExecuteJourneyAction();
        }
    }
}
