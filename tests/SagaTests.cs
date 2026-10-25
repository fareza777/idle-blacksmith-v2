using System;
using System.Text.Json;
using ForgeEmpire;

internal static class SagaTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static WorkshopSimulation Reload(WorkshopSimulation sim){var j=new JsonSerializerOptions{IncludeFields=true};return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(sim.State,j),j));}
    static CraftedItem Gear(int id){return new CraftedItem{Name=GameContent.Recipes[id].Name,RecipeId=id,Quality=3,Value=100,Masterwork=true,Alloy=1};}
    static WorkshopSimulation Ready(int region,int third=3)
    {
        var state=new WorkshopState{Gold=10000,Ingots=100,ChroniclesEnabled=true};
        state.Campaign=new CampaignState();for(int r=0;r<6;r++)for(int step=0;step<3;step++)state.Campaign.QuestClaims[GameContent.MainQuestId(r,step)]=true;
        var s=new WorkshopSimulation(state);for(int h=0;h<6;h++){var hero=s.State.Campaign.Heroes[h];hero.Recruited=true;hero.Level=20;hero.Weapon=Gear(region*4);hero.Armor=Gear(region*4+2);}
        Check(s.SetParty(1,1)&&s.SetParty(2,third),"party");return s;
    }
    static void Finish(WorkshopSimulation s,bool skills=true)
    {
        for(int step=0;step<16000&&s.SagaInProgress&&s.State.Saga.Run.Phase!=SagaPhase.Result;step++)
        {
            var r=s.State.Saga.Run;
            if(r.Phase==SagaPhase.Choice){Check(s.ChooseSagaPath(0),"path");continue;}
            if(r.Phase==SagaPhase.Camp){Check(s.ChooseSagaPath(0),"camp");continue;}
            if(skills)for(int slot=0;slot<3;slot++)s.UsePartySkill(slot);
            s.Advance(.1);
        }
        Check(s.State.Saga.Run.Phase==SagaPhase.Result,"bounded finish");
    }
    static WorkshopSimulation MechanicFixture(int id)
    {
        var s=Ready(id/2,id==4?4:2);s.StartSaga(id/2,id%2==0?2:3);var r=s.State.Saga.Run;r.Room=r.TotalRooms-1;r.ClearedRooms=r.Room;s.ChooseSagaPath(0);
        r.EnemyMaxHP=r.EnemyHP=100000;
        foreach(var u in r.Units){u.Power=1;u.Guard=0;u.MaxHP=u.HP=1000;u.AttackTicks=1000;u.Effects=0;u.Damage=1;u.Protection=1;}
        return s;
    }
    static double Health(WorkshopSimulation s){double hp=0;foreach(var u in s.State.Saga.Run.Units)hp+=u.HP;return hp;}
    public static void Run(Action<string,Action> test)
    {
        test("guardian telegraphs require a response during the announced window",()=>{
            var early=MechanicFixture(2);var late=MechanicFixture(2);
            early.UsePartySkill(2);early.Advance(6);
            late.Advance(4.1);late.UsePartySkill(2);late.Advance(1.9);
            Check(Health(late)>Health(early),"an expired early interrupt still counters a later bell");
        });
        test("saga migration resolves mutually exclusive encounter identities before ticking",()=>{
            var s=MechanicFixture(2);s.State.Saga.Run.EnemyId=999;s=new WorkshopSimulation(s.State);s.Advance(.1);
            Check(s.State.Saga.Run.BossId==2&&s.State.Saga.Run.EnemyId==-1,"guardian retained an invalid ordinary enemy identity");
        });
        test("saga preparation and retry guidance follow the actual party leader",()=>{
            var s=Ready(1);s.State.Campaign.SelectedHero=5;s.State.Campaign.Heroes[5].Weapon=null;
            Check(s.ExpeditionBlocker(1,0)=="","an unassigned hero blocked an armed party");
            Check(s.SetParty(0,2)&&s.StartSaga(1,0),"different party leader");Check(s.State.Campaign.SelectedHero==2,"retry guidance would train a hero outside the party");
        });
        test("saga empty nested records preserve an in-flight legacy expedition",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=2});s.TryStart(Station.Forge);s.Advance(4.5);s.TryEquip(0);Check(s.StartExpedition(0,0),"legacy expedition");
            double hp=s.State.Campaign.Battle.HeroHP;s.State.Saga.Run=new SagaRunState();s=new WorkshopSimulation(s.State);
            Check(s.State.Campaign.Battle.Active&&s.State.Campaign.Battle.HeroHP==hp&&!s.SagaInProgress,"empty saga erased legacy battle");
        });
        test("saga canonicalizes corrupt equipment on an empty party slot",()=>{
            var s=Ready(0);Check(s.SetParty(1,-1)&&s.SetParty(2,-1),"solo party");s.StartSaga(0,2);var r=s.State.Saga.Run;r.Room=4;r.ClearedRooms=4;s.ChooseSagaPath(0);
            r.Units[1].WeaponRecipe=999;r.Units[1].ArmorRecipe=999;s=new WorkshopSimulation(s.State);s.Advance(1);
            Check(s.SagaInProgress&&s.State.Saga.Run.Units[1].HeroId==-1&&s.State.Saga.Run.Units[1].WeaponRecipe==-1,"empty slot retained content references");
        });
        test("saga Riposte is fully absorbed while a sufficient barrier remains",()=>{
            var s=MechanicFixture(0);var r=s.State.Saga.Run;var u=r.Units[2];u.HeroId=4;u.Power=20;r.EnemyGuard=0;r.EnemyMaxHP=r.EnemyHP=1000;r.EnemyBarrier=100;
            Check(s.UsePartySkill(2),"riposte");Check(r.EnemyBarrier==68&&r.EnemyHP==1000,"barrier leaked half the absorbed hit into health");
        });
        test("Cinder Colossus honors its advertised Focus counter as well as disruption",()=>{
            var plain=MechanicFixture(6);var focus=MechanicFixture(6);plain.Advance(6);focus.Advance(4.1);Check(focus.UsePartySkill(1),"focus during pressure window");focus.Advance(1.9);
            Check(Health(focus)>Health(plain)&&focus.State.Saga.Run.Pressure==0,"focused shot did not vent the pressure");
        });
        foreach(int bad in new[]{-1,2,999})
        {
            int id=bad;test("saga rejects invalid weapon snapshot "+id+" before it can index content",()=>{
                var s=MechanicFixture(0);s.State.Saga.Run.Units[0].WeaponRecipe=id;double gold=s.State.Gold;s=new WorkshopSimulation(s.State);s.Advance(1);
                Check(!s.SagaInProgress&&s.State.Gold==gold,"invalid weapon snapshot retained");
            });
        }
        foreach(int bad in new[]{-2,0,999})
        {
            int id=bad;test("saga rejects invalid armor snapshot "+id+" before it can index content",()=>{
                var s=MechanicFixture(0);s.State.Saga.Run.Units[0].ArmorRecipe=id;s=new WorkshopSimulation(s.State);s.Advance(1);Check(!s.SagaInProgress,"invalid armor snapshot retained");
            });
        }
        for(int mechanic=0;mechanic<12;mechanic++)
        {
            int id=mechanic;test("guardian "+id+" counter changes the actual attack outcome",()=>{
                var plain=MechanicFixture(id);var response=MechanicFixture(id);plain.Advance(6);response.Advance(4.1);
                int slot=new[]{1,0,2,0,2,1,2,2,2,1,0,1}[id];Check(response.UsePartySkill(slot),"response skill");response.Advance(1.9);
                Check(Health(response)>Health(plain),"specific counter gave no protection");
            });
        }
        test("saga locks its three unique party snapshots for the whole route",()=>{
            var s=Ready(0);Check(!s.SetParty(1,0),"duplicate hero");Check(s.StartSaga(0,0),"depart");int power=s.State.Saga.Run.Units[0].Power;
            Check(!s.SelectHero(1)&&!s.SetParty(1,2)&&!s.TryRecruit(5),"run selection locked");
            s.State.Campaign.Heroes[0].Level=30;Check(s.State.Saga.Run.Units[0].Power==power,"snapshot unchanged");
            s=Reload(s);Check(s.SagaInProgress&&s.State.Saga.Run.Units[0].Power==power,"snapshot survives save");
        });
        test("saga camps carry injuries and cannot skip the final encounter",()=>{
            var s=Ready(0);s.StartSaga(0,0);Check(!s.ChooseSagaPath(1),"cannot skip opening");s.ChooseSagaPath(0);while(s.State.Saga.Run.Phase==SagaPhase.Combat)s.Advance(.1);
            s.State.Saga.Run.Units[0].HP=20;Check(s.ChooseSagaPath(1),"camp branch");s=Reload(s);Check(s.ChooseSagaPath(0),"rest");
            Check(s.State.Saga.Run.Units[0].HP>20&&!s.ChooseSagaPath(1),"healed but final mandatory");Finish(s);Check(s.State.Saga.Run.Won,"rested win");
        });
        test("saga route results pay once after reload and never during room choices",()=>{
            var s=Ready(0);double gold=s.State.Gold;s.StartSaga(0,0);s.Advance(28800);Check(s.State.Gold==gold&&!s.ClaimSaga(),"choices wait offline");
            Finish(s);Check(s.State.Saga.Run.Won,"won");s=Reload(s);Check(s.ClaimSaga()&&!s.ClaimSaga(),"one claim");double paid=s.State.Gold;
            s=Reload(s);Check(!s.ClaimSaga()&&s.State.Gold==paid&&paid>gold&&s.State.Campaign.RegionWins[0]==1,"once-only reward");
        });
        test("saga frame subdivisions and mid-fight saves keep identical combat",()=>{
            var a=Ready(4);var b=Ready(4);a.StartSaga(4,2);b.StartSaga(4,2);a.ChooseSagaPath(0);b.ChooseSagaPath(0);
            a.Advance(3.7);for(int i=0;i<37;i++){b.Advance(.1);if(i==11)b=Reload(b);}
            Check(Math.Abs(a.State.Saga.Run.EnemyHP-b.State.Saga.Run.EnemyHP)<1e-7,"enemy clock");
            for(int i=0;i<3;i++)Check(Math.Abs(a.State.Saga.Run.Units[i].HP-b.State.Saga.Run.Units[i].HP)<1e-7,"party clock");
        });
        for(int boss=0;boss<12;boss++)
        {
            int id=boss;test("saga guardian "+id+" has a distinct reachable three-phase encounter",()=>{
                var s=Ready(id/2,id%3==0?2:3);Check(s.StartSaga(id/2,id%2==0?2:3),"guardian route");
                while(s.State.Saga.Run.Room<s.State.Saga.Run.TotalRooms-1){s.ChooseSagaPath(0);while(s.State.Saga.Run.Phase==SagaPhase.Combat){for(int slot=0;slot<3;slot++)s.UsePartySkill(slot);s.Advance(.1);}Check(s.State.Saga.Run.Phase!=SagaPhase.Result,"survived approach");}
                s.ChooseSagaPath(0);Check(s.State.Saga.Run.BossId==id&&s.State.Saga.Run.Mechanic==id&&s.SagaMechanicHint.Length>20,"specific mechanic");
                Finish(s);Check(s.State.Saga.Run.Won&&s.State.Saga.Run.BossPhase==2,"guardian defeat and phase progression");s=Reload(s);Check(s.ClaimSaga()&&s.State.Saga.BossMastery[id]==1,"mastery from actual victory");
            });
        }
        for(int region=0;region<6;region++)for(int archetype=0;archetype<8;archetype++)
        {
            int r=region,a=archetype;test("saga region "+r+" creature "+a+" can actually spawn",()=>{
                var s=Ready(r);s.State.Campaign.ExpeditionsWon=a;s.StartSaga(r,0);s.ChooseSagaPath(0);
                Check(s.State.Saga.Run.EnemyId==r*8+a&&s.SagaMechanicHint.Length>20,"reachable catalog enemy");s.Advance(1);Check(s.State.Saga.Run.Tick>0&&s.State.Saga.Run.EnemyHP<s.State.Saga.Run.EnemyMaxHP,"real simulation");
            });
        }
        test("saga invalid inputs and forged terminal data cannot give loot",()=>{
            var s=Ready(0);Check(!s.StartSaga(6,0)&&!s.StartSaga(0,4)&&!s.StartSaga(0,0,4),"invalid route");s.StartSaga(0,0);s.ChooseSagaPath(0);
            var r=s.State.Saga.Run;r.Phase=SagaPhase.Result;r.Won=true;r.RewardGold=int.MaxValue;r.EnemyHP=double.NaN;r.ClearedRooms=r.TotalRooms;
            s=new WorkshopSimulation(s.State);Check(!s.SagaInProgress&&!s.ClaimSaga(),"invalid raw health rejected");
        });
        test("saga optional legacy preserves heroes story and collection",()=>{
            var s=Ready(5);s.State.Inventory.Add(Gear(20));for(int i=0;i<12;i++)s.State.Saga.BossMastery[i]=1;s.State.Saga.Mastery=12;
            Check(s.AdvanceLegacy()&&!s.AdvanceLegacy(),"mastery cost");Check(s.State.Saga.Legacy==1&&s.CampaignComplete&&s.State.Inventory.Count==1&&s.State.Campaign.Heroes[0].Level==20,"no progress reset");
        });
        test("world restoration and NPC choices pay once with real benefits",()=>{
            var s=Ready(5);s.State.Campaign.QuestClaims[16]=true;s.State.Campaign.Materials[0]=20;double before=s.GetDuration(Station.Mine);
            Check(s.TryRestoreProject(0)&&s.GetDuration(Station.Mine)<before,"mining benefit");
            Check(s.TalkToNpc(0,0,0)&&!s.TalkToNpc(0,0,1),"one conversation reward");s=Reload(s);Check(s.State.World.Projects[0]==1&&s.State.World.Trust[0]==2,"world persists");
        });
    }
}
