using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        readonly RawImage[] townsfolk=new RawImage[3],townRepairs=new RawImage[3],townShadows=new RawImage[3];
        readonly RectTransform[] raindrops=new RectTransform[20];
        RawImage customerShadow;
        Image nightShade;
        static readonly Vector2[] WalkFrom={new Vector2(105,290),new Vector2(294,468),new Vector2(162,548)};
        static readonly Vector2[] WalkTo={new Vector2(275,290),new Vector2(240,526),new Vector2(300,548)};

        RawImage GroundShadow(string name,Vector2 feet,float width,float height)
        {
            var shadow=ForgeUI.Light(name,scene,(feet.x-width*.5f)*Pixel,(feet.y-height*.5f)*Pixel,width*Pixel,height*Pixel);
            shadow.color=new Color(0,0,0,.76f);return shadow;
        }
        void BuildTownLife()
        {
            townRepairs[0]=HandDrawnArt.Prop("RestoredFlowers",scene,5,5*Pixel,365*Pixel,48*Pixel,31*Pixel);
            townRepairs[1]=HandDrawnArt.Prop("RestoredLantern",scene,6,327*Pixel,408*Pixel,27*Pixel,43*Pixel);
            townRepairs[2]=HandDrawnArt.Prop("RestoredNoticeboard",scene,8,13*Pixel,308*Pixel,67*Pixel,57*Pixel);
            for(int i=0;i<3;i++)
            {
                townShadows[i]=GroundShadow("TownFeet"+i,WalkFrom[i],28,5);
                townsfolk[i]=HandDrawnArt.Person("Townsfolk"+i,scene,"walkers",i*8,WalkFrom[i]*Pixel,56*Pixel,74*Pixel);
            }
            ForgeUI.Button("TownNoticeboard",scene,"",18*Pixel,303*Pixel,86*Pixel,60*Pixel,Color.clear,Color.clear,()=>ShowQuests(true));
            nightShade=ForgeUI.Fill("Nightfall",scene,0,0,1080,1665,new Color(.05f,.09f,.19f,0));
            for(int i=0;i<raindrops.Length;i++)
            {
                raindrops[i]=ForgeUI.Fill("Rain"+i,scene,0,0,2,12,new Color(.68f,.81f,.89f,.16f)).rectTransform;
                raindrops[i].gameObject.SetActive(false);
            }
            selection.SetAsLastSibling();foreach(var progress in bars)progress.SetAsLastSibling();
        }

        void TickTownLife(float delta)
        {
            if(!nightShade)return;
            double clock=app.Sim.State.SimulationTime;bool still=app.Sim.State.ReducedMotion;
            float phase=still?.55f:(float)(clock%600)/600;
            nightShade.color=new Color(.05f,.09f,.19f,.035f*(.5f+.5f*Mathf.Cos(phase*Mathf.PI*2)));
            for(int i=0;i<3;i++)
            {
                var pose=WalkMotion.Sample(still?0:sceneTime+i*4.7,Vector2.Distance(WalkFrom[i],WalkTo[i]),i==0?20:17,1.3,24);
                var feet=Vector2.Lerp(WalkFrom[i],WalkTo[i],(float)pose.Progress);
                bool right=WalkTo[i].x>WalkFrom[i].x?pose.FacingRight:!pose.FacingRight;
                HandDrawnArt.Pose(townsfolk[i],pose.Moving?"walkers":"idles",pose.Moving?i*8+pose.Frame:i*2+(pose.Turning?1:0),feet*Pixel,56*Pixel,74*Pixel,right);
                townShadows[i].rectTransform.anchoredPosition=new Vector2((feet.x-14)*Pixel,-(feet.y-2.5f)*Pixel);
            }
            bool rain=!still&&(int)(clock/180)%4==2;
            for(int i=0;i<raindrops.Length;i++)
            {
                raindrops[i].gameObject.SetActive(rain);
                if(rain)raindrops[i].anchoredPosition=new Vector2((i*137+sceneTime*21)%1080,-((i*211+sceneTime*350)%1665));
            }
        }
        void TickCustomer()
        {
            var from=new Vector2(321,522);var to=new Vector2(151,522);
            var pose=WalkMotion.Sample(app.Sim.State.ReducedMotion?0:sceneTime+5.1,170,19,1.6,24);
            var feet=Vector2.Lerp(from,to,(float)pose.Progress);
            HandDrawnArt.Pose(customer,pose.Moving?"walkers":"idles",pose.Moving?24+pose.Frame:6+(pose.Turning?1:0),feet*Pixel,56*Pixel,74*Pixel,!pose.FacingRight);
            customerShadow.rectTransform.anchoredPosition=new Vector2((feet.x-14)*Pixel,-(feet.y-2.5f)*Pixel);
            carriedSword.gameObject.SetActive(false);
        }
        void RefreshTownLife()
        {
            if(!nightShade)return;
            for(int i=0;i<3;i++)townRepairs[i].gameObject.SetActive(app.Sim.State.Campaign.TownLevel>i);
        }
    }
}
