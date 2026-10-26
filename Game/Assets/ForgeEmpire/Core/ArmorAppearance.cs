namespace ForgeEmpire
{
    public enum ArmorSilhouette { None, Mail, Leather, Robe }

    public static class ArmorAppearance
    {
        // Released IDs encode the authored armor families; display names are not an API.
        public static ArmorSilhouette ForRecipe(int recipeId)
        {
            if(recipeId<0||recipeId>=GameContent.RecipeCount||GameContent.Recipes[recipeId].Slot!=GearSlot.Armor)return ArmorSilhouette.None;
            if(recipeId<24)return recipeId%4==3?ArmorSilhouette.Robe:ArmorSilhouette.Mail;
            int offset=(recipeId-24)%14;
            return offset==7?ArmorSilhouette.Leather:offset==8?ArmorSilhouette.Robe:ArmorSilhouette.Mail;
        }
    }
}
