namespace ForgeEmpire
{
    public enum WeaponKind { Sword, Axe, Bow, Hammer, Staff, Daggers, Greatsword, Spear, None }
    public enum GearEffect { None, ChainLightning, BraceBarrier, LifeOnKill, ArmorBreak, Mend, Haste, Counter, Burn, Frostbite, Focus, Shelter }

    public sealed class EnemyDefinition
    {
        public int Id, Region, Archetype, Power, Health, Guard, AttackTicks;
        public string Name, Description, Element;
    }

    public sealed class BossDefinition
    {
        public int Id, Region, Mechanic, Health, Power;
        public string Name, Description, Telegraph, Counter;
    }
}
