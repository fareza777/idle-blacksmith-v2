using System;

namespace ForgeEmpire
{
    public struct WalkPose
    {
        public double Progress, FootPhase;
        public bool Moving, FacingRight, Turning;
        public int Frame;
    }

    public static class WalkMotion
    {
        public static WalkPose Sample(double time,double distance,double speed,double pause,double stride)
        {
            if(double.IsNaN(time)||double.IsInfinity(time)||distance<=0||speed<=0||stride<=0)return new WalkPose{FacingRight=true};
            pause=Math.Max(.1,pause);
            double travel=distance/speed,leg=travel+pause,clock=Math.Max(0,time)%(leg*2);
            bool outward=clock<leg;double local=outward?clock:clock-leg;
            bool moving=local>=pause;
            double fraction=moving?Math.Min(1,(local-pause)/travel):0;
            const double ramp=.1;
            double eased=fraction<ramp?fraction*fraction/(2*ramp*(1-ramp)):
                fraction>1-ramp?1-(1-fraction)*(1-fraction)/(2*ramp*(1-ramp)):(fraction-ramp*.5)/(1-ramp);
            double phase=(eased*distance/stride)%1;
            return new WalkPose{
                Progress=outward?eased:1-eased,Moving=moving,FacingRight=outward,Turning=!moving&&local<.24,
                FootPhase=phase,Frame=moving?(int)(phase*8)%8:0
            };
        }
    }
}
