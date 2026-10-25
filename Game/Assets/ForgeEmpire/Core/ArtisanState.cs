using System;

namespace ForgeEmpire
{
    public enum ArtisanStage { Heat, Shape, Quench, Engrave, Ready, Complete }
    [Serializable] public sealed class ArtisanState { public MasterworkProject Project; public int Completed; }
    [Serializable] public sealed class MasterworkProject
    {
        public int RecipeId, Alloy, Temper, Rune, Strikes, Quality;
        public ArtisanStage Stage;
    }
    public sealed class ArtisanOption
    {
        public int Id,ExtraIngots,GoldCost;
        public string Name,Description;
        public ArtisanOption(int id,string name,string description,int ingots=0,int gold=0)
        {Id=id;Name=name;Description=description;ExtraIngots=ingots;GoldCost=gold;}
    }
}
