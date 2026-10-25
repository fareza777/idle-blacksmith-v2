using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView : MonoBehaviour
    {
        const float Pixel = 2.8125f;
        WorkshopApp app;
        RectTransform root, scene, marker, selection, worldViewport, workshopContent, workshopViewport, guidePanel, contextDock;
        ScrollRect worldScroll, workshopScroll;
        Text gold, ore, ingots, toast, quality, stationTitle, stationInfo, actionText, upgradeText, questTitle, questDetail;
        Button actionButton, upgradeButton, strikeButton;
        Image forgeGlow;
        RawImage smoke, sparks, customer, carriedSword, workLight, topVeil, bottomVeil;
        RectTransform dockProgress;
        Button guideAction, recipePicker, purposeButton, merchantPause;
        Text purposeText;
        Image toastPanel;
        readonly Image[] navFills=new Image[3];
        readonly Text[] navTexts=new Text[3];
        float headerHeight,guideHeight=84,dockHeight;
        const float NavHeight=174;
        const float SceneZoom=1.18f;
        float strikeFeedbackUntil;
        bool perfectFeedback;
        readonly RawImage[] actors = new RawImage[4], improvements = new RawImage[5];
        readonly RawImage[] rackSwords = new RawImage[3], orePieces = new RawImage[3], ingotPieces = new RawImage[3];
        readonly RectTransform[] bars = new RectTransform[4];
        readonly float[] animationTimes = new float[4], previousProgress = new float[4];
        readonly bool[] previousRunning = new bool[4];
        static readonly int[] FrameCounts = {6,6,6,6};
        static readonly float[][] FrameDurations = {new[]{.34f,.18f,.16f,.07f,.09f,.30f},new[]{.11f,.09f,.11f,.09f,.11f,.09f},new[]{.28f,.18f,.16f,.065f,.08f,.30f},new[]{.6f,.22f,.20f,.40f,.22f,.5f}};
        static readonly Rect[] StationAreas = {new Rect(17,142,126,139),new Rect(235,147,131,145),new Rect(113,311,159,117),new Rect(21,446,122,99)};
        static readonly Vector2[] Stations = {new Vector2(80,263),new Vector2(291,272),new Vector2(201,383),new Vector2(92,514)};
        readonly string[] names = {"Iron mine","Smelting hearth","The iron forge","Market stall"};
        readonly Flight[] flights = new Flight[8];
        Rect lastSafeArea;
        int lastWidth, lastHeight, lastMined, lastCrafted, lastSold, lastTier;
        float toastUntil, nextRefresh, sparkUntil, sceneTime;
        Station selected = Station.Mine;
        string modalKind;
        RectTransform modal;
        public float StrikeAccuracy { get; private set; }
        public bool HasModal { get { return modal != null; } }
        public RectTransform Root { get { return root; } }
        public Station SelectedStation { get { return selected; } }

        sealed class Flight
        {
            public RawImage Image;
            public Vector2 From, To;
            public float Time, Duration;
        }

        public void Build(WorkshopApp owner)
        {
            app=owner;
            ForgeUI.TextScale=app.Sim.State.TextSize==2?1.5f:app.Sim.State.TextSize==1?1.25f:1;
            var canvas=GetComponent<Canvas>();if(!canvas)canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            if(!GetComponent<GraphicRaycaster>())gameObject.AddComponent<GraphicRaycaster>();
            if(!FindFirstObjectByType<EventSystem>())new GameObject("Input",typeof(EventSystem),typeof(StandaloneInputModule));
            var backdrop=ForgeUI.Fill("Backdrop",transform,0,0,Screen.width,Screen.height,ForgeUI.Background);
            backdrop.rectTransform.anchorMin=Vector2.zero;backdrop.rectTransform.anchorMax=Vector2.one;
            backdrop.rectTransform.offsetMin=backdrop.rectTransform.offsetMax=Vector2.zero;
            root=ForgeUI.Rect("Workshop",transform,0,0,1080,2160);
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);
            FitScreen();
            workshopViewport=ForgeUI.Rect("WorkshopViewport",root,0,0,1080,root.rect.height-NavHeight);
            workshopViewport.gameObject.AddComponent<RectMask2D>();
            var workshopHit=workshopViewport.gameObject.AddComponent<Image>();workshopHit.color=Color.clear;
            workshopScroll=workshopViewport.gameObject.AddComponent<ScrollRect>();workshopScroll.horizontal=false;workshopScroll.viewport=workshopViewport;
            workshopScroll.movementType=ScrollRect.MovementType.Clamped;workshopScroll.scrollSensitivity=90;workshopScroll.enabled=false;
            workshopContent=ForgeUI.Rect("WorkshopContent",workshopViewport,0,0,1080,root.rect.height-NavHeight);workshopScroll.content=workshopContent;
            BuildDiorama();
            topVeil=ForgeUI.Fade("RoofVeil",workshopContent,0,0,1080,420,new Color(.06f,.08f,.07f,.96f));
            bottomVeil=ForgeUI.Fade("FloorVeil",workshopContent,0,0,1080,440,new Color(.06f,.08f,.07f,.98f),true);
            BuildHeader();
            BuildJourney();
            BuildDock();
            BuildNavigation();
            BuildTownLife();
            toastPanel=ForgeUI.Fill("ToastPanel",root,30,0,1020,144,new Color(.06f,.08f,.07f,.94f));
            toast=ForgeUI.Label("Toast",toastPanel.transform,"",24,18,972,108,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold,TextAnchor.MiddleCenter);
            toastPanel.gameObject.SetActive(false);
            lastMined=app.Sim.State.TotalMined;lastCrafted=app.Sim.State.TotalCrafted;lastSold=app.Sim.State.TotalSold;lastTier=app.Sim.WorkshopTier;
            for(int i=0;i<4;i++){previousRunning[i]=app.Sim.IsRunning((Station)i);previousProgress[i]=(float)app.Sim.GetProgress((Station)i);}
            Refresh();
            FocusStation(selected,journeyFollowing);
        }

        void FitScreen()
        {
            Rect safe=Screen.safeArea;lastSafeArea=safe;lastWidth=Screen.width;lastHeight=Screen.height;
            float scale=safe.width/1080f;
            root.localScale=Vector3.one*scale;
            root.sizeDelta=new Vector2(1080,safe.height/scale);
            root.anchoredPosition=safe.center-new Vector2(Screen.width*.5f,Screen.height*.5f);
            if(workshopViewport)workshopViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,root.rect.height-NavHeight);
            var nav=root.Find("Navigation") as RectTransform;
            if(nav)nav.anchoredPosition=new Vector2(0,-(root.rect.height-NavHeight));
            if(workshopContent)LayoutWorkshop();
            if(HasModal)RequestPageRebuild();
        }

        void BuildHeader()
        {
            headerHeight=124+80*ForgeUI.TextScale;
            ForgeUI.TextShadow(ForgeUI.Label("Title",workshopContent,"Ember & Oak",42,8,780,112*ForgeUI.TextScale,ForgeUI.Subheading,ForgeUI.Cream));
            var settings=ForgeUI.Medallion("Settings",workshopContent,948,22,96,ShowSettings);
            ForgeUI.Icon("SettingsIcon",settings.transform,7,22,22,52);
            gold=Resource("Gold",3,48,headerHeight-82*ForgeUI.TextScale,330,ForgeUI.Gold);
            ore=Resource("Ore",0,390,headerHeight-82*ForgeUI.TextScale,330,ForgeUI.Cream);
            ingots=Resource("Ingots",1,732,headerHeight-82*ForgeUI.TextScale,330,ForgeUI.Cream);
        }

        Text Resource(string title,int icon,float x,float y,float w,Color color)
        {
            var p=ForgeUI.Rect(title+"Resource",workshopContent,x,y,w,80*ForgeUI.TextScale);
            ForgeUI.Icon(title+"Icon",p,icon,0,12*ForgeUI.TextScale,52);
            var value=ForgeUI.Label(title+"Value",p,"0",72,0,w-76,80*ForgeUI.TextScale,ForgeUI.Body,color,FontStyle.Bold);
            ForgeUI.TextShadow(value);return value;
        }

        void BuildJourney()
        {
            guidePanel=ForgeUI.Rect("NextStep",workshopContent,40,headerHeight+6,866,90);
            guideAction=ForgeUI.Button("GuideAction",guidePanel,"",0,0,866,90,Color.clear,ForgeUI.Gold,ShowJourney);
            guideAction.navigation=new Navigation{mode=Navigation.Mode.Automatic};
            ForgeUI.Glyph("ObjectiveMark",guideAction.transform,"check",0,22,34,ForgeUI.Gold);
            questTitle=ForgeUI.Label("JourneyTitle",guideAction.transform,"",54,0,810,90,ForgeUI.Small,ForgeUI.Gold);
            ForgeUI.TextShadow(questTitle);
            questDetail=ForgeUI.Label("JourneyDetail",guideAction.transform,"",0,0,1,1,ForgeUI.Small,ForgeUI.Muted);questDetail.gameObject.SetActive(false);
            ForgeUI.Medallion("TabQUESTS",workshopContent,948,headerHeight+20,96,()=>ShowQuests(false),"book");
            ForgeUI.Medallion("TabCREW",workshopContent,948,headerHeight+140,96,ShowCrew,"crew");
        }

        void BuildDiorama()
        {
            var viewport=ForgeUI.Fill("WorldViewport",workshopContent,0,0,1080,900,ForgeUI.Background);
            viewport.raycastTarget=true;worldViewport=viewport.rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            worldScroll=viewport.gameObject.AddComponent<ScrollRect>();worldScroll.horizontal=false;worldScroll.viewport=worldViewport;
            worldScroll.movementType=ScrollRect.MovementType.Clamped;worldScroll.scrollSensitivity=65;
            scene=ForgeUI.Rect("Diorama",worldViewport,0,0,1080,1665);worldScroll.content=scene;
            scene.localScale=Vector3.one*SceneZoom;
            var texture=Resources.Load<Texture2D>("Art/HandDrawn/workshop");
            ForgeUI.Picture("WorkshopArt",scene,texture,0,0,1080,1665).color=Color.white;
            workLight=ForgeUI.Light("ActiveWorkLight",scene,0,0,600,500);
            GroundShadow("MinerFeet",new Vector2(76,270),30,5);
            actors[0]=HandDrawnArt.Person("Miner",scene,"workers",0,new Vector2(76*Pixel,270*Pixel),60*Pixel,84*Pixel);
            actors[1]=Actor("fire",272,214,32,40,6);
            actors[1].gameObject.SetActive(false);
            GroundShadow("SmithFeet",new Vector2(177,381),34,6);
            actors[2]=HandDrawnArt.Person("Blacksmith",scene,"workers",6,new Vector2(177*Pixel,381*Pixel),66*Pixel,94*Pixel);
            actors[3]=HandDrawnArt.Person("Merchant",scene,"workers",12,new Vector2(78*Pixel,508*Pixel),56*Pixel,80*Pixel);
            customerShadow=GroundShadow("CustomerFeet",new Vector2(308,521),28,5);
            customer=HandDrawnArt.Person("Customer",scene,"walkers",24,new Vector2(308*Pixel,521*Pixel),56*Pixel,74*Pixel,false);
            Crop(texture,"AnvilForeground",new Rect(178,368,68,40));
            Crop(texture,"StallForeground",new Rect(25,493,113,36));
            Rect[] patches={new Rect(107,247,42,34),new Rect(330,260,40,36),new Rect(276,318,54,39),new Rect(29,431,104,38),new Rect(174,115,38,38)};
            for(int i=0;i<5;i++) {
                var p=patches[i];
                improvements[i]=HandDrawnArt.Prop("BuildingUpgrade"+i,scene,i,p.x*Pixel,p.y*Pixel,p.width*Pixel,p.height*Pixel);
            }
            smoke=ForgeUI.Light("ChimneySmoke",scene,278*Pixel,38*Pixel,26*Pixel,58*Pixel);smoke.color=new Color(.38f,.44f,.47f,.28f);
            sparks=Actor("sparks",175,337,48,48,5);sparks.gameObject.SetActive(false);
            forgeGlow=ForgeUI.Fill("HotSteel",scene,190*Pixel,366*Pixel,30*Pixel,2*Pixel,new Color(1,.69f,.28f,0));
            carriedSword=ForgeUI.Icon("CustomerSword",scene,2,0,0,22*Pixel);carriedSword.gameObject.SetActive(false);
            for(int i=0;i<3;i++) {
                rackSwords[i]=EquipmentIcon("RackSword"+i,scene,0,(250+i*12)*Pixel,389*Pixel,27*Pixel);
                orePieces[i]=ForgeUI.Icon("OrePile"+i,scene,0,(103+i*7)*Pixel,(247-i%2*5)*Pixel,17*Pixel);
                ingotPieces[i]=ForgeUI.Icon("IngotRack"+i,scene,1,(257+i*10)*Pixel,279*Pixel,16*Pixel);
            }
            for(int i=0;i<flights.Length;i++) {
                var image=ForgeUI.Icon("MaterialTransit"+i,scene,0,0,0,22*Pixel);image.gameObject.SetActive(false);
                flights[i]=new Flight{Image=image};
            }
            selection=ForgeUI.Rect("StationSelection",scene,0,0,24,24);
            var selectedMark=ForgeUI.Fill("SelectedMark",selection,5,5,14,14,ForgeUI.Gold);selectedMark.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            for(int i=0;i<4;i++) {
                int station=i;var area=StationAreas[i];
                ForgeUI.Button("World"+(Station)i,scene,"",area.x*Pixel,area.y*Pixel,area.width*Pixel,area.height*Pixel,Color.clear,Color.clear,()=>WorldTap((Station)station));
                bars[i]=ForgeUI.Fill("WorldProgress"+i,scene,area.x*Pixel,(area.y+area.height)*Pixel,1,8,ForgeUI.Teal).rectTransform;
            }
            ForgeUI.Button("StockRack",scene,"",245*Pixel,382*Pixel,63*Pixel,51*Pixel,Color.clear,Color.clear,ShowInventory);
            ForgeUI.Button("CustomerVisit",scene,"",145*Pixel,456*Pixel,160*Pixel,69*Pixel,Color.clear,Color.clear,()=>WorldTap(Station.Market));
        }

        RawImage Actor(string name,float x,float y,float width,float height,int frames)
        {
            var image=ForgeUI.Picture(name,scene,Resources.Load<Texture2D>("Art/"+name),x*Pixel,y*Pixel,width*Pixel,height*Pixel);
            image.uvRect=new Rect(0,0,1f/frames,1);return image;
        }

        void Crop(Texture2D texture,string name,Rect region)
        {
            var image=ForgeUI.Picture(name,scene,texture,region.x*Pixel,region.y*Pixel,region.width*Pixel,region.height*Pixel);
            image.uvRect=new Rect(region.x/384,1-(region.y+region.height)/592,region.width/384,region.height/592);
        }

        void BuildDock()
        {
            contextDock=ForgeUI.Rect("ContextDock",workshopContent,42,0,996,320);
            stationTitle=ForgeUI.Label("SelectedStationTitle",contextDock,"",0,0,810,94,ForgeUI.Subheading,ForgeUI.Cream);
            stationInfo=ForgeUI.Label("SelectedStationInfo",contextDock,"",0,94,810,90,ForgeUI.Small,ForgeUI.Muted);
            ForgeUI.TextShadow(stationTitle);ForgeUI.TextShadow(stationInfo);
            var dock=contextDock;
            quality=ForgeUI.Label("Quality",dock.transform,"",24,0,984,76,ForgeUI.Small,ForgeUI.Teal);
            actionButton=ForgeUI.Button("ContextAction",dock,"",0,0,690,136,ForgeUI.Gold,ForgeUI.Background,ContextAction);
            actionText=actionButton.GetComponentInChildren<Text>();
            strikeButton=ForgeUI.Button("Strike",dock,"Strike",0,0,690,136,ForgeUI.Gold,ForgeUI.Background,()=>app.Strike());
            upgradeButton=ForgeUI.Button("UpgradeSelected",dock,"",714,0,282,136,Color.clear,ForgeUI.Gold,ShowStationDetails,ForgeUI.Small);
            upgradeText=upgradeButton.GetComponentInChildren<Text>();
            recipePicker=ForgeUI.Medallion("TabRECIPES",dock,840,0,96,ShowRecipes,"book");
            ForgeUI.TextShadow(ForgeUI.Label("RecipeHint",recipePicker.transform,"Recipes",-80,96,256,90*ForgeUI.TextScale,ForgeUI.Small,ForgeUI.Cream,FontStyle.Normal,TextAnchor.MiddleCenter));
            purposeButton=ForgeUI.Button("CraftPurpose",dock,"",0,0,660,96,Color.clear,ForgeUI.Teal,()=>{app.Sim.State.KeepCraftedEquipment=!app.Sim.State.KeepCraftedEquipment;app.Save();Refresh();},ForgeUI.Small);
            purposeText=purposeButton.GetComponentInChildren<Text>();
            purposeText.alignment=TextAnchor.MiddleLeft;
            merchantPause=ForgeUI.Button("MerchantPause",dock,"",0,0,960,96,Color.clear,ForgeUI.Teal,()=>app.ToggleAutomation(Station.Market),ForgeUI.Small);
            var timing=ForgeUI.Rect("Timing",dock.transform,24,0,984,76);
            ForgeUI.Fill("TimingTrack",timing,0,14,984,48,ForgeUI.Line);
            ForgeUI.Fill("PerfectWindow",timing,403,14,178,48,ForgeUI.Teal);
            marker=ForgeUI.Fill("TimingMarker",timing,0,0,12,76,ForgeUI.Gold).rectTransform;
            dockProgress=ForgeUI.Fill("DockProgress",dock.transform,0,0,1,8,ForgeUI.Teal).rectTransform;
        }

        void BuildNavigation()
        {
            var nav=ForgeUI.Fill("Navigation",root,0,root.rect.height-NavHeight,1080,NavHeight,new Color(.07f,.09f,.08f,1)).rectTransform;
            ForgeUI.Ornament(nav,64,0,952);
            string[] labels={"Workshop","Heroes","Adventure"};string[] icons={"workshop","hero","map"};
            string[] ids={"NavWorkshop","NavHeroes","NavAdventure"};
            Action[] actions={CloseModal,ShowHeroes,ShowWorld};
            for(int i=0;i<3;i++) {
                int tab=i;
                var button=ForgeUI.Button(ids[i],nav,"",12+i*360,10,336,164,Color.clear,ForgeUI.Cream,()=>OpenPrimary(tab,actions[tab]));
                button.navigation=new Navigation{mode=Navigation.Mode.Automatic};
                navFills[i]=button.GetComponent<Image>();
                ForgeUI.Glyph("TabIcon",button.transform,icons[i],143,12,50,ForgeUI.Gold);
                navTexts[i]=ForgeUI.Label("TabLabel",button.transform,labels[i],0,66,336,96,ForgeUI.Small,ForgeUI.Muted,FontStyle.Normal,TextAnchor.MiddleCenter);
            }
        }

        bool Supplied(int i)
        {
            var s=app.Sim.State;
            return i==0||i==1&&s.Ore>=2||i==2&&app.Sim.CanCraftSelected||i==3&&app.Sim.NextSaleIndex>=0;
        }

        public void FocusStation(Station station,bool guided=false)
        {
            journeyFollowing=guided;
            if(!guided)app.Sim.State.ReserveJourneyGear=false;
            selected=station;
            if(worldScroll&&scene.rect.height*SceneZoom>worldViewport.rect.height) {
                float target=Stations[(int)station].y*Pixel*SceneZoom-worldViewport.rect.height*.52f;
                float shift=Mathf.Clamp(Stations[(int)station].x*Pixel*SceneZoom-540,0,1080*(SceneZoom-1));
                MoveCamera(new Vector2(-shift,Mathf.Clamp(target,0,scene.rect.height*SceneZoom-worldViewport.rect.height)));
                worldScroll.StopMovement();
            }
            Refresh();
        }

        void WorldTap(Station station)
        {
            FocusStation(station);
            if(!app.Sim.IsRunning(station)&&!Supplied((int)station)) {
                FocusStation(station);Notify(app.Sim.WorkBlocker(station));return;
            }
            app.Work(station);Refresh();
        }

        void FollowJourney()
        {
            ResumeJourney();
        }

        public void Tick(float delta)
        {
            if(Screen.safeArea!=lastSafeArea||Screen.width!=lastWidth||Screen.height!=lastHeight)FitScreen();
            sceneTime+=delta;
            TickPresentationMotion();
            TickWorkflow();
            TickTownLife(delta);
            TickBattle(delta);
            TickSaga(delta);TickArtisan();
            for(int i=0;i<4;i++) {
                bool running=app.Sim.IsRunning((Station)i);float progress=(float)app.Sim.GetProgress((Station)i);
                if(running&&(!previousRunning[i]||progress<previousProgress[i]-.1f))animationTimes[i]=0;
                animationTimes[i]+=delta*(i==1&&!running?.55f:1);
                bool active=running;int frame=0;
                if(active) {
                    float length=0;foreach(float duration in FrameDurations[i])length+=duration;
                    float time=animationTimes[i]%length;
                    while(frame<FrameCounts[i]-1&&time>=FrameDurations[i][frame]){time-=FrameDurations[i][frame];frame++;}
                }
                if(app.Sim.State.ReducedMotion)frame=0;
                if(i==0)HandDrawnArt.Pose(actors[i],"workers",frame,new Vector2(76*Pixel,270*Pixel),60*Pixel,84*Pixel);
                else if(i==2)HandDrawnArt.Pose(actors[i],"workers",6+frame,new Vector2(177*Pixel,381*Pixel),66*Pixel,94*Pixel);
                else if(i==3)HandDrawnArt.Pose(actors[i],"workers",12+frame,new Vector2(78*Pixel,508*Pixel),56*Pixel,80*Pixel);
                if(running&&(!previousRunning[i]||progress<previousProgress[i]-.1f)) {
                    if(i==1)Fly(0,Stations[0],Stations[1],.9f);
                    if(i==2)Fly(1,Stations[1],Stations[2],.8f);
                    if(i==3)Fly(2,Stations[2],new Vector2(177,489),.85f);
                }
                if(i==2&&running&&frame==3&&sparkUntil<sceneTime-.15f)sparkUntil=sceneTime+.4f;
                previousRunning[i]=running;previousProgress[i]=progress;
                bars[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,384*progress);
            }
            var state=app.Sim.State;
            if(state.TotalMined>lastMined){Fly(0,Stations[0],Stations[0]+new Vector2(27,-25),.6f);lastMined=state.TotalMined;}
            if(state.TotalCrafted>lastCrafted){Fly(2,Stations[2],new Vector2(257,417),.9f,app.Sim.LastCraftedRecipe);lastCrafted=state.TotalCrafted;}
            if(state.TotalSold>lastSold){Fly(3,new Vector2(174,487),new Vector2(92,484),.7f);lastSold=state.TotalSold;}
            if(app.Sim.WorkshopTier>lastTier){lastTier=app.Sim.WorkshopTier;Notify("WORKSHOP EXPANDED · Welcome to your guild forge",true);}
            float phase=Mathf.PingPong(Time.unscaledTime*.80f,1);
            StrikeAccuracy=1-Mathf.Abs(phase-.5f)*2;
            marker.anchoredPosition=new Vector2(972*phase,0);
            bool forging=app.Sim.IsRunning(Station.Forge);
            forgeGlow.color=new Color(1,.69f,.28f,forging?.18f+.72f*Mathf.Clamp01((sparkUntil-sceneTime)/.4f):0);
            sparks.gameObject.SetActive(sparkUntil>sceneTime&&!state.ReducedMotion);
            if(sparks.gameObject.activeSelf)sparks.uvRect=new Rect(Mathf.Clamp((int)((.4f-(sparkUntil-sceneTime))*12.5f),0,4)/5f,0,.2f,1);
            float smokeRise=state.ReducedMotion?0:(sceneTime*.18f)%1;
            smoke.rectTransform.anchoredPosition=new Vector2((278+Mathf.Sin(smokeRise*4)*3)*Pixel,-(38-smokeRise*26)*Pixel);
            smoke.color=new Color(.38f,.44f,.47f,state.ReducedMotion?.18f:.25f*Mathf.Sin(smokeRise*Mathf.PI));
            TickCustomer();
            foreach(var flight in flights)if(flight.Image.gameObject.activeSelf) {
                flight.Time+=delta;float t=Mathf.Clamp01(flight.Time/flight.Duration);
                float eased=1-Mathf.Pow(1-t,3);
                var p=Vector2.Lerp(flight.From,flight.To,eased);if(!state.ReducedMotion)p.y-=22*4*eased*(1-eased);
                flight.Image.rectTransform.anchoredPosition=new Vector2(p.x*Pixel,-p.y*Pixel);
                if(t>=1)flight.Image.gameObject.SetActive(false);
            }
            dockProgress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,996*(float)app.Sim.GetProgress(selected));
            if(Time.unscaledTime>toastUntil&&toastPanel)toastPanel.gameObject.SetActive(false);
            if(Time.unscaledTime>=nextRefresh){Refresh();nextRefresh=Time.unscaledTime+.2f;}
            if(Input.GetKeyDown(KeyCode.Escape)&&HasModal)GoBack();
            UpdatePageFocus();
        }

        void Fly(int icon,Vector2 from,Vector2 to,float duration,int recipe=-1)
        {
            if(app.Sim.State.ReducedMotion)return;
            foreach(var flight in flights)if(!flight.Image.gameObject.activeSelf) {
                flight.From=from;flight.To=to;flight.Time=0;flight.Duration=duration;
                if(recipe>=0)HandDrawnArt.EquipmentFrame(flight.Image,recipe,22*Pixel,22*Pixel);
                else {
                    flight.Image.texture=Resources.Load<Texture2D>("Art/icons");flight.Image.uvRect=new Rect(icon/8f,0,1f/8,1);
                    flight.Image.rectTransform.pivot=new Vector2(0,1);flight.Image.rectTransform.sizeDelta=new Vector2(22*Pixel,22*Pixel);
                }
                flight.Image.rectTransform.anchoredPosition=new Vector2(from.x*Pixel,-from.y*Pixel);
                flight.Image.gameObject.SetActive(true);return;
            }
        }

        public void Refresh()
        {
            var s=app.Sim.State;
            SyncWorkflow();
            gold.text=Compact(s.Gold);ore.text=Compact(s.Ore);ingots.text=Compact(s.Ingots);
            for(int i=0;i<3;i++) {
                rackSwords[i].gameObject.SetActive(s.Inventory.Count>i);
                if(s.Inventory.Count>i)HandDrawnArt.EquipmentFrame(rackSwords[i],s.Inventory[i].RecipeId,27*Pixel,27*Pixel);
                orePieces[i].gameObject.SetActive(s.Ore>i*2);
                ingotPieces[i].gameObject.SetActive(s.Ingots>i);
            }
            for(int i=0;i<4;i++) {
                bool running=app.Sim.IsRunning((Station)i);
                bars[i].gameObject.SetActive((int)selected==i&&running);
                improvements[i].gameObject.SetActive(s.Levels[i]>=2);
            }
            improvements[4].gameObject.SetActive(app.Sim.WorkshopTier>=1);
            selection.anchoredPosition=new Vector2(Stations[(int)selected].x*Pixel-12,-((Stations[(int)selected].y-96)*Pixel));
            workLight.rectTransform.anchoredPosition=new Vector2(Stations[(int)selected].x*Pixel-300,-((Stations[(int)selected].y-28)*Pixel-250));
            int index=(int)selected;bool busy=app.Sim.IsRunning(selected),canWork=Supplied(index);
            bool striking=selected==Station.Forge&&busy&&!app.Sim.MasterworkActive;
            stationTitle.text=selected==Station.Forge?GameContent.Recipes[app.Sim.State.SelectedRecipe].Name:names[index];
            var recipe=GameContent.Recipes[busy&&index==2?s.PendingRecipe:s.SelectedRecipe];
            string conversion=index==0?"2 ore each trip":index==1?"2 ore → 1 ingot":index==2?app.Sim.RecipeCost(recipe.Id):"Oldest unmarked gear → gold";
            string blocker=app.Sim.WorkBlocker(selected);
            double shownDuration=selected==Station.Forge&&!busy?Math.Max(6,app.Sim.GetDuration(selected)):app.Sim.GetDuration(selected);
            stationInfo.text=conversion+" · "+WorkshopSimulation.Number(shownDuration)+"s";
            actionButton.gameObject.SetActive(!striking);strikeButton.gameObject.SetActive(striking);
            strikeButton.GetComponentInChildren<Text>().text=s.ManualForge?"Strike":"Temper by hand";
            actionButton.interactable=!busy;
            if(selected==Station.Forge&&app.Sim.MasterworkActive)actionButton.interactable=true;
            actionText.text=busy?"Working…":canWork?(index==2?(s.KeepCraftedEquipment?"Forge & keep":"Forge for sale"):index==0?"Mine ore":index==1?"Smelt iron":"Sell next item"):index==1?"Get ore":index==2?(s.Inventory.Count>=120?"Open storage":s.Ingots<recipe.Ingots?"Get ingots":"Find materials"):"Open storage";
            if(selected==Station.Forge&&app.Sim.MasterworkActive){actionText.text="Continue masterwork";stationInfo.text=app.Sim.State.Artisan.Project.Stage+" · Materials reserved";}
            upgradeButton.interactable=true;
            upgradeText.text="Improve";
            marker.parent.gameObject.SetActive(striking);
            quality.text=striking?(Time.unscaledTime<strikeFeedbackUntil?(perfectFeedback?"Perfect · ":"Good · "):"")+WorkshopSimulation.QualityName(s.PendingQuality)+" · "+s.PendingQuality+"/3":"";
            purposeText.text=s.KeepCraftedEquipment?"✓ Keep gear · Change":"For sale · Change";
            purposeButton.interactable=!app.Sim.IsRunning(Station.Forge);
            merchantPause.GetComponentInChildren<Text>().text=s.AutomationPaused[3]?"Resume merchant ›":"Pause merchant ›";
            RefreshWorkflowDock();
            RefreshGuide();LayoutWorkshop();
            RefreshTownLife();
            RefreshDialog();
        }

        public void Notify(string message,bool flash=false)
        {
            toast.text=message;toastUntil=Time.unscaledTime+3.5f;
            float height=ForgeUI.Height(message,972,ForgeUI.Small,FontStyle.Bold)+36;
            toast.rectTransform.sizeDelta=new Vector2(972,height-36);toastPanel.rectTransform.sizeDelta=new Vector2(1020,height);
            toastPanel.rectTransform.anchoredPosition=new Vector2(30,-12);
            toastPanel.gameObject.SetActive(true);toastPanel.transform.SetAsLastSibling();
            if(flash)sparkUntil=sceneTime+.4f;
            Refresh();
        }

        public static string Compact(double n)
        {
            return n>=1e15?WorkshopSimulation.Number(n/1e15)+"Qa":n>=1e12?WorkshopSimulation.Number(n/1e12)+"T":n>=1e9?WorkshopSimulation.Number(n/1e9)+"B":n>=1e6?WorkshopSimulation.Number(n/1e6)+"M":n>=1e4?WorkshopSimulation.Number(n/1e3)+"K":Math.Floor(n).ToString("0");
        }
        public void ShowStrikeFeedback(bool perfect)
        {
            perfectFeedback=perfect;strikeFeedbackUntil=Time.unscaledTime+1.2f;
            animationTimes[2]=FrameDurations[2][0]+FrameDurations[2][1]+FrameDurations[2][2];
            sparkUntil=sceneTime+(perfect?.4f:.24f);
            Refresh();
        }
    }
}
