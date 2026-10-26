using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed class ChronicleAtmosphere
    {
        readonly RawImage background;
        readonly RawImage[] motes=new RawImage[10];
        readonly int region;
        readonly float width,height;
        public ChronicleAtmosphere(Transform stage,RawImage landscape,int place)
        {
            region=place;background=landscape;var bounds=(RectTransform)stage;width=bounds.rect.width;height=bounds.rect.height;
            background.rectTransform.sizeDelta=new Vector2(width+12,height+7);
            if(!stage.GetComponent<RectMask2D>())stage.gameObject.AddComponent<RectMask2D>();
            for(int i=0;i<motes.Length;i++)
            {
                bool mist=region==1&&i<2;
                motes[i]=ForgeUI.Light("Atmosphere"+i,stage,0,0,mist?780:region==2?7:5,mist?125:region==2?7:5);
                motes[i].raycastTarget=false;
            }
        }
        public void Tick(float time,bool reduced)
        {
            if(!background)return;float t=reduced?0:time;
            background.rectTransform.anchoredPosition=new Vector2(-6+Mathf.Sin(t*.13f)*3,3);
            for(int i=0;i<motes.Length;i++)
            {
                var image=motes[i];if(!image)continue;bool mist=region==1&&i<2;
                float seed=i*97.13f,x=mist?-100+Mathf.Sin(t*.06f+i)*90:Mathf.Repeat(seed*13+t*(region==2?4:2),width-10),y=mist?height*.43f+i*80:Mathf.Repeat(seed*7+t*(region==2?19:-11),height*.84f)+20;
                image.rectTransform.anchoredPosition=new Vector2(x,-y);
                Color tint=region==0||region==3?new Color(1,.66f,.32f,.14f):region==2?new Color(.84f,.94f,1,.27f):region==1?new Color(.76f,.9f,.83f,mist?.055f:.13f):new Color(.95f,.87f,.62f,.18f);
                image.color=tint;
            }
        }
    }
}
