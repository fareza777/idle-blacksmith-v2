#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ForgeEmpire.Tests
{
    public sealed class HandDrawnMotionCapture : MonoBehaviour
    {
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--redesign-motion")<0)yield break;
            yield return new WaitForSecondsRealtime(.8f);
            var app=FindFirstObjectByType<WorkshopApp>();
            if(!ProbeRapidClose(app)){Application.Quit(1);yield break;}
            app.Sim.State.Ore=30;app.Sim.State.Ingots=30;
            for(int i=0;i<3;i++)app.Sim.State.Automated[i]=true;
            app.View.FocusStation(Station.Forge);
            string output=Application.isMobilePlatform?Path.Combine(Application.persistentDataPath,"motion-qa"):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/redesign-20261024/motion"));
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(.5f);
            for(int i=0;i<96;i++)
            {
                string frame="frame-"+i.ToString("D3")+".png";
                ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?Path.Combine("motion-qa",frame):Path.Combine(output,frame));
                yield return new WaitForSecondsRealtime(1f/12);
            }
            yield return new WaitForEndOfFrame();
            Debug.Log("HAND_DRAWN_MOTION_CAPTURE 96 frames");Application.Quit(0);
        }
        static bool ProbeRapidClose(WorkshopApp app)
        {
            try
            {
                app.View.ShowSettings();app.View.CloseModal();app.View.Tick(0);
                app.View.ShowHeroes();app.View.CloseModal();app.View.Tick(0);
                app.View.ShowWorld();app.View.CloseModal();app.View.Tick(0);
                if(app.View.HasModal)throw new Exception("Rapid close left a page open");
                Debug.Log("MOTION_RAPID_CLOSE_PASS");return true;
            }
            catch(Exception error){Debug.LogException(error);return false;}
        }
    }
}
#endif
