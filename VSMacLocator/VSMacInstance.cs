// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace VSMacLocator;

public class VSMacInstance
{
    /// <summary>
    /// Path of the app bundle.
    /// </summary>
    public string BundlePath { get; }

    /// <summary>
    /// Directory within the app bundle containing the managed binaries.
    /// </summary>
    public string BinDir { get; }

    /// <summary>
    /// Version of the app bundle.
    /// </summary>
    public string BundleVersion { get; }

    /// <summary>
    /// Internal release identifier string of the Visual Studio for Mac build.
    /// </summary>
    public string ReleaseId { get; }

    /// <summary>
    /// Whether the app bundle is a preview release.
    /// </summary>
    public bool IsPreview { get; }

    VSMacInstance(string path, string binDir, string version, string releaseId, bool isPreview)
    {
        BundlePath = path;
        BinDir = binDir;
        BundleVersion = version;
        ReleaseId = releaseId;
        IsPreview = isPreview;
    }

    /// <summary>
    /// The path to the `msbuild` executable inside the app bundle.
    /// </summary>
    public string MSBuildDllPath => Path.Combine(BinDir, "MSBuild", "Current", "bin", "MSBuild.dll");

    /// <summary>
    /// The path to the `vstool` executable inside the app bundle.
    /// </summary>
    public string VSToolPath => Path.Combine(BundlePath, "Contents", "MacOS", "vstool");

    /// <summary>
    /// Finds all Visual Studio for Mac instances installed on the machine.
    /// </summary>
    public static IList<VSMacInstance> FindAll()
    {
        var instances = new List<VSMacInstance>();

        AddInstancesFromBundleId("com.microsoft.visual-studio");
        AddInstancesFromBundleId("com.microsoft.visual-studio-preview", isPreview: true);

        // sort by releaseid, newest first
        instances.Sort((VSMacInstance a, VSMacInstance b) => string.CompareOrdinal(b.ReleaseId, a.ReleaseId));

        return instances;

        void AddInstancesFromBundleId (string bundleId, bool isPreview = false)
        {
            if (MacInterop.GetApplicationUrls(bundleId, out _) is string?[] urls)
            {
                instances.AddRange(
                    urls
                    .Select(bundlePath => TryCreateInstance(bundlePath, isPreview))
                    .OfType<VSMacInstance>()
                );
            }
        }

        static VSMacInstance? TryCreateInstance(string? bundlePath, bool? isPreview = null)
        {
            if (bundlePath is null)
            {
                return null;
            }

            const string bundleShortVersionKey = "CFBundleShortVersionString";
            const string releaseIdKey = "ReleaseId";

            var infoPlistPath = Path.Combine(bundlePath, "Contents", "Info.plist");
            var values = MacInterop.GetStringValuesFromPlist(infoPlistPath, bundleShortVersionKey, releaseIdKey);
            var bundleVersion = values[bundleShortVersionKey];
            var releaseId = values[releaseIdKey];

            if ((bundleVersion is null) || (releaseId is null))
            {
                return null;
            }

            // This is the path to the binaries in current versions of Visual Studio for Mac
            var binDir = Path.Combine(bundlePath, "Contents", "MonoBundle");
            var brandingFile = Path.Combine(binDir, "branding", "Branding.xml");
            if (!File.Exists(brandingFile)) {
                // If the file isn't there, check the previous location for backward compatibility
                binDir = Path.Combine(bundlePath, "Contents", "Resources", "lib", "monodevelop", "bin");
                brandingFile = Path.Combine(binDir, "branding", "Branding.xml");
                if (!File.Exists(brandingFile)) {
                    return null;
                }
            }
            isPreview ??= File.ReadLines(brandingFile).Any(l => l.IndexOf("Preview", System.StringComparison.Ordinal) > -1);

            VSMacInstance instance = new VSMacInstance(bundlePath, binDir, bundleVersion, releaseId, isPreview.Value);
            return instance;
        }
    }
}
