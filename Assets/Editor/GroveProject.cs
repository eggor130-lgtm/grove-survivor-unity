using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GroveSurvivor.Editor
{
    public static class GroveProject
    {
        const string Scene = "Assets/Scenes/Main.unity";
        [InitializeOnLoadMethod]
        static void Configure()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                EnsureMaterial();
                PlayerSettings.companyName = "Roman";
                PlayerSettings.productName = "Сумрачный лес";
                PlayerSettings.defaultScreenWidth = 1600;
                PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerSettings.runInBackground = false;
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            };
        }
        // A Resources material preserves the runtime-generated art's shader in player builds.
        static void EnsureMaterial()
        {
            const string path = "Assets/Resources/GroveBase.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Directory.CreateDirectory("Assets/Resources");
            var shader = Shader.Find("Standard");
            if (shader == null) throw new Exception("Built-in Standard shader is required. Open with Built-in Render Pipeline.");
            AssetDatabase.CreateAsset(new Material(shader), path);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Grove Survivor/Открыть главную сцену")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Scene);
        }
        [MenuItem("Grove Survivor/Собрать Windows x64")]
        public static void BuildWindows()
        {
            EnsureMaterial();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene }, locationPathName = "Builds/Windows/GroveSurvivor.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("Windows build ready: " + Path.GetFullPath("Builds/Windows/GroveSurvivor.exe"));
        }
    }
}
