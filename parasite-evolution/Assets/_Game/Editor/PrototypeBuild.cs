using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ParasiteEvolution.Editor
{
    public static class PrototypeBuild
    {
        const string Data="Assets/_Game/Generated";
        const string Scene="Assets/_Game/Scenes/Bootstrap.unity";

        [InitializeOnLoadMethod]
        static void Register()
        { EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode)Prepare();}; }

        [MenuItem("Parasite/Prepare renderer")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath,"_Game/Generated"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath,"_Game/Resources"));
            AssetDatabase.Refresh();
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Data+"/PrototypeURP.asset");
            if(pipeline==null)
            {
                var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name="Prototype renderer";
                AssetDatabase.CreateAsset(renderer,Data+"/PrototypeRenderer.asset");
                pipeline=UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount=2;pipeline.shadowDistance=25;
                AssetDatabase.CreateAsset(pipeline,Data+"/PrototypeURP.asset");
            }
            GraphicsSettings.defaultRenderPipeline=pipeline;
            QualitySettings.renderPipeline=pipeline;
            const string materialPath="Assets/_Game/Resources/PrototypeMaterial.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Simple Lit");
                if(shader==null)throw new InvalidOperationException("URP shader not imported.");
                AssetDatabase.CreateAsset(new Material(shader),materialPath);
            }
            // Emissive template: a material with _EMISSION enabled must exist in the build, or the
            // keyword variant is stripped and runtime glow materials render without emission.
            const string glowPath="Assets/_Game/Resources/PrototypeGlow.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(glowPath)==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Simple Lit");
                if(shader==null)throw new InvalidOperationException("URP shader not imported.");
                var glow=new Material(shader);glow.EnableKeyword("_EMISSION");
                glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
                if(glow.HasProperty("_EmissionColor"))glow.SetColor("_EmissionColor",Color.white);
                AssetDatabase.CreateAsset(glow,glowPath);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Parasite/Create and open bootstrap")]
        public static void CreateBootstrap()
        {
            Prepare();Directory.CreateDirectory(Path.Combine(Application.dataPath,"_Game/Scenes"));
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            if(File.Exists(Path.Combine(Application.dataPath,"_Game/Scenes/Bootstrap.unity")))
            {EditorSceneManager.OpenScene(Scene);return;}
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            if(!EditorSceneManager.SaveScene(scene,Scene))throw new IOException("Could not save bootstrap scene.");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        }

        [MenuItem("Parasite/Build Android debug APK")]
        public static void BuildAndroid()
        {
            Build(Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/ParasiteEvolution-debug.apk")));
        }

        // Entry point for GameCI (game-ci/unity-builder buildMethod). Honours -customBuildPath.
        public static void BuildAndroidCI()
        {
            var path=Argument("-customBuildPath");
            if(string.IsNullOrEmpty(path))path=Path.Combine(Application.dataPath,"../Builds/ParasiteEvolution-debug.apk");
            if(!path.EndsWith(".apk",StringComparison.OrdinalIgnoreCase))path=Path.Combine(path,"ParasiteEvolution-debug.apk");
            try{Build(Path.GetFullPath(path));}
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }

        // Pre-export method for Unity Build Automation: generates the scene and Android settings, then
        // UBA builds the scenes listed in EditorBuildSettings with its own signing configuration.
        public static void PrepareCloudBuild(){ConfigureAndroid();}

        static string Argument(string name)
        {
            var args=Environment.GetCommandLineArgs();
            for(var i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1];
            return null;
        }

        static void ConfigureAndroid()
        {
            CreateBootstrap();
            PlayerSettings.companyName="ParasitePrototype";PlayerSettings.productName="Parasite Evolution";
            PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.parasiteprototype.evolution");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode=1;
            EditorUserBuildSettings.buildAppBundle=false;
            AssetDatabase.SaveAssets();
        }

        static void Build(string path)
        {
            ConfigureAndroid();
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
                throw new InvalidOperationException("Select Android in Build Profiles, or launch batch mode with -buildTarget Android.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{Scene},locationPathName=path,target=BuildTarget.Android,
                options=BuildOptions.Development|BuildOptions.AllowDebugging
            });
            if(report.summary.result!=BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: "+report.summary.result);
            Debug.Log("APK built: "+path);
        }
    }
}
