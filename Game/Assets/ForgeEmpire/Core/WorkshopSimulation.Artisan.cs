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
        public ArtisanQuote QuoteMasterwork(int recipeId,int alloy)
        {
            var quote=new ArtisanQuote();
            if(!ValidRecipe(recipeId)||alloy<0||alloy>=AlloyOptions.Length){quote.Blocker="Choose a recipe and alloy.";return quote;}
            var recipe=GameContent.Recipes[recipeId];
            quote.Reserved=IsRunning(Station.Forge)&&State.PendingRecipe==recipeId&&State.Progress[2]<1;
            quote.Ingots=AlloyOptions[alloy].ExtraIngots+(quote.Reserved?0:recipe.Ingots);
            quote.RareCost=quote.Reserved?0:recipe.RareCost;
            quote.RareRegion=recipe.RareCost>0?recipe.Region-1:-1;
            quote.Quality=quote.Reserved?State.PendingQuality:0;
            if(!IsRecipeUnlocked(recipeId))quote.Blocker="Unlock this region's pattern first.";
            else if(MasterworkActive)quote.Blocker="Continue the creation already on your bench.";
            else if(IsRunning(Station.Forge)&&!quote.Reserved)quote.Blocker="Let the current forge piece finish first.";
            else if(!quote.Reserved&&State.Inventory.Count>=InventoryCapacity)quote.Blocker="Storage is full. Equip, deliver or sell a piece.";
            else if(State.Ingots<quote.Ingots)quote.Blocker="Need "+(quote.Ingots-State.Ingots)+" more ingots.";
            else if(quote.RareCost>0&&State.Campaign.Materials[quote.RareRegion]<quote.RareCost)quote.Blocker="Need "+(quote.RareCost-State.Campaign.Materials[quote.RareRegion])+" more "+MaterialName(quote.RareRegion).ToLowerInvariant()+".";
            quote.CanBegin=quote.Blocker.Length==0;return quote;
        }
        public static CraftedItem PreviewMasterwork(int recipeId,int quality,int alloy,int temper,int rune)
        {
            if(!ValidRecipe(recipeId)||quality<0||quality>3||alloy<0||alloy>=AlloyOptions.Length||temper<0||temper>=TemperOptions.Length||rune<0||rune>=RuneOptions.Length)return null;
            return new CraftedItem{RecipeId=recipeId,Name=GameContent.Recipes[recipeId].Name,Quality=quality,Alloy=alloy,Temper=temper,Rune=rune,Masterwork=true,Protected=true};
        }
        public string CreationEquipBlocker(CraftedItem item)
        {
            if(item==null||!State.Inventory.Contains(item)||!ValidRecipe(item.RecipeId))return "This piece has already left storage.";
            if(State.Campaign.Battle.Active||SagaInProgress)return "Finish the expedition before changing equipment.";
            var hero=State.Campaign.Heroes[State.Campaign.SelectedHero];
            if(!hero.Recruited)return "Choose a recruited companion to equip this piece.";
            var slot=GameContent.Recipes[item.RecipeId].Slot;
            var previous=slot==GearSlot.Weapon?hero.Weapon:slot==GearSlot.Armor?hero.Armor:hero.Accessory;
            if(State.Inventory.Count-1+(previous==null?0:1)+(IsRunning(Station.Forge)?1:0)>InventoryCapacity)return "Free a storage slot for the replaced equipment.";
            return "";
        }
        public void NormalizeArtisan()
        {
            if(State.Artisan==null)State.Artisan=new ArtisanState();
            State.Artisan.Completed=Clamp(State.Artisan.Completed,0,MaxCounter);
            var last=State.Artisan.LastCreation;
            if(last!=null)
            {
                if(!ValidRecipe(last.RecipeId)||last.Stage!=ArtisanStage.Complete||last.CreationNumber<1||last.CreationNumber>State.Artisan.Completed)State.Artisan.LastCreation=null;
                else NormalizeArtisanProject(last);
            }
            foreach(var item in State.Inventory)NormalizeArtisanItem(item);
            foreach(var hero in State.Campaign.Heroes){NormalizeArtisanItem(hero.Weapon);NormalizeArtisanItem(hero.Armor);NormalizeArtisanItem(hero.Accessory);}
            var p=State.Artisan.Project;
            if(p==null)return;
            if(!IsRunning(Station.Forge)||p.RecipeId!=State.PendingRecipe||!ValidRecipe(p.RecipeId)||p.Alloy<0||p.Alloy>2||(int)p.Stage<0||p.Stage>ArtisanStage.Ready)
            {State.Artisan.Project=null;return;}
            NormalizeArtisanProject(p);
            State.Progress[2]=0;State.PendingQuality=p.Quality;State.ManualForge=true;State.PendingProtection=true;
        }
        static void NormalizeArtisanProject(MasterworkProject p)
        {
            p.Alloy=Clamp(p.Alloy,0,2);p.Temper=Clamp(p.Temper,0,2);p.Rune=Clamp(p.Rune,0,3);p.Strikes=Clamp(p.Strikes,0,3);p.Quality=Clamp(p.Quality,0,3);
            if(p.Stage==ArtisanStage.Shape&&p.Strikes>=3)p.Stage=ArtisanStage.Quench;
            var source=p.Feedback;var feedback=new ArtisanStrikeFeedback[3];
            for(int i=0;source!=null&&i<Math.Min(p.Strikes,source.Length);i++)
            {
                var f=source[i];
                if(f==null||f.Strike!=i+1||!Finite(f.Accuracy)||f.Accuracy<0||f.Accuracy>1||f.QualityBefore<0||f.QualityBefore>3||f.QualityAfter!=Math.Min(3,f.QualityBefore+(f.Accuracy>=.82?1:0)))continue;
                feedback[i]=f;
            }
            // A legacy piece keeps its saved quality; absent strike records stay absent.
            p.Feedback=feedback;
        }
        static void NormalizeArtisanItem(CraftedItem item)
        {
            if(item==null)return;
            item.Alloy=item.Masterwork?Clamp(item.Alloy,0,2):0;item.Temper=item.Masterwork?Clamp(item.Temper,0,2):0;item.Rune=item.Masterwork?Clamp(item.Rune,0,3):0;item.Provenance=Clamp(item.Provenance,0,MaxCounter);
        }
        public bool BeginMasterwork(int recipeId,int alloy)
        {
            var quote=QuoteMasterwork(recipeId,alloy);if(!quote.CanBegin)return false;
            if(!quote.Reserved)
            {
                int previous=State.SelectedRecipe;State.SelectedRecipe=recipeId;
                if(!TryStart(Station.Forge,true)){State.SelectedRecipe=previous;return false;}
            }
            State.Ingots-=AlloyOptions[alloy].ExtraIngots;State.PendingProtection=true;
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
            int qualityBefore=p.Quality;p.Strikes++;
            if(accuracy>=.82){p.Quality=Math.Min(3,p.Quality+1);State.Combo=Increment(State.Combo,1);State.BestCombo=Math.Max(State.BestCombo,State.Combo);}
            else State.Combo=0;
            p.Feedback[p.Strikes-1]=new ArtisanStrikeFeedback{Strike=p.Strikes,Accuracy=accuracy,QualityBefore=qualityBefore,QualityAfter=p.Quality};
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
            item.Value=Math.Min(MaxItemValue,(int)(item.Value*1.25));p.Stage=ArtisanStage.Complete;p.CreationNumber=item.Provenance;State.Artisan.LastCreation=p;State.Artisan.Completed=Increment(State.Artisan.Completed,1);
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
