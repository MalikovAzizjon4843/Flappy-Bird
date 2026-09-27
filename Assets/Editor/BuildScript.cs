using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menyu: Tools/Build Android APK va Tools/Build Windows.
// Batchmode'dan ham ishlaydi:
//   Unity.exe -batchmode -quit -projectPath "<loyiha>" -executeMethod BuildScript.BuildAndroid -logFile Logs/build-android.log
// Xato bo'lsa, sabablar Logs/BuildReport-<platforma>.txt ga yoziladi.
public static class BuildScript
{
    const string AndroidPath = "Build/Android/Flappy Bird.apk";
    const string WindowsPath = "Build/Windows/Flappy Bird.exe";

    [MenuItem("Tools/Build Android APK")]
    public static void BuildAndroid()
    {
        if (!CheckModule(BuildTargetGroup.Android, BuildTarget.Android, "Android Build Support"))
        {
            return;
        }

        // Reliz sozlamalari: package, nom, versiya, Portrait, IL2CPP + ARM64 (Editor/AndroidRelease.cs)
        AndroidRelease.ApplySettings();
        // Sahna: CameraFitter, qush fizikasi, InputSystemUIInputModule — keyin saqlash taklif qilinadi
        AndroidRelease.EnsureSceneSetup();
        EditorUserBuildSettings.buildAppBundle = false; // .aab emas, .apk

        // Build diskdagi sahnani oladi — saqlanmagan o'zgarishlar bo'lsa, saqlashni taklif qilish
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (AndroidRelease.RunChecks(out string report) > 0)
        {
            Fail("Android", "Tayyorlik tekshiruvi o'tmadi:\n" + report);
            return;
        }

        Build("Android", BuildTargetGroup.Android, BuildTarget.Android, AndroidPath);
    }

    [MenuItem("Tools/Build Windows")]
    public static void BuildWindows()
    {
        if (!CheckModule(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, "Windows Build Support"))
        {
            return;
        }

        Build("Windows", BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, WindowsPath);
    }

    static void Build(string label, BuildTargetGroup group, BuildTarget target, string path)
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Fail(label, "Build Settings'da yoqilgan sahna yo'q (Assets/Scenes/SampleScene.unity qo'shilishi kerak).");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = target,
            targetGroup = group,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            // summary.totalSize debug-simvollar papkasini ham qo'shadi — haqiqiy hajm uchun faylning o'zini o'lchaymiz
            long bytes = File.Exists(path) ? new FileInfo(path).Length : (long)summary.totalSize;
            Debug.Log($"BuildScript: {label} build tayyor — {path} ({bytes / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F0} s)");
            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(path);
            }
            return;
        }

        // Har bir build bosqichidagi xato va ogohlantirishlarni yig'ish
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Natija: {summary.result}, xatolar: {summary.totalErrors}, ogohlantirishlar: {summary.totalWarnings}");
        foreach (BuildStep step in report.steps)
        {
            foreach (BuildStepMessage msg in step.messages)
            {
                if (msg.type == LogType.Error || msg.type == LogType.Exception || msg.type == LogType.Assert)
                {
                    sb.AppendLine($"[{step.name}] {msg.content}");
                }
            }
        }
        Fail(label, sb.ToString());
    }

    static bool CheckModule(BuildTargetGroup group, BuildTarget target, string moduleName)
    {
        if (BuildPipeline.IsBuildTargetSupported(group, target))
        {
            return true;
        }
        Fail(target.ToString(), $"{moduleName} moduli o'rnatilmagan. Unity Hub → Installs → 6000.6.3f1 → Add modules.");
        return false;
    }

    static void Fail(string label, string reason)
    {
        string logPath = $"Logs/BuildReport-{label}.txt";
        Directory.CreateDirectory("Logs");
        File.WriteAllText(logPath, $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{reason}\n");

        Debug.LogError($"BuildScript: {label} build muvaffaqiyatsiz. Sabab ({logPath}):\n{reason}");

        if (Application.isBatchMode)
        {
            // CI uchun: noldan farqli chiqish kodi
            EditorApplication.Exit(1);
        }
        else
        {
            EditorUtility.DisplayDialog($"{label} build xatosi", reason.Length > 800 ? reason.Substring(0, 800) + "…" : reason, "OK");
        }
    }
}
