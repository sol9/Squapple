using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidDevelopmentBuild
{
    [MenuItem("Tools/Squapple/Build Android Development")]
    public static void Build()
    {
        const string outputPath = "Builds/Android/Squapple-development.apk";
        var previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        var previousBundleSetting = EditorUserBuildSettings.buildAppBundle;
        var previousExportSetting = EditorUserBuildSettings.exportAsGoogleAndroidProject;

        try
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.squapple.development");
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android development build failed: {report.summary.result}");

            Debug.Log($"Android development build succeeded: {Path.GetFullPath(outputPath)} ({report.summary.totalSize} bytes)");
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, previousIdentifier);
            EditorUserBuildSettings.buildAppBundle = previousBundleSetting;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExportSetting;
        }
    }
}
