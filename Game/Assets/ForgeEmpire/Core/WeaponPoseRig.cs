using System;

namespace ForgeEmpire
{
    public struct WeaponRigPose
    {
        public double GripX,GripY,Angle,Reach,Lean,Sway,Draw;
        public int BodyPose;
    }

    // Normalized attachment and joint motion in painted-body coordinates, top-left origin.
    public static class WeaponPoseRig
    {
        static readonly double[] handX={.67,.56,.49,.86,.87,.72,.55,.73,.61,.51,.79,.72,.62,.59,.64,.66,.67,.69,.67,.65};
        static readonly double[] handY={.56,.43,.17,.36,.40,.57,.36,.32,.56,.59,.39,.45,.18,.71,.55,.52,.54,.57,.55,.52};
        static readonly double[] heroX={0,-.015,.01,.01,-.015,.005},heroY={0,-.015,.005,.005,.005,-.01};
        public static WeaponRigPose Evaluate(WeaponKind kind,int pose,int hero,double age,bool reduced)
        {
            if((int)kind<0||(int)kind>8||pose<0||pose>19||hero<0||hero>5||double.IsNaN(age)||double.IsInfinity(age))throw new ArgumentOutOfRangeException();
            age=Math.Max(0,age);bool contact=pose==3||pose==4,heavy=kind==WeaponKind.Hammer||kind==WeaponKind.Greatsword||kind==WeaponKind.Axe;
            var sample=new WeaponRigPose{GripX=handX[pose]+heroX[hero],GripY=handY[pose]+heroY[hero],BodyPose=pose,Angle=pose==2?35:pose==3?-82:pose==4?-132:pose==6?-25:pose==7?-70:-135};
            double pulse=contact?Math.Sin(Math.Min(1,age/(heavy?.40:.24))*Math.PI):0;
            if(kind==WeaponKind.Bow)
            {
                sample.BodyPose=pose==1||pose==2?10:pose;
                sample.GripX=pose==1||pose==2?.78+heroX[hero]:contact?.86+heroX[hero]:.67+heroX[hero];
                sample.GripY=pose==1||pose==2?.39+heroY[hero]:contact?.36+heroY[hero]:.56+heroY[hero];
                sample.Angle=contact?-5:pose==1||pose==2?0:18;sample.Draw=pose==1||pose==2?Math.Min(1,age/.35):0;
            }
            else if(kind==WeaponKind.Staff)
            {
                if(pose==1||pose==2||contact){sample.BodyPose=10;sample.GripX=.79+heroX[hero];sample.GripY=.39+heroY[hero];}
                sample.Angle=contact?-20:-8;
            }
            else if(kind==WeaponKind.Spear)
            {
                if(pose==2){sample.BodyPose=1;sample.GripX=.56+heroX[hero];sample.GripY=.43+heroY[hero];}
                sample.Angle=contact?-90:pose==1||pose==2?-76:-32;
                sample.Reach=contact?.13*(.35+.65*pulse):0;
            }
            else if(heavy&&contact)sample.Angle=pose==3?-58-68*Math.Min(1,age/.25):-145;
            if(!reduced)
            {
                sample.Lean=contact?(heavy?3.5:1.6)*pulse:pose==1||pose==2?-1.8:pose==8?-3:0;
                sample.Sway=Math.Sin(age*7+hero*.7)*(contact?2.3:.7);
            }
            return sample;
        }
    }
}
