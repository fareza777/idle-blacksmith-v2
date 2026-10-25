using System;

namespace ForgeEmpire
{
    public static partial class GameContent
    {
        public static readonly RecipeDefinition[] Recipes;
        public static readonly RegionDefinition[] Regions;
        public static readonly HeroDefinition[] Heroes;
        public static readonly QuestDefinition[] Quests;
        public static int RegionCount { get { return Regions.Length; } }
        public static int RecipeCount { get { return Recipes.Length; } }
        public static int HeroCount { get { return Heroes.Length; } }
        public static int QuestCount { get { return Quests.Length; } }
        public static int MainQuestId(int region,int step){return region<5?region*3+step:30+step;}
        public static int SideQuestId(int region,int step){return region<5?15+region*3+step:33+step;}
        static GameContent()
        {
            Recipes=new RecipeDefinition[108];Array.Copy(BaseRecipes,Recipes,20);
            foreach(var recipe in ChronicleContent.ExtraRecipes)Recipes[recipe.Id]=recipe;
            for(int i=0;i<20;i++)
            {
                Recipes[i].Kind=i%4<2?(i%4==0?WeaponKind.Sword:WeaponKind.Axe):WeaponKind.None;
                Recipes[i].Effect=i==16?GearEffect.ChainLightning:i==17?GearEffect.LifeOnKill:i%4==3?GearEffect.BraceBarrier:GearEffect.None;
            }
            Regions=new RegionDefinition[6];Array.Copy(BaseRegions,Regions,5);
            Regions[5]=new RegionDefinition{Id=5,Name="Dawnforge",BossName="Dawnforge Prime",Weakness="Void",Description="A sunken observatory rises beyond the restored citadel. Void interrupts its solar engines; Storm is resisted. Sunsteel unlocks legendary commissions and the last restoration."};
            string[] lore={"Burning roots choke the old road. Frost breaks their heat; Flame is resisted. Supply trails recover cinders, while rescues bring the charcoal apprentices home.","Lost ferries drift through venomous mist. Flame sears the spirits; Frost is resisted. Recover marsh glass, rescue the missing travelers and restore the true crossing.","An icebound watchtower guards the mountain pass. Flame breaks the curse; Frost is resisted. Peak crystals strengthen the forge, while the missing beacon tender waits beyond the safe trail.","The mountain's ancient furnace has awakened. Frost quenches its sentries; Flame is resisted. Recover obsidian, rescue the cooling crew and tame the pressure heart.","A fallen star feeds the citadel's void wards. Storm disrupts them; Void is resisted. Star shards arm the final ascent, while returning scribes reveal the road to Dawnforge."};
            for(int i=0;i<5;i++)Regions[i].Description=lore[i];
            Heroes=new HeroDefinition[6];Array.Copy(BaseHeroes,Heroes,3);
            Heroes[3]=new HeroDefinition{Id=3,Name="Mira",Role="Alchemist",Cost=520,Power=5,Guard=2,Health=95,Description="A field apothecary. Mend heals the entire party and cleanses pressure. Staves and Renewal runes make her a steady companion on long routes."};
            Heroes[4]=new HeroDefinition{Id=4,Name="Toren",Role="Duelist",Cost=680,Power=10,Guard=1,Health=90,Description="A veteran of the citadel dueling halls. Riposte deals damage and counters the next charged blow. Fast blades reward careful timing."};
            Heroes[5]=new HeroDefinition{Id=5,Name="Edda",Role="Warden",Cost=820,Power=5,Guard=5,Health=120,Description="A mountain rescuer who stayed with the last beacon. Sanctuary raises barriers for every companion and briefly protects the weakest hero."};
            Heroes[0].Description="The town's last guardian. Bastion protects the entire party. High health and guard make Rowan a steady front line; equip a weapon and armor before a dangerous journey.";
            Heroes[1].Description="A pathfinder searching for the lost caravans. Pinpoint marks the true target and deals focused damage. Fast attacks reward elemental weapons, but Lyra needs protection against heavy blows.";
            Heroes[2].Description="Keeper of the shattered star-runes. Runebreak disrupts channels and breaks armor. Matching a region's weakness grants 60% weapon damage and 30% armor protection.";
            Quests=new QuestDefinition[36];Array.Copy(BaseQuests,Quests,30);
            foreach(var quest in Quests)if(quest!=null)quest.Consequence=quest.Consequence.Replace("four recipes","eighteen patterns");
            Quests[14].Consequence="The citadel is free. Its restored lens reveals Dawnforge and eighteen final patterns beyond the valley.";
            Quests[30]=Quest(30,5,true,QuestKind.Craft,"A Light Beyond the Valley","Keeper Aster","The observatory has risen. Aster needs equipment that can safely carry its impossible light.","Craft any Dawnforge recipe.","Aster opens the observatory ledger and funds the ascent.",1,350,28);
            Quests[31]=Quest(31,5,true,QuestKind.Expedition,"The Sunken Meridian","Keeper Aster","Silent engines measure a dawn that never arrives. Establish a living path through the broken meridian.","Win and claim a Dawnforge expedition.","The restored meridian returns power to the valley's workshops.",1,430,35);
            Quests[32]=Quest(32,5,true,QuestKind.Boss,"The Dawn We Choose","Keeper Aster","Dawnforge Prime would lock the valley in a perfect, lifeless morning. Stop its engine and let the world change again.","Defeat and claim Dawnforge Prime.","Six regions are free. Twelve guardians, legendary commissions and optional legacy await your guild.",1,750,70);
            Quests[33]=Quest(33,5,false,QuestKind.Delivery,"A Lens for Every Home","Glassworker Neve","Neve needs a stable Eclipse Sword to cut safe lenses for the returning families.","Deliver one unequipped Eclipse Sword.","The village receives new windows and you receive gold and sunsteel.",1,390,28,20,0,2);
            Quests[34]=Quest(34,5,false,QuestKind.Rescue,"The Last Morning Watch","Keeper Aster","The observatory apprentices are trapped in the outer solar ring. Lead an elite rescue.","Win and claim the Dawnforge elite route.","The apprentices restore the observatory and bring three sunsteel.",1,450,32,0,0,3,1);
            Quests[35]=Quest(35,5,false,QuestKind.QualityDelivery,"A Promise of Tomorrow","Carpenter Pell","Pell wants the new bell tower to survive the next impossible dawn.","Deliver a quality 2+ Dawnstar Ward.","The ward anchors the bell tower; the town rewards your masterful work.",1,520,38,23,2,2);
        }
    }
}
