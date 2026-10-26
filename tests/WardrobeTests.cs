using System;
using ForgeEmpire;

public static class WardrobeTests
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(Action<string,Action> test)
    {
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
