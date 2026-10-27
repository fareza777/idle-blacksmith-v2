using UnityEngine;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        readonly GameObject[,] townProjectStages=new GameObject[6,3];
        void DrawRestorationProgress(Transform parent,int region,int level,float x,float y,float width,float height)
        {
            var stage=ForgeUI.Rect("RestorationStage"+region+"Tier"+level,parent,x,y,width,height);
            // The first paid tier reopens a worksite; the second repairs the landmark;
            // the third adds the community's permanent finishing work.
            HandDrawnArt.Piece("RestorationLandmark"+region,stage,"landmarks",region*2+(level>=2?1:0),width*.58f,0,width*.41f,height*.94f);
            if(level==0)return;
            HandDrawnArt.Piece("RestorationMaterials"+region,stage,"props",7,width*.08f,height*.57f,width*.2f,height*.4f);
            HandDrawnArt.Piece("RestorationTools"+region,stage,"props",2,width*.32f,height*.16f,width*.18f,height*.32f);
            if(level>=2)HandDrawnArt.Person("RestorationWorker"+region,stage,"idles",(region%3)*2,new Vector2(width*.43f,height*.96f),width*.17f,height*.7f,region%2==0);
            if(level>=3)
            {
                HandDrawnArt.Piece("RestorationPennants"+region,stage,"props",3,width*.18f,0,width*.48f,height*.22f);
                HandDrawnArt.Piece("RestorationFlowers"+region,stage,"props",5,width*.62f,height*.77f,width*.2f,height*.23f);
                HandDrawnArt.Piece("RestorationLantern"+region,stage,"props",6,width*.52f,height*.34f,width*.1f,height*.37f);
                ForgeUI.Light("RestorationLamplight"+region,stage,width*.48f,height*.34f,width*.18f,height*.44f).color=new Color(1,.82f,.5f,.35f);
            }
        }
        // Root integration: invoke after BuildDiorama's WorkshopArt, before foreground actors.
        void BuildTownProgressionScene()
        {
            Vector2[] locations={new Vector2(7,281),new Vector2(300,422),new Vector2(329,309),new Vector2(301,130),new Vector2(5,333),new Vector2(169,88)};
            int[] regionalProps={0,7,6,1,8,4};
            for(int region=0;region<6;region++)for(int tier=1;tier<=3;tier++)
            {
                var location=locations[region];var group=ForgeUI.Rect("EarnedTownRegion"+region+"Tier"+tier,scene,location.x*Pixel,location.y*Pixel,64*Pixel,64*Pixel);
                townProjectStages[region,tier-1]=group.gameObject;
                HandDrawnArt.Piece("TownProjectProp"+region,group,"props",tier==1?7:regionalProps[region],0,12*Pixel,48*Pixel,46*Pixel);
                if(tier>=2)
                {
                    HandDrawnArt.Piece("TownProjectTools"+region,group,"props",2,36*Pixel,14*Pixel,25*Pixel,21*Pixel);
                    HandDrawnArt.Person("TownProjectWorker"+region,group,"idles",(region%3)*2,new Vector2(54*Pixel,64*Pixel),25*Pixel,42*Pixel,region%2==0);
                }
                if(tier>=3)
                {
                    HandDrawnArt.Piece("TownProjectPennant"+region,group,"props",3,0,0,60*Pixel,19*Pixel);
                    HandDrawnArt.Piece("TownProjectFlowers"+region,group,"props",5,24*Pixel,47*Pixel,32*Pixel,18*Pixel);
                }
            }
            RefreshTownProgressionScene();
        }
        // Root integration: invoke beside RefreshTownLife() in Refresh().
        void RefreshTownProgressionScene()
        {
            for(int region=0;region<6;region++)for(int tier=1;tier<=3;tier++)
                if(townProjectStages[region,tier-1])townProjectStages[region,tier-1].SetActive(app.Sim.State.World.Projects[region]==tier);
        }
    }
}
