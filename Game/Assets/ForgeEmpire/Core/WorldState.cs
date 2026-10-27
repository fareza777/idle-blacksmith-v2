using System;

namespace ForgeEmpire
{
    [Serializable] public sealed class WorldState
    {
        public int[] Projects=new int[6],Trust=new int[6],Conversations=new int[12];
        public bool[] LegendaryClaims=new bool[12];
        public int ChoicesMade,RoomsVisited,SkillsUsed,RunsStarted;
    }
    public sealed class RestorationDefinition
    {
        public string Name,Npc,Description,Benefit;
        public RestorationDefinition(string name,string npc,string description,string benefit){Name=name;Npc=npc;Description=description;Benefit=benefit;}
    }
}
