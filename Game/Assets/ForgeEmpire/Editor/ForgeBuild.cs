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
            PlayerSettings.companyName="EmberOakGames";
            PlayerSettings.productName="Idle Blacksmith - Forge Empire";
            PlayerSettings.bundleVersion="0.5.0";
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
            PlayerSettings.Android.bundleVersionCode=5;
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
                settings.loadType=path.EndsWith("workshop.wav")?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.65f;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ForgeUI.Background;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            camera.gameObject.AddComponent<AudioListener>();
            var app=new GameObject("ForgeEmpire");app.AddComponent<WorkshopApp>();app.AddComponent<WorkshopSmoke>();app.AddComponent<MobileFrameProbe>();app.AddComponent<RedesignSmoke>();app.AddComponent<HandDrawnMotionCapture>();app.AddComponent<ProgressionSmoke>();app.AddComponent<IllustratedWorldSmoke>();
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
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName=path,target=target,options=BuildOptions.Development });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("FORGE_BUILD_SUCCESS "+path+" "+report.summary.totalSize+" bytes");
        }
    }
}
