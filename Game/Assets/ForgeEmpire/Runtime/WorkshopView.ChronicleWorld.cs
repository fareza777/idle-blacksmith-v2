using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        int challengeModifier;
        static string PartySummary(WorkshopSimulation sim)
        {
            string result="";foreach(int id in sim.State.Saga.Party)if(id>=0)result+=(result==""?"":" · ")+GameContent.Heroes[id].Name;return result;
        }
        void ShowParty()
        {
            var sim=app.Sim;var p=Page("party","Guild / Three companions","Your expedition party",ShowParty);
            Landscape("PartyLandscape",p,sim.UnlockedRegion,0,0,1020,470);
            ForgeUI.Fade("PartyLandscapeFoot",p,0,300,1020,170,ForgeUI.Background,true);
            for(int slot=0;slot<3;slot++)
            {
                int id=sim.State.Saga.Party[slot];if(id<0)continue;var hero=sim.State.Campaign.Heroes[id];
                new ChronicleActor("PartyActor"+slot,p,id,hero.Weapon,hero.Armor,new Vector2(180+slot*330,440),330);
            }
            float y=470+Gap;y=Paragraph(p,"PartyPurpose","Up to three companions fight together. Equip each one, then choose skills that answer the guardian's mechanics.",y)+Gap;
            if(sim.SagaInProgress)y=Paragraph(p,"PartyLocked","This party and its equipment are reserved until the expedition result is collected.",y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            for(int slot=0;slot<3;slot++)
            {
                int index=slot,id=sim.State.Saga.Party[slot];string name=id<0?"Empty companion slot":GameContent.Heroes[id].Name+" · "+GameContent.Heroes[id].Role;
                Rule(p,y);y+=Gap;y=Paragraph(p,"PartySlotName"+slot,(slot+1)+". "+name,y,ForgeUI.Subheading,null,true)+12;
                y=Paragraph(p,"PartySlotSkill"+slot,id<0?"Recruit a companion below, then assign them here.":sim.SagaSkillName(slot)+" · "+sim.SagaSkillDescription(slot),y,ForgeUI.Small,ForgeUI.Teal)+16;
                if(id>=0)
                {
                    var hero=sim.State.Campaign.Heroes[id];y=Paragraph(p,"PartySlotGear"+slot,(hero.Weapon==null?"Weapon needed":hero.Weapon.Name)+"\n"+(hero.Armor==null?"No armor assigned":hero.Armor.Name),y,ForgeUI.Small,ForgeUI.Muted)+16;
                }
                var button=WideButton(p,"PartyAssign"+slot,"Choose companion for slot "+(slot+1),y,()=>ShowPartyAssignment(index));button.interactable=!sim.SagaInProgress;y+=ActionHeight+Gap;
            }
            y=AddAction(p,"PartyContinue",sim.SagaInProgress?"Continue the expedition":"Choose an expedition",y,ShowWorld);
            y=Paragraph(p,"RosterHeading","Companions of the valley",y,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            for(int hero=0;hero<GameContent.HeroCount;hero++)
            {
                int id=hero;var definition=GameContent.Heroes[id];bool owned=sim.State.Campaign.Heroes[id].Recruited;
                y=AddAction(p,"Companion"+id,definition.Name+" · "+definition.Role+(owned?"":" · "+definition.Cost+" G"),y,()=>ShowCompanion(id),false);
            }
            EndPage(y);
        }
        void ShowPartyAssignment(int slot)
        {
            var sim=app.Sim;var p=Page("party-assignment","Party slot "+(slot+1),"Choose a companion",()=>ShowPartyAssignment(slot));float y=0;
            for(int hero=0;hero<6;hero++)
            {
                int id=hero;var definition=GameContent.Heroes[id];bool owned=sim.State.Campaign.Heroes[id].Recruited;
                y=Paragraph(p,"AssignmentRole"+id,definition.Name+" · "+definition.Role,y,ForgeUI.Subheading)+12;
                y=Paragraph(p,"AssignmentSkill"+id,definition.Description,y,ForgeUI.Small,ForgeUI.Muted)+16;
                bool assigned=false;for(int i=0;i<3;i++)if(i!=slot&&sim.State.Saga.Party[i]==id)assigned=true;
                var button=WideButton(p,"AssignHero"+id,!owned?"Recruit · "+definition.Cost+" gold":assigned?"Already in another slot":"Assign "+definition.Name,y,()=>{
                    if(!owned){if(app.RecruitHero(id))RebuildCurrentPage();return;}
                    if(sim.SetParty(slot,id)){app.Save();ShowParty();}
                });button.interactable=!sim.SagaInProgress&&!assigned;y+=button.GetComponent<RectTransform>().rect.height+Gap;
            }
            y=AddAction(p,"ClearPartySlot","Leave this slot empty",y,()=>{if(sim.SetParty(slot,-1)){app.Save();ShowParty();}else Notify("Keep at least one companion in the party.");},false);EndPage(y);
        }
        void ShowCompanion(int id)
        {
            var sim=app.Sim;var definition=GameContent.Heroes[id];var hero=sim.State.Campaign.Heroes[id];
            var p=Page("companion",definition.Role,definition.Name,()=>ShowCompanion(id));Landscape("CompanionLandscape",p,sim.UnlockedRegion,0,0,1020,500);
            new ChronicleActor("CompanionActor",p,id,hero.Weapon,hero.Armor,new Vector2(510,480),440);
            ForgeUI.Fade("CompanionFoot",p,0,410,1020,90,ForgeUI.Background,true);
            float y=Paragraph(p,"CompanionStory",definition.Description,528)+Gap;
            y=Paragraph(p,"CompanionLevel","Level "+hero.Level+" · "+hero.Experience+" / "+hero.Level*30+" XP",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            if(!hero.Recruited)y=AddAction(p,"RecruitCompanion","Recruit · "+definition.Cost+" gold",y,()=>{if(app.RecruitHero(id))RebuildCurrentPage();});
            else
            {
                foreach(var slot in new[]{GearSlot.Weapon,GearSlot.Armor,GearSlot.Accessory})
                {
                    var item=slot==GearSlot.Weapon?hero.Weapon:slot==GearSlot.Armor?hero.Armor:hero.Accessory;
                    Rule(p,y);y+=Gap;y=Paragraph(p,"CompanionGear"+slot,slot+" · "+(item==null?"Empty":item.Name),y,ForgeUI.Subheading)+12;
                    if(item!=null)y=Paragraph(p,"CompanionBuild"+slot,WorkshopSimulation.ItemBuildDescription(item),y,ForgeUI.Small,ForgeUI.Teal)+Gap;
                }
                var gear=WideButton(p,"CompanionEquipment","Change "+definition.Name+"'s equipment",y,()=>{if(sim.SelectHero(id)){app.Save();ShowInventory();}});gear.interactable=!sim.SagaInProgress;y+=gear.GetComponent<RectTransform>().rect.height+Gap;
                y=AddAction(p,"CompanionParty","Assign companions to the party",y,ShowParty,false);
            }
            EndPage(y);
        }
        void ShowAtlas()
        {
            var sim=app.Sim;var p=Page("atlas","Chapter "+(sim.UnlockedRegion+1)+" / 6","A living valley",ShowAtlas);
            float y=Paragraph(p,"AtlasGoal",sim.LongTermGoal,0,ForgeUI.Small,ForgeUI.Gold)+Gap;
            y=Paragraph(p,"AtlasParty",PartySummary(sim)+"\nNext: "+sim.NextStep.Title,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            y=BuildLivingAtlas(p,y);
            y=Paragraph(p,"AtlasRegionNavigation","Regions & route previews",y,ForgeUI.Subheading,null,true)+Gap;
            for(int region=0;region<6;region++)
            {
                int id=region;string status=sim.IsFrontierCleared(id)?"Story restored":id<=sim.UnlockedRegion?"Open":"Locked";
                y=AddAction(p,"AtlasRegionButton"+id,(id+1)+". "+GameContent.Regions[id].Name+" · "+status,y,()=>ShowChronicleRegion(id),false);
            }
            y=AddWorldGoals(p,y);
            y=AddAction(p,"AtlasContinue",sim.SagaInProgress?"Continue the expedition":"Continue the chapter",y,ContinueLivingGoal);
            y=AddAction(p,"AtlasPartyButton","Prepare the party",y,ShowParty,false);
            y=AddAction(p,"AtlasTown","Town restoration & people",y,ShowRestoration,false);
            y=AddAction(p,"AtlasMastery","Guardians, legends & legacy",y,ShowLegends,false);
            y=AddAction(p,"AtlasJournal","Story journal",y,()=>ShowQuests(false),false);EndPage(y);
        }
        void ShowChronicleRegion(int region)
        {
            var sim=app.Sim;var d=GameContent.Regions[region];bool open=region<=sim.UnlockedRegion;
            var p=Page("chronicle-region","Adventure / "+d.Weakness+" weakness",d.Name,()=>ShowChronicleRegion(region));
            Landscape("ChronicleRegionLandscape",p,region,0,0,1020,450);
            HandDrawnArt.Person("ChronicleRegionGuardian",p,"boss-"+((region*2)/4),(region*2%4)*4,new Vector2(820,435),320,350);
            float y=Paragraph(p,"ChronicleRegionLore",d.Description,478)+Gap;
            if(!open){y=Paragraph(p,"ChronicleRegionLock",sim.RegionUnlockText(region),y,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;EndPage(y);return;}
            y=Paragraph(p,"ChronicleRegionObjective",sim.GuardianObjective(region),y,ForgeUI.Small,ForgeUI.Gold)+Gap;
            string[] names={"Supply trail","Rescue expedition",d.BossName,ChronicleContent.Bosses[region*2+1].Name};
            string[] descriptions={"3 rooms · Safe material supply · One optional camp","4 rooms · Rescue townsfolk · Stronger enemy formations","5 rooms · Three-phase guardian · Opens the next chapter","5 rooms · Legendary guardian · Earn mastery for challenges"};
            for(int route=0;route<4;route++)
            {
                int id=route;Rule(p,y);y+=Gap;y=Paragraph(p,"ChronicleRouteName"+id,names[id],y,ForgeUI.Subheading,null,true)+12;
                y=Paragraph(p,"ChronicleRouteHelp"+id,descriptions[id],y,ForgeUI.Small,ForgeUI.Muted)+16;
                var reward=sim.PreviewRouteRewards(region,id);string blocker=sim.ExpeditionBlocker(region,id);
                y=Paragraph(p,"ChronicleRouteReward"+id,"Base victory: "+reward.Gold+" gold · "+reward.Material+" "+WorkshopSimulation.MaterialName(region).ToLowerInvariant()+" · "+reward.Experience+" XP"+(id>=2?"\nGuardian victory: mastery; collect the result to keep its reward.":""),y,ForgeUI.Small,ForgeUI.Gold)+16;
                if(blocker.Length>0)y=Paragraph(p,"ChronicleRouteBlocker"+id,blocker,y,ForgeUI.Small,ForgeUI.Orange)+16;
                y=AddAction(p,"ChronicleRoute"+id,"Prepare · "+names[id],y,()=>ShowSagaDeparture(region,id),id==0);
            }
            y=AddAction(p,"ChronicleRegionTown","Visit "+WorkshopSimulation.Restorations[region].Npc,y,ShowRestoration,false);EndPage(y);
        }
        void ShowSagaDeparture(int region,int route)
        {
            var sim=app.Sim;var p=Page("saga-departure","Party / Preparation",GameContent.Regions[region].Name,()=>ShowSagaDeparture(region,route));
            Landscape("DepartureLandscape",p,region,0,0,1020,380);float y=Paragraph(p,"DepartureParty",PartySummary(sim),408,ForgeUI.Subheading,ForgeUI.Teal,true)+Gap;
            y=Paragraph(p,"DepartureHow","Choose each room on the road. Injuries carry forward; a shelter can restore the party. Equipment stays with this party until you return.",y)+Gap;
            y=Paragraph(p,"DepartureElement",GameContent.Regions[region].Weakness+" weapons deal 30% more damage here (60% for Kael). Matching armor reduces incoming damage by 20% (30% for Kael).",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            if(sim.SagaSynergyHint.Length>0)y=Paragraph(p,"DepartureSynergy",sim.SagaSynergyHint,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            var reward=sim.PreviewRouteRewards(region,route);
            y=Paragraph(p,"DepartureRewards","Base return: "+reward.Gold+" gold · "+reward.Material+" "+WorkshopSimulation.MaterialName(region).ToLowerInvariant()+"\nBranch choices and challenge rank can add to this reward.",y,ForgeUI.Small,ForgeUI.Gold)+Gap;
            if(route>=2)y=Paragraph(p,"DepartureMechanic",ChronicleContent.Bosses[region*2+(route==3?1:0)].Counter,y,ForgeUI.Body,ForgeUI.Teal)+Gap;
            y=Paragraph(p,"ChallengeHeading","Challenge rank "+challengeModifier+" / 3 · "+sim.State.Saga.Mastery+" mastery",y,ForgeUI.Small,ForgeUI.Gold)+16;
            y=AddAction(p,"ChangeChallenge","Change challenge rank",y,()=>{challengeModifier=(challengeModifier+1)%4;RebuildCurrentPage();},false);
            string blocker=sim.ExpeditionBlocker(region,route);if(sim.State.Saga.Mastery<challengeModifier*3)blocker="Earn "+challengeModifier*3+" mastery to select this challenge rank.";
            if(blocker!="")y=Paragraph(p,"SagaDepartureBlocker",blocker,y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            var depart=WideButton(p,"SagaDepart","Depart with this party",y,()=>{if(sim.StartSaga(region,route,challengeModifier)){app.Save();app.Play("click");ShowSaga();}else Notify("Check the party and collect any previous expedition result.");});depart.interactable=blocker=="";y+=depart.GetComponent<RectTransform>().rect.height+Gap;
            y=AddAction(p,"SagaPrepareParty","Change party or equipment",y,ShowParty,false);EndPage(y);
        }
    }
}
