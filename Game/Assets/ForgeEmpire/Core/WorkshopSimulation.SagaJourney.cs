using System;
namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        public bool SagaBoonPending { get { return SagaInProgress&&State.Saga.Run.PendingJourney!=0; } }
        public string SagaPathTitle(int choice)
        {
            if(!SagaInProgress||choice<0||choice>5)return "Unavailable path";var r=State.Saga.Run;
            if(r.PendingJourney!=0)return SagaBoonTitle(choice);
            if(r.Phase==SagaPhase.Camp)return choice<3?new[]{"Rest · Recover 32% health","Search the shelter · +2 materials","Chart a shortcut · Gold & ready skills"}[choice]:"Unavailable path";
            return new[]{r.Room==r.TotalRooms-1&&r.Route>=2?"Face the guardian":"Follow the safe trail","Stop at a shelter","Answer a rescue call · Higher risk","Visit the trail trader","Listen at a waystone · Choose a boon","Challenge an elite patrol"}[choice];
        }
        public string SagaPathDescription(int choice)
        {
            if(!SagaInProgress||choice<0||choice>5)return "";var r=State.Saga.Run;
            if(r.PendingJourney!=0)return SagaBoonDescription(choice);
            if(r.Phase==SagaPhase.Camp)return choice<3?new[]{"Restore 32% maximum health, including fallen companions, and ease skill cooldowns by 4s.","Add two regional materials to the expedition reward.","Add "+((r.Region+1)*25)+" gold to the expedition reward and ready every party skill."}[choice]:"";
            return new[]{"Face the room's formation. Wounds and skill cooldowns carry onward.","Use the route's one shelter at an intermediate room. Rest, gather supplies or chart a shortcut.","Enemies have 25% more health. Victory adds one material and "+((r.Region+1)*20)+" gold to the expedition reward.","Spend this room trading. Leave freely, buy Might for "+((r.Region+1)*25)+" gold, or buy Shelter for one regional material.","Spend this room choosing a free temporary expedition boon. Each waystone can be visited once per expedition.","Face a three-enemy formation with 45% more health. Victory adds two materials and "+((r.Region+1)*20)+" gold to the expedition reward."}[choice];
        }
        public bool CanChooseSagaPath(int choice)
        {
            if(!SagaInProgress||choice<0||choice>5)return false;var r=State.Saga.Run;
            if(r.PendingJourney!=0)
            {
                if(r.Phase!=SagaPhase.Choice||choice>2)return false;
                return r.PendingJourney!=3||choice==0||choice==1&&State.Gold>=(r.Region+1)*25||choice==2&&State.Campaign.Materials[r.Region]>0;
            }
            if(r.Phase==SagaPhase.Camp)return choice<3;
            if(r.Phase!=SagaPhase.Choice)return false;
            if(choice==0||choice==2)return true;
            if(r.Room<=0||r.Room>=r.TotalRooms-1)return false;
            if(choice==1)return !r.CampUsed;return (r.JourneyUsedMask&(1<<choice))==0;
        }
        public string SagaBoonTitle(int choice)
        {
            if(choice<0||choice>2)return "Unavailable choice";
            if(SagaInProgress&&State.Saga.Run.PendingJourney==3)return new[]{"Leave the trader · Continue safely","Buy Might · "+((State.Saga.Run.Region+1)*25)+" gold","Buy Shelter · 1 regional material"}[choice];
            return new[]{"Might · +12% damage","Shelter · Heal & protect","Clarity · Faster skills"}[choice];
        }
        public string SagaBoonDescription(int choice)
        {
            if(choice<0||choice>2)return "";
            if(SagaInProgress&&State.Saga.Run.PendingJourney==3)
            {
                if(choice==0)return "Continue to the next room without spending resources.";
                choice=choice==1?0:1;
            }
            return new[]{"Deal 12% more damage during the next two rooms. Replaces the previous boon.","Immediately heal each living companion for 12% maximum health and take 12% less damage during the next two rooms. Replaces the previous boon.","Newly used party skills recover 20% faster during the next two rooms. Replaces the previous boon."}[choice];
        }
        public bool ChooseSagaBoon(int choice)
        {
            if(!SagaBoonPending||!CanChooseSagaPath(choice))return false;var r=State.Saga.Run;int pending=r.PendingJourney;
            int boon=pending==3?choice==0?-1:choice==1?0:1:choice;
            if(pending==3&&choice==1)State.Gold-=(r.Region+1)*25;
            if(pending==3&&choice==2)State.Campaign.Materials[r.Region]--;
            r.PendingJourney=0;r.RoomChoices[r.Room]=pending;r.ClearedRooms++;r.Room++;ExpireSagaBoon();
            if(boon>=0)
            {
                r.Boon=boon;r.BoonUntilRoom=Math.Min(r.TotalRooms-1,r.Room+1);
                if(boon==1)foreach(var u in r.Units)if(u.HeroId>=0&&u.HP>0)HealSagaUnit(u,u.MaxHP*.12,-1,"Shelter boon");
            }
            r.LastEvent=boon<0?"The trader bids the party a safe journey.":SagaBoonTitle(boon)+" · active through room "+(r.BoonUntilRoom+1)+".";
            RecordChoice();PublishSagaBattle();return true;
        }
        bool BeginSagaJourney(int choice)
        {
            var r=State.Saga.Run;r.PendingJourney=choice;r.JourneyUsedMask|=1<<choice;
            r.LastEvent=choice==3?"A trail trader lays out supplies. Every purchase is optional.":"A weathered waystone offers one blessing for the road.";
            RecordChoice();PublishSagaBattle();return true;
        }
        bool ActiveSagaBoon(int boon){return SagaInProgress&&State.Saga.Run.Boon==boon&&State.Saga.Run.Room<=State.Saga.Run.BoonUntilRoom;}
        void NormalizeSagaJourney()
        {
            var r=State.Saga.Run;if(r.RoomChoices==null||r.RoomChoices.Length!=5)
            {
                var old=r.RoomChoices;r.RoomChoices=new[]{-1,-1,-1,-1,-1};if(old!=null)Array.Copy(old,r.RoomChoices,Math.Min(5,old.Length));
            }
            for(int i=0;i<5;i++)r.RoomChoices[i]=Clamp(r.RoomChoices[i],-1,5);
            r.JourneyUsedMask&=(1<<3)|(1<<4)|(1<<5);
            if(r.PendingJourney!=3&&r.PendingJourney!=4||r.Phase!=SagaPhase.Choice||r.Room<=0||r.Room>=r.TotalRooms-1)r.PendingJourney=0;
            else r.JourneyUsedMask|=1<<r.PendingJourney;
            if(r.Boon<0||r.Boon>2||r.BoonUntilRoom<r.Room||r.BoonUntilRoom>=r.TotalRooms){r.Boon=-1;r.BoonUntilRoom=-1;}
        }
        void ExpireSagaBoon(){var r=State.Saga.Run;if(r.Room>r.BoonUntilRoom){r.Boon=-1;r.BoonUntilRoom=-1;}}
    }
}
