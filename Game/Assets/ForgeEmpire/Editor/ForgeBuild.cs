using System;
using System.IO;
using ForgeEmpire.Tests;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgeEmpire.Editor
{
    public static class ForgeBuild
    {
        const string ScenePath="Assets/ForgeEmpire/Workshop.unity";
        static string Root { get { return Path.GetFullPath(Path.Combine(Application.dataPath,"../..")); } }

        static void Setup()
        {
            foreach(var target in new[]{NamedBuildTarget.Standalone,NamedBuildTarget.Android})
            {
                string symbols=PlayerSettings.GetScriptingDefineSymbols(target);
                if(Array.IndexOf(symbols.Split(';'),"FORGE_QA")<0)PlayerSettings.SetScriptingDefineSymbols(target,(symbols+";FORGE_QA").Trim(';'));
            }
            PlayerSettings.companyName="EmberOakGames";
            PlayerSettings.productName="Idle Blacksmith - Forge Empire";
            PlayerSettings.bundleVersion="0.7.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.emberoak.forgeempire");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.emberoak.forgeempire");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;
            PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=1080;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.SplashScreen.show=false;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)26;
            PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode=7;
            PlayerSettings.Android.renderOutsideSafeArea=true;
            // Legacy input receives Back as Escape, not through predictive-back callbacks.
            PlayerSettings.Android.predictiveBackSupport=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android,ManagedStrippingLevel.Low);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Art"})) {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.filterMode=path.Contains("/HandDrawn/")?FilterMode.Bilinear:FilterMode.Point;importer.mipmapEnabled=false;
                importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.isReadable=false;
                if(path.Contains("/HandDrawn/"))
                {
                    var mobile=importer.GetPlatformTextureSettings("Android");mobile.name="Android";mobile.overridden=true;
                    mobile.maxTextureSize=2048;mobile.format=TextureImporterFormat.ETC2_RGBA8;
                    mobile.textureCompression=TextureImporterCompression.CompressedHQ;mobile.compressionQuality=80;
                    importer.SetPlatformTextureSettings(mobile);
                }
                importer.SaveAndReimport();
            }
            foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio"})) {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono=true;var settings=importer.defaultSampleSettings;
                settings.loadType=path.EndsWith("workshop.wav")||path.Contains("/Chronicles/region-")||path.Contains("/Chronicles/ambience-")||path.Contains("/Chronicles/boss-")?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.65f;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ForgeUI.Background;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            camera.gameObject.AddComponent<AudioListener>();
            var app=new GameObject("ForgeEmpire");app.AddComponent<WorkshopApp>();app.AddComponent<WorkshopSmoke>();app.AddComponent<MobileFrameProbe>();app.AddComponent<RedesignSmoke>();app.AddComponent<HandDrawnMotionCapture>();app.AddComponent<ProgressionSmoke>();app.AddComponent<IllustratedWorldSmoke>();app.AddComponent<ChroniclesSmoke>();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Verify();
        }

        public static void Verify()
        {
            int checks=0;
            foreach(string name in new[]{"workshop","blacksmith","miner","merchant","fire","icons","improvements","smoke","sparks","customer","worldmap","battlefields","heroes","enemies","equipment","townlife","villagers"}) {
                var texture=Resources.Load<Texture2D>("Art/"+name);
                Require(texture!=null,"Missing texture "+name);Require(texture.filterMode==FilterMode.Point,"Blurred texture "+name);checks+=2;
            }
            foreach(string name in new[]{"mine","smelt","forge","coin","click","workshop"}){Require(Resources.Load<AudioClip>("Audio/"+name)!=null,"Missing audio "+name);checks++;}
            foreach(string name in new[]{"workshop","workers","walkers","heroes","props","idles","equipment","enemies","guardians","worldmap","emberwood","marsh","frostpeak","caldera","starfall"}){
                var texture=Resources.Load<Texture2D>("Art/HandDrawn/"+name);
                Require(texture&&texture.filterMode==FilterMode.Bilinear,"Missing hand-drawn art "+name);checks++;
            }
            foreach(string name in new[]{"hero-rowan","hero-lyra","hero-kael","hero-mira","hero-toren","hero-edda","enemy-0","enemy-1","enemy-2","enemy-3","enemy-4","enemy-5","gear-0","gear-1","gear-2","gear-3","gear-4","boss-0","boss-1","boss-2","worldmap-chronicles","dawnforge","forge-closeup","people","landmarks"})
            {Require(Resources.Load<Texture2D>("Art/HandDrawn/"+name),"Missing Chronicles painting "+name);checks++;}
            foreach(string prefix in new[]{"region-","ambience-","boss-"})for(int i=0;i<6;i++)
            {Require(Resources.Load<AudioClip>("Audio/Chronicles/"+prefix+i),"Missing Chronicles audio "+prefix+i);checks++;}
            foreach(string hero in ChronicleWardrobe.Heroes)foreach(string family in ChronicleWardrobe.Families)
            {
                string name="worn-"+hero+"-"+family;
                Require(Resources.Load<Texture2D>("Art/HandDrawn/"+name),"Missing worn outfit "+name);
                var layout=Resources.Load<TextAsset>("Art/HandDrawn/"+name+"-layout");
                Require(layout&&JsonUtility.FromJson<HandDrawnArt.Layout>(layout.text).frames.Length==20,"Missing worn poses "+name);checks+=2;
            }
            var artisan=new WorkshopSimulation(new WorkshopState{Ingots=10,ChroniclesEnabled=true});Require(artisan.BeginMasterwork(0,0)&&artisan.StrikeMasterwork(1),"Artisan setup failed");
            var artisanReload=new WorkshopSimulation(JsonUtility.FromJson<WorkshopState>(JsonUtility.ToJson(artisan.State)));
            artisanReload.Advance(100);Require(artisanReload.MasterworkActive&&artisanReload.State.TotalCrafted==0&&artisanReload.State.Artisan.Project.Stage==ArtisanStage.Shape,"Unity save lost paid artisan stage");checks++;
            var legacyBattle=new WorkshopSimulation(new WorkshopState{Ingots=2});legacyBattle.TryStart(Station.Forge);legacyBattle.Advance(4.5);legacyBattle.TryEquip(0);Require(legacyBattle.StartExpedition(0,0),"Legacy battle setup failed");
            var legacyRaw=JsonUtility.FromJson<WorkshopState>(JsonUtility.ToJson(legacyBattle.State));
            bool emptyRecord=legacyRaw.Saga!=null&&legacyRaw.Saga.Run!=null;
            var legacyReload=new WorkshopSimulation(legacyRaw);Require(legacyReload.State.Campaign.Battle.Active&&!legacyReload.SagaInProgress,"Unity empty saga record erased an active legacy expedition");checks++;
            Debug.Log("CHRONICLES_LEGACY_SAVE_PASS: empty nested Saga.Run="+emptyRecord);
            var state=new WorkshopState();var sim=new WorkshopSimulation(state);
            sim.TryStart(Station.Mine);sim.Advance(.5);
            string json=JsonUtility.ToJson(state);var restored=JsonUtility.FromJson<WorkshopState>(json);
            Require(restored.Running[0]&&Math.Abs(restored.Progress[0]-state.Progress[0])<1e-7,"Unity save lost active cycle");checks++;
            string testPath=Path.Combine(Root,"QA","save-roundtrip.json");
            WorkshopSave.Write(testPath,state);state.Gold=73;WorkshopSave.Write(testPath,state);
            Require(WorkshopSave.Load(testPath,out bool recovered).Gold==73&&!recovered,"Save roundtrip failed");checks++;
            File.WriteAllText(testPath,"broken");
            Require(WorkshopSave.Load(testPath,out recovered).Gold==40&&recovered,"Backup recovery failed");checks++;
            File.WriteAllText(Path.Combine(Root,"QA","unity-verification.json"),"{\"passed\":true,\"checks\":"+checks+"}");
            Debug.Log("FORGE_VERIFY_PASS: "+checks+" asset and Unity save checks.");
        }

        static void Require(bool condition,string message) { if(!condition)throw new Exception(message); }
        public static void Desktop()
        {
            Setup();
            Build(BuildTarget.StandaloneWindows64,Path.Combine(Root,"Builds","Windows","ForgeEmpire.exe"));
        }
        public static void Android()
        {
            Setup();EditorUserBuildSettings.buildAppBundle=false;
            Build(BuildTarget.Android,Path.Combine(Root,"Builds","Android","ForgeEmpire.apk"));
        }
        public static void Delivery()
        {
            Desktop();Android();
        }
        static void Build(BuildTarget target,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName=path,target=target,options=BuildOptions.None });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("FORGE_BUILD_SUCCESS "+path+" "+report.summary.totalSize+" bytes");
        }
    }
}
