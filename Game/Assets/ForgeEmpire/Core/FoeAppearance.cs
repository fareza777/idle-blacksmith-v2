using System;
namespace ForgeEmpire
{
    public static class FoeAppearance
    {
        public static string Sheet(int enemy)
        {if(enemy<0||enemy>=48)throw new ArgumentOutOfRangeException("enemy");return "foes-"+(enemy/8)+"-"+(enemy%8/2);}
        public static int Frame(int enemy,int pose)
        {if(enemy<0||enemy>=48||pose<0||pose>5)throw new ArgumentOutOfRangeException();return enemy%2*6+pose;}
    }
}
