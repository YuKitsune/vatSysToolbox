using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Tools.GitVersion;
using Fallout.Common.Tools.ILRepack;
using Octokit;
using Serilog;

[SupportedOSPlatform("Windows")]
[GitHubActions(
    "build",
    GitHubActionsImage.WindowsLatest,
    OnPushBranches = ["main"],
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(Compile)],
    FetchDepth = 0)]
[GitHubActions(
    "release",
    GitHubActionsImage.WindowsLatest,
    OnPushTags = ["v*"],
    InvokedTargets = [nameof(Publish), nameof(Release)],
    ImportSecrets = [nameof(GitHubToken)],
    EnableGitHubToken = true,
    FetchDepth = 0,
    WritePermissions = [GitHubActionsPermissions.Contents, GitHubActionsPermissions.Packages])]
class Build : FalloutBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("GitHub token for creating releases and publishing packages")]
    [Secret]
    readonly string GitHubToken;

    [Parameter("Name of the vatSys profile to install the plugin to")]
    readonly string ProfileName;

    [GitRepository]
    readonly GitRepository GitRepository;

    [GitVersion]
    readonly GitVersion GitVersion;

    AbsolutePath ProjectPath => RootDirectory / "source" / "vatSysToolbox" / "vatSysToolbox.csproj";
    AbsolutePath BuildOutputDirectory => TemporaryDirectory / "build";
    AbsolutePath PackageOutputDirectory => TemporaryDirectory / "package";

    // Plugin paths
    const string ReleasePluginName = "EurocatMenusPlugin";
    const string DebugPluginName = "EurocatMenusPlugin - Debug";
    const string PluginAssemblyFileName = "EurocatMenusPlugin.dll";
    string PluginName => Configuration == Configuration.Debug ? DebugPluginName : ReleasePluginName;
    AbsolutePath PluginProjectPath => RootDirectory / "source" / "EurocatMenusPlugin" / "EurocatMenusPlugin.csproj";
    AbsolutePath PluginBuildOutputDirectory => TemporaryDirectory / "build-plugin";
    AbsolutePath PluginPackageDirectory => TemporaryDirectory / "package-plugin";
    AbsolutePath PluginZipPath => TemporaryDirectory / $"EurocatMenusPlugin.{GetSemanticVersion()}.zip";

    // vatSys paths
    [Parameter("Path to the vatSys installation")]
    AbsolutePath VatSysPath { get; }
    AbsolutePath VatSysSetupDirectory => TemporaryDirectory / "vatsys-setup";
    AbsolutePath VatSysExePath => VatSysPath ?? VatSysSetupDirectory / "bin" / "vatSys.exe";

    public Target CheckVersion => _ => _
        .Description("Prints the semantic version that will be assigned to the binaries.")
        .Unlisted()
        .Executes(() => Log.Information("Version: {Version}", GetSemanticVersion()));

    Target DownloadVatSys => _ => _
        .OnlyWhenStatic(() => VatSysPath == null && !VatSysExePath.FileExists())
        .Executes(async () =>
        {
            var vatSysSetupUrl = "https://vatsys.sawbe.com/downloads/vatSysSetup.zip";
            var zipPath = TemporaryDirectory / "vatSysSetup.zip";
            var msiPath = TemporaryDirectory / "vatSysSetup.msi";

            Log.Information("Downloading vatSys from {Url}", vatSysSetupUrl);
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(vatSysSetupUrl);
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(zipPath);
            await response.Content.CopyToAsync(fileStream);
            fileStream.Close();

            Log.Information("Extracting vatSysSetup.zip");
            ZipFile.ExtractToDirectory(zipPath, TemporaryDirectory, overwriteFiles: true);

            Log.Information("Extracting vatSysSetup.msi");
            VatSysSetupDirectory.CreateOrCleanDirectory();

            // Use msiexec to extract the MSI contents
            var msiExtractProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/a \"{msiPath}\" /qn TARGETDIR=\"{VatSysSetupDirectory}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (msiExtractProcess != null)
            {
                await msiExtractProcess.WaitForExitAsync();
                if (msiExtractProcess.ExitCode != 0)
                {
                    var error = await msiExtractProcess.StandardError.ReadToEndAsync();
                    throw new Exception($"Failed to extract MSI: {error}");
                }
            }

            if (!VatSysExePath.FileExists())
                throw new Exception($"vatSys.exe not found at {VatSysExePath}");

            Log.Information("vatSys.exe extracted to {Path}", VatSysExePath);
        });

    Target Compile => _ => _
        .Description("Compiles the toolbox")
        .DependsOn(DownloadVatSys)
        .Executes(() =>
        {
            var version = GetSemanticVersion();
            Log.Information(
                "Building version {Version} with configuration {Configuration} to {OutputDirectory}",
                version,
                Configuration,
                BuildOutputDirectory);

            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(ProjectPath)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(BuildOutputDirectory)
                .SetVersion(version)
                .SetAssemblyVersion(GitVersion.MajorMinorPatch)
                .SetFileVersion(GitVersion.MajorMinorPatch)
                .SetInformationalVersion(version)
                .SetProperty("VatSysPath", VatSysExePath.Parent.Parent));
        });

    Target Pack => _ => _
        .Description("Creates the NuGet package for the toolbox.")
        .DependsOn(DownloadVatSys)
        .Requires(() => Configuration == Configuration.Release)
        .Executes(() =>
        {
            var version = GetSemanticVersion();
            PackageOutputDirectory.CreateOrCleanDirectory();

            Log.Information("Packing version {Version} to {OutputDirectory}", version, PackageOutputDirectory);
            DotNetTasks.DotNetPack(s => s
                .SetProject(ProjectPath)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageOutputDirectory)
                .SetVersion(version)
                .SetAssemblyVersion(GitVersion.MajorMinorPatch)
                .SetFileVersion(GitVersion.MajorMinorPatch)
                .SetInformationalVersion(version)
                .SetProperty("VatSysPath", VatSysExePath.Parent.Parent));
        });

    Target CompilePlugin => _ => _
        .Description("Compiles the Eurocat Menus plugin")
        .DependsOn(DownloadVatSys)
        .Executes(() =>
        {
            var version = GetSemanticVersion();
            Log.Information(
                "Building plugin version {Version} with configuration {Configuration} to {OutputDirectory}",
                version,
                Configuration,
                PluginBuildOutputDirectory);

            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(PluginProjectPath)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PluginBuildOutputDirectory)
                .SetVersion(version)
                .SetAssemblyVersion(GitVersion.MajorMinorPatch)
                .SetFileVersion(GitVersion.MajorMinorPatch)
                .SetInformationalVersion(version)
                .SetProperty("VatSysPath", VatSysExePath.Parent.Parent));
        });

    Target RepackPlugin => _ => _
        .Description("Merges the plugin dependencies (including the toolbox) into the plugin assembly with ILRepack.")
        .DependsOn(CompilePlugin)
        .Executes(() =>
        {
            var mainAssembly = PluginBuildOutputDirectory / PluginAssemblyFileName;
            if (!mainAssembly.FileExists())
                throw new Exception($"Main assembly not found: {mainAssembly}");

            var assembliesToMerge = PluginBuildOutputDirectory
                .GlobFiles("*.dll")
                .Where(a => a != mainAssembly)
                .ToArray();

            if (assembliesToMerge.Length == 0)
            {
                Log.Information("No assemblies found to repack, skipping");
                return;
            }

            var settings = new ILRepackSettings()
                .SetAssemblies([mainAssembly.ToString(), ..assembliesToMerge.Select(a => a.ToString())])
                .SetInternalize(false)
                .SetParallel(true)
                .SetOutput(mainAssembly.ToString())
                .SetLib(PluginBuildOutputDirectory.ToString());

            Log.Information("Repacking {Count} assemblies into {MainAssembly}", assembliesToMerge.Length, mainAssembly);
            foreach (var assembly in assembliesToMerge)
                Log.Information("  - {Assembly}", assembly.Name);

            ILRepackTasks.ILRepack(settings);

            foreach (var assembly in assembliesToMerge)
                assembly.DeleteFile();
        });

    Target Uninstall => _ => _
        .Description("Uninstalls the plugin from the specified profile.")
        .Requires(() => ProfileName)
        .Executes(() =>
        {
            var pluginsDirectory = GetVatSysPluginsDirectory(ProfileName);
            AbsolutePath[] pluginDirectories =
            [
                pluginsDirectory / DebugPluginName,
                pluginsDirectory / ReleasePluginName
            ];

            foreach (var pluginDirectory in pluginDirectories)
            {
                pluginDirectory.DeleteDirectory();
                Log.Information("Plugin uninstalled from {Directory}", pluginDirectory);
            }
        });

    Target Install => _ => _
        .Description("Installs the plugin to the specified profile.")
        .Requires(() => ProfileName)
        .DependsOn(RepackPlugin)
        .DependsOn(Uninstall)
        .Executes(() =>
        {
            var pluginsDirectory = GetVatSysPluginsDirectory(ProfileName);
            Log.Information("Installing plugin to {TargetDirectory}", pluginsDirectory);

            if (!pluginsDirectory.Exists())
                pluginsDirectory.CreateDirectory();

            var pluginDirectory = pluginsDirectory / PluginName;
            pluginDirectory.CreateOrCleanDirectory();
            foreach (var absolutePath in PluginBuildOutputDirectory.GetFiles())
            {
                absolutePath.CopyToDirectory(pluginDirectory, ExistsPolicy.MergeAndOverwrite);
            }

            Log.Information("Plugin installed to {PluginDirectory}", pluginDirectory);
        });

    Target PackagePlugin => _ => _
        .Description("Bundles the plugin into a zip archive.")
        .DependsOn(RepackPlugin)
        .Requires(() => Configuration == Configuration.Release)
        .Executes(() =>
        {
            PluginPackageDirectory.CreateOrCleanDirectory();

            foreach (var absolutePath in PluginBuildOutputDirectory.GetFiles().Where(f => f.Extension != ".pdb"))
            {
                absolutePath.CopyToDirectory(PluginPackageDirectory, ExistsPolicy.MergeAndOverwrite);
            }

            if (PluginZipPath.FileExists())
                PluginZipPath.DeleteFile();

            Log.Information("Packaging {OutputDirectory} to {ZipPath}", PluginPackageDirectory, PluginZipPath);
            PluginPackageDirectory.ZipTo(PluginZipPath);
        });

    Target Publish => _ => _
        .Description("Pushes the NuGet package to the GitHub Packages feed.")
        .DependsOn(Pack)
        .Requires(() => GitHubToken)
        .Requires(() => GitRepository)
        .Requires(() => Configuration == Configuration.Release)
        .Executes(() =>
        {
            var feedUrl = $"https://nuget.pkg.github.com/{GitRepository.GetGitHubOwner()}/index.json";

            foreach (var packagePath in PackageOutputDirectory.GlobFiles("*.nupkg"))
            {
                Log.Information("Pushing {Package} to {FeedUrl}", packagePath.Name, feedUrl);
                DotNetTasks.DotNetNuGetPush(s => s
                    .SetTargetPath(packagePath)
                    .SetSource(feedUrl)
                    .SetApiKey(GitHubToken));
            }
        });

    Target Release => _ => _
        .Description("Creates a GitHub release with the NuGet package and the Eurocat Menus plugin attached.")
        .DependsOn(Pack, PackagePlugin)
        .Requires(() => GitHubToken)
        .Requires(() => GitRepository)
        .Requires(() => Configuration == Configuration.Release)
        .Executes(async () =>
        {
            var version = GetSemanticVersion();
            var tagName = $"v{version}";

            Log.Information("Creating GitHub release {TagName}", tagName);

            var credentials = new Credentials(GitHubToken);
            var githubClient = new GitHubClient(new ProductHeaderValue("fallout-build"))
            {
                Credentials = credentials
            };

            var repositoryOwner = GitRepository.GetGitHubOwner();
            var repositoryName = GitRepository.GetGitHubName();

            var newRelease = new NewRelease(tagName)
            {
                Name = version,
                Draft = false,
                Prerelease = false,
                GenerateReleaseNotes = true
            };

            var release = await githubClient.Repository.Release.Create(repositoryOwner, repositoryName, newRelease);
            Log.Information("Release created: {ReleaseUrl}", release.HtmlUrl);

            foreach (var packagePath in PackageOutputDirectory.GlobFiles("*.nupkg"))
            {
                using var packageStream = File.OpenRead(packagePath);
                var assetUpload = new ReleaseAssetUpload
                {
                    FileName = packagePath.Name,
                    ContentType = "application/octet-stream",
                    RawData = packageStream
                };

                var asset = await githubClient.Repository.Release.UploadAsset(release, assetUpload);
                Log.Information("Package asset uploaded: {AssetUrl}", asset.BrowserDownloadUrl);
            }

            using var pluginZipStream = File.OpenRead(PluginZipPath);
            var pluginAssetUpload = new ReleaseAssetUpload
            {
                FileName = PluginZipPath.Name,
                ContentType = "application/zip",
                RawData = pluginZipStream
            };

            var pluginAsset = await githubClient.Repository.Release.UploadAsset(release, pluginAssetUpload);
            Log.Information("Plugin asset uploaded: {AssetUrl}", pluginAsset.BrowserDownloadUrl);
        });

    static AbsolutePath GetVatSysPluginsDirectory(string profileName)
    {
        return GetVatSysProfilePath(profileName) / "Plugins";
    }

    static AbsolutePath GetVatSysProfilePath(string profileName)
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, "vatSys Files", "Profiles", profileName);
    }

    string GetSemanticVersion()
    {
        // For main/master branch, or if we're on a detached head (building from a tag)
        // use major.minor.patch (e.g., "1.2.3")
        if (string.IsNullOrEmpty(GitVersion.BranchName) || GitVersion.BranchName is "main" or "master")
        {
            return GitVersion.MajorMinorPatch;
        }

        // For other branches (develop, hotfix, etc.): use SemVer format
        return GitVersion.SemVer;
    }
}
