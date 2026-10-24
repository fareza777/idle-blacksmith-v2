using System;

namespace ForgeEmpire
{
    [Serializable]
    public sealed class GuildContract
    {
        public bool Active;
        public int RecipeId, Region, Quantity, MinimumQuality;
    }

    public sealed class CommissionDefinition
    {
        public int RecipeId, Region, Quantity, MinimumQuality, Gold, Reputation;
        public string Patron, Title;
    }
}
