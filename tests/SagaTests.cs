using System;
using System.Collections;
using System.Reflection;
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
    static WorkshopSimulation CadenceFixture(bool guardian=false,int level=6)
    {
        var s=Ready(1);foreach(var h in s.State.Campaign.Heroes)
        {
            h.Level=level;h.Weapon.Quality=1;h.Weapon.Masterwork=false;h.Weapon.Alloy=0;
            h.Armor.Quality=1;h.Armor.Masterwork=false;h.Armor.Alloy=0;
        }
        Check(s.StartSaga(1,guardian?2:0),"cadence departure");var r=s.State.Saga.Run;
        if(guardian){r.Room=r.TotalRooms-1;r.ClearedRooms=r.Room;}
        Check(s.ChooseSagaPath(0),"cadence encounter");return s;
    }
    static double EncounterSeconds(WorkshopSimulation s,bool skills)
    {
        var r=s.State.Saga.Run;for(int i=0;i<12000&&r.Phase==SagaPhase.Combat;i++)
        {
            if(skills)for(int slot=0;slot<3;slot++)s.UsePartySkill(slot);
            s.Advance(.1);
        }
        return r.RoomTick*.1;
    }
    static FieldInfo Field(Type type,string name){var f=type.GetField(name);Check(f!=null,"missing persisted combat field "+name);return f;}
    static object Get(object value,string name){return Field(value.GetType(),name).GetValue(value);}
    static double Property(WorkshopSimulation s,string name){var p=typeof(WorkshopSimulation).GetProperty(name);Check(p!=null,"missing combat presentation property "+name);return Convert.ToDouble(p.GetValue(s));}
    static bool Pause(WorkshopSimulation s,bool paused){var m=typeof(WorkshopSimulation).GetMethod("SetSagaPaused");Check(m!=null,"missing persistent tactical pause");return (bool)m.Invoke(s,new object[]{paused});}
    static string CombatJson(WorkshopSimulation s){return JsonSerializer.Serialize(s.State.Saga.Run,new JsonSerializerOptions{IncludeFields=true});}
    static string CombatDifference(WorkshopSimulation a,WorkshopSimulation b)
    {
        using(var left=JsonDocument.Parse(CombatJson(a)))using(var right=JsonDocument.Parse(CombatJson(b)))
            foreach(var p in left.RootElement.EnumerateObject())if(p.Value.GetRawText()!=right.RootElement.GetProperty(p.Name).GetRawText())
                return p.Name+": "+p.Value.GetRawText()+" / "+right.RootElement.GetProperty(p.Name).GetRawText();
        return "";
    }
    public static void Run(Action<string,Action> test)
    {
        SagaFormationTests.Run(test);
        test("saga measured same-region loadout gives time for readable decisions",()=>{
            var boss=CadenceFixture(true);double ordinary=EncounterSeconds(CadenceFixture(),true),guardian=EncounterSeconds(boss,true);
            Console.WriteLine("SAGA_CADENCE region 1 level 6 three companions quality 1 gear skills: ordinary "+ordinary.ToString("0.0")+"s; guardian "+guardian.ToString("0.0")+"s");
            Check(ordinary>=15&&ordinary<=30,"ordinary encounter ended before a readable exchange: "+ordinary+"s");
            Check(guardian>=25&&guardian<180,"guardian did not sustain multiple decisions: "+guardian+"s");
            Check(boss.State.Saga.Run.PulseCount>=2,"party breaks prevented every guardian decision");
        });
        test("saga arrival gives every companion time before the first contact",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;double enemy=r.EnemyHP,hp=Health(s);s.Advance(.8);
            Check(r.EnemyHP==enemy&&Health(s)==hp,"damage landed during the opening arrival");
            s.Advance(.8);Check(r.EnemyHP<enemy,"arrival never handed over to a real attack");
        });
        test("saga first ordinary contacts are staggered across companions",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;int[] seen={0,0,0};int contacts=0;
            for(int tick=0;tick<32&&contacts<3;tick++)
            {
                s.Advance(.1);int simultaneous=0;for(int slot=0;slot<3;slot++)if(r.Units[slot].Attacks>seen[slot]){seen[slot]=r.Units[slot].Attacks;simultaneous++;contacts++;}
                Check(simultaneous<=1,"companions made an unreadable simultaneous first contact");
            }
            Check(contacts>=3,"a companion never joined the exchange");
        });
        test("saga ordinary enemy target is announced for at least twelve ticks before contact",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;foreach(var u in r.Units){u.Power=1;u.AttackTicks=1000;u.Guard=0;}
            int announced=0;double hp=Health(s);for(int tick=0;tick<60&&Health(s)==hp;tick++){s.Advance(.1);if(!string.IsNullOrEmpty(r.Telegraph))announced++;}
            Check(Health(s)<hp&&announced>=12,"enemy only announced "+announced+" ticks before its hit");
            int target=(int)Property(s,"SagaThreatTarget");Check(target>=-1&&target<3,"announced target is outside the party");
        });
        test("saga tactical pause persists and production continues without accumulating combat time",()=>{
            var s=CadenceFixture();s.Advance(1.05);Check(s.TryStart(Station.Mine),"start workshop while fighting");
            Check(Pause(s,true),"pause accepted");var r=s.State.Saga.Run;int tick=r.Tick;double hp=r.EnemyHP,remainder=r.TickRemainder;
            s.Advance(5);Check(r.Tick==tick&&r.EnemyHP==hp&&r.TickRemainder==remainder,"paused saga advanced or banked time");
            Check(s.State.Ore>0&&!s.UsePartySkill(0),"pause stopped production or allowed a combat skill");
            s=Reload(s);Check((bool)Get(s.State.Saga.Run,"Paused"),"pause did not survive a save");
            Check(Pause(s,false),"resume accepted");s.Advance(.05);Check(s.State.Saga.Run.Tick==tick+1,"resume released a bank of paused damage");
        });
        test("saga opening and shared command recovery prevent simultaneous skill bursts",()=>{
            var s=CadenceFixture();double hp=s.State.Saga.Run.EnemyHP;
            Check(!s.UsePartySkill(1)&&s.State.Saga.Run.EnemyHP==hp,"opening skill bypassed arrival");
            var ready=typeof(WorkshopSimulation).GetMethod("CanUseSagaSkill");Check(ready!=null,"missing truthful skill readiness");
            s.Advance(.8);Check((bool)ready.Invoke(s,new object[]{1})&&s.UsePartySkill(1),"skill did not become reachable after arrival");
            Check(!(bool)ready.Invoke(s,new object[]{2})&&!s.UsePartySkill(2),"two skills resolved on the same tick");
            s=Reload(s);s.Advance(.7);Check(s.UsePartySkill(2),"command recovery did not expire across a save");
        });
        test("saga publishes bounded ordered actual impacts without replaying them on reload",()=>{
            var s=MechanicFixture(1);s.Advance(18);var r=s.State.Saga.Run;
            var events=(IList)Get(r,"Events");Check(events.Count>0&&events.Count<=24,"event history missing or unbounded");
            int last=0;foreach(var e in events){int id=(int)Get(e,"Id");Check(id>last,"events not strictly ordered");last=id;Check((double)Get(e,"Amount")>=0,"negative event magnitude");}
            int serial=(int)Get(r,"EventSerial");s=Reload(s);Check((int)Get(s.State.Saga.Run,"EventSerial")==serial,"reload invented a contact");
        });
        test("saga break has a vulnerability window and a recovery that prevents permanent lock",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;r.EnemyHP=r.EnemyMaxHP=100000;foreach(var u in r.Units){u.Power=1;u.Guard=500;}
            bool broken=false;int firstBreak=-1,recovered=-1;
            for(int tick=0;tick<250;tick++)
            {
                for(int slot=0;slot<3;slot++)s.UsePartySkill(slot);s.Advance(.1);
                double progress=Property(s,"SagaBreakProgress");Check(progress>=0&&progress<=1,"break meter left its bounds");
                int until=(int)Get(r,"BreakUntilTick");if(until>r.RoomTick&&!broken){broken=true;firstBreak=r.RoomTick;}
                if(broken&&until<=r.RoomTick&&recovered<0)recovered=r.RoomTick;
                if(recovered>=0&&r.RoomTick<recovered+30)Check(until<=r.RoomTick,"enemy was immediately locked in another break");
            }
            Check(broken&&recovered>firstBreak&&recovered-firstBreak<=25,"no brief bounded break vulnerability");
        });
        test("saga old running saves migrate to future contacts without free damage",()=>{
            var s=CadenceFixture();s.Advance(.5);var r=s.State.Saga.Run;double hp=r.EnemyHP;
            Field(r.GetType(),"TimingVersion").SetValue(r,0);s=Reload(s);r=s.State.Saga.Run;
            Check((int)Get(r,"TimingVersion")>0&&r.EnemyHP==hp,"migration replayed combat or did not version the schedule");
            s.Advance(.7);Check(r.EnemyHP==hp,"migration landed an old pending hit twice");
        });
        test("saga saved schedules and events match uninterrupted play at every combat tick",()=>{
            var a=CadenceFixture(true);var b=CadenceFixture(true);
            for(int tick=0;tick<400&&a.State.Saga.Run.Phase==SagaPhase.Combat;tick++)
            {
                for(int slot=0;slot<3;slot++)Check(a.UsePartySkill(slot)==b.UsePartySkill(slot),"reload changed skill eligibility at tick "+tick);
                a.Advance(.1);b.Advance(.1);b=Reload(b);
                string difference=CombatDifference(a,b);Check(difference=="","reload changed combat at tick "+(tick+1)+" · "+difference);
            }
        });
        test("saga enemy contact retains the announced target when party injuries change",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;foreach(var u in r.Units){u.AttackTicks=1000;u.Guard=0;}r.Units[1].HP=r.Units[1].MaxHP*.6;
            s.Advance(1.6);Check(s.SagaThreatTarget==1,"skulker did not announce the wounded companion");
            r.Units[2].HP=r.Units[2].MaxHP*.2;double hp1=r.Units[1].HP,hp2=r.Units[2].HP;s.Advance(1.2);
            Check(r.Units[1].HP<hp1&&r.Units[2].HP==hp2,"enemy switched away from its announced target");
            bool actual=false;foreach(var e in r.Events)if(e.Kind==SagaEventKind.Hit&&e.SourceSlot==-1){Check(e.TargetSlot==1&&!e.Enemy,"impact destination contradicts the telegraph");actual=true;}
            Check(actual,"no actual enemy contact event");
        });
        test("saga guardian group threat keeps a full three-second response window",()=>{
            var s=MechanicFixture(1);var r=s.State.Saga.Run;s.Advance(3);
            Check(r.ThreatMechanic&&s.SagaThreatTarget==-1&&r.ThreatImpactTick-r.ThreatStartTick==30,"group attack was not announced truthfully");
            double hp=Health(s);s.Advance(2.9);Check(Health(s)==hp&&s.SagaThreatProgress>.9,"another attack interrupted the announced window");
            s.Advance(.1);int impacts=0;foreach(var e in r.Events)if(e.Kind==SagaEventKind.Hit&&e.SourceSlot==-1&&e.Tick==60){Check(!e.Enemy&&e.TargetSlot>=0,"group contact omitted its actual recipient");impacts++;}
            Check(impacts==3&&Health(s)<hp,"the announced whole-party attack did not match its contacts");
        });
        test("saga hit and heal events report the actual post-defense change",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;s.Advance(.8);var ranger=r.Units[1];ranger.Power=20;ranger.Damage=1;ranger.Effects=0;r.EnemyGuard=5;r.EnemyBarrier=20;
            double hp=r.EnemyHP;int before=r.EventSerial;Check(s.UsePartySkill(1),"focused strike");
            double hit=0,blocked=0;foreach(var e in r.Events)if(e.Id>before){if(e.Kind==SagaEventKind.Hit&&e.Enemy)hit+=e.Amount;if(e.Kind==SagaEventKind.Block&&e.Enemy)blocked+=e.Amount;}
            Check(Math.Abs(hit-33)<1e-9&&Math.Abs(hp-r.EnemyHP-hit)<1e-9&&blocked==20,"damage feedback ignored armor or the doubled barrier interaction");
            s.Advance(.7);for(int slot=0;slot<3;slot++)r.Units[slot].HP=r.Units[slot].MaxHP-5;
            before=r.EventSerial;Check(s.UsePartySkill(2),"mend");int heals=0;foreach(var e in r.Events)if(e.Id>before&&e.Kind==SagaEventKind.Heal){Check(e.Amount==5&&!e.Enemy&&e.SourceSlot==2,"healing feedback included overheal");heals++;}
            Check(heals==3,"mend did not report each real recipient");
        });
        test("saga authored encounter health preserves the benefit of better equipment",()=>{
            var modest=CadenceFixture(false,6);var strong=CadenceFixture(false,20);
            Check(modest.State.Saga.Run.EnemyMaxHP==strong.State.Saga.Run.EnemyMaxHP,"enemy copied the player's damage into its health");
            double slow=EncounterSeconds(modest,true),fast=EncounterSeconds(strong,true);Check(fast<slow*.8,"stronger equipment or levels lost their advantage");
        });
        test("saga eclipse gold seal has a real protective counter on its second pulse",()=>{
            var plain=MechanicFixture(11);var guarded=MechanicFixture(11);plain.Advance(11.1);guarded.Advance(11.1);
            Check(guarded.State.Saga.Run.ThreatMechanic&&guarded.State.Saga.Run.PulseCount==1,"gold seal not reachable");
            Check(guarded.UsePartySkill(0),"protect the gold seal");plain.Advance(2.9);guarded.Advance(2.9);
            Check(Health(guarded)>Health(plain)&&guarded.State.Saga.Run.PulseCount==2,"alternating seal did not honor its announced protection");
        });
        test("saga whole-party attacks skip empty slots without changing the advertised group",()=>{
            var s=Ready(0);Check(s.SetParty(0,-1)&&s.SetParty(2,-1),"one ranger in middle slot");Check(s.StartSaga(0,3),"solo guardian route");var r=s.State.Saga.Run;r.Room=4;r.ClearedRooms=4;s.ChooseSagaPath(0);
            r.Units[1].MaxHP=r.Units[1].HP=1000;r.Units[1].Power=1;r.Units[1].AttackTicks=1000;
            s.Advance(3);Check(s.SagaThreatTarget==-1,"empty slots removed the group announcement");s.Advance(3);
            int impacts=0;foreach(var e in r.Events)if(e.SourceSlot==-1&&e.Kind==SagaEventKind.Hit&&e.Tick==60){Check(e.TargetSlot==1,"group attack struck an empty slot");impacts++;}
            Check(impacts==1,"occupied companion did not receive the group attack");
        });
        test("saga pausing a pending contact and reloading resolves that contact exactly once",()=>{
            var a=CadenceFixture();var b=CadenceFixture();a.Advance(1.1);b.Advance(1.1);
            Check(b.SetSagaPaused(true),"pause pending contact");b.Advance(30);b=Reload(b);Check(b.SetSagaPaused(false),"resume pending contact");
            a.Advance(.1);b.Advance(.1);Check(CombatDifference(a,b)=="","pause changed a scheduled impact");
            int attacks=b.State.Saga.Run.Units[0].Attacks;b=Reload(b);a.Advance(.1);b.Advance(.1);
            Check(attacks==1&&b.State.Saga.Run.Units[0].Attacks==1&&CombatDifference(a,b)=="","pending contact repeated after resume");
        });
        test("saga normalization rejects reversed event time and repairs an impossible group promise",()=>{
            var s=MechanicFixture(1);s.Advance(3.2);var r=s.State.Saga.Run;r.ThreatTarget=0;r.Events.Clear();r.EventSerial=0;
            r.Events.Add(new SagaCombatEvent{Id=1,Tick=r.Tick,Kind=SagaEventKind.Hit,Amount=1,SourceSlot=0,Enemy=true});
            r.Events.Add(new SagaCombatEvent{Id=2,Tick=r.Tick-1,Kind=SagaEventKind.Hit,Amount=1,SourceSlot=0,Enemy=true});
            s=Reload(s);r=s.State.Saga.Run;
            Check(r.Events.Count==1&&r.Events[0].Id==1,"save retained events that run backward in time");
            Check(r.ThreatImpactTick==0||s.SagaThreatTarget==-1,"guardian advertised one companion then attacked the group");
        });
        test("saga counter cannot heal a nearly defeated enemy or publish negative damage",()=>{
            var s=MechanicFixture(4);s.Advance(4.1);Check(s.UsePartySkill(2),"duel response");var r=s.State.Saga.Run;r.EnemyHP=.5;s.Advance(1.9);
            Check(r.EnemyHP==.5,"nonlethal counter healed the enemy to its damage floor");
            foreach(var e in r.Events)Check(e.Amount>=0,"counter published negative damage");
        });
        test("saga ordinary parry cannot publish negative reflection against a nearly defeated enemy",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;foreach(var u in r.Units)u.AttackTicks=1000;s.Advance(2.7);
            r.EnemyHP=.5;r.Units[0].CounterSeconds=1;s.Advance(.1);
            Check(r.EnemyHP==.5,"parry reflection healed the enemy");foreach(var e in r.Events)Check(e.Amount>=0,"negative reflected damage");
        });
        test("saga an earned break increases actual damage only during its vulnerability",()=>{
            var s=CadenceFixture();var r=s.State.Saga.Run;r.EnemyHP=r.EnemyMaxHP=50000;foreach(var u in r.Units){u.Power=1;u.Guard=500;}
            for(int tick=0;tick<120&&r.BreakUntilTick<=r.RoomTick;tick++){for(int slot=0;slot<3;slot++)s.UsePartySkill(slot);s.Advance(.1);}
            Check(r.BreakUntilTick>r.RoomTick,"party did not earn a break");s.Advance(.7);var control=Reload(s);var plain=control.State.Saga.Run;
            plain.BreakUntilTick=plain.RoomTick;
            foreach(var run in new[]{r,plain}){run.EnemyGuard=0;run.Units[1].Power=20;run.Units[1].Damage=1;run.Units[1].Effects=0;run.Units[1].Cooldown=0;}
            double hp=r.EnemyHP,normalHP=plain.EnemyHP;Check(s.UsePartySkill(1)&&control.UsePartySkill(1),"compare the same focused hit");
            Check(Math.Abs(hp-r.EnemyHP-64.8)<1e-8&&Math.Abs(normalHP-plain.EnemyHP-48)<1e-8,"break did not expose a real damage vulnerability");
            s.Advance(2);Check(r.BreakUntilTick<=r.RoomTick&&s.SagaBreakProgress==0,"break vulnerability did not recover");
        });
        test("saga frame subdivisions retain the same events and pending threat through breaks",()=>{
            var a=CadenceFixture(true);var b=CadenceFixture(true);a.Advance(16.35);
            for(int frame=0;frame<1635;frame++){b.Advance(.01);if(frame%73==0)b=Reload(b);}
            Check(Math.Abs(a.State.Saga.Run.TickRemainder-b.State.Saga.Run.TickRemainder)<1e-8,"fractional clock changed");
            a.State.Saga.Run.TickRemainder=0;b.State.Saga.Run.TickRemainder=0;
            Check(CombatDifference(a,b)=="","frame subdivisions changed scheduled contacts, damage or events: "+CombatDifference(a,b));
        });
        test("guardian telegraphs require a response during the announced window",()=>{
            var early=MechanicFixture(2);var late=MechanicFixture(2);
            early.Advance(.8);Check(early.UsePartySkill(2),"early interrupt before the mechanic window");early.Advance(5.2);
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
            s.Advance(.8);Check(s.UsePartySkill(2),"riposte");Check(r.EnemyBarrier==68&&r.EnemyHP==1000,"barrier leaked half the absorbed hit into health");
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
                Check(s.State.Saga.Run.EnemyId==r*8+a&&s.SagaMechanicHint.Length>20,"reachable catalog enemy");s.Advance(8);Check(s.State.Saga.Run.Tick>0&&s.State.Saga.Run.EnemyHP<s.State.Saga.Run.EnemyMaxHP,"real simulation");
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
