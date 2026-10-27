using System;
using System.Collections.Generic;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        public string GuildRank
        {
            get
            {
                int reputation=State.Campaign.Reputation;
                return reputation>=600?"Master artisan":reputation>=250?"Guild artisan":reputation>=90?"Journeyman":reputation>=25?"Apprentice":"Newcomer";
            }
        }
        public int NextRankReputation { get { int r=State.Campaign.Reputation;return r<25?25:r<90?90:r<250?250:r<600?600:0; } }

        CommissionDefinition ContractDefinition(int recipe,int quantity,int quality)
        {
            var r=GameContent.Recipes[recipe];
            return new CommissionDefinition{RecipeId=recipe,Region=r.Region,Quantity=quantity,MinimumQuality=quality,
                Gold=(int)Math.Ceiling(r.Value*quantity*(1.7+quality*.3)*(1+(State.World==null?0:State.World.Projects[4])*.08)),Reputation=6+r.Region*3+quality*3,
                Patron=quality>0?"The guild quartermaster":r.Slot==GearSlot.Weapon?"The road patrol":"The valley guard",
                Title=quality>0?"An artisan's touch":r.Slot==GearSlot.Weapon?"Arm the next patrol":"Protection for the road"};
        }
        public CommissionDefinition CommissionOffer(int slot)
        {
            if(slot<0||slot>2)return null;
            int region=UnlockedRegion;
            int variation=State.ContractsCompleted%2;
            int tier=State.Campaign.Reputation>=250?2:State.Campaign.Reputation>=90?1:0;
            int recipe=region*4+(slot==0?variation:slot==1?2:3);
            int quantity=slot==0?1+tier:slot==1?2+tier:1+Math.Min(1,tier);
            int quality=slot==2?(tier==2?3:2):slot==1&&tier>=1||slot==0&&tier==2?1:0;
            return ContractDefinition(recipe,quantity,quality);
        }
        public CommissionDefinition ActiveCommission
        {
            get { var c=State.Contract;return c!=null&&c.Active?ContractDefinition(c.RecipeId,c.Quantity,c.MinimumQuality):null; }
        }
        public bool TryAcceptCommission(int slot)
        {
            if(ActiveCommission!=null)return false;
            var offer=CommissionOffer(slot);if(offer==null)return false;
            State.Contract=new GuildContract{Active=true,RecipeId=offer.RecipeId,Region=offer.Region,Quantity=offer.Quantity,MinimumQuality=offer.MinimumQuality};
            State.TrackCommission=true;return true;
        }
        public bool IsCommissionReserved(CraftedItem item)
        {
            var c=State.Contract;
            return c!=null&&c.Active&&item!=null&&item.RecipeId==c.RecipeId&&item.Quality>=c.MinimumQuality;
        }
        public bool IsJourneyReserved(CraftedItem item)
        {
            return State.ReserveJourneyGear&&item!=null&&item.RecipeId==State.ReservedJourneyRecipe;
        }
        public int CommissionProgress
        {
            get
            {
                var c=State.Contract;if(c==null||!c.Active)return 0;int count=0;
                foreach(var item in State.Inventory)if(IsCommissionReserved(item))count++;
                return Math.Min(c.Quantity,count);
            }
        }
        public bool TryDeliverCommission()
        {
            var offer=ActiveCommission;if(offer==null||CommissionProgress<offer.Quantity)return false;
            var stock=new List<CraftedItem>();
            // Use the least valuable qualifying stock; equipped gear is never in this list.
            for(int quality=offer.MinimumQuality;quality<=3&&stock.Count<offer.Quantity;quality++)
                foreach(var item in State.Inventory)
                    if(stock.Count<offer.Quantity&&item.RecipeId==offer.RecipeId&&item.Quality==quality)stock.Add(item);
            if(stock.Count!=offer.Quantity)return false;
            State.Contract.Active=false;State.TrackCommission=false;
            foreach(var item in stock)State.Inventory.Remove(item);
            AddGold(offer.Gold);State.Campaign.Reputation=Increment(State.Campaign.Reputation,offer.Reputation);
            State.ContractsCompleted=Increment(State.ContractsCompleted,1);return true;
        }
        public bool CancelCommission()
        {
            if(ActiveCommission==null)return false;
            State.Contract.Active=false;State.TrackCommission=false;return true;
        }
    }
}
