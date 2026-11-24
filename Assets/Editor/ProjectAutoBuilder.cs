using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ProjectAutoBuilder
{
    private static readonly string[] SCENES = new string[]
    {
        "Assets/HorrorEyes/HE Project/Scenes/GameStartScene.unity",
        "Assets/HorrorEyes/HE Project/Scenes/MainMenu.unity",
        "Assets/HorrorEyes/HE Project/Scenes/LoadScene.unity",
        "Assets/HorrorEyes/HE Project/Scenes/Morgue.unity",
        "Assets/HorrorEyes/HE Project/Scenes/School_Demo.unity"
    };

    private static string GetProjectRoot()
    {
        return Directory.GetParent(Application.dataPath).FullName;
    }

    [MenuItem("Build/1. Build Windows (Single Portable EXE)", false, 1)]
    public static void BuildWindowsMenu()
    {
        bool success = BuildWindowsStandalone();
        if (success)
        {
            EditorUtility.DisplayDialog("Build Complete", "Windows Single-File EXE successfully built in Builds/ folder!", "OK");
            OpenBuildsFolder();
        }
        else
        {
            EditorUtility.DisplayDialog("Build Failed", "Windows Build failed. Check the Console for details.", "OK");
        }
    }

    [MenuItem("Build/2. Build Android (APK)", false, 2)]
    public static void BuildAndroidMenu()
    {
        bool success = BuildAndroidAPK();
        if (success)
        {
            EditorUtility.DisplayDialog("Build Complete", "Android APK successfully built in Builds/ folder!", "OK");
            OpenBuildsFolder();
        }
        else
        {
            EditorUtility.DisplayDialog("Build Failed", "Android Build failed. Check the Console for details.", "OK");
        }
    }

    [MenuItem("Build/3. Build Both (APK & Single Portable EXE)", false, 3)]
    public static void BuildBothMenu()
    {
        bool winOk = BuildWindowsStandalone();
        bool androidOk = BuildAndroidAPK();

        if (winOk && androidOk)
        {
            EditorUtility.DisplayDialog("Build Complete", "Both Windows EXE and Android APK successfully built in Builds/ folder!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Build Finished", $"Windows: {(winOk ? "Success" : "Failed")}\nAndroid: {(androidOk ? "Success" : "Failed")}\nCheck Console for logs.", "OK");
        }
        OpenBuildsFolder();
    }

    [MenuItem("Build/Open Builds Folder", false, 20)]
    public static void OpenBuildsFolder()
    {
        string buildsDir = Path.Combine(GetProjectRoot(), "Builds");
        if (!Directory.Exists(buildsDir))
        {
            Directory.CreateDirectory(buildsDir);
        }
        EditorUtility.RevealInFinder(buildsDir);
    }

    public static bool BuildWindowsStandalone()
    {
        string projectRoot = GetProjectRoot();
        string buildsDir = Path.Combine(projectRoot, "Builds");
        string winOutputDir = Path.Combine(buildsDir, "Windows");
        string exePath = Path.Combine(winOutputDir, "Granny.exe");

        if (!Directory.Exists(winOutputDir))
        {
            Directory.CreateDirectory(winOutputDir);
        }

        Debug.Log("[AutoBuilder] Switching active build target to Windows Standalone...");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        Debug.Log("[AutoBuilder] Starting Windows Standalone build...");

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = SCENES,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AutoBuilder] Windows build succeeded: {summary.totalSize / (1024 * 1024)} MB");
            // Package into single portable EXE
            PackageSingleFileExe(projectRoot, winOutputDir, buildsDir);
            return true;
        }
        else
        {
            Debug.LogError($"[AutoBuilder] Windows build failed with result: {summary.result}");
            return false;
        }
    }

    private static void PackageSingleFileExe(string projectRoot, string winOutputDir, string buildsDir)
    {
        string rarExePath = @"C:\Program Files\WinRAR\Rar.exe";
        string targetSingleExe = Path.Combine(buildsDir, "Granny_Game_PC.exe");

        if (!File.Exists(rarExePath))
        {
            Debug.LogWarning("[AutoBuilder] Rar.exe not found. Windows build is available in Builds/Windows/ folder.");
            return;
        }

        if (File.Exists(targetSingleExe))
        {
            try { File.Delete(targetSingleExe); } catch { }
        }

        // Create temporary SFX config comment file
        string sfxConfigFile = Path.Combine(buildsDir, "sfx_config.txt");
        string sfxContent = ";SFX script commands\r\n" +
                            "Setup=Granny.exe\r\n" +
                            "TempMode\r\n" +
                            "Silent=1\r\n" +
                            "Overwrite=1\r\n" +
                            "Title=Granny 3D\r\n";
        File.WriteAllText(sfxConfigFile, sfxContent);

        try
        {
            Debug.Log("[AutoBuilder] Packaging Windows build into single portable EXE...");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = rarExePath,
                Arguments = $"a -sfx -r -ep1 -m3 -z\"{sfxConfigFile}\" \"{targetSingleExe}\" \"{winOutputDir}\\*\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit(120000); // 2 minute timeout
                if (process.ExitCode == 0 && File.Exists(targetSingleExe))
                {
                    FileInfo fi = new FileInfo(targetSingleExe);
                    Debug.Log($"[AutoBuilder] Single portable EXE successfully created: {targetSingleExe} ({fi.Length / (1024 * 1024)} MB)");
                }
                else
                {
                    Debug.LogWarning($"[AutoBuilder] WinRAR packaging exited with code {process.ExitCode}. Output: {process.StandardOutput.ReadToEnd()}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoBuilder] Error during SFX packaging: {ex.Message}");
        }
        finally
        {
            if (File.Exists(sfxConfigFile))
            {
                try { File.Delete(sfxConfigFile); } catch { }
            }
        }
    }

    public static bool BuildAndroidAPK()
    {
        string projectRoot = GetProjectRoot();
        string buildsDir = Path.Combine(projectRoot, "Builds");
        string apkPath = Path.Combine(buildsDir, "Granny_Mobile.apk");

        if (!Directory.Exists(buildsDir))
        {
            Directory.CreateDirectory(buildsDir);
        }

        Debug.Log("[AutoBuilder] Configuring Android build settings...");

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // Ensure APK output, not AAB
        EditorUserBuildSettings.buildAppBundle = false;

        // Use standard debug keystore to avoid missing keystore password errors
        PlayerSettings.Android.useCustomKeystore = false;

        // Set target architectures: ARMv7 (1) + ARM64 (2) = 3
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        Debug.Log("[AutoBuilder] Starting Android APK build...");

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = SCENES,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AutoBuilder] Android APK build succeeded: {summary.totalSize / (1024 * 1024)} MB at {apkPath}");
            return true;
        }
        else
        {
            Debug.LogError($"[AutoBuilder] Android APK build failed with result: {summary.result}");
            return false;
        }
    }

    // CLI batch entry points
    public static void BuildWindowsBatch()
    {
        BuildWindowsStandalone();
    }

    public static void BuildAndroidBatch()
    {
        BuildAndroidAPK();
    }

    public static void BuildAllBatch()
    {
        BuildWindowsStandalone();
        BuildAndroidAPK();
    }
}
