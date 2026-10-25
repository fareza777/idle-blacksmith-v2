using System;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        public bool SagaInProgress { get { return State.Saga!=null&&State.Saga.Run!=null&&State.Saga.Run.TotalRooms>0; } }
        public string SagaEncounterName
        {
            get
            {
                var r=State.Saga.Run;if(r==null)return "The road ahead";
                return r.BossId>=0?ChronicleContent.Bosses[r.BossId].Name:r.EnemyId>=0?ChronicleContent.Enemies[r.EnemyId].Name:"The road ahead";
            }
        }
        public string SagaNextAction
        {
            get
            {
                if(!SagaInProgress)return "Prepare your party and choose a route.";var r=State.Saga.Run;
                return r.Phase==SagaPhase.Result?(r.Won?"Bring the expedition's rewards home.":"Return safely, improve your build and try again."):
                    r.Phase==SagaPhase.Choice?"Choose the next path. Your workshop keeps working.":r.Phase==SagaPhase.Camp?"Decide how to use this shelter.":"Time your party skills against the enemy's announced attack.";
            }
        }
        public string SagaMechanicHint { get { return !SagaInProgress?"":State.Saga.Run.BossId>=0?ChronicleContent.Bosses[State.Saga.Run.BossId].Counter:State.Saga.Run.EnemyId>=0?ChronicleContent.Enemies[State.Saga.Run.EnemyId].Description:SagaNextAction; } }
        public string SagaSkillName(int slot)
        {
            int id=PartyHero(slot);return id<0?"Empty slot":new[]{"Bastion","Pinpoint","Runebreak","Mend","Riposte","Sanctuary"}[id];
        }
        public string SagaSkillDescription(int slot)
        {
            int id=PartyHero(slot);return id<0?"Recruit and assign a companion.":new[]{"Protect the party for 2.4s. Cooldown 7s.","2.4× focused damage; mark the true target. Cooldown 5s.","1.8× damage, break armor and disrupt. Cooldown 7s.","Heal every companion for 24%; cleanse pressure. Cooldown 10s.","1.6× damage; counter a blow within 2s. Cooldown 6s.","Barrier for 18% party health; protect for 2s. Cooldown 9s."}[id];
        }
        int PartyHero(int slot)
        {
            if(slot<0||slot>2)return -1;
            if(SagaInProgress)return State.Saga.Run.Units[slot]==null?-1:State.Saga.Run.Units[slot].HeroId;
            return State.Saga.Party[slot];
        }
        public void NormalizeSaga()
        {
            if(State.Saga==null)State.Saga=new SagaState();var saga=State.Saga;
            if(saga.Party==null||saga.Party.Length==0)saga.Party=new[]{0,-1,-1};
            else if(saga.Party.Length!=3){var old=saga.Party;saga.Party=new[]{0,-1,-1};Array.Copy(old,saga.Party,Math.Min(3,old.Length));}
            bool any=false;for(int i=0;i<3;i++)
            {
                int hero=saga.Party[i];bool duplicate=false;for(int j=0;j<i;j++)if(hero==saga.Party[j])duplicate=true;
                if(hero<0||hero>=GameContent.HeroCount||!State.Campaign.Heroes[hero].Recruited||duplicate)saga.Party[i]=-1;else any=true;
            }
            if(!any)saga.Party[0]=0;
            saga.BossMastery=NormalizeCounters(saga.BossMastery,12);saga.Mastery=Clamp(saga.Mastery,0,MaxCounter);saga.Legacy=Clamp(saga.Legacy,0,20);
            var r=saga.Run;if(r==null)return;
            // Unity may materialize a null nested record as a zero-field object.
            // That is absence of a saga, not permission to discard a legacy battle.
            if(r.TotalRooms==0){saga.Run=null;return;}
            bool invalid=r.TotalRooms!=(r.Route==0?3:r.Route==1?4:5)||r.Region<0||r.Region>=6||r.Region>UnlockedRegion||r.Route<0||r.Route>3||
                (int)r.Phase<0||(int)r.Phase>3||r.Room<0||r.Room>=r.TotalRooms||r.Units==null||r.Units.Length!=3||r.Modifier<0||r.Modifier>3;
            if(invalid){saga.Run=null;State.Campaign.Battle=new ExpeditionState();return;}
            any=false;int used=0;
            for(int slot=0;slot<r.Units.Length;slot++)
            {
                if(r.Units[slot]==null)r.Units[slot]=new SagaUnitState();var u=r.Units[slot];
                if(u.HeroId<0){r.Units[slot]=new SagaUnitState();continue;}
                if(u.HeroId>=6||(used&(1<<u.HeroId))!=0||!State.Campaign.Heroes[u.HeroId].Recruited){invalid=true;break;}
                if(!ValidRecipe(u.WeaponRecipe)||GameContent.Recipes[u.WeaponRecipe].Slot!=GearSlot.Weapon||u.ArmorRecipe< -1||
                    u.ArmorRecipe>=0&&(!ValidRecipe(u.ArmorRecipe)||GameContent.Recipes[u.ArmorRecipe].Slot!=GearSlot.Armor)){invalid=true;break;}
                used|=1<<u.HeroId;u.Power=Clamp(u.Power,1,1000);u.Guard=Clamp(u.Guard,0,1000);u.MaxHP=Clamp(u.MaxHP,1,1000);u.HP=Clamp(u.HP,0,u.MaxHP);u.Cooldown=Clamp(u.Cooldown,0,10);u.Barrier=Clamp(u.Barrier,0,u.MaxHP*.6);u.Attacks=Clamp(u.Attacks,0,MaxCounter);u.AttackTicks=Clamp(u.AttackTicks,4,16);u.Damage=Clamp(u.Damage,.5,3);u.Protection=Clamp(u.Protection,.5,1);u.Effects=Clamp(u.Effects,0,8191);u.CounterSeconds=Clamp(u.CounterSeconds,0,2);if(u.HP>0)any=true;
            }
            if(invalid){saga.Run=null;State.Campaign.Battle=new ExpeditionState();return;}
            r.BonusGold=Clamp(r.BonusGold,0,6000);r.BonusMaterial=Clamp(r.BonusMaterial,0,30);r.Tick=Clamp(r.Tick,0,12000);r.RoomTick=Clamp(r.RoomTick,0,r.Tick);r.ClearedRooms=Clamp(r.ClearedRooms,0,r.TotalRooms);r.Pressure=Clamp(r.Pressure,0,12);r.PulseCount=Clamp(r.PulseCount,0,300);
            foreach(var u in r.Units)u.ActionUntilTick=Clamp(u.ActionUntilTick,0,r.RoomTick+6);
            r.TickRemainder=Clamp(r.TickRemainder,0,.1-ClockTolerance);r.GuardSeconds=Clamp(r.GuardSeconds,0,2.4);r.DisruptSeconds=Clamp(r.DisruptSeconds,0,2);r.SlowSeconds=Clamp(r.SlowSeconds,0,1.5);r.BossPhase=Clamp(r.BossPhase,0,2);r.ResponseMask=Clamp(r.ResponseMask,0,31);
            if(!any&&r.Phase!=SagaPhase.Result){EndSaga(false);PublishSagaBattle();return;}
            bool encounter=r.EnemyId>=0&&r.EnemyId<48&&ChronicleContent.Enemies[r.EnemyId].Region==r.Region;
            bool boss=r.BossId>=0&&r.BossId<12&&r.BossId==r.Region*2+(r.Route==3?1:0)&&r.Route>=2&&r.Room==r.TotalRooms-1;
            if(r.Phase==SagaPhase.Combat||r.Phase==SagaPhase.Result)
            {
                if((!encounter&&!boss)||(r.Route>=2&&r.Room==r.TotalRooms-1&&!boss)||!Finite(r.EnemyHP)||!Finite(r.EnemyMaxHP)||r.EnemyMaxHP<=0){saga.Run=null;State.Campaign.Battle=new ExpeditionState();return;}
                if(boss)r.EnemyId=-1;else r.BossId=-1;
                r.EnemyMaxHP=Clamp(r.EnemyMaxHP,1,20000);r.EnemyHP=Clamp(r.EnemyHP,0,r.EnemyMaxHP);r.EnemyBarrier=Clamp(r.EnemyBarrier,0,r.EnemyMaxHP*.4);r.EnemyGuard=Clamp(r.EnemyGuard,0,150);r.Mechanic=boss?r.BossId:-1;
                if(!any||r.Tick>=12000)EndSaga(false);
                else if(r.Phase==SagaPhase.Result&&(!r.Won||r.ClearedRooms!=r.TotalRooms||r.EnemyHP!=0))EndSaga(false);
                else if(r.Phase==SagaPhase.Combat&&r.EnemyHP==0)EndSaga(false);
            }
            PublishSagaBattle();
        }
        public bool SetParty(int slot,int heroId)
        {
            if(slot<0||slot>2||heroId< -1||heroId>=GameContent.HeroCount||SagaInProgress||State.Campaign.Battle.Active||State.Campaign.Battle.Finished)return false;
            if(heroId>=0&&!State.Campaign.Heroes[heroId].Recruited)return false;
            for(int i=0;i<3;i++)if(i!=slot&&heroId>=0&&State.Saga.Party[i]==heroId)return false;
            bool any=heroId>=0;for(int i=0;i<3;i++)if(i!=slot&&State.Saga.Party[i]>=0)any=true;
            if(!any)return false;State.Saga.Party[slot]=heroId;return true;
        }
        public bool StartSaga(int region,int route,int modifier=0)
        {
            if(region<0||region>=6||region>UnlockedRegion||route<0||route>3||modifier<0||modifier>3||State.Saga.Mastery<modifier*3||SagaInProgress||State.Campaign.Battle.Active||State.Campaign.Battle.Finished)return false;
            if(route==3&&!IsFrontierCleared(region))return false;
            var r=new SagaRunState{Region=region,Route=route,Modifier=modifier,TotalRooms=route==0?3:route==1?4:5,Phase=SagaPhase.Choice,Seed=State.Campaign.ExpeditionsWon%8};bool any=false;
            for(int i=0;i<3;i++)
            {
                int heroId=State.Saga.Party[i];if(heroId<0){r.Units[i]=new SagaUnitState();continue;}
                var h=State.Campaign.Heroes[heroId];if(!h.Recruited||h.Weapon==null)return false;
                var u=new SagaUnitState{HeroId=heroId,Power=PowerOf(heroId),Guard=GuardOf(heroId),MaxHP=Math.Floor(HealthOf(heroId)*WorldHealthMultiplier),AttackTicks=heroId==1?8:heroId==2?12:heroId==4?7:10,WeaponRecipe=h.Weapon.RecipeId,ArmorRecipe=h.Armor==null?-1:h.Armor.RecipeId};
                u.HP=u.MaxHP;string weakness=GameContent.Regions[region].Weakness,element=GameContent.Recipes[h.Weapon.RecipeId].Element;
                u.Damage=(element==weakness?(heroId==2?1.6:1.3):element==ResistedElements[region]?.75:1)*WorldDamageMultiplier;
                u.Protection=h.Armor!=null&&GameContent.Recipes[h.Armor.RecipeId].Element==weakness?(heroId==2?.7:.8):1;
                foreach(var item in new[]{h.Weapon,h.Armor,h.Accessory})u.Effects|=1<<(int)ItemEffect(item);
                if(HasEffect(u,GearEffect.Haste))u.AttackTicks=Math.Max(4,(int)Math.Round(u.AttackTicks*.85));
                r.Units[i]=u;any=true;
            }
            if(!any)return false;
            foreach(var unit in r.Units)if(unit.HeroId>=0){State.Campaign.SelectedHero=unit.HeroId;break;}
            State.Saga.Run=r;State.World.RunsStarted=Increment(State.World.RunsStarted,1);PublishSagaBattle();return true;
        }
        public bool ChooseSagaPath(int choice)
        {
            if(!SagaInProgress||choice<0||choice>2)return false;var r=State.Saga.Run;
            if(r.Phase==SagaPhase.Camp)
            {
                if(choice==0){foreach(var u in r.Units)if(u.HeroId>=0){u.HP=Math.Min(u.MaxHP,u.HP+u.MaxHP*.32);u.Cooldown=Math.Max(0,u.Cooldown-4);}r.LastEvent="The party rested. Health +32%; cooldowns eased.";}
                if(choice==1){r.BonusMaterial+=2;r.LastEvent="The shelter held two useful regional materials.";}
                if(choice==2){r.BonusGold+=(r.Region+1)*25;foreach(var u in r.Units)u.Cooldown=0;r.LastEvent="You charted a shortcut and recovered a trader's purse.";}
                r.Room++;r.ClearedRooms++;r.Phase=SagaPhase.Choice;RecordChoice();PublishSagaBattle();return true;
            }
            if(r.Phase!=SagaPhase.Choice)return false;
            if(choice==1)
            {
                if(r.Room==0||r.Room==r.TotalRooms-1||r.CampUsed)return false;
                r.CampUsed=true;r.Phase=SagaPhase.Camp;r.LastEvent="A quiet shelter offers a moment to breathe.";RecordChoice();PublishSagaBattle();return true;
            }
            r.Phase=SagaPhase.Combat;r.RoomTick=0;r.Telegraph="";r.EnemyBarrier=0;r.Pressure=0;r.ResponseMask=0;r.Reborn=false;r.BossPhase=0;
            foreach(var unit in r.Units){unit.Action=unit.HP>0?"ready":"defeat";unit.ActionUntilTick=0;}
            bool boss=r.Route>=2&&r.Room==r.TotalRooms-1;int count=0;foreach(var u in r.Units)if(u.HeroId>=0)count++;
            double scale=(1+.35*(count-1))*(1+r.Modifier*.22)*(choice==2?1.25:1);
            if(boss)
            {
                r.BossId=r.Region*2+(r.Route==3?1:0);r.EnemyId=-1;var d=ChronicleContent.Bosses[r.BossId];r.Mechanic=d.Mechanic;r.EnemyMaxHP=Math.Round(d.Health*scale);r.EnemyGuard=4+r.Region*3;
                if(r.Mechanic==0||r.Mechanic==5||r.Mechanic==9)r.EnemyBarrier=r.EnemyMaxHP*.15;
            }
            else {r.BossId=-1;r.Mechanic=-1;r.EnemyId=r.Region*8+(r.Room*3+r.Route+r.Seed+(choice==2?2:0))%8;var d=ChronicleContent.Enemies[r.EnemyId];r.EnemyMaxHP=Math.Round(d.Health*scale);r.EnemyGuard=d.Guard;if(d.Archetype==6)r.EnemyBarrier=r.EnemyMaxHP*.12;}
            r.EnemyHP=r.EnemyMaxHP;
            if(choice==2){r.BonusMaterial++;r.BonusGold+=(r.Region+1)*20;r.LastEvent="You accepted a harder rescue encounter. Extra supplies await victory.";}else r.LastEvent="The party takes the safer trail.";
            State.World.RoomsVisited=Increment(State.World.RoomsVisited,1);RecordChoice();PublishSagaBattle();return true;
        }
        void RecordChoice(){State.World.ChoicesMade=Increment(State.World.ChoicesMade,1);}
        static bool HasEffect(SagaUnitState u,GearEffect effect){return (u.Effects&(1<<(int)effect))!=0;}
        public bool UsePartySkill(int slot)
        {
            if(!SagaInProgress||slot<0||slot>2)return false;var r=State.Saga.Run;var u=r.Units[slot];
            if(r.Phase!=SagaPhase.Combat||u.HeroId<0||u.HP<=0||u.Cooldown>ClockTolerance)return false;
            int hero=u.HeroId;u.Cooldown=new[]{7,5,7,10,6,9}[hero];u.Action=hero==3?"heal":hero==0||hero==5?"brace":hero==2?"cast":hero==4?"parry":"attack";
            u.ActionUntilTick=r.RoomTick+5;
            if(hero==0){r.GuardSeconds=2.4;r.ResponseMask|=2;PartyBarrier(.06);}
            if(hero==1){r.ResponseMask|=1;HitSagaEnemy(u,2.4*(HasEffect(u,GearEffect.Focus)?1.2:1),true);}
            if(hero==2){r.ResponseMask|=4;r.DisruptSeconds=2;r.EnemyGuard*=.55;r.EnemyBarrier=Math.Max(0,r.EnemyBarrier-u.Power*1.5);HitSagaEnemy(u,1.8,true);}
            if(hero==3){r.ResponseMask|=16;r.Pressure=0;foreach(var ally in r.Units)if(ally.HeroId>=0&&ally.HP>0)ally.HP=Math.Min(ally.MaxHP,ally.HP+ally.MaxHP*.24);}
            if(hero==4){r.ResponseMask|=8;u.CounterSeconds=2;HitSagaEnemy(u,1.6,true);}
            if(hero==5){r.ResponseMask|=2;r.GuardSeconds=2;PartyBarrier(.18);}
            if(hero==0||hero==5||hero==4)
            {
                if(HasEffect(u,GearEffect.BraceBarrier))u.Barrier=Math.Min(u.MaxHP*.6,u.Barrier+u.MaxHP*.14);
                if(HasEffect(u,GearEffect.Counter))u.CounterSeconds=2;
            }
            State.World.SkillsUsed=Increment(State.World.SkillsUsed,1);PublishSagaBattle();return true;
        }
        void PartyBarrier(double amount){foreach(var u in State.Saga.Run.Units)if(u.HeroId>=0&&u.HP>0)u.Barrier=Math.Min(u.MaxHP*.6,u.Barrier+u.MaxHP*amount);}
        void HitSagaEnemy(SagaUnitState u,double multiplier,bool skill=false)
        {
            var r=State.Saga.Run;if(r.Phase!=SagaPhase.Combat)return;
            double armor=r.EnemyGuard*(HasEffect(u,GearEffect.ArmorBreak)?.5:1),damage=Math.Max(2,u.Power*u.Damage*multiplier-armor);
            if(!skill){u.Attacks++;if(HasEffect(u,GearEffect.ChainLightning)&&u.Attacks%3==0)damage*=1.55;if(HasEffect(u,GearEffect.Mend)&&u.Attacks%5==0)u.HP=Math.Min(u.MaxHP,u.HP+u.MaxHP*.05);}
            if(HasEffect(u,GearEffect.Burn))damage*=1.08;
            if(HasEffect(u,GearEffect.Frostbite))r.SlowSeconds=1.5;
            if(r.Mechanic==9&&(r.ResponseMask&1)==0)damage*=.7;
            if(r.EnemyBarrier>0)
            {
                double barrierMultiplier=skill&&(u.HeroId==1||u.HeroId==2)?2:1;
                double barrierDamage=damage*barrierMultiplier;
                double used=Math.Min(r.EnemyBarrier,barrierDamage);r.EnemyBarrier-=used;damage=Math.Max(0,damage-used/barrierMultiplier);
            }
            r.EnemyHP=Math.Max(0,r.EnemyHP-damage);
            if(r.BossId>=0)
            {
                int phase=r.EnemyHP<=r.EnemyMaxHP*.33?2:r.EnemyHP<=r.EnemyMaxHP*.66?1:0;
                if(phase>r.BossPhase){r.BossPhase=phase;r.LastEvent="Phase "+(phase+1)+" · "+ChronicleContent.Bosses[r.BossId].Counter;if(r.Mechanic==0||r.Mechanic==5)r.EnemyBarrier=r.EnemyMaxHP*.1;}
                bool frost=false;foreach(var ally in r.Units)if(ally.HeroId>=0&&ValidRecipe(ally.WeaponRecipe)&&GameContent.Recipes[ally.WeaponRecipe].Element=="Frost")frost=true;
                if(r.Mechanic==7&&r.EnemyHP==0&&!r.Reborn&&!frost&&(r.ResponseMask&4)==0){r.Reborn=true;r.EnemyHP=r.EnemyMaxHP*.25;r.LastEvent="The phoenix rekindles once. Frost or disruption prevents its return.";}
            }
            if(r.EnemyHP>0)return;
            foreach(var ally in r.Units)if(ally.HeroId>=0&&ally.HP>0&&HasEffect(ally,GearEffect.LifeOnKill))ally.HP=Math.Min(ally.MaxHP,ally.HP+ally.MaxHP*.12);
            r.ClearedRooms++;
            if(r.Room==r.TotalRooms-1)EndSaga(true);
            else {r.Room++;r.Phase=SagaPhase.Choice;r.Telegraph="";r.LastEvent="Encounter cleared. Choose a route for the next room.";}
        }
        public void AdvanceSaga(double seconds)
        {
            if(!SagaInProgress||!Finite(seconds)||seconds<=0)return;var r=State.Saga.Run;if(r.Phase!=SagaPhase.Combat)return;
            double accumulated=r.TickRemainder+Math.Min(seconds,OfflineLimit);int ticks=(int)Math.Floor((accumulated+ClockTolerance)/.1);r.TickRemainder=Math.Max(0,accumulated-ticks*.1);
            for(int i=0;i<ticks&&r.Phase==SagaPhase.Combat;i++)
            {
                r.Tick++;r.RoomTick++;r.GuardSeconds=Math.Max(0,r.GuardSeconds-.1);r.DisruptSeconds=Math.Max(0,r.DisruptSeconds-.1);r.SlowSeconds=Math.Max(0,r.SlowSeconds-.1);
                foreach(var u in r.Units)
                {
                    if(u.HeroId<0||u.HP<=0)continue;u.Cooldown=Math.Max(0,u.Cooldown-.1);u.CounterSeconds=Math.Max(0,u.CounterSeconds-.1);
                    if(r.RoomTick>=u.ActionUntilTick)u.Action="ready";
                    if(r.RoomTick%u.AttackTicks==0){if(r.RoomTick>=u.ActionUntilTick){u.Action="attack";u.ActionUntilTick=r.RoomTick+3;}HitSagaEnemy(u,1);}
                    if(r.Phase!=SagaPhase.Combat)break;
                }
                if(r.Phase!=SagaPhase.Combat)break;
                // Only actions in the announced two-second window answer this mechanic.
                if(r.BossId>=0&&r.RoomTick%60==40)r.ResponseMask=0;
                int interval=r.BossId>=0?24:ChronicleContent.Enemies[r.EnemyId].AttackTicks;
                if(r.EnemyId>=0&&ChronicleContent.Enemies[r.EnemyId].Archetype==5&&r.EnemyHP<r.EnemyMaxHP*.5)interval=Math.Max(10,interval-6);
                if(r.SlowSeconds>0)interval+=4;
                int until=interval-r.RoomTick%interval;
                r.Telegraph=until<=8?(r.BossId>=0?"The guardian prepares a strike.":"Enemy strike incoming."):"";
                if(r.BossId>=0&&r.RoomTick%60>=40)r.Telegraph=ChronicleContent.Bosses[r.BossId].Telegraph;
                if(r.Mechanic==11&&r.RoomTick%60>=40)r.Telegraph=r.PulseCount%2==0?"Violet seal · Use Focus before it closes.":"Golden seal · Protect the party before impact.";
                if(r.RoomTick%interval==0)EnemySagaAttack(false);
                if(r.BossId>=0&&r.RoomTick%60==0){EnemySagaAttack(true);r.PulseCount++;r.ResponseMask=0;}
                if(r.EnemyId>=0&&ChronicleContent.Enemies[r.EnemyId].Archetype==6&&r.RoomTick%50==0)r.EnemyBarrier=Math.Min(r.EnemyMaxHP*.2,r.EnemyBarrier+r.EnemyMaxHP*.08);
                bool alive=false;foreach(var u in r.Units)if(u.HeroId>=0&&u.HP>0)alive=true;
                if(!alive||r.Tick>=12000)EndSaga(false);
            }
            if(r.Phase!=SagaPhase.Combat)r.TickRemainder=0;PublishSagaBattle();
        }
        void EnemySagaAttack(bool mechanic)
        {
            var r=State.Saga.Run;int target=-1;
            for(int i=0;i<3;i++)if(r.Units[i].HeroId>=0&&r.Units[i].HP>0){if(target<0||r.Units[i].HeroId==0)target=i;}
            if(target<0)return;
            int archetype=r.EnemyId<0?-1:ChronicleContent.Enemies[r.EnemyId].Archetype;
            if(archetype==0||archetype==7)for(int i=0;i<3;i++)if(r.Units[i].HeroId>=0&&r.Units[i].HP>0&&r.Units[i].HP/r.Units[i].MaxHP<r.Units[target].HP/r.Units[target].MaxHP)target=i;
            double power=(r.BossId>=0?ChronicleContent.Bosses[r.BossId].Power:ChronicleContent.Enemies[r.EnemyId].Power)*(1+r.Modifier*.18)*(1+r.BossPhase*.18);
            bool all=false;double scale=1;
            if(mechanic)
            {
                int m=r.Mechanic;bool guard=(r.ResponseMask&2)!=0||r.GuardSeconds>0,disrupt=(r.ResponseMask&4)!=0,focus=(r.ResponseMask&1)!=0,counter=(r.ResponseMask&8)!=0;
                if(m==0){if(focus||disrupt){r.EnemyBarrier=0;power*=.5;}else r.EnemyBarrier=Math.Min(r.EnemyMaxHP*.3,r.EnemyBarrier+r.EnemyMaxHP*.1);}
                if(m==1){all=true;scale=guard?.5:1.1;}
                if(m==2){if(disrupt)return;all=true;r.Pressure=Math.Min(6,r.Pressure+1);scale=1+r.Pressure*.1;}
                if(m==3){all=true;scale=guard?.4:1.3;if(!guard)r.Pressure=Math.Min(6,r.Pressure+1);}
                if(m==4){if(counter){r.EnemyHP=Math.Max(1,r.EnemyHP-power*2);return;}scale=guard?.4:1.8;}
                if(m==5){r.EnemyBarrier=Math.Min(r.EnemyMaxHP*.25,r.EnemyBarrier+(focus?0:r.EnemyMaxHP*.08));scale=focus?.5:1.1;}
                if(m==6){if(disrupt||focus||HasPartyEffect(GearEffect.ArmorBreak)){r.EnemyGuard=Math.Max(0,r.EnemyGuard-5);power*=.5;}else{r.Pressure=Math.Min(6,r.Pressure+1);scale=1+r.Pressure*.15;}all=true;}
                if(m==7){all=true;scale=disrupt?.4:1.2;}
                if(m==8){if(disrupt)return;all=true;scale=HasPartyElement("Storm")?.7:1.5;}
                if(m==9){if(focus){r.EnemyBarrier=0;power*=.4;}else{r.EnemyBarrier=Math.Min(r.EnemyMaxHP*.2,r.EnemyBarrier+r.EnemyMaxHP*.1);all=true;}}
                if(m==10){all=true;scale=guard?.35:1.8;}
                if(m==11){all=true;scale=(r.PulseCount%2==0?focus:guard)?.4:1.6;}
            }
            if(archetype==2&&r.RoomTick%3==0)scale*=1.4;
            if(archetype==3&&r.DisruptSeconds<=0){r.Pressure=Math.Min(6,r.Pressure+1);power*=1+r.Pressure*.06;}
            if(archetype==7&&r.Units[target].HP<r.Units[target].MaxHP*.4)scale*=1.6;
            for(int i=0;i<3;i++)
            {
                var u=r.Units[i];if(u.HeroId<0||u.HP<=0||(!all&&i!=target))continue;
                double damage=Math.Max(2,power*scale-u.Guard*.82)*u.Protection;
                if(r.GuardSeconds>0)damage*=.35;if(HasEffect(u,GearEffect.Shelter))damage*=.92;
                bool parried=u.CounterSeconds>0;
                if(parried){damage*=.45;r.EnemyHP=Math.Max(1,r.EnemyHP-u.Power*.6);u.CounterSeconds=0;}
                double absorbed=Math.Min(u.Barrier,damage);u.Barrier-=absorbed;damage-=absorbed;u.HP=Math.Max(0,u.HP-damage);u.Action=u.HP>0?(parried?"parry":damage>0?"hurt":"brace"):"defeat";u.ActionUntilTick=r.RoomTick+4;
                if(archetype==4)r.EnemyHP=Math.Min(r.EnemyMaxHP,r.EnemyHP+damage*.2);
            }
        }
        bool HasPartyEffect(GearEffect effect){foreach(var u in State.Saga.Run.Units)if(u.HeroId>=0&&u.HP>0&&HasEffect(u,effect))return true;return false;}
        bool HasPartyElement(string element){foreach(var u in State.Saga.Run.Units)if(u.HeroId>=0&&u.WeaponRecipe>=0&&GameContent.Recipes[u.WeaponRecipe].Element==element)return true;return false;}
        void EndSaga(bool won)
        {
            var r=State.Saga.Run;r.Phase=SagaPhase.Result;r.Won=won;r.Telegraph="";r.TickRemainder=0;
            r.RewardGold=won?(int)(((r.Region+1)*(55+40*r.Route)+r.BonusGold)*(1+r.Modifier*.35)):0;
            r.RewardMaterial=won?5+r.Route*3+r.BonusMaterial+State.World.Projects[1]:0;
            foreach(var u in r.Units)if(u.HeroId>=0)u.Action=won?(u.HP>0?"victory":"defeat"):u.HP>0?"stagger":"defeat";
        }
        public bool ClaimSaga()
        {
            if(!SagaInProgress||State.Saga.Run.Phase!=SagaPhase.Result)return false;var r=State.Saga.Run;
            bool alive=false;foreach(var u in r.Units)if(u.HeroId>=0&&u.HP>0)alive=true;
            bool won=r.Won&&alive&&r.ClearedRooms==r.TotalRooms&&r.EnemyHP==0;
            EndSaga(won);State.Saga.Run=null;State.Campaign.Battle=new ExpeditionState();
            if(!won){State.Campaign.LastLostRegion=r.Region;State.Campaign.LastLostRoute=Math.Min(2,r.Route);State.Campaign.RetryLevel=Math.Min(30,State.Campaign.Heroes[State.Campaign.SelectedHero].Level+1);return true;}
            var c=State.Campaign;AddGold(r.RewardGold);c.Materials[r.Region]=Increment(c.Materials[r.Region],r.RewardMaterial);c.Reputation=Increment(c.Reputation,(r.Region+1)*(r.Route+1)*2);c.ExpeditionsWon=Increment(c.ExpeditionsWon,1);c.RegionWins[r.Region]=Increment(c.RegionWins[r.Region],1);
            if(r.Route==1)c.EliteWins[r.Region]=Increment(c.EliteWins[r.Region],1);
            if(r.Route==2)c.BossKills[r.Region]=Increment(c.BossKills[r.Region],1);
            if(r.BossId>=0){State.Saga.BossMastery[r.BossId]=Increment(State.Saga.BossMastery[r.BossId],1);State.Saga.Mastery=Increment(State.Saga.Mastery,1+r.Modifier);}
            foreach(var u in r.Units)
            {
                if(u.HeroId<0)continue;var h=c.Heroes[u.HeroId];h.Experience=Increment(h.Experience,25+20*r.Route+r.Region*12);
                while(h.Level<30&&h.Experience>=h.Level*30){h.Experience-=h.Level*30;h.Level++;}if(h.Level==30)h.Experience=0;
            }
            c.RetryLevel=0;CaptureJourney();return true;
        }
        public bool AdvanceLegacy()
        {
            if(SagaInProgress||!CampaignComplete||State.Saga.Legacy>=20||State.Saga.Mastery<12*(State.Saga.Legacy+1))return false;
            foreach(int count in State.Saga.BossMastery)if(count<1)return false;
            State.Saga.Mastery-=12*(State.Saga.Legacy+1);State.Saga.Legacy++;return true;
        }
        void PublishSagaBattle()
        {
            if(!SagaInProgress)return;var r=State.Saga.Run;double hp=0,max=0;int leader=0;
            for(int i=0;i<3;i++){var u=r.Units[i];if(u!=null&&u.HeroId>=0){if(max==0)leader=u.HeroId;hp+=u.HP;max+=u.MaxHP;}}
            State.Campaign.Battle=new ExpeditionState{Active=r.Phase==SagaPhase.Combat,Finished=r.Phase==SagaPhase.Result,Won=r.Won,Region=r.Region,Route=r.Route,Wave=r.Room,HeroIndex=leader,EnemyKind=r.EnemyId,HeroHP=hp,HeroMaxHP=max,EnemyHP=r.EnemyHP,EnemyMaxHP=r.EnemyMaxHP,Elapsed=r.Tick*.1,TotalTicks=r.Tick,WaveTicks=r.RoomTick,RewardGold=r.RewardGold,RewardMaterial=r.RewardMaterial,RewardExperience=25+20*r.Route+r.Region*12,GuardSeconds=r.GuardSeconds};
        }
    }
}
