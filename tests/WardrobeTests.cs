using System;
using System.Reflection;
using ForgeEmpire;

public static class WardrobeTests
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(Action<string,Action> test)
    {
        test("weapon pose rig keeps grips bounded and weapon actions distinct through every hero pose",()=>{
            var type=typeof(ArmorAppearance).Assembly.GetType("ForgeEmpire.WeaponPoseRig");
            Check(type!=null,"missing weapon-specific pose rig");
            var method=type.GetMethod("Evaluate",BindingFlags.Public|BindingFlags.Static);Check(method!=null,"missing pose evaluation");
            foreach(WeaponKind kind in new[]{WeaponKind.Sword,WeaponKind.Axe,WeaponKind.Bow,WeaponKind.Hammer,WeaponKind.Staff,WeaponKind.Daggers,WeaponKind.Greatsword,WeaponKind.Spear})
            for(int hero=0;hero<6;hero++)for(int pose=0;pose<20;pose++)
            {
                object sample=method.Invoke(null,new object[]{kind,pose,hero,.18,false});var shape=sample.GetType();
                Func<string,double> value=name=>Convert.ToDouble(shape.GetField(name).GetValue(sample));
                Check(value("GripX")>=0&&value("GripX")<=1.1&&value("GripY")>=0&&value("GripY")<=1,"grip outside painted body");
                Check(!double.IsNaN(value("Angle"))&&!double.IsInfinity(value("Angle")),"invalid weapon angle");
                Check(value("BodyPose")>=0&&value("BodyPose")<20,"pose outside painted atlas");
                object still=method.Invoke(null,new object[]{kind,pose,hero,.18,true});
                Check(Convert.ToDouble(shape.GetField("Lean").GetValue(still))==0&&Convert.ToDouble(shape.GetField("Sway").GetValue(still))==0,"reduced motion moved skeleton");
            }
            object sword=method.Invoke(null,new object[]{WeaponKind.Sword,3,0,.08,false}),hammer=method.Invoke(null,new object[]{WeaponKind.Hammer,3,0,.08,false});
            Check(!sword.GetType().GetField("Angle").GetValue(sword).Equals(hammer.GetType().GetField("Angle").GetValue(hammer)),"heavy and light contact must have distinct motion");
            object spear=method.Invoke(null,new object[]{WeaponKind.Spear,3,0,.08,false}),bow=method.Invoke(null,new object[]{WeaponKind.Bow,2,1,.18,false});
            Check(Convert.ToDouble(spear.GetType().GetField("Reach").GetValue(spear))>0,"spear never thrusts");
            Check(Convert.ToDouble(bow.GetType().GetField("Draw").GetValue(bow))>0,"bow never draws");
        });
        test("unfinished armor uses its authored worn family including released wards",()=>{
            foreach(var recipe in GameContent.Recipes)if(recipe.Slot==GearSlot.Armor)
            {
                var family=ArmorAppearance.ForRecipe(recipe.Id);
                var expected=family==ArmorSilhouette.Robe?ArtisanWorkpieceFamily.Robe:family==ArmorSilhouette.Leather?ArtisanWorkpieceFamily.Leather:ArtisanWorkpieceFamily.Mail;
                Check(WorkshopSimulation.ArtisanWorkpieceFor(recipe.Id)==expected,"unfinished family differs for "+recipe.Name);
            }
        });
        test("worn armor maps released mail leather and ward recipes to coherent body families",()=>{
            foreach(var recipe in GameContent.Recipes)
            {
                var family=ArmorAppearance.ForRecipe(recipe.Id);
                if(recipe.Slot!=GearSlot.Armor){Check(family==ArmorSilhouette.None,"non-armor became an outfit");continue;}
                var expected=recipe.Name.Contains("Leather")?ArmorSilhouette.Leather:recipe.Name.Contains("Ward")||recipe.Name.Contains("Robe")?ArmorSilhouette.Robe:ArmorSilhouette.Mail;
                Check(family==expected,"wrong worn shape for "+recipe.Name);
            }
            Check(ArmorAppearance.ForRecipe(-1)==ArmorSilhouette.None&&ArmorAppearance.ForRecipe(999)==ArmorSilhouette.None,"invalid armor must not index content");
        });
    }
}
