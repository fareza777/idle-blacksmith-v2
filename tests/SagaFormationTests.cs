using System;
using System.Reflection;
using System.Text.Json;
using ForgeEmpire;
internal static class SagaFormationTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static CraftedItem Gear(int id){return new CraftedItem{RecipeId=id,Name=GameContent.Recipes[id].Name,Quality=3,Masterwork=true,Alloy=1,Value=100};}
    static WorkshopSimulation Ready(int room=1)
    {
        var state=new WorkshopState{Gold=10000,ChroniclesEnabled=true};state.Campaign=new CampaignState();
        for(int region=0;region<6;region++)for(int step=0;step<3;step++)state.Campaign.QuestClaims[GameContent.MainQuestId(region,step)]=true;
        var s=new WorkshopSimulation(state);for(int h=0;h<6;h++){var u=s.State.Campaign.Heroes[h];u.Recruited=true;u.Level=20;u.Weapon=Gear(4);u.Armor=Gear(6);}
        s.SetParty(1,1);s.SetParty(2,2);Check(s.StartSaga(1,0),"departure");s.State.Saga.Run.Room=room;s.State.Saga.Run.ClearedRooms=room;return s;
    }
    static WorkshopSimulation Reload(WorkshopSimulation s){var options=new JsonSerializerOptions{IncludeFields=true};return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(s.State,options),options));}
    static string Difference(WorkshopSimulation a,WorkshopSimulation b)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};using(var left=JsonDocument.Parse(JsonSerializer.Serialize(a.State.Saga.Run,options)))using(var right=JsonDocument.Parse(JsonSerializer.Serialize(b.State.Saga.Run,options)))
            foreach(var p in left.RootElement.EnumerateObject())if(p.Value.GetRawText()!=right.RootElement.GetProperty(p.Name).GetRawText())return p.Name+": "+p.Value.GetRawText()+" / "+right.RootElement.GetProperty(p.Name).GetRawText();
        return "";
    }
    static int Count(WorkshopSimulation s){var p=typeof(WorkshopSimulation).GetProperty("SagaEnemyCount");Check(p!=null,"formations missing: second room cannot expose multiple real opponents");return (int)p.GetValue(s);}
    public static void Run(Action<string,Action> test)
    {
        test("automatic contact action follows the locked weapon for every hero while Runebreak remains a cast",()=>{
            for(int hero=0;hero<6;hero++)for(int kind=0;kind<8;kind++)
            {
                var s=Ready(0);s.State.Saga.Run=null;s.State.Saga.Party=new[]{hero,-1,-1};
                int recipe=-1;foreach(var item in GameContent.Recipes)if(item.Slot==GearSlot.Weapon&&(int)item.Kind==kind){recipe=item.Id;break;}
                Check(recipe>=0,"missing weapon family");s.State.Campaign.Heroes[hero].Weapon=Gear(recipe);Check(s.StartSaga(1,0)&&s.ChooseSagaPath(0),"automatic action fixture");
                var r=s.State.Saga.Run;r.EnemyHP=r.EnemyMaxHP=50000;s.Advance(1.2);
                Check(r.Units[0].Attacks==1&&r.Units[0].Action==(kind==(int)WeaponKind.Staff?"cast":"attack"),"automatic contact pose ignores weapon for hero "+hero+" family "+kind);
                if(hero==2&&kind==(int)WeaponKind.Sword)Check(s.UsePartySkill(0)&&r.Units[0].Action=="cast","Runebreak skill must keep its deliberate spell cast");
            }
        });
        test("living forge equipped rune synergy preview matches the locked applied effects",()=>{
            var s=Ready();s.State.Saga.Run=null;s.State.Campaign.Battle=new ExpeditionState();s.State.Campaign.Heroes[0].Weapon.Rune=1;
            Check(s.SagaSynergyHint.Contains("Runes or lightning"),"applied Storm rune on a sword is absent from departure synergy");
            Check(s.StartSaga(1,0),"rune departure");s.State.Campaign.Heroes[0].Weapon.Rune=0;
            Check(s.SagaSynergyHint.Contains("Runes or lightning"),"run synergy reads changed workshop equipment instead of the locked effect");
        });
        test("living forge fallen companion pending strike is canceled at the death tick before saves",()=>{
            var a=Ready(0);a.ChooseSagaPath(0);var r=a.State.Saga.Run;r.EnemyHP=r.EnemyMaxHP=50000;
            for(int slot=0;slot<3;slot++){var u=r.Units[slot];u.HP=u.MaxHP=1000;u.Guard=0;u.Power=1;u.NextAttackTick=slot==0?26:80;}r.Units[0].HP=1;
            a.Advance(2.8);Check(r.Units[0].HP==0,"announced strike did not defeat fixture companion");var b=Reload(a);
            Check(Difference(a,b)=="","death tick retained a contact that normalization drops: "+Difference(a,b));
            a.Advance(.2);b.Advance(.2);Check(Difference(a,b)=="","fallen companion pending strike changed after reload: "+Difference(a,b));
        });
        test("living forge missing formation records cannot constitute an earned terminal victory",()=>{
            var s=Ready(2);s.ChooseSagaPath(0);var r=s.State.Saga.Run;double gold=s.State.Gold;r.Enemies=new SagaEnemyState[]{null,null};r.EnemyHP=0;r.MirrorHP=0;
            r.Phase=SagaPhase.Result;r.Won=true;r.ClearedRooms=r.TotalRooms;
            Check(s.ClaimSaga()&&s.State.Gold==gold&&s.State.Campaign.RegionWins[1]==0,"empty slots were counted as a defeated formation");
        });
        test("living forge lost pending target and source normalize identically with and without reload",()=>{
            var a=Ready();a.ChooseSagaPath(0);a.Advance(.8);var options=new JsonSerializerOptions{IncludeFields=true};var b=Reload(a);
            a.State.Saga.Run.Enemies[0].HP=0;b.State.Saga.Run.Enemies[0].HP=0;b=Reload(b);a.Advance(.4);b.Advance(.4);
            Check(JsonSerializer.Serialize(a.State.Saga.Run,options)==JsonSerializer.Serialize(b.State.Saga.Run,options),"lost party recipient left different pending poses/schedules after reload: "+Difference(a,b));
            a=Ready();a.ChooseSagaPath(0);foreach(var u in a.State.Saga.Run.Units)u.NextAttackTick=50;a.Advance(1.6);b=Reload(a);
            a.State.Saga.Run.Enemies[0].HP=0;b.State.Saga.Run.Enemies[0].HP=0;b=Reload(b);a.Advance(.1);b.Advance(.1);
            Check(JsonSerializer.Serialize(a.State.Saga.Run,options)==JsonSerializer.Serialize(b.State.Saga.Run,options),"lost enemy source scheduled a different replacement after reload: "+Difference(a,b));
        });
        test("living forge result claim cannot pay while a hidden formation member survives",()=>{
            var s=Ready(2);s.ChooseSagaPath(0);var r=s.State.Saga.Run;double gold=s.State.Gold;r.Enemies[0].HP=0;r.EnemyHP=0;r.MirrorHP=0;
            r.Phase=SagaPhase.Result;r.Won=true;r.ClearedRooms=r.TotalRooms;
            Check(s.ClaimSaga()&&s.State.Gold==gold&&s.State.Campaign.RegionWins[1]==0,"a surviving formation member allowed forged victory loot");
        });
        test("living forge temporary might clarity and shelter match their real combat benefits",()=>{
            var might=Ready();var clarity=Ready();var shelter=Ready();
            foreach(var sim in new[]{might,clarity,shelter}){sim.ChooseSagaPath(4);}
            might.ChooseSagaBoon(0);clarity.ChooseSagaBoon(2);shelter.ChooseSagaBoon(1);
            foreach(var sim in new[]{might,clarity,shelter})
            {
                Check(sim.ChooseSagaPath(0),"boon battle");var run=sim.State.Saga.Run;run.EnemyHP=run.EnemyMaxHP=10000;run.EnemyGuard=0;run.EnemyBarrier=0;
                foreach(var u in run.Units){u.NextAttackTick=5000;u.Power=20;u.Damage=1;u.Effects=0;u.WeaponRecipe=4;u.Guard=0;u.Protection=1;u.HP=u.MaxHP=1000;}sim.Advance(.8);
            }
            double hp=might.State.Saga.Run.EnemyHP;Check(might.UsePartySkill(1),"might skill");Check(Math.Abs(hp-might.State.Saga.Run.EnemyHP-53.76)<1e-8,"Might preview differs from12% actual damage");
            Check(clarity.UsePartySkill(1)&&Math.Abs(clarity.State.Saga.Run.Units[1].Cooldown-4)<1e-8,"Clarity did not reduce the real five-second cooldown20%");
            var control=Ready(2);control.ChooseSagaPath(0);var r=control.State.Saga.Run;r.EnemyHP=r.EnemyMaxHP=10000;r.EnemyGuard=0;r.EnemyBarrier=0;
            foreach(var u in r.Units){u.NextAttackTick=5000;u.Power=20;u.Damage=1;u.Effects=0;u.WeaponRecipe=4;u.Guard=0;u.Protection=1;u.HP=u.MaxHP=1000;}
            control.Advance(2.8);shelter.Advance(2);double plain=3000;foreach(var u in r.Units)plain-=u.HP;double protectedDamage=3000;foreach(var u in shelter.State.Saga.Run.Units)protectedDamage-=u.HP;
            Check(plain>0&&Math.Abs(protectedDamage-plain*.88)<1e-8,"Shelter description differs from actual12% mitigation");
        });
        test("living forge riposte contact identifies attacking source after focus changes",()=>{
            var s=Ready();s.ChooseSagaPath(0);var r=s.State.Saga.Run;foreach(var u in r.Units)u.NextAttackTick=5000;
            s.Advance(2.7);Check(r.ThreatEnemy==0,"no announced leader strike");s.SelectSagaEnemy(1);int target=r.ThreatTarget;r.Units[target].CounterSeconds=1;
            double hp=r.Enemies[0].HP;s.Advance(.1);Check(r.Enemies[0].HP<hp,"riposte struck current focus instead of attacker");
            bool seen=false;foreach(var e in r.Events)if(e.Text=="Riposte"){Check(e.EnemySlot==0,"riposte feedback identifies current focus instead of actual source");seen=true;}Check(seen,"no reflection feedback");
        });
        test("living forge corrupted support promise cancels instead of surviving with an impossible ally",()=>{
            var s=Ready();s.ChooseSagaPath(0);var r=s.State.Saga.Run;foreach(var u in r.Units){u.NextAttackTick=5000;u.Guard=500;}r.Enemies[0].HP*=.5;
            for(int i=0;i<200&&r.ThreatAction!="heal";i++)s.Advance(.1);Check(r.ThreatAction=="heal","no healer promise");r.ThreatSupportTarget=999;
            s=Reload(s);Check(s.State.Saga.Run.ThreatImpactTick==0,"invalid support promise retained on reload");
        });
        test("living forge formation contacts are identical across batch frame pause and saved ticks",()=>{
            var a=Ready(2);var b=Ready(2);a.ChooseSagaPath(0);b.ChooseSagaPath(0);a.Advance(19.4);
            var options=new JsonSerializerOptions{IncludeFields=true};
            for(int frame=0;frame<1940;frame++)
            {
                b.Advance(.01);if(frame%31==0)b=Reload(b);
                if(frame==500){Check(b.SetSagaPaused(true),"pause");b.Advance(30);b=Reload(b);Check(b.SetSagaPaused(false),"resume");}
            }
            a.State.Saga.Run.TickRemainder=b.State.Saga.Run.TickRemainder=0;
            Check(JsonSerializer.Serialize(a.State.Saga.Run,options)==JsonSerializer.Serialize(b.State.Saga.Run,options),"formation changed a real impact/event/schedule across saves or subdivisions");
        });
        test("living forge party windup locks its recipient through focus switch and reload",()=>{
            var s=Ready();s.ChooseSagaPath(0);s.Advance(.8);var r=s.State.Saga.Run;
            Check(r.Units[0].TargetEnemy==0&&r.Units[0].ImpactTick==12,"first strike not locked");
            Check(s.SelectSagaEnemy(1),"focus healer");double leader=r.Enemies[0].HP,healer=r.Enemies[1].HP;
            s=Reload(s);r=s.State.Saga.Run;s.Advance(.4);
            Check(r.Enemies[0].HP<leader&&r.Enemies[1].HP==healer,"focus switch stole pending strike");
            foreach(var e in r.Events)if(e.Kind==SagaEventKind.Hit&&e.SourceSlot==0)Check(e.EnemySlot==0,"contact published wrong recipient");
        });
        test("living forge healer announces and heals one locked ally after reload",()=>{
            var s=Ready();s.ChooseSagaPath(0);var r=s.State.Saga.Run;foreach(var u in r.Units){u.NextAttackTick=5000;u.Guard=500;}
            r.Enemies[0].HP=r.Enemies[0].MaxHP*.5;s.SelectSagaEnemy(1);
            for(int tick=0;tick<200&&r.ThreatAction!="heal";tick++)s.Advance(.1);
            Check(r.ThreatEnemy==1&&r.ThreatSupportTarget==0&&r.ThreatAction=="heal","healer never announced actual support");
            int remaining=r.ThreatImpactTick-r.RoomTick;double hp=r.Enemies[0].HP,amount=r.Enemies[0].MaxHP*.1;
            Check(s.SelectSagaEnemy(0),"switch focus away from announced source");s=Reload(s);r=s.State.Saga.Run;s.Advance(remaining*.1);
            Check(Math.Abs(r.Enemies[0].HP-hp-amount)<1e-8,"healer changed recipient or lost its saved announcement");
            bool seen=false;foreach(var e in r.Events)if(e.Kind==SagaEventKind.Heal&&e.Enemy){Check(e.EnemySlot==0&&e.Amount==amount,"support event differs from real healing");seen=true;}Check(seen,"missing support contact");
        });
        test("living forge defeating nonfocused support preserves leader and cancels its promised action",()=>{
            var s=Ready();s.ChooseSagaPath(0);var r=s.State.Saga.Run;foreach(var u in r.Units){u.NextAttackTick=5000;u.Guard=500;}
            r.Enemies[0].HP=r.Enemies[0].MaxHP*.5;s.SelectSagaEnemy(1);
            for(int tick=0;tick<200&&r.ThreatAction!="heal";tick++)s.Advance(.1);
            Check(r.ThreatAction=="heal","no support promise");r.Enemies[1].HP=1;double hp=r.Enemies[0].HP;
            Check(s.UsePartySkill(1),"finish healer");Check(r.Enemies[1].HP==0&&r.Enemies[0].HP==hp&&r.Phase==SagaPhase.Combat&&r.FocusEnemy==0,"support death lost encounter leader");
            s.Advance(1.2);Check(r.Enemies[0].HP==hp,"dead healer released a support action");
        });
        test("living forge protector and ranged roles produce their advertised real effects",()=>{
            var s=Ready(2);s.ChooseSagaPath(0);var r=s.State.Saga.Run;foreach(var u in r.Units){u.NextAttackTick=5000;u.Guard=0;u.HP=u.MaxHP=1000;}
            Check(s.SagaEnemyCount==3&&r.Enemies[1].Role=="Protector"&&r.Enemies[2].Role=="Ranged","third room missing authored roles");
            bool shield=false,ranged=false;for(int tick=0;tick<250;tick++)
            {
                s.Advance(.1);foreach(var e in r.Events){if(e.Enemy&&e.Kind==SagaEventKind.Block&&e.Text.Contains("Protector")){Check(e.EnemySlot>=0&&r.Enemies[e.EnemySlot].Barrier>0,"announced protector did not shield a real ally");shield=true;}
                if(!e.Enemy&&e.Kind==SagaEventKind.Hit&&e.EnemySlot==2)ranged=true;}
            }
            Check(shield&&ranged,"support rotation omitted a protector or ranged contact");
        });
        test("living forge frost heavy and rune sequence changes real damage with bounded consumed triggers",()=>{
            var s=Ready(0);s.ChooseSagaPath(0);var r=s.State.Saga.Run;r.EnemyHP=r.EnemyMaxHP=10000;r.EnemyGuard=0;r.EnemyBarrier=0;
            foreach(var u in r.Units){u.NextAttackTick=5000;u.Power=20;u.Damage=1;u.Effects=0;}r.Units[1].WeaponRecipe=29;r.Units[2].WeaponRecipe=25;
            s.Advance(.8);Check(s.UsePartySkill(1)&&r.Enemies[0].FrostUntilTick>r.RoomTick,"Frost did not prime the target");s.Advance(.7);
            double hp=r.EnemyHP;Check(s.UsePartySkill(2),"heavy shatter");Check(Math.Abs(hp-r.EnemyHP-45)<1e-8&&r.Enemies[0].ChargedUntilTick>r.RoomTick,"heavy shatter failed its real25% bonus");
            s.Advance(.7);r.Units[1].Cooldown=0;r.Units[1].WeaponRecipe=26;hp=r.EnemyHP;Check(s.UsePartySkill(1),"rune followup");
            Check(Math.Abs(hp-r.EnemyHP-62.4)<1e-8&&r.Enemies[0].ChargedUntilTick==0,"rune surge failed or remained infinitely reusable");
        });
        test("living forge optional trader charges and heals once while unaffordable options are noops",()=>{
            var s=Ready();Check(s.ChooseSagaPath(3),"open trader");s.State.Gold=0;s.State.Campaign.Materials[1]=0;
            Check(!s.ChooseSagaBoon(1)&&!s.ChooseSagaBoon(2)&&s.SagaBoonPending,"blocked purchase mutated pending event");
            s.State.Campaign.Materials[1]=2;foreach(var u in s.State.Saga.Run.Units)u.HP=u.MaxHP*.5;
            s=Reload(s);Check(s.ChooseSagaBoon(2),"buy shelter");double hp=s.State.Saga.Run.Units[0].HP;
            Check(s.State.Campaign.Materials[1]==1&&s.State.Saga.Run.Boon==1&&!s.ChooseSagaBoon(2),"trader repeated its cost");
            s=Reload(s);Check(s.State.Campaign.Materials[1]==1&&s.State.Saga.Run.Units[0].HP==hp,"reload repeated trader healing");
        });
        test("living forge pending waystone remains safe for legacy choice zero and forbids final event skips",()=>{
            var s=Ready();s.ChooseSagaPath(4);Check(s.ChooseSagaPath(0),"legacy path zero cannot resolve event");
            Check(!s.CanChooseSagaPath(3)&&!s.CanChooseSagaPath(4)&&!s.CanChooseSagaPath(5)&&!s.ChooseSagaPath(4),"optional path skipped mandatory final encounter");
            Check(s.ChooseSagaPath(0),"final battle blocked");
        });
        test("living forge corrupt formation cannot resurrect or pay and dead target contact never retargets",()=>{
            var s=Ready();s.ChooseSagaPath(0);s.Advance(.8);var r=s.State.Saga.Run;double gold=s.State.Gold;r.Enemies[0].HP=0;
            double hp=r.Enemies[1].HP;s=Reload(s);r=s.State.Saga.Run;s.Advance(.4);Check(r.Enemies[1].HP==hp,"lost windup target redirected damage");
            foreach(var e in r.Enemies)e.HP=0;s=Reload(s);Check(!s.SagaInProgress&&!s.ClaimSaga()&&s.State.Gold==gold,"all-dead corrupted formation resurrected or paid");
        });
        test("living forge pending waystone boon persists and settles its room once",()=>{
            var s=Ready();Check(s.CanChooseSagaPath(4)&&s.ChooseSagaPath(4),"eligible waystone did not open a real event");
            int room=s.State.Saga.Run.Room;s=Reload(s);Check(s.SagaBoonPending,"pending event lost at reload");
            Check(s.ChooseSagaBoon(0)&&!s.ChooseSagaBoon(0),"event claim did not settle exactly once");
            Check(s.State.Saga.Run.Room==room+1&&s.State.Saga.Run.ClearedRooms==room+1&&s.State.Saga.Run.Boon==0,"boon did not persist with its actual room reward");
        });
        test("living forge second room has independently damageable formation slots",()=>{
            var s=Ready();Check(s.ChooseSagaPath(0),"safe trail");Check(Count(s)>=2,"second room still a single opponent");
            var at=typeof(WorkshopSimulation).GetMethod("SagaEnemyAt");var select=typeof(WorkshopSimulation).GetMethod("SelectSagaEnemy");Check(at!=null&&select!=null,"no actual targeting API");
            var leader=at.Invoke(s,new object[]{0});double hp=(double)leader.GetType().GetField("HP").GetValue(leader);
            Check((bool)select.Invoke(s,new object[]{1}),"select second enemy");s.Advance(.8);Check(s.UsePartySkill(1),"focused skill");
            Check((double)leader.GetType().GetField("HP").GetValue(leader)==hp,"focused attack damaged the leader instead");
        });
    }
}
