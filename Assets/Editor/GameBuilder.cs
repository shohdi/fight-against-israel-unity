using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace FightAgainstIsrael.Editor
{
    public static class GameBuilder
    {
        [MenuItem("Fight against Israel/Prepare scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Fight against Israel");
            root.AddComponent<FighterGame>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/AirCombat.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/AirCombat.unity", true) };
            PlayerSettings.companyName = "Shohdi";
            PlayerSettings.productName = "Fight against Israel";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:Combat";
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 80;
            QualitySettings.pixelLightCount = 4;
            foreach (string clipPath in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio" }))
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(clipPath)) as AudioImporter;
                if (importer == null) continue;
                var sampleSettings = importer.defaultSampleSettings;
                if (sampleSettings.preloadAudioData) continue;
                sampleSettings.preloadAudioData = true;
                importer.defaultSampleSettings = sampleSettings;
                importer.SaveAndReimport();
            }
            var graphicsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsAsset.Length > 0)
            {
                var settings = new SerializedObject(graphicsAsset[0]);
                var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
                foreach (string shaderName in new[] { "Standard", "Unlit/Color", "Unlit/Texture", "Unlit/Transparent", "FightAgainstIsrael/CoastalOcean" })
                {
                    Shader shader = Shader.Find(shaderName);
                    bool included = false;
                    for (int i = 0; i < shaders.arraySize; i++)
                        if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) included = true;
                    if (!included && shader != null)
                    {
                        shaders.InsertArrayElementAtIndex(shaders.arraySize);
                        shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
                    }
                }
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Fight against Israel/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();
            Build("Builds/Windows/FightAgainstIsrael.exe", BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Fight against Israel/Build WebGL")]
        public static void BuildWebGL()
        {
            Prepare();
            Build("docs", BuildTarget.WebGL);
        }

        public static void BuildAll()
        {
            BuildWindows();
            BuildWebGL();
        }

        private static void Build(string output, BuildTarget target)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/AirCombat.unity" },
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(target + " build failed: " + report.summary.result);
            Debug.Log("FIGHT_BUILD_OK " + target + " " + report.summary.totalSize);
        }
    }
}
