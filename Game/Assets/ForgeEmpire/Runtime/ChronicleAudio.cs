using UnityEngine;

namespace ForgeEmpire
{
    public sealed class ChronicleAudio : MonoBehaviour
    {
        WorkshopApp app;
        AudioSource a,b,ambience,boss,effects;
        readonly AudioClip[] themes=new AudioClip[6],beds=new AudioClip[6],tension=new AudioClip[6];
        readonly AudioClip[,] impacts=new AudioClip[5,3];
        int region=-2,variant;bool usingA=true,paused;
        public void Initialize(WorkshopApp owner,AudioSource effectSource)
        {
            app=owner;effects=effectSource;a=Source();b=Source();ambience=Source();boss=Source();
            for(int r=0;r<6;r++){themes[r]=Resources.Load<AudioClip>("Audio/Chronicles/region-"+r);beds[r]=Resources.Load<AudioClip>("Audio/Chronicles/ambience-"+r);tension[r]=Resources.Load<AudioClip>("Audio/Chronicles/boss-"+r);}
            string[] materials={"iron","wood","stone","crystal","cloth"};for(int m=0;m<5;m++)for(int v=0;v<3;v++)impacts[m,v]=Resources.Load<AudioClip>("Audio/Chronicles/impact-"+materials[m]+"-"+v);
        }
        AudioSource Source(){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=true;s.volume=0;return s;}
        void Update()
        {
            if(app==null||app.Sim==null||paused)return;
            int target=app.Sim.SagaInProgress?app.Sim.State.Saga.Run.Region:-1;
            if(target!=region)
            {
                region=target;usingA=!usingA;var source=usingA?a:b;source.clip=target<0?Resources.Load<AudioClip>("Audio/workshop"):themes[target];source.volume=0;source.Play();
                ambience.clip=target<0?beds[0]:beds[target];ambience.Play();boss.clip=target<0?null:tension[target];if(boss.clip)boss.Play();else boss.Stop();
            }
            bool music=app.Sim.State.MusicEnabled,sound=app.Sim.State.SoundEnabled;
            float speed=Time.unscaledDeltaTime*.35f;
            a.volume=Mathf.MoveTowards(a.volume,music&&usingA?.21f:0,speed);b.volume=Mathf.MoveTowards(b.volume,music&&!usingA?.21f:0,speed);
            ambience.volume=Mathf.MoveTowards(ambience.volume,sound?.23f:0,speed);
            bool active=app.Sim.SagaInProgress&&app.Sim.State.Saga.Run.Phase==SagaPhase.Combat&&app.Sim.State.Saga.Run.BossId>=0;
            float pressure=active?.10f+app.Sim.State.Saga.Run.BossPhase*.035f:0;boss.volume=Mathf.MoveTowards(boss.volume,music?pressure:0,speed);
            if(a.volume==0&&!usingA)a.Stop();if(b.volume==0&&usingA)b.Stop();
        }
        public bool Impact(string name)
        {
            if(!name.StartsWith("impact-"))return false;if(!app.Sim.State.SoundEnabled)return true;
            int material=name.Contains("wood")?1:name.Contains("stone")?2:name.Contains("crystal")?3:name.Contains("cloth")?4:0;
            effects.PlayOneShot(impacts[material,variant++%3],.65f);return true;
        }
        void OnApplicationPause(bool value)
        {
            paused=value;if(a==null)return;foreach(var source in new[]{a,b,ambience,boss})if(value)source.Pause();else source.UnPause();
        }
    }
}
