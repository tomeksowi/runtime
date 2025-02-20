// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.Workload.Build.Tasks
{
    public partial class InstallWorkloadFromArtifacts : PatchNuGetConfig
    {
        [Required, NotNull]
        public ITaskItem[]    WorkloadIds        { get; set; } = Array.Empty<ITaskItem>();

        [Required, NotNull]
        public ITaskItem[]    InstallTargets     { get; set; } = Array.Empty<ITaskItem>();

        [Required, NotNull]
        public string?        VersionBandForSdkManifestsDir        { get; set; }

        [Required, NotNull]
        public string?        VersionBandForManifestPackages       { get; set; }

        [Required, NotNull]
        public string         SdkWithNoWorkloadInstalledPath { get; set; } = string.Empty;

        public string         ExtraWorkloadInstallCommandArguments { get; set; } = string.Empty;
        public string?        IntermediateOutputPath { get; set; }
        public bool           OnlyUpdateManifests { get; set; }
        public bool           SkipTempDirectoryCleanup { get; set; }

        // Should match enum values for MessageImportance - Low, Normal (default), High
        public string?        WorkloadInstallCommandOutputImportance { get; set; }

        private string AllManifestsStampPath => Path.Combine(SdkWithNoWorkloadInstalledPath, ".all-manifests.stamp");
        private string _tempDir = string.Empty;
        private string _nugetCachePath = string.Empty;
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        [GeneratedRegex(@"^\d+\.\d+\.\d+(-(?!rtm)[A-z]*\.*\d*)?")]
        private static partial Regex bandVersionRegex();

        public override bool Execute()
        {
            _tempDir = Path.Combine(IntermediateOutputPath ?? Path.GetTempPath(), $"workload-{Path.GetRandomFileName()}");
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
            Directory.CreateDirectory(_tempDir);
            _nugetCachePath = Path.Combine(_tempDir, "nuget-cache");
            if (SkipTempDirectoryCleanup)
            {
                Log.LogMessage(MessageImportance.High, $"Using temporary directory {_tempDir} for installing workloads from artifacts.");
            }

            try
            {
                if (!Directory.Exists(SdkWithNoWorkloadInstalledPath))
                    throw new LogAsErrorException($"Cannot find {nameof(SdkWithNoWorkloadInstalledPath)}={SdkWithNoWorkloadInstalledPath}");

                if (!Directory.Exists(LocalNuGetsPath))
                    throw new LogAsErrorException($"Cannot find {nameof(LocalNuGetsPath)}={LocalNuGetsPath} . " +
                                                    "Set it to the Shipping packages directory in artifacts.");

                if (!InstallAllManifests())
                    return false;

                if (OnlyUpdateManifests)
                    return !Log.HasLoggedErrors;

                if (InstallTargets.Length == 0)
                    throw new LogAsErrorException($"No install targets specified.");

                InstallWorkloadRequest[] selectedRequests = InstallTargets
                    .SelectMany(workloadToInstall =>
                    {
                        if (!HasMetadata(workloadToInstall, nameof(workloadToInstall), "Variants", Log))
                            throw new LogAsErrorException($"Missing Variants metadata on item '{workloadToInstall.ItemSpec}'");

                        return workloadToInstall
                                .GetMetadata("Variants")
                                .Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(v => (variant: v, target: workloadToInstall));
                    })
                    .SelectMany(w =>
                    {
                        IEnumerable<InstallWorkloadRequest> workloads = WorkloadIds.Where(wi => wi.GetMetadata("Variant") == w.variant)
                                                                                    .Select(wi => new InstallWorkloadRequest(wi, w.target));
                        return workloads.Any()
                                ? workloads
                                : throw new LogAsErrorException($"Could not find any workload variant named '{w.variant}'");
                    }).ToArray();

                foreach (InstallWorkloadRequest req in selectedRequests)
                {
                    if (Directory.Exists(req.TargetPath))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Deleting directory {req.TargetPath}");
                        Directory.Delete(req.TargetPath, recursive: true);
                    }
                }

                string lastTargetPath = string.Empty;
                foreach (InstallWorkloadRequest req in selectedRequests)
                {
                    if (req.TargetPath != lastTargetPath)
                        Log.LogMessage(MessageImportance.High, $"{Environment.NewLine}** Preparing {req.TargetPath} **");
                    lastTargetPath = req.TargetPath;

                    Log.LogMessage(MessageImportance.High, $"    - {req.WorkloadId}: Installing workload");
                    if (!req.Validate(Log))
                        return false;

                    if (!ExecuteInternal(req) && !req.IgnoreErrors)
                        return false;

                    File.WriteAllText(req.StampPath, string.Empty);
                }

                return !Log.HasLoggedErrors;
            }
            catch (LogAsErrorException laee)
            {
                Log.LogError(laee.Message);
                return false;
            }
            finally
            {
                if (!SkipTempDirectoryCleanup && !string.IsNullOrEmpty(_tempDir) && Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
        }

        private bool ExecuteInternal(InstallWorkloadRequest req)
        {
            if (!File.Exists(TemplateNuGetConfigPath))
            {
                Log.LogError($"Cannot find TemplateNuGetConfigPath={TemplateNuGetConfigPath}");
                return false;
            }

            Log.LogMessage(MessageImportance.Low, $"Duplicating {SdkWithNoWorkloadInstalledPath} into {req.TargetPath}");
            Utils.DirectoryCopy(SdkWithNoWorkloadInstalledPath, req.TargetPath);

            string nugetConfigContents = GetNuGetConfig();
            if (!InstallPacks(req, nugetConfigContents))
                return false;

            return !Log.HasLoggedErrors;
        }

        private bool InstallAllManifests()
        {
            var allManifestPkgs = Directory.EnumerateFiles(LocalNuGetsPath, "*Manifest*nupkg");
            if (!AnyInputsNewerThanOutput(AllManifestsStampPath, allManifestPkgs))
            {
                Log.LogMessage(MessageImportance.Low,
                                    $"Skipping installing manifests because the {AllManifestsStampPath} " +
                                    $"is newer than packages {string.Join(',', allManifestPkgs)}.");
                return true;
            }

            string nugetConfigContents = GetNuGetConfig();
            HashSet<string> manifestsInstalled = new();
            foreach (ITaskItem workload in WorkloadIds)
            {
                InstallWorkloadRequest req = new(workload, new TaskItem());

                if (manifestsInstalled.Contains(req.ManifestName))
                {
                    Log.LogMessage(MessageImportance.High, $"** {req.WorkloadId}: Manifests are already installed **");
                    continue;
                }

                if (string.IsNullOrEmpty(req.Version))
                {
                    Log.LogError($"No Version set for workload manifest {req.ManifestName} in workload install requests.");
                    return false;
                }

                Log.LogMessage(MessageImportance.High, $"{Environment.NewLine}** {req.WorkloadId}: Installing manifests **");
                if (!InstallWorkloadManifest(workload,
                                             req.ManifestName,
                                             req.Version,
                                             SdkWithNoWorkloadInstalledPath,
                                             nugetConfigContents,
                                             stopOnMissing: true))
                {
                    return false;
                }

                manifestsInstalled.Add(req.ManifestName);
            }

            File.WriteAllText(AllManifestsStampPath, string.Empty);

            return true;
        }

        private bool InstallPacks(InstallWorkloadRequest req, string nugetConfigContents)
        {
            string nugetConfigPath = Path.Combine(_tempDir, $"NuGet.{Path.GetRandomFileName()}.config");
            File.WriteAllText(nugetConfigPath, nugetConfigContents);

            if (string.IsNullOrEmpty(WorkloadInstallCommandOutputImportance) ||
                !Enum.TryParse<MessageImportance>(WorkloadInstallCommandOutputImportance, out var outputImportance))
            {
                outputImportance = MessageImportance.Normal;
            }

            // Log.LogMessage(MessageImportance.High, $"{Environment.NewLine}** dotnet workload install {req.WorkloadId} **{Environment.NewLine}");
            (int exitCode, string output) = Utils.TryRunProcess(
                                                    Log,
                                                    Path.Combine(req.TargetPath, "dotnet"),
                                                    $"workload install --skip-manifest-update --skip-sign-check --configfile \"{nugetConfigPath}\" --temp-dir \"{_tempDir}/workload-install-temp\" {ExtraWorkloadInstallCommandArguments} {req.WorkloadId}",
                                                    workingDir: _tempDir,
                                                    envVars: new Dictionary<string, string>() {
                                                        ["NUGET_PACKAGES"] = _nugetCachePath
                                                    },
                                                    logStdErrAsMessage: req.IgnoreErrors,
                                                    silent: false,
                                                    debugMessageImportance: outputImportance);
            if (exitCode != 0)
            {
                if (req.IgnoreErrors)
                {
                    Log.LogMessage(MessageImportance.High, output);
                    Log.LogMessage(MessageImportance.High,
                                    $"{Environment.NewLine} ** Ignoring workload installation failure exit code {exitCode}. **{Environment.NewLine}");
                }
                else
                {
                    Log.LogError($"workload install failed with exit code {exitCode}: {output}");
                }

                Log.LogMessage(MessageImportance.Low, $"List of the relevant paths in {req.TargetPath}");
                foreach (string dir in Directory.EnumerateDirectories(Path.Combine(req.TargetPath, "sdk-manifests"), "*", SearchOption.AllDirectories))
                    Log.LogMessage(MessageImportance.Low, $"\t{Path.Combine(req.TargetPath, "sdk-manifests", dir)}");

                foreach (string dir in Directory.EnumerateDirectories(Path.Combine(req.TargetPath, "packs"), "*", SearchOption.AllDirectories))
                    Log.LogMessage(MessageImportance.Low, $"\t{Path.Combine(req.TargetPath, "packs", dir)}");
            }

            return !Log.HasLoggedErrors;
        }

        private string GetNuGetConfig()
        {
            var nugetConfigPath = Path.GetTempFileName();
            PatchNuGetConfig.GetNuGetConfig(TemplateNuGetConfigPath, LocalNuGetsPath, PackageSourceNameForBuiltPackages, NuGetConfigPackageSourceMappings, nugetConfigPath);
            string contents = File.ReadAllText(nugetConfigPath);
            File.Delete(nugetConfigPath);
            return contents;
        }

        private bool InstallWorkloadManifest(ITaskItem workloadId, string name, string version, string sdkDir, string nugetConfigContents, bool stopOnMissing)
        {
            Log.LogMessage(MessageImportance.High, $"    - Installing manifest: {name}/{version}");

            // Find any existing directory with the manifest name, ignoring the case
            // Multiple directories for a manifest, differing only in case causes
            // workload install to fail due to duplicate manifests!
            // This is applicable only on case-sensitive filesystems
            string manifestVersionBandDir = Path.Combine(sdkDir, "sdk-manifests", VersionBandForSdkManifestsDir);
            if (!Directory.Exists(manifestVersionBandDir))
            {
                Log.LogMessage(MessageImportance.Low, $"    Could not find {manifestVersionBandDir}. Creating it..");
                Directory.CreateDirectory(manifestVersionBandDir);
            }

            string outputDir = FindSubDirIgnoringCase(manifestVersionBandDir, name);

            if (!Directory.Exists(outputDir))
            {
                Log.LogMessage($"Could not find {name} directory at {outputDir}. Creating it..");
                Directory.CreateDirectory(outputDir);
            }

            // If we one sub entry, it's workload manifest version and we should install into it (aka workload sets)
            string[] outputSubEntries = Directory.GetFileSystemEntries(outputDir);
            if (outputSubEntries.Length == 1)
                outputDir = outputSubEntries[0];

            var bandVersion = VersionBandForManifestPackages;
            // regex matching the version band, e.g. 6.0.100-preview.3.21202.5 => 6.0.100-preview.3
            string packagePreleaseVersion = bandVersionRegex().Match(version).Groups[1].Value;
            string bandPreleaseVersion = bandVersionRegex().Match(bandVersion).Groups[1].Value;

            if (!string.IsNullOrEmpty(bandPreleaseVersion) &&
                packagePreleaseVersion != bandPreleaseVersion &&
                packagePreleaseVersion != "-dev" &&
                packagePreleaseVersion != "-ci" &&
                packagePreleaseVersion != "-rtm")
            {
                bandVersion = bandVersion.Replace (bandPreleaseVersion, packagePreleaseVersion);
            }

            PackageReference pkgRef = new(Name: $"{name}.Manifest-{bandVersion}",
                                          Version: version,
                                          OutputDir: outputDir,
                                          relativeSourceDir: "data");

            if (!PackageInstaller.Install(new[] { pkgRef }, nugetConfigContents, _tempDir, Log, stopOnMissing, packagesPath: _nugetCachePath))
                return false;

            string manifestDir = pkgRef.OutputDir;
            string jsonPath = Path.Combine(manifestDir, "WorkloadManifest.json");
            if (!File.Exists(jsonPath))
            {
                Log.LogError($"Could not find WorkloadManifest.json at {jsonPath}");
                return false;
            }

            ManifestInformation? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<ManifestInformation>(
                                                    File.ReadAllBytes(jsonPath),
                                                    s_jsonOptions);

                if (manifest == null)
                {
                    Log.LogError($"Could not parse manifest from {jsonPath}.");
                    return false;
                }
            }
            catch (JsonException je)
            {
                Log.LogError($"Failed to read from {jsonPath}: {je.Message}");
                return false;
            }

            if (manifest.DependsOn != null)
            {
                foreach ((string depName, string depVersion) in manifest.DependsOn)
                {
                    if (!InstallWorkloadManifest(workloadId, depName, depVersion, sdkDir, nugetConfigContents, stopOnMissing: false))
                    {
                        Log.LogWarning($"Could not install manifest {depName}/{depVersion}. This can be ignored if the workload {workloadId.ItemSpec} doesn't depend on it.");
                        continue;
                    }
                }
            }

            return true;
        }

        private static bool HasMetadata(ITaskItem item, string itemName, string metadataName, TaskLoggingHelper log)
        {
            if (!string.IsNullOrEmpty(item.GetMetadata(metadataName)))
                return true;

            log.LogError($"{itemName} item ({item.ItemSpec}) is missing {metadataName} metadata");
            return false;
        }

        private string FindSubDirIgnoringCase(string parentDir, string dirName)
        {
            string[] matchingDirs = Directory.EnumerateDirectories(parentDir,
                                                            dirName,
                                                            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })
                                                .ToArray();

            string? first = matchingDirs.FirstOrDefault();
            if (matchingDirs.Length > 1)
            {
                Log.LogWarning($"Found multiple directories with names that differ only in case. {string.Join(", ", matchingDirs)}"
                                + $"{Environment.NewLine}Using the first one: {first}");
            }

            return first ?? Path.Combine(parentDir, dirName.ToLower(CultureInfo.InvariantCulture));
        }

        private static bool AnyInputsNewerThanOutput(string output, IEnumerable<string> inputs)
            => inputs.Any(i => Utils.IsNewerThan(i, output));

        private sealed record ManifestInformation(
            object Version,
            string Description,

            [property: JsonPropertyName("depends-on")]
            IDictionary<string, string> DependsOn,
            IDictionary<string, WorkloadInformation> Workloads,
            IDictionary<string, PackVersionInformation> Packs,
            object Data
        );

        private sealed record WorkloadInformation(
            bool Abstract,
            string Kind,
            string Description,

            List<string> Packs,
            List<string> Extends,
            List<string> Platforms
        );

        private sealed record PackVersionInformation(
            string Kind,
            string Version,
            [property: JsonPropertyName("alias-to")]
            Dictionary<string, string> AliasTo
        );

        internal sealed record InstallWorkloadRequest(
            ITaskItem Workload,
            ITaskItem Target)
        {
            public string ManifestName => Workload.GetMetadata("ManifestName");
            public string Version => Workload.GetMetadata("Version");
            public string TargetPath => Target.GetMetadata("InstallPath");
            public string StampPath => Target.GetMetadata("StampPath");
            public bool IgnoreErrors => Workload.GetMetadata("IgnoreErrors").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            public string WorkloadId => Workload.ItemSpec;

            public bool Validate(TaskLoggingHelper log)
            {
                if (!HasMetadata(Workload, nameof(Workload), "Version", log) ||
                    !HasMetadata(Workload, nameof(Workload), "ManifestName", log) ||
                    !HasMetadata(Target, nameof(Target), "InstallPath", log))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(TargetPath))
                {
                    log.LogError($"InstallPath is empty for workload {Workload.ItemSpec}");
                    return false;
                }

                return true;
            }
        }
    }

    internal sealed record PackageReference(string Name,
                                     string Version,
                                     string OutputDir,
                                     string relativeSourceDir = "");
}
