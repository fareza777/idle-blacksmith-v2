using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        sealed class PageEntry
        {
            public string Kind;
            public Action Render;
            public float Scroll;
        }
        readonly List<PageEntry> pageHistory=new List<PageEntry>();
        PageEntry currentPage;
        ScrollRect pageScroll;
        bool rebuildingPage,pageRebuildRequested;
        int primaryTab;
        GameObject lastFocusedControl;

        static void Place(RectTransform rect,float x,float y,float w,float h)
        {
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
            if(rect.GetComponent<Button>())
            {
                var caption=rect.Find("Label") as RectTransform;
                if(caption){caption.anchoredPosition=new Vector2(24,0);caption.sizeDelta=new Vector2(w-48,h);}
            }
        }
        float Gap { get { return 28*ForgeUI.TextScale; } }
        float ActionHeight { get { return Mathf.Max(132,112*ForgeUI.TextScale); } }
        void RefreshGuide()
        {
            var sim=app.Sim;
            questTitle.text=sim.CampaignComplete?"Mastery · "+sim.GuildRank+"  ›":"Chapter "+(sim.UnlockedRegion+1)+" · "+sim.ChapterProgress+" / 7 milestones  ›";
            guideHeight=Mathf.Max(90,ForgeUI.Height(questTitle.text,810,ForgeUI.Small));
            Place(questTitle.rectTransform,54,0,810,guideHeight);
            guidePanel.sizeDelta=new Vector2(866,guideHeight);
            guideAction.GetComponent<RectTransform>().sizeDelta=new Vector2(866,guideHeight);
        }
        void LayoutWorkshop()
        {
            if(!contextDock||!stationInfo||!guidePanel)return;
            bool forging=selected==Station.Forge&&app.Sim.IsRunning(Station.Forge)&&(!journeyFollowing||app.Sim.NextStep.Action==GuideAction.Forge);
            bool choosePurpose=!journeyFollowing&&selected==Station.Forge&&!forging;
            bool merchant=!journeyFollowing&&selected==Station.Market&&app.Sim.State.Automated[3];
            bool recipes=!journeyFollowing&&selected==Station.Forge;
            upgradeButton.gameObject.SetActive(!journeyFollowing);
            recipePicker.gameObject.SetActive(recipes);
            float headingHeight=ForgeUI.Height(stationTitle.text,recipes?810:996,ForgeUI.Subheading);
            Place(stationTitle.rectTransform,0,0,recipes?810:996,headingHeight);
            float y=headingHeight+4;
            float infoHeight=ForgeUI.Height(stationInfo.text,recipes?810:996,ForgeUI.Small);
            Place(stationInfo.rectTransform,0,y,recipes?810:996,infoHeight);y+=infoHeight+14;
            purposeButton.gameObject.SetActive(choosePurpose);
            if(choosePurpose){Place(purposeButton.GetComponent<RectTransform>(),-24,y,810,86*ForgeUI.TextScale);y+=86*ForgeUI.TextScale+4;}
            merchantPause.gameObject.SetActive(merchant);
            if(merchant){Place(merchantPause.GetComponent<RectTransform>(),0,y,996,86*ForgeUI.TextScale);y+=86*ForgeUI.TextScale+4;}
            quality.gameObject.SetActive(forging);
            if(forging)
            {
                float qh=ForgeUI.Height(quality.text,996,ForgeUI.Small);
                Place(quality.rectTransform,0,y,996,qh);y+=qh+8;
                Place(marker.parent as RectTransform,6,y,984,58);y+=78;
            }
            float actionWidth=journeyFollowing?996:690;
            float actionHeight=Mathf.Max(136,ForgeUI.Height(actionText.text,actionWidth-48,ForgeUI.Body,FontStyle.Bold)+34);
            float improveHeight=ForgeUI.Height(upgradeText.text,234,ForgeUI.Small,FontStyle.Bold)+24;
            actionHeight=Mathf.Max(actionHeight,improveHeight);
            Place(actionButton.GetComponent<RectTransform>(),0,y,actionWidth,actionHeight);
            Place(strikeButton.GetComponent<RectTransform>(),0,y,actionWidth,actionHeight);
            Place(upgradeButton.GetComponent<RectTransform>(),714,y,282,actionHeight);
            dockHeight=y+actionHeight+30;
            float surfaceHeight=root.rect.height-NavHeight;
            workshopViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,surfaceHeight);
            var nav=root.Find("Navigation") as RectTransform;if(nav)nav.anchoredPosition=new Vector2(0,-surfaceHeight);
            var crew=workshopContent.Find("TabCREW");if(crew)crew.gameObject.SetActive(surfaceHeight>=1450);
            workshopContent.sizeDelta=new Vector2(1080,surfaceHeight);workshopContent.anchoredPosition=Vector2.zero;
            Place(worldViewport,0,0,1080,surfaceHeight);
            Place(contextDock,42,surfaceHeight-dockHeight,996,dockHeight);
            Place(dockProgress,0,dockHeight-15,Mathf.Max(1,dockProgress.rect.width),4);
            Place(guidePanel,40,headerHeight+2,866,guideHeight);
            Place(topVeil.rectTransform,0,0,1080,headerHeight+guideHeight+180);
            Place(bottomVeil.rectTransform,0,surfaceHeight-dockHeight-180,1080,dockHeight+180);
            workshopScroll.vertical=false;
            scene.anchoredPosition=new Vector2(Mathf.Clamp(scene.anchoredPosition.x,-1080*(SceneZoom-1),0),Mathf.Clamp(scene.anchoredPosition.y,0,Mathf.Max(0,scene.rect.height*SceneZoom-worldViewport.rect.height)));
        }
        void ContextAction()
        {
            if(journeyFollowing){ExecuteJourneyAction();return;}
            if(Supplied((int)selected)){app.Work(selected);return;}
            if(selected==Station.Smelter){FocusStation(Station.Mine);return;}
            if(selected==Station.Forge)
            {
                if(app.Sim.State.Inventory.Count>=120){ShowInventory();return;}
                if(app.Sim.State.Ingots<GameContent.Recipes[app.Sim.State.SelectedRecipe].Ingots){FocusStation(app.Sim.State.Ore>=2?Station.Smelter:Station.Mine);return;}
                ShowWorld();return;
            }
            ShowInventory();
        }
        void OpenPrimary(int tab,Action open)
        {
            if(app.Sim.State.TutorialStep==0){app.Sim.State.TutorialStep=1;app.Save();}
            app.Play("click");pageHistory.Clear();currentPage=null;primaryTab=tab;
            if(tab==0)CloseModal();else open();
            RefreshNavigation();
        }
        void RefreshNavigation()
        {
            for(int i=0;i<3;i++)if(navFills[i])
            {
                navFills[i].color=Color.clear;
                navTexts[i].color=i==primaryTab?ForgeUI.Gold:ForgeUI.Muted;
                var glyph=navFills[i].GetComponentInChildren<RawImage>();if(glyph)glyph.color=i==primaryTab?ForgeUI.Gold:ForgeUI.Muted;
            }
        }
        RectTransform Page(string kind,string section,string title,Action render)
        {
            if(currentPage!=null&&pageScroll)currentPage.Scroll=pageScroll.content.anchoredPosition.y;
            if(!rebuildingPage&&currentPage!=null)pageHistory.Add(currentPage);
            DestroyPage();
            currentPage=new PageEntry{Kind=kind,Render=render};modalKind=kind;
            if(kind=="heroes"||kind=="inventory"||kind=="recruit")primaryTab=1;
            else if(kind=="world"||kind=="region"||kind=="battle")primaryTab=2;
            else if(kind!="settings")primaryTab=0;
            modal=ForgeUI.Fill("Page",root,0,0,1080,root.rect.height-NavHeight,ForgeUI.Background).rectTransform;
            modal.GetComponent<Image>().raycastTarget=true;
            BeginPageMotion();
            var workshopInput=workshopViewport.GetComponent<CanvasGroup>()??workshopViewport.gameObject.AddComponent<CanvasGroup>();
            workshopInput.interactable=false;workshopInput.blocksRaycasts=false;
            int headingSize=root.rect.height<1600?ForgeUI.Subheading:ForgeUI.Heading;
            float titleHeight=ForgeUI.Height(title,834,headingSize,FontStyle.Bold);
            float eyebrowHeight=ForgeUI.Height(section,834,ForgeUI.Small);
            float header=Mathf.Max(210,52+eyebrowHeight+titleHeight);
            var back=ForgeUI.Medallion("PageBack",modal,30,32,112,GoBack,"back");
            back.navigation=new Navigation{mode=Navigation.Mode.Automatic};
            ForgeUI.Label("PageSection",modal,section,204,20,834,eyebrowHeight,ForgeUI.Small,ForgeUI.Gold);
            ForgeUI.Label("PageHeading",modal,title,204,32+eyebrowHeight,834,titleHeight,headingSize,ForgeUI.Cream,FontStyle.Bold);
            ForgeUI.Ornament(modal,30,header-4,1020);
            var viewport=ForgeUI.Fill("PageViewport",modal,30,header+20,1020,Mathf.Max(100,modal.rect.height-header-40),Color.clear);
            viewport.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            pageScroll=viewport.gameObject.AddComponent<ScrollRect>();pageScroll.horizontal=false;pageScroll.viewport=viewport.rectTransform;
            pageScroll.movementType=ScrollRect.MovementType.Clamped;pageScroll.scrollSensitivity=110;
            modalBody=ForgeUI.Rect("PageContent",viewport.transform,0,0,1020,1);pageScroll.content=modalBody;
            RefreshNavigation();
            EventSystem.current.SetSelectedGameObject(back.gameObject);
            return modalBody;
        }
        void EndPage(float height)
        {
            modalBody.sizeDelta=new Vector2(1020,Mathf.Max(height+36,pageScroll.viewport.rect.height));
            RefreshDialog();
        }
        float Paragraph(Transform parent,string name,string text,float y,int size=ForgeUI.Body,Color? color=null,bool bold=false,float x=0,float width=1020)
        {
            float h=ForgeUI.Height(text,width,size,bold?FontStyle.Bold:FontStyle.Normal);
            ForgeUI.Label(name,parent,text,x,y,width,h,size,color??ForgeUI.Cream,bold?FontStyle.Bold:FontStyle.Normal);
            return y+h;
        }
        Button WideButton(Transform parent,string name,string text,float y,Action action,bool primary=true)
        {
            return ForgeUI.Button(name,parent,text,0,y,1020,Mathf.Max(ActionHeight,ForgeUI.Height(text,972,ForgeUI.Body,FontStyle.Bold)+36),primary?ForgeUI.Gold:ForgeUI.Surface,primary?ForgeUI.Background:ForgeUI.Cream,action);
        }
        void DestroyPage()
        {
            modalRefresh.Clear();pageRebuildRequested=false;
            if(modal){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}
            pageScroll=null;modalKind=null;
            pageFade=null;
            lastFocusedControl=null;
            if(workshopViewport){var input=workshopViewport.GetComponent<CanvasGroup>();if(input){input.interactable=true;input.blocksRaycasts=true;}}
        }

        void UpdatePageFocus()
        {
            if(!pageScroll||!EventSystem.current)return;
            var selected=EventSystem.current.currentSelectedGameObject;
            if(selected==lastFocusedControl)return;
            lastFocusedControl=selected;
            if(!selected||!selected.transform.IsChildOf(pageScroll.content))return;
            var rect=selected.GetComponent<RectTransform>();if(!rect)return;
            Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(pageScroll.viewport,rect);
            var visible=pageScroll.viewport.rect;
            float delta=bounds.max.y>visible.yMax?visible.yMax-bounds.max.y:bounds.min.y<visible.yMin?visible.yMin-bounds.min.y:0;
            if(Mathf.Abs(delta)<1)return;
            pageScroll.StopMovement();
            pageScroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(pageScroll.content.anchoredPosition.y+delta,0,Mathf.Max(0,pageScroll.content.rect.height-visible.height)));
        }
        public void CloseModal()
        {
            DestroyPage();pageHistory.Clear();currentPage=null;primaryTab=0;
            RefreshNavigation();
            if(guideAction&&EventSystem.current)EventSystem.current.SetSelectedGameObject(guideAction.gameObject);
        }
        void GoBack()
        {
            if(pageHistory.Count==0){CloseModal();return;}
            var entry=pageHistory[pageHistory.Count-1];pageHistory.RemoveAt(pageHistory.Count-1);
            rebuildingPage=true;
            try{entry.Render();RestoreScroll(entry.Scroll);}finally{rebuildingPage=false;}
        }
        void RestoreScroll(float offset)
        {
            if(!pageScroll)return;
            Canvas.ForceUpdateCanvases();pageScroll.StopMovement();
            pageScroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(offset,0,Mathf.Max(0,pageScroll.content.rect.height-pageScroll.viewport.rect.height)));
        }
        void RequestPageRebuild(){pageRebuildRequested=true;}
        void RebuildCurrentPage()
        {
            if(currentPage==null)return;
            var entry=currentPage;float offset=pageScroll?pageScroll.content.anchoredPosition.y:0;
            rebuildingPage=true;
            try{entry.Render();RestoreScroll(offset);}finally{rebuildingPage=false;}
        }
        void RefreshDialog()
        {
            for(int i=0;i<modalRefresh.Count;i++)modalRefresh[i]();
            if(pageRebuildRequested){pageRebuildRequested=false;RebuildCurrentPage();}
        }
        void RebuildInterface()
        {
            bool following=journeyFollowing;
            var entry=currentPage;var history=new List<PageEntry>(pageHistory);
            float offset=pageScroll?pageScroll.content.anchoredPosition.y:0;
            foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            root=null;modal=null;worldViewport=null;workshopContent=null;contextDock=null;toast=null;currentPage=null;pageScroll=null;
            modalRefresh.Clear();pageHistory.Clear();Build(app);
            journeyFollowing=following;
            if(entry!=null){rebuildingPage=true;try{entry.Render();RestoreScroll(offset);}finally{rebuildingPage=false;}}
            pageHistory.Clear();pageHistory.AddRange(history);
            Refresh();
        }
    }
}
