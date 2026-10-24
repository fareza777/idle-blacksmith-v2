using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        Vector2 cameraFrom,cameraTo;
        float cameraStarted,pageStarted;
        bool cameraMoving;
        CanvasGroup pageFade;
        static float EaseOut(float value){return 1-Mathf.Pow(1-Mathf.Clamp01(value),3);}
        void MoveCamera(Vector2 target)
        {
            cameraFrom=scene.anchoredPosition;cameraTo=target;cameraStarted=Time.unscaledTime;
            cameraMoving=!app.Sim.State.ReducedMotion&&app.enabled;
            if(!cameraMoving)scene.anchoredPosition=target;
        }
        void BeginPageMotion()
        {
            pageFade=modal.gameObject.AddComponent<CanvasGroup>();pageStarted=Time.unscaledTime;
            pageFade.alpha=app.Sim.State.ReducedMotion||!app.enabled?1:0;
        }
        void TickPresentationMotion()
        {
            ForgeUI.ReducedMotion=app.Sim.State.ReducedMotion;
            if(cameraMoving)
            {
                if(Input.GetMouseButton(0)){cameraMoving=false;}
                else{
                    float t=ForgeUI.ReducedMotion?1:(Time.unscaledTime-cameraStarted)/.24f;
                    scene.anchoredPosition=Vector2.Lerp(cameraFrom,cameraTo,EaseOut(t));if(t>=1)cameraMoving=false;
                }
            }
            if(pageFade&&modal&&pageFade.alpha<1)
            {
                float t=ForgeUI.ReducedMotion?1:(Time.unscaledTime-pageStarted)/.18f;
                pageFade.alpha=EaseOut(t);modal.anchoredPosition=new Vector2(0,-12*(1-EaseOut(t)));
            }
        }
    }
}
