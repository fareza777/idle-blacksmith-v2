using System;

namespace ForgeEmpire
{
    public enum ArtisanStage { Heat, Shape, Quench, Engrave, Ready, Complete }
    public enum ArtisanMethod { Strike, Fitting, Engraving }
    public enum ArtisanWorkpieceFamily { Blade, Hammer, Pole, Mail, Bow, Leather, Robe, Accessory }
    [Serializable] public sealed class ArtisanState
    {
        public MasterworkProject Project,LastCreation;
        public int Completed;
        public bool Guided;
    }
    [Serializable] public sealed class MasterworkProject
    {
        public int RecipeId, Alloy, Temper, Rune, Strikes, Quality, Technique;
        public int CreationNumber;
        public ArtisanStage Stage;
        public ArtisanStrikeFeedback[] Feedback = new ArtisanStrikeFeedback[3];
    }
    [Serializable] public sealed class ArtisanStrikeFeedback
    {
        public int Strike,QualityBefore,QualityAfter;
        public double Accuracy;
    }
    public sealed class ArtisanQuote
    {
        public int Ingots,RareCost,RareRegion=-1,Quality;
        public bool Reserved,CanBegin;
        public string Blocker="";
    }
    public sealed class ArtisanOption
    {
        public int Id,ExtraIngots,GoldCost;
        public string Name,Description;
        public ArtisanOption(int id,string name,string description,int ingots=0,int gold=0)
        {Id=id;Name=name;Description=description;ExtraIngots=ingots;GoldCost=gold;}
    }
}
