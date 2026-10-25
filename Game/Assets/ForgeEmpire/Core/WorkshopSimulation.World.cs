using System;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        public static readonly RestorationDefinition[] Restorations={
            new RestorationDefinition("Charcoal Bridge","Elowen","Returning apprentices help Pell rebuild the charcoal road.","Mining is 6% faster per restoration level."),
            new RestorationDefinition("Truebell Ferry","Oren","A working ferry carries supplies through the living mist.","Expeditions return 1 extra material per restoration level."),
            new RestorationDefinition("Mountain Beacon","Sera","The rescued beacon tender trains your companions.","Party maximum health rises by 6% per level."),
            new RestorationDefinition("Cooling Aqueduct","Bram","The cooling crew brings a steady flow back to the furnace.","Routine forging is 6% faster per level."),
            new RestorationDefinition("Valley Archive","Nessa","Returning scribes publish the stories of your equipment.","Guild commissions pay 8% more gold per level."),
            new RestorationDefinition("Dawn Observatory","Aster","The last morning watch stabilizes the observatory lens.","Party damage rises by 5% per restoration level.")
        };
        public void NormalizeWorld()
        {
            if(State.World==null)State.World=new WorldState();var w=State.World;
            w.Projects=NormalizeCounters(w.Projects,6);w.Trust=NormalizeCounters(w.Trust,6);w.Conversations=NormalizeCounters(w.Conversations,12);w.LegendaryClaims=ResizeCampaign(w.LegendaryClaims,12);
            for(int i=0;i<6;i++){w.Projects[i]=State.Campaign.QuestClaims[GameContent.SideQuestId(i,1)]?Math.Min(3,w.Projects[i]):0;w.Trust[i]=Math.Min(4,w.Trust[i]);}
            for(int i=0;i<12;i++)w.Conversations[i]=Math.Min(2,w.Conversations[i]);
            w.ChoicesMade=Clamp(w.ChoicesMade,0,MaxCounter);w.RoomsVisited=Clamp(w.RoomsVisited,0,MaxCounter);w.SkillsUsed=Clamp(w.SkillsUsed,0,MaxCounter);w.RunsStarted=Clamp(w.RunsStarted,0,MaxCounter);
        }
        public int RestorationGoldCost(int region){return region>=0&&region<6?(int)Math.Ceiling(90*(region+1)*(State.World.Projects[region]+1)*(1-State.World.Trust[region]*.05)):0;}
        public int RestorationMaterialCost(int region){return region>=0&&region<6?4+State.World.Projects[region]*3:0;}
        public bool TryRestoreProject(int region)
        {
            if(region<0||region>=6||region>UnlockedRegion||State.World.Projects[region]>=3||!State.Campaign.QuestClaims[GameContent.SideQuestId(region,1)])return false;
            int gold=RestorationGoldCost(region),material=RestorationMaterialCost(region);
            if(State.Gold<gold||State.Campaign.Materials[region]<material)return false;
            State.Gold-=gold;State.Campaign.Materials[region]-=material;State.World.Projects[region]++;return true;
        }
        public bool CanTalk(int region,int topic)
        {
            return region>=0&&region<6&&topic>=0&&topic<2&&region<=UnlockedRegion&&State.World.Conversations[region*2+topic]==0&&
                State.Campaign.QuestClaims[topic==0?GameContent.MainQuestId(region,1):GameContent.SideQuestId(region,1)];
        }
        public bool TalkToNpc(int region,int topic,int choice)
        {
            if(!CanTalk(region,topic)||choice<0||choice>1)return false;
            State.World.Conversations[region*2+topic]=choice+1;State.World.Trust[region]=Math.Min(4,State.World.Trust[region]+(choice==0?2:1));
            if(choice==0)State.Campaign.Materials[region]=Increment(State.Campaign.Materials[region],3);else AddGold((region+1)*45);
            return true;
        }
        public static string NpcQuestion(int region,int topic)
        {
            string[] first={"Should we strengthen the bridge patrol or reopen the market stalls?","Should the first ferry carry repair crews or the waiting traders?","Should we train more rescuers or sell the recovered watch supplies?","Should the cooling crew repair the channel or auction its spare fittings?","Should the scribes teach new apprentices or sell copies to collectors?","Should the observatory share its sunsteel or fund a new trading expedition?"};
            return topic==0?first[Clamp(region,0,5)]:"The rescued townsfolk want to help. Shall we give them a place in the guild or let them start a business?";
        }
        public int LegendaryRecipeId(int contract){return contract>=0&&contract<12?24+(contract/2)*14+(contract%2==0?0:5):-1;}
        public bool ClaimLegendaryContract(int contract)
        {
            if(contract<0||contract>=12||SagaInProgress||State.World.LegendaryClaims[contract]||State.Saga.BossMastery[contract]<1)return false;
            int recipe=LegendaryRecipeId(contract),index=State.Inventory.FindIndex(i=>i.Masterwork&&i.RecipeId==recipe&&i.Quality==3);
            if(index<0)return false;
            State.Inventory.RemoveAt(index);State.World.LegendaryClaims[contract]=true;AddGold((contract/2+1)*750);State.Saga.Mastery=Increment(State.Saga.Mastery,5);return true;
        }
        public double WorldHealthMultiplier { get { return 1+State.World.Projects[2]*.06; } }
        public double WorldDamageMultiplier { get { return 1+State.World.Projects[5]*.05+State.Saga.Legacy*.02; } }
        public string LongTermGoal
        {
            get
            {
                if(!CampaignComplete)return "Restore "+GameContent.Regions[UnlockedRegion].Name+" · "+ChapterProgress+" / 7 milestones";
                int bosses=0,buildings=0,contracts=0;foreach(int value in State.Saga.BossMastery)if(value>0)bosses++;foreach(int value in State.World.Projects)if(value>0)buildings++;foreach(bool value in State.World.LegendaryClaims)if(value)contracts++;
                return bosses+" / 12 guardians · "+buildings+" / 6 restorations · "+contracts+" / 12 legendary contracts";
            }
        }
    }
}
