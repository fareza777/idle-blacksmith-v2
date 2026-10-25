using System;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        bool artisanDelivery;
        public static readonly ArtisanOption[] AlloyOptions={
            new ArtisanOption(0,"Honest iron","Balanced power and guard. Uses the recipe's normal materials."),
            new ArtisanOption(1,"Embersteel","Power +20%; guard -10%. Reserve 2 extra ingots.",2),
            new ArtisanOption(2,"Moonsteel","Guard +25%; power -10%. Reserve 2 extra ingots.",2)
        };
        public static readonly ArtisanOption[] TemperOptions={
            new ArtisanOption(0,"Oil bath","A patient, balanced temper. No stat penalty."),
            new ArtisanOption(1,"Brine quench","Guard +15%; power -8%. A defensive temper."),
            new ArtisanOption(2,"Flash quench","Power +15%; guard -10%. A sharp, aggressive temper.")
        };
        public static readonly ArtisanOption[] RuneOptions={
            new ArtisanOption(0,"Unbound","Keep the recipe's natural affinity."),
            new ArtisanOption(1,"Storm","Every third hit releases lightning for 55% bonus damage.",0,12),
            new ArtisanOption(2,"Aegis","Protective skills add a barrier worth 14% max health.",0,12),
            new ArtisanOption(3,"Renewal","Defeating an enemy restores 12% max health.",0,12)
        };
        public bool MasterworkActive { get { return State.Artisan!=null&&State.Artisan.Project!=null&&State.Artisan.Project.Stage!=ArtisanStage.Complete; } }
        public bool ArtisanBlocksClock { get { return MasterworkActive&&!artisanDelivery; } }
        public void NormalizeArtisan()
        {
            if(State.Artisan==null)State.Artisan=new ArtisanState();
            State.Artisan.Completed=Clamp(State.Artisan.Completed,0,MaxCounter);
            foreach(var item in State.Inventory)NormalizeArtisanItem(item);
            foreach(var hero in State.Campaign.Heroes){NormalizeArtisanItem(hero.Weapon);NormalizeArtisanItem(hero.Armor);NormalizeArtisanItem(hero.Accessory);}
            var p=State.Artisan.Project;
            if(p==null)return;
            if(!IsRunning(Station.Forge)||p.RecipeId!=State.PendingRecipe||!ValidRecipe(p.RecipeId)||p.Alloy<0||p.Alloy>2||(int)p.Stage<0||p.Stage>ArtisanStage.Ready)
            {State.Artisan.Project=null;return;}
            p.Temper=Clamp(p.Temper,0,2);p.Rune=Clamp(p.Rune,0,3);p.Strikes=Clamp(p.Strikes,0,3);p.Quality=Clamp(p.Quality,0,3);
            State.Progress[2]=0;State.PendingQuality=p.Quality;State.ManualForge=true;State.PendingProtection=true;
        }
        static void NormalizeArtisanItem(CraftedItem item)
        {
            if(item==null)return;
            item.Alloy=item.Masterwork?Clamp(item.Alloy,0,2):0;item.Temper=item.Masterwork?Clamp(item.Temper,0,2):0;item.Rune=item.Masterwork?Clamp(item.Rune,0,3):0;item.Provenance=Clamp(item.Provenance,0,MaxCounter);
        }
        public bool BeginMasterwork(int recipeId,int alloy)
        {
            if(alloy<0||alloy>=AlloyOptions.Length||MasterworkActive||!IsRecipeUnlocked(recipeId))return false;
            var recipe=GameContent.Recipes[recipeId];int extra=AlloyOptions[alloy].ExtraIngots;
            bool reserved=IsRunning(Station.Forge)&&State.PendingRecipe==recipeId&&State.Progress[2]<1;
            if(IsRunning(Station.Forge)&&!reserved||State.Ingots<extra+(reserved?0:recipe.Ingots))return false;
            if(!reserved)
            {
                int previous=State.SelectedRecipe;State.SelectedRecipe=recipeId;
                if(!TryStart(Station.Forge,true)){State.SelectedRecipe=previous;return false;}
            }
            State.Ingots-=extra;State.PendingProtection=true;
            State.ManualForge=true;State.Progress[2]=0;
            State.Artisan.Project=new MasterworkProject{RecipeId=recipeId,Alloy=alloy,Quality=State.PendingQuality,Stage=ArtisanStage.Heat};
            return true;
        }
        public bool StrikeMasterwork(double accuracy)
        {
            if(!MasterworkActive||!Finite(accuracy)||accuracy<0||accuracy>1)return false;
            var p=State.Artisan.Project;
            if(p.Stage!=ArtisanStage.Heat&&p.Stage!=ArtisanStage.Shape)return false;
            if(State.SimulationTime-State.LastStrikeTime+ClockTolerance<.2)return false;
            State.LastStrikeTime=State.SimulationTime;
            if(p.Stage==ArtisanStage.Heat){p.Stage=ArtisanStage.Shape;return true;}
            p.Strikes++;
            if(accuracy>=.82){p.Quality=Math.Min(3,p.Quality+1);State.Combo=Increment(State.Combo,1);State.BestCombo=Math.Max(State.BestCombo,State.Combo);}
            else State.Combo=0;
            State.PendingQuality=p.Quality;
            if(p.Strikes>=3)p.Stage=ArtisanStage.Quench;
            return true;
        }
        public bool QuenchMasterwork(int temper)
        {
            if(!MasterworkActive||State.Artisan.Project.Stage!=ArtisanStage.Quench||temper<0||temper>=TemperOptions.Length)return false;
            State.Artisan.Project.Temper=temper;State.Artisan.Project.Stage=ArtisanStage.Engrave;return true;
        }
        public bool EngraveMasterwork(int rune)
        {
            if(!MasterworkActive||State.Artisan.Project.Stage!=ArtisanStage.Engrave||rune<0||rune>=RuneOptions.Length||State.Gold<RuneOptions[rune].GoldCost)return false;
            State.Gold-=RuneOptions[rune].GoldCost;State.Artisan.Project.Rune=rune;State.Artisan.Project.Stage=ArtisanStage.Ready;return true;
        }
        public bool CompleteMasterwork()
        {
            if(!MasterworkActive||State.Artisan.Project.Stage!=ArtisanStage.Ready||State.Inventory.Count>=InventoryCapacity)return false;
            artisanDelivery=true;State.Progress[2]=1;
            try { Advance(.000001); } finally { artisanDelivery=false; }
            return !MasterworkActive;
        }
        void DecorateCraftedItem(CraftedItem item)
        {
            if(!artisanDelivery||!MasterworkActive||item.RecipeId!=State.Artisan.Project.RecipeId)return;
            var p=State.Artisan.Project;item.Masterwork=true;item.Alloy=p.Alloy;item.Temper=p.Temper;item.Rune=p.Rune;item.Provenance=State.Artisan.Completed+1;item.Protected=true;
            item.Value=Math.Min(MaxItemValue,(int)(item.Value*1.25));p.Stage=ArtisanStage.Complete;State.Artisan.Completed=Increment(State.Artisan.Completed,1);
        }
        public static int ArtisanStat(CraftedItem item,bool power)
        {
            if(item==null||!ValidRecipe(item.RecipeId))return 0;
            var recipe=GameContent.Recipes[item.RecipeId];double stat=(power?recipe.Power:recipe.Guard)*(4+Clamp(item.Quality,0,3))/4;
            if(item.Masterwork)
            {
                if(item.Alloy==1)stat*=power?1.2:.9;else if(item.Alloy==2)stat*=power?.9:1.25;
                if(item.Temper==1)stat*=power?.92:1.15;else if(item.Temper==2)stat*=power?1.15:.9;
            }
            return (int)Math.Floor(stat);
        }
        public static GearEffect ItemEffect(CraftedItem item)
        {
            if(item==null||!ValidRecipe(item.RecipeId))return GearEffect.None;
            if(item.Masterwork&&item.Rune>0&&item.Rune<4)return item.Rune==1?GearEffect.ChainLightning:item.Rune==2?GearEffect.BraceBarrier:GearEffect.LifeOnKill;
            return GameContent.Recipes[item.RecipeId].Effect;
        }
        public static string EffectDescription(GearEffect effect)
        {
            switch(effect)
            {
                case GearEffect.ChainLightning:return "Every third hit: +55% lightning damage.";
                case GearEffect.BraceBarrier:return "Protective skills: barrier for 14% max health.";
                case GearEffect.LifeOnKill:return "Each defeated enemy restores 12% max health.";
                case GearEffect.ArmorBreak:return "Attacks ignore half the enemy's armor.";
                case GearEffect.Mend:return "Every fifth hit restores 5% max health.";
                case GearEffect.Haste:return "Automatic attacks are 15% faster.";
                case GearEffect.Counter:return "Protective skills prime a 60% damage counter.";
                case GearEffect.Burn:return "Hits add a short burning wound.";
                case GearEffect.Frostbite:return "Hits slow the next enemy attack.";
                case GearEffect.Focus:return "Focused skills deal 20% more damage.";
                case GearEffect.Shelter:return "Incoming damage is reduced by 8%.";
                default:return "Reliable steel with no special combat effect.";
            }
        }
        public static string ItemBuildDescription(CraftedItem item)
        {
            if(item==null)return "No equipment assigned.";
            string origin=item.Masterwork?AlloyOptions[Clamp(item.Alloy,0,2)].Name+" · "+TemperOptions[Clamp(item.Temper,0,2)].Name+"\n":"";
            return origin+ArtisanStat(item,true)+" Power · "+ArtisanStat(item,false)+" Guard\n"+EffectDescription(ItemEffect(item));
        }
    }
}
