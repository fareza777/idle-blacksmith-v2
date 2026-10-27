using System;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        static readonly ArtisanOption[] strikeTechniques={
            new ArtisanOption(0,"Even balance","Keep the pattern's power, guard and natural affinity."),
            new ArtisanOption(1,"Guarded balance","Power -15%; add guard equal to 20% of the quality-adjusted power (at least 1). Keep the natural affinity."),
            new ArtisanOption(2,"Serrated grind","Power -10%; "+EffectDescription(GearEffect.ArmorBreak)+" Adds this secondary affinity alongside the pattern or chosen rune.")
        };
        static readonly ArtisanOption[] fittingTechniques={
            new ArtisanOption(0,"Balanced fitting","Keep the pattern's guard, power and natural affinity."),
            new ArtisanOption(1,"Mobile fitting","Guard -15%; add power equal to 20% of the quality-adjusted guard (at least 1). Keep the natural affinity."),
            new ArtisanOption(2,"Mending seams","Guard -12%; "+EffectDescription(GearEffect.Mend)+" Adds this secondary affinity alongside the pattern or chosen rune.")
        };
        static readonly ArtisanOption[] engravingTechniques={
            new ArtisanOption(0,"Open channels","Keep the pattern's power, guard and natural affinity."),
            new ArtisanOption(1,"Sheltered channels","Power -15%; add guard equal to 30% of the quality-adjusted power (at least 1). Keep the natural affinity."),
            new ArtisanOption(2,"Focused channels","Power and guard -10%; "+EffectDescription(GearEffect.Focus)+" Adds this secondary affinity alongside the pattern or chosen rune.")
        };
        static readonly ArtisanOption[] stormGrindTechniques={
            strikeTechniques[0],strikeTechniques[1],
            new ArtisanOption(2,"Storm-forged grind","Power -10%; "+EffectDescription(GearEffect.ChainLightning)+" Adds a secondary affinity; keeps the pattern or chosen rune active. With a Storm rune, the technique instead adds armor break.")
        };
        static readonly ArtisanOption[] shelteringTechniques={
            fittingTechniques[0],fittingTechniques[1],
            new ArtisanOption(2,"Sheltering seams","Guard -12%; "+EffectDescription(GearEffect.Shelter)+" Adds this secondary affinity alongside the pattern or chosen rune.")
        };
        static readonly ArtisanOption[] wardingTechniques={
            engravingTechniques[0],engravingTechniques[1],
            new ArtisanOption(2,"Warding channels","Power and guard -10%; "+EffectDescription(GearEffect.BraceBarrier)+" Adds a secondary affinity; keeps the pattern or chosen rune active. With an Aegis rune, the technique instead adds Focus.")
        };

        public static ArtisanMethod ArtisanMethodFor(int recipeId)
        {
            if(!ValidRecipe(recipeId))return ArtisanMethod.Strike;
            var recipe=GameContent.Recipes[recipeId];
            return recipe.Slot==GearSlot.Accessory||recipe.Kind==WeaponKind.Staff?ArtisanMethod.Engraving:recipe.Slot==GearSlot.Armor?ArtisanMethod.Fitting:ArtisanMethod.Strike;
        }
        public static double ArtisanTimingSpeed(int recipeId)
        {
            var method=ArtisanMethodFor(recipeId);
            return method==ArtisanMethod.Fitting?.48:method==ArtisanMethod.Engraving?.55:.72;
        }
        public static double ArtisanTimingTarget(int recipeId,int strikes)
        {
            var method=ArtisanMethodFor(recipeId);int mark=Clamp(strikes,0,2);
            if(method==ArtisanMethod.Fitting)return mark==0?.30:mark==1?.70:.50;
            if(method==ArtisanMethod.Engraving)return mark==0?.38:mark==1?.62:.50;
            return .50;
        }
        public static double ArtisanTimingMarker(int recipeId,double sceneSeconds)
        {
            if(!Finite(sceneSeconds))return 0;
            double phase=sceneSeconds*ArtisanTimingSpeed(recipeId)%2;
            if(phase<0)phase+=2;
            return phase<=1?phase:2-phase;
        }
        public static double ArtisanTimingAccuracy(int recipeId,int strikes,double marker)
        {
            if(!Finite(marker))return 0;
            return Math.Max(0,Math.Min(1,1-Math.Abs(marker-ArtisanTimingTarget(recipeId,strikes))*2));
        }
        public static ArtisanOption[] TechniqueOptionsFor(int recipeId)
        {
            var method=ArtisanMethodFor(recipeId);var effect=ArtisanTechniqueEffect(recipeId);
            return method==ArtisanMethod.Fitting?(effect==GearEffect.Shelter?shelteringTechniques:fittingTechniques):
                method==ArtisanMethod.Engraving?(effect==GearEffect.BraceBarrier?wardingTechniques:engravingTechniques):
                effect==GearEffect.ChainLightning?stormGrindTechniques:strikeTechniques;
        }
        static GearEffect ArtisanTechniqueEffect(int recipeId)
        {
            var method=ArtisanMethodFor(recipeId);var natural=ValidRecipe(recipeId)?GameContent.Recipes[recipeId].Effect:GearEffect.None;
            if(method==ArtisanMethod.Fitting)return natural==GearEffect.Mend?GearEffect.Shelter:GearEffect.Mend;
            if(method==ArtisanMethod.Engraving)return natural==GearEffect.Focus?GearEffect.BraceBarrier:GearEffect.Focus;
            return natural==GearEffect.ArmorBreak?GearEffect.ChainLightning:GearEffect.ArmorBreak;
        }

        static double ArtisanTechniqueStat(CraftedItem item,bool power)
        {
            var recipe=GameContent.Recipes[item.RecipeId];int quality=Clamp(item.Quality,0,3);
            double p=recipe.Power*(4+quality)/4,g=recipe.Guard*(4+quality)/4;
            var method=ArtisanMethodFor(item.RecipeId);
            if(item.Technique==1)
            {
                if(method==ArtisanMethod.Fitting){p+=Math.Max(1,Math.Floor(g*.2));g*=.85;}
                else {g+=Math.Max(1,Math.Floor(p*(method==ArtisanMethod.Engraving?.3:.2)));p*=.85;}
            }
            else if(item.Technique==2)
            {
                if(method==ArtisanMethod.Fitting)g*=.88;
                else {p*=.9;if(method==ArtisanMethod.Engraving)g*=.9;}
            }
            return power?p:g;
        }

        public static ArtisanWorkpieceFamily ArtisanWorkpieceFor(int recipeId)
        {
            if(!ValidRecipe(recipeId))return ArtisanWorkpieceFamily.Blade;
            var recipe=GameContent.Recipes[recipeId];
            if(recipe.Slot==GearSlot.Accessory)return ArtisanWorkpieceFamily.Accessory;
            if(recipe.Slot==GearSlot.Armor)
            {
                var silhouette=ArmorAppearance.ForRecipe(recipeId);
                if(silhouette==ArmorSilhouette.Leather)return ArtisanWorkpieceFamily.Leather;
                if(silhouette==ArmorSilhouette.Robe)return ArtisanWorkpieceFamily.Robe;
                return ArtisanWorkpieceFamily.Mail;
            }
            if(recipe.Kind==WeaponKind.Bow)return ArtisanWorkpieceFamily.Bow;
            if(recipe.Kind==WeaponKind.Hammer||recipe.Kind==WeaponKind.Axe)return ArtisanWorkpieceFamily.Hammer;
            if(recipe.Kind==WeaponKind.Spear||recipe.Kind==WeaponKind.Staff)return ArtisanWorkpieceFamily.Pole;
            return ArtisanWorkpieceFamily.Blade;
        }
        public static int ArtisanWorkpieceStage(ArtisanStage stage,int strikes)
        {return stage>=ArtisanStage.Ready?4:stage==ArtisanStage.Engrave?3:stage==ArtisanStage.Quench?2:stage==ArtisanStage.Shape?Clamp(strikes,0,2):0;}
    }
}
