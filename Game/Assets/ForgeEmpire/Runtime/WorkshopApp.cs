using System;
using System.IO;
using UnityEngine;

namespace ForgeEmpire
{
    public sealed class WorkshopApp : MonoBehaviour
    {
        public WorkshopSimulation Sim { get; private set; }
        public WorkshopView View { get; private set; }
        AudioSource effects, music;
        AudioClip mineClip, smeltClip, forgeClip, coinClip, clickClip;
        string savePath;
        float nextSave;
        double lastForegroundTime;
        bool suspended;
        int lastCrafted,lastSold,lastMined;
        public string SavePath { get { return savePath; } }

        void Start()
        {
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            Screen.orientation=ScreenOrientation.Portrait;
            Input.backButtonLeavesApp=false;
            savePath=Path.Combine(Application.persistentDataPath,"workshop.json");
            var arguments=Environment.GetCommandLineArgs();
            bool previewRun=Array.IndexOf(arguments,"--redesign-preview")>=0||Array.IndexOf(arguments,"--redesign-motion")>=0;
            bool progressionRun=Array.IndexOf(arguments,"--progression-smoke")>=0;
            bool qaRun=previewRun||progressionRun||Array.IndexOf(arguments,"--redesign-smoke")>=0;
            if(qaRun)savePath=Application.isMobilePlatform?Path.Combine(Application.persistentDataPath,"redesign-qa",previewRun?"preview-workshop.json":"isolated-workshop.json"):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/redesign-20261024/"+(previewRun?"preview-workshop.json":"isolated-workshop.json")));
            if(progressionRun)savePath=Application.isMobilePlatform?Path.Combine(Application.persistentDataPath,"progression-qa/isolated-workshop.json"):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/progression-20261024/isolated-workshop.json"));
            bool recovered=false;
            var state=qaRun?new WorkshopState():WorkshopSave.Load(savePath,out recovered);
            if(previewRun)state.TutorialStep=1;
            if(state.TutorialStep==0&&state.TotalCrafted==0)state.KeepCraftedEquipment=true;
            Sim=new WorkshopSimulation(state);
            lastForegroundTime=Time.realtimeSinceStartupAsDouble;
            double away=state.LastSavedUtc>0?Math.Max(0,(DateTime.UtcNow.Ticks-state.LastSavedUtc)/(double)TimeSpan.TicksPerSecond):0;
            double earned=Sim.ApplyOffline(away);
            effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.volume=.46f;
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.volume=.21f;
            mineClip=Resources.Load<AudioClip>("Audio/mine");smeltClip=Resources.Load<AudioClip>("Audio/smelt");forgeClip=Resources.Load<AudioClip>("Audio/forge");coinClip=Resources.Load<AudioClip>("Audio/coin");clickClip=Resources.Load<AudioClip>("Audio/click");
            music.clip=Resources.Load<AudioClip>("Audio/workshop");
            View=new GameObject("WorkshopUI").AddComponent<WorkshopView>();View.Build(this);
            UpdateAudio();Save();nextSave=Time.unscaledTime+10;
            lastCrafted=state.TotalCrafted;lastSold=state.TotalSold;lastMined=state.TotalMined;
            if(state.TutorialStep==0)View.ShowWelcome();else if(away>=60)View.ShowOffline(earned,away);
            if(recovered)View.Notify("Your progress was recovered from its backup.");
        }

        void Update()
        {
            if(Sim==null||suspended)return;
            AdvanceForeground();
            int frameRate=Sim.State.ReducedMotion&&!Sim.IsRunning(Station.Forge)&&!Sim.State.Campaign.Battle.Active?30:60;
            if(Application.targetFrameRate!=frameRate)Application.targetFrameRate=frameRate;
            View.Tick(Time.unscaledDeltaTime);
            if(Sim.State.TotalCrafted>lastCrafted){Play("forge");lastCrafted=Sim.State.TotalCrafted;}
            if(Sim.State.TotalSold>lastSold){Play("coin");if(!Sim.State.Automated[3]||Sim.State.AutomationPaused[3])View.Notify("Sold. Gold added to your workshop.");lastSold=Sim.State.TotalSold;}
            if(Sim.State.TotalMined>lastMined){lastMined=Sim.State.TotalMined;}
            if(Time.unscaledTime>=nextSave){Save();nextSave=Time.unscaledTime+10;}
        }

        public void Work(Station station)
        {
            View.FocusStation(station);
            if(station==Station.Forge&&Sim.IsRunning(station)){Strike();return;}
            if(!Sim.TryStart(station,station==Station.Forge))return;
            if(station==Station.Forge)View.ExpectCraftResult();
            Play(station==Station.Mine?"mine":station==Station.Smelter?"smelt":"click");
            Save();View.Refresh();
        }

        public void Strike()
        {
            if(Sim.TryBeginTempering()){View.ExpectCraftResult();Play("click");Save();View.Refresh();return;}
            float accuracy=View.StrikeAccuracy;
            if(!Sim.TryStrike(accuracy))return;
            bool perfect=accuracy>=.82f;
            Play("forge");
            View.ShowStrikeFeedback(perfect);
#if UNITY_ANDROID && !UNITY_EDITOR
            if(perfect&&Sim.State.HapticsEnabled) {
                using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var window=activity.Call<AndroidJavaObject>("getWindow"))
                using(var view=window.Call<AndroidJavaObject>("getDecorView"))
                    view.Call<bool>("performHapticFeedback",1);
            }
#endif
            Save();
        }

        public void Upgrade(Station station)
        {
            if(!Sim.TryUpgrade(station))return;
            Play("coin");View.Notify(station+" upgraded to level "+Sim.State.Levels[(int)station]+".");Save();
        }
        public void Hire(Station station)
        {
            if(!Sim.TryAutomate(station))return;
            Play("coin");View.Notify("Crew hired. This station now works automatically.");Save();
        }
        public void ClaimOrder()
        {
            double reward=Sim.OrderReward;
            if(!Sim.ClaimOrder())return;
            Play("coin");View.Notify("Commission complete! +"+WorkshopView.Compact(reward)+" gold.");Save();
        }
        public void ToggleAutomation(Station station)
        {
            if(!Sim.SetAutomationPaused(station,!Sim.State.AutomationPaused[(int)station]))return;
            Save();Play("click");View.Notify(Sim.State.AutomationPaused[(int)station]?"Worker paused after the current job.":"Worker resumed.");
        }
        public bool ChooseRecipe(int id)
        {
            if(!Sim.SelectRecipe(id)){View.Notify("Finish the current forge cycle or unlock this recipe first.");return false;}
            Save();return true;
        }
        public bool Equip(int index)
        {
            if(!Sim.TryEquip(index)){View.Notify("Equipment cannot change during an active expedition.");return false;}
            Save();Play("coin");return true;
        }
        public bool RecruitHero(int id)
        {
            if(!Sim.TryRecruit(id)){View.Notify("You need more gold to recruit this hero.");return false;}
            Save();Play("coin");View.Notify(GameContent.Heroes[id].Name+" joined your guild.");return true;
        }
        public bool BeginExpedition(int region,int route)
        {
            if(!Sim.StartExpedition(region,route)){View.Notify("Prepare your hero and collect any previous expedition result.");return false;}
            Save();Play("click");View.Refresh();return true;
        }
        public void BattleSkill(int skill)
        {
            if(!Sim.UseBattleSkill(skill))return;
            Save();Play(skill==0?"forge":"smelt");View.Refresh();
        }
        public bool CollectExpedition()
        {
            if(!Sim.ClaimExpedition())return false;
            Save();Play("coin");return true;
        }
        public void ClaimCampaignQuest(int id,bool announce=true)
        {
            if(!Sim.ClaimQuest(id))return;
            Save();Play("coin");if(announce)View.Notify("Quest complete · +"+GameContent.Quests[id].RewardGold+" gold",true);
        }
        public void Play(string name)
        {
            if(effects==null||Sim==null||!Sim.State.SoundEnabled)return;
            AudioClip clip=name=="mine"?mineClip:name=="smelt"?smeltClip:name=="forge"?forgeClip:name=="coin"?coinClip:clickClip;
            if(clip)effects.PlayOneShot(clip);
        }
        public void UpdateAudio()
        {
            if(music==null||Sim==null)return;
            if(Sim.State.MusicEnabled){if(!music.isPlaying&&music.clip)music.Play();}
            else music.Pause();
        }
        public void Save()
        {
            if(Sim==null||savePath==null)return;
            if(!suspended)Sim.State.LastSavedUtc=DateTime.UtcNow.Ticks;
            try {WorkshopSave.Write(savePath,Sim.State);}
            catch(IOException ex){Debug.LogWarning("Unable to save workshop: "+ex.Message);if(View)View.Notify("Couldn't save. Please check your device storage.");}
        }
        void AdvanceForeground()
        {
            double now=Time.realtimeSinceStartupAsDouble;
            Sim.Advance(now-lastForegroundTime);
            lastForegroundTime=now;
        }
        void OnApplicationPause(bool pause) { Suspend(pause); }
        void OnApplicationFocus(bool focus) { if(Application.isMobilePlatform)Suspend(!focus); }
        void Suspend(bool pause)
        {
            if(Sim==null||suspended==pause)return;
            if(pause){AdvanceForeground();Save();suspended=true;return;}
            suspended=false;
            double away=Math.Max(0,(DateTime.UtcNow.Ticks-Sim.State.LastSavedUtc)/(double)TimeSpan.TicksPerSecond);
            double earned=Sim.ApplyOffline(away);
            lastForegroundTime=Time.realtimeSinceStartupAsDouble;
            lastCrafted=Sim.State.TotalCrafted;lastSold=Sim.State.TotalSold;lastMined=Sim.State.TotalMined;
            Save();
            if(away>=60)View.ShowOffline(earned,away);
            View.Refresh();
        }
        void OnApplicationQuit(){Save();}
    }
}
