using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        readonly List<Action> modalRefresh=new List<Action>();
        RectTransform modalBody;

        float AddAction(Transform parent,string name,string text,float y,Action action,bool primary=true)
        {
            var button=WideButton(parent,name,text,y,action,primary);
            return y+button.GetComponent<RectTransform>().rect.height+Gap;
        }
        void Rule(Transform parent,float y){ForgeUI.Fill("SectionRule",parent,0,y,1020,3,ForgeUI.Line);}

        void ShowStationDetails()
        {
            var station=selected;int index=(int)station;
            var p=Page("station","Workshop · Level "+app.Sim.State.Levels[index],names[index],ShowStationDetails);
            if(index==1)HandDrawnArt.Prop("WorkerPortrait",p,1,370,0,280,280);
            else HandDrawnArt.Person("WorkerPortrait",p,"workers",index==0?0:index==2?6:12,new Vector2(510,280),280,280);
            float y=Paragraph(p,"StationDescription",index==0?"Mine iron ore for your workshop.":index==1?"Smelt 2 ore into 1 iron ingot.":index==2?"Forge equipment from the recipe you choose.":"Sell unmarked equipment. Gear you keep stays safe.",310)+Gap;
            y=Paragraph(p,"UpgradeForecast","Work time\n"+WorkshopSimulation.Number(app.Sim.GetDuration(station))+"s  →  "+WorkshopSimulation.Number(app.Sim.NextDuration(station))+"s",y,ForgeUI.Subheading,ForgeUI.Teal)+Gap;
            var upgrade=WideButton(p,"ConfirmUpgrade","",y,()=>{app.Upgrade(station);RebuildCurrentPage();});
            modalRefresh.Add(()=>{
                bool maximum=app.Sim.State.Levels[index]>=25;
                upgrade.interactable=!maximum&&app.Sim.State.Gold>=app.Sim.GetUpgradeCost(station);
                upgrade.GetComponentInChildren<Text>().text=maximum?"Fully improved":"Improve · "+Math.Ceiling(app.Sim.GetUpgradeCost(station))+" gold";
            });y+=ActionHeight+Gap;
            y=AddAction(p,"StationCrew","Manage this worker",y,ShowCrew,false);
            EndPage(y);
        }

        public void ShowWelcome()
        {
            var p=Page("welcome","A small fire. A new beginning.","Welcome, Blacksmith",ShowWelcome);
            Landscape("WelcomeScene",p,0,0,0,1020,300);
            float y=Paragraph(p,"WelcomeGoal","Forge the equipment that brings this valley back to life.",332,ForgeUI.Subheading,null,true)+Gap;
            y=Paragraph(p,"WelcomeSteps","Your first goal: arm Rowan.\nMine → smelt → forge → equip.\nThen scout the road, rescue your neighbors and rebuild your workshop.",y)+Gap;
            y=Paragraph(p,"KeepExplanation","Your first creations are kept for heroes and town requests. You choose what to sell.",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            y=AddAction(p,"Begin","Begin · Arm Rowan",y,()=>{app.Sim.State.TutorialStep=1;app.Save();app.Play("forge");ResumeJourney();});
            EndPage(y);
        }

        public void ShowCrew()
        {
            var p=Page("crew","Workshop / Automation","Your crew",ShowCrew);
            float y=Paragraph(p,"CrewExplanation","Hire once. Workers start new jobs when materials are ready. Pausing lets the current job finish.",0)+Gap;
            y=Paragraph(p,"CrewProjectExplanation","Your blacksmith pauses when guided equipment or an order is ready. Quality orders are finished by hand, so rare materials stay safe.",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            y=Paragraph(p,"MerchantExplanation","The merchant sells only unmarked stock. Keep equipment in storage to reserve it for heroes or requests.",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            string[] names={"Bram · Miner","Mira · Smelter","Orin · Blacksmith","Sable · Merchant"};
            string[] jobs={"Brings back 2 ore per trip.","Turns 2 ore into 1 iron ingot.","Makes the selected recipe when all its materials are available.","Sells your oldest unmarked equipment. Kept items stay in storage."};
            for(int i=0;i<4;i++)
            {
                int id=i;
                y=Paragraph(p,"WorkerName"+i,names[i],y,ForgeUI.Subheading,null,true)+12;
                y=Paragraph(p,"WorkerJob"+i,jobs[i],y,ForgeUI.Small,ForgeUI.Muted)+20;
                var hire=WideButton(p,"Hire"+(Station)i,"",y,()=>{if(app.Sim.State.Automated[id])app.ToggleAutomation((Station)id);else app.Hire((Station)id);});
                y+=ActionHeight+Gap;
                modalRefresh.Add(()=>{
                    bool owned=app.Sim.State.Automated[id];
                    hire.interactable=owned||app.Sim.State.Gold>=app.Sim.GetAutomationCost((Station)id);
                    hire.GetComponentInChildren<Text>().text=owned?(app.Sim.State.AutomationPaused[id]?"Resume worker":"Pause worker"):"Hire · "+app.Sim.GetAutomationCost((Station)id)+" gold";
                    hire.GetComponent<Image>().color=owned?Color.clear:ForgeUI.Gold;
                });
                Rule(p,y);y+=Gap;
            }
            EndPage(y);
        }

        public void ShowOrders()
        {
            var p=Page("orders","Quests / Trade","Trading commissions",ShowOrders);
            float y=Paragraph(p,"CommissionIntro","Sell equipment to supply the town guard. Every sale counts toward the next commission.",0)+Gap;
            var progress=ForgeUI.Label("OrderProgress",p,"",0,y,1020,148*ForgeUI.TextScale,ForgeUI.Subheading,ForgeUI.Cream,FontStyle.Bold);
            y+=148*ForgeUI.TextScale+Gap;
            ForgeUI.Fill("OrderTrack",p,0,y,1020,20,ForgeUI.Panel);
            var bar=ForgeUI.Fill("OrderFill",p,0,y,1,20,ForgeUI.Teal).rectTransform;y+=48+Gap;
            var claim=WideButton(p,"ClaimOrder","",y,()=>app.ClaimOrder());y+=ActionHeight+Gap;
            y=Paragraph(p,"CommissionHint","Commissions award gold. Quest and expedition rewards build your reputation.",y,ForgeUI.Small,ForgeUI.Muted);
            modalRefresh.Add(()=>{
                progress.text=app.Sim.State.TotalSold+" / "+app.Sim.OrderTarget+" equipment sold";
                bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1020*Mathf.Clamp01(app.Sim.State.TotalSold/(float)app.Sim.OrderTarget));
                claim.interactable=app.Sim.OrderReady;
                claim.GetComponentInChildren<Text>().text=(app.Sim.OrderReady?"Claim ":"Reward: ")+Compact(app.Sim.OrderReward)+" gold";
            });
            EndPage(y);
        }

        public void ShowInventory()
        {
            var p=Page("inventory","Heroes / Equipment","Your storage",ShowInventory);
            var sim=app.Sim;var state=sim.State;int heroIndex=state.Campaign.SelectedHero;
            string hero=GameContent.Heroes[heroIndex].Name;
            float y=Paragraph(p,"StorageHero","Equipment for "+hero,0,ForgeUI.Subheading,null,true)+12;
            y=Paragraph(p,"StockCapacity",state.Inventory.Count+" / 120 slots · "+sim.HeroPower+" Power · "+sim.HeroGuard+" Guard",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=Paragraph(p,"StockPurpose","Kept equipment stays safe from the merchant.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            if(sim.ActiveCommission!=null)y=AddAction(p,"InventoryOrder","Guild order · "+sim.CommissionProgress+" / "+sim.ActiveCommission.Quantity+" ready",y,ShowGuildOrders,false);
            if(state.Campaign.Battle.Active)
                y=Paragraph(p,"EquipmentLocked","Equipment is locked until this expedition ends. You can still keep or sell stored gear.",y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            var displayed=new List<CraftedItem>(state.Inventory);
            if(displayed.Count==0)
            {
                EquipmentIcon("EmptyStock",p,0,400,y,220);y+=246;
                y=Paragraph(p,"EmptyStockText","No equipment in storage yet. Forge a weapon, then return here to equip your hero.",y)+Gap;
                y=AddAction(p,"InventoryRecipes","Choose a recipe",y,ShowRecipes);
            }
            for(int i=0;i<displayed.Count;i++)
            {
                var item=displayed[i];var recipe=GameContent.Recipes[item.RecipeId];
                Rule(p,y);y+=Gap;
                float nameH=ForgeUI.Height(item.Name,842,ForgeUI.Subheading,FontStyle.Bold);
                EquipmentIcon("ItemIcon"+i,p,item.RecipeId,0,y,144);
                ForgeUI.Label("ItemName"+i,p,item.Name,174,y,846,nameH,ForgeUI.Subheading,ForgeUI.Cream,FontStyle.Bold);
                string meta=WorkshopSimulation.QualityName(item.Quality)+" ("+item.Quality+"/3) · "+recipe.Element+" "+recipe.Slot.ToString().ToLowerInvariant()+(sim.IsCommissionReserved(item)?" · Reserved for order":"");
                float metaH=ForgeUI.Height(meta,846,ForgeUI.Small);
                ForgeUI.Label("ItemMeta"+i,p,meta,174,y+nameH+10,846,metaH,ForgeUI.Small,ForgeUI.Teal);
                y+=Mathf.Max(144,nameH+metaH+10)+20;
                y=Paragraph(p,"ItemStats"+i,sim.ItemPower(item)+" Power · "+sim.ItemGuard(item)+" Guard",y,ForgeUI.Body,ForgeUI.Cream)+12;
                int power=sim.EquipmentDelta(i,true),guard=sim.EquipmentDelta(i,false);
                y=Paragraph(p,"ItemCompare"+i,hero+": "+Signed(power)+" Power · "+Signed(guard)+" Guard",y,ForgeUI.Small,power+guard>=0?ForgeUI.Teal:ForgeUI.Orange)+20;
                var equip=ForgeUI.Button("InventoryEquip"+i,p,"Equip",0,y,480,ActionHeight,ForgeUI.Gold,ForgeUI.Background,()=>{
                    int index=state.Inventory.IndexOf(item);
                    if(index>=0&&app.Equip(index))RebuildCurrentPage();
                });
                equip.interactable=!state.Campaign.Battle.Active;
                ForgeUI.Button("InventoryProtect"+i,p,item.Protected?"Kept":"Keep",504,y,240,ActionHeight,item.Protected?ForgeUI.Teal:ForgeUI.Surface,item.Protected?ForgeUI.Background:ForgeUI.Cream,()=>{
                    int index=state.Inventory.IndexOf(item);
                    if(sim.TryProtectItem(index,!item.Protected))
                    {
                        if(!item.Protected&&sim.IsJourneyReserved(item)){journeyFollowing=false;sim.State.ReserveJourneyGear=false;}
                        app.Save();RebuildCurrentPage();
                    }
                },ForgeUI.Small);
                var sell=ForgeUI.Button("InventorySell"+i,p,"Sell\n"+Compact(sim.SaleValue(item))+" G",768,y,252,ActionHeight,ForgeUI.Surface,ForgeUI.Cream,()=>{
                    int index=state.Inventory.IndexOf(item);
                    if(sim.TrySellItem(index)){app.Save();app.Play("coin");RebuildCurrentPage();}
                },ForgeUI.Small);
                sell.interactable=!item.Protected&&!sim.IsCommissionReserved(item)&&!sim.IsJourneyReserved(item)&&!sim.IsRunning(Station.Market);
                y+=ActionHeight+Gap;
                modalRefresh.Add(()=>{
                    equip.interactable=!state.Campaign.Battle.Active;
                    sell.interactable=state.Inventory.Contains(item)&&!item.Protected&&!sim.IsCommissionReserved(item)&&!sim.IsJourneyReserved(item)&&!sim.IsRunning(Station.Market);
                });
            }
            if(displayed.Count>0||state.Campaign.Heroes[heroIndex].Weapon!=null)y=AddAction(p,"InventoryContinue","Continue to adventure ›",y,ShowWorld,false);
            modalRefresh.Add(()=>{
                if(state.Inventory.Count!=displayed.Count){RequestPageRebuild();return;}
                for(int i=0;i<displayed.Count;i++)if(!ReferenceEquals(displayed[i],state.Inventory[i])){RequestPageRebuild();break;}
            });
            EndPage(y);
        }
        static string Signed(int n){return n>0?"+"+n:n.ToString();}

        public void ShowSettings()
        {
            var p=Page("settings","Preferences","Make it comfortable",ShowSettings);
            float y=Paragraph(p,"TextSizeTitle","Text size",0,ForgeUI.Subheading,null,true)+Gap;
            string[] labels={"100%","125%","150%"};
            for(int i=0;i<3;i++)
            {
                int size=i;bool selectedSize=app.Sim.State.TextSize==i;
                ForgeUI.Button("TextSize"+i,p,labels[i],i*348,y,324,ActionHeight,selectedSize?ForgeUI.Gold:ForgeUI.Surface,selectedSize?ForgeUI.Background:ForgeUI.Cream,()=>{
                    app.Sim.State.TextSize=size;app.Save();RebuildInterface();
                });
            }
            y+=ActionHeight+Gap;
            y=Paragraph(p,"TextSizeHint","Text and rows grow together. Scroll any page to reach its remaining actions.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=SettingsToggle(p,"MotionToggle","Reduced motion",y,()=>app.Sim.State.ReducedMotion,()=>app.Sim.State.ReducedMotion=!app.Sim.State.ReducedMotion);
            y=SettingsToggle(p,"SoundToggle","Sound effects",y,()=>app.Sim.State.SoundEnabled,()=>app.Sim.State.SoundEnabled=!app.Sim.State.SoundEnabled);
            y=SettingsToggle(p,"MusicToggle","Ambient music",y,()=>app.Sim.State.MusicEnabled,()=>app.Sim.State.MusicEnabled=!app.Sim.State.MusicEnabled);
            y=SettingsToggle(p,"HapticsToggle","Touch vibration",y,()=>app.Sim.State.HapticsEnabled,()=>app.Sim.State.HapticsEnabled=!app.Sim.State.HapticsEnabled);
            y=Paragraph(p,"SaveHint","Progress is saved automatically. Running jobs and hired workers can progress for up to 8 hours while you are away.",y,ForgeUI.Small,ForgeUI.Muted);
            EndPage(y);
        }
        float SettingsToggle(Transform parent,string name,string label,float y,Func<bool> read,Action toggle)
        {
            float height=Mathf.Max(144,ForgeUI.Height(label,720,ForgeUI.Body)+40);
            var control=ForgeUI.Button(name,parent,label,0,y,1020,height,Color.clear,ForgeUI.Cream,()=>{toggle();app.Save();app.UpdateAudio();RefreshDialog();});
            var caption=control.GetComponentInChildren<Text>();caption.alignment=TextAnchor.MiddleLeft;caption.rectTransform.sizeDelta=new Vector2(720,height);
            var indicator=ForgeUI.Label("ToggleValue",control.transform,"",780,0,240,height,ForgeUI.Body,ForgeUI.Teal,FontStyle.Bold,TextAnchor.MiddleRight);
            ForgeUI.Fill("ToggleRule",control.transform,0,height-2,1020,2,new Color(.4f,.4f,.3f,.25f));
            modalRefresh.Add(()=>{
                indicator.text=read()?"On  ●":"Off  ○";indicator.color=read()?ForgeUI.Teal:ForgeUI.Muted;
            });
            return y+height+Gap;
        }
        public void ShowOffline(double earned,double away)
        {
            var p=Page("offline","Welcome back","The fire is still warm",()=>ShowOffline(earned,away));
            float y=Paragraph(p,"AwayTime","You were away for "+Math.Floor(away/60)+" minutes.",0)+Gap;
            y=Paragraph(p,"OfflineGold","+ "+Compact(earned)+" gold",y,ForgeUI.Heading,ForgeUI.Gold,true)+Gap;
            y=Paragraph(p,"OfflineDescription","Running jobs and hired workers continued for up to 8 hours. Your expedition also progressed. These rewards are already saved.",y)+Gap;
            y=AddAction(p,"CollectOffline","Return to the workshop",y,CloseModal);
            EndPage(y);
        }
    }
}
