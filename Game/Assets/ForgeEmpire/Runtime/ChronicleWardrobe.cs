using System;

namespace ForgeEmpire
{
    public static class ChronicleWardrobe
    {
        public static readonly string[] Heroes={"rowan","lyra","kael","mira","toren","edda"};
        public static readonly string[] Families={"mail","leather","robe"};
        public static string Sheet(int heroId,int armorRecipe)
        {
            if(heroId<0||heroId>=Heroes.Length)throw new ArgumentOutOfRangeException(nameof(heroId));
            if(armorRecipe<0)return "hero-"+Heroes[heroId];
            var kind=ArmorAppearance.ForRecipe(armorRecipe);
            if(kind==ArmorSilhouette.None)throw new ArgumentException("The selected equipment is not armor.",nameof(armorRecipe));
            return "worn-"+Heroes[heroId]+"-"+Families[(int)kind-1];
        }
    }
}
