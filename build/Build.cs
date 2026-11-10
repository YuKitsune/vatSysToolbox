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
using Fallout.Common.Tools.Git;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Tools.ILRepack;
using Octokit;
using Serilog;

[SupportedOSPlatform("Windows")]
[GitHubActions(
    "build",
    GitHubActionsImage.WindowsLatest,
    OnPushBranches = ["main"],
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(Compile), nameof(RepackPlugin)],
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

    AbsolutePath ProjectPath => RootDirectory / "source" / "vatSysToolbox" / "vatSysToolbox.csproj";
    AbsolutePath BuildOutputDirectory => TemporaryDirectory / "build";
    AbsolutePath PackageOutputDirectory => TemporaryDirectory / "package";

    // The plugins. Each name is the name of the project directory, the project, and the assembly.
    static readonly string[] Plugins = ["EurocatMenusPlugin", "TagListPlugin"];
    const string HarmonyAssemblyFileName = "0Harmony.dll";

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
                .SetAssemblyVersion(GetMajorMinorPatch())
                .SetFileVersion(GetMajorMinorPatch())
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
                .SetAssemblyVersion(GetMajorMinorPatch())
                .SetFileVersion(GetMajorMinorPatch())
                .SetInformationalVersion(version)
                .SetProperty("VatSysPath", VatSysExePath.Parent.Parent));
        });

    Target CompilePlugin => _ => _
        .Description("Compiles the plugins")
        .DependsOn(DownloadVatSys)
        .Executes(() =>
        {
            var version = GetSemanticVersion();
            foreach (var plugin in Plugins)
            {
                var outputDirectory = GetPluginBuildOutputDirectory(plugin);
                Log.Information(
                    "Building {Plugin} version {Version} with configuration {Configuration} to {OutputDirectory}",
                    plugin,
                    version,
                    Configuration,
                    outputDirectory);

                DotNetTasks.DotNetBuild(s => s
                    .SetProjectFile(RootDirectory / "source" / plugin / $"{plugin}.csproj")
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(outputDirectory)
                    .SetVersion(version)
                    .SetAssemblyVersion(GetMajorMinorPatch())
                    .SetFileVersion(GetMajorMinorPatch())
                    .SetInformationalVersion(version)
                    .SetProperty("VatSysPath", VatSysExePath.Parent.Parent));
            }
        });

    Target RepackPlugin => _ => _
        .Description("Merges the dependencies (including the toolbox) into each plugin assembly with ILRepack.")
        .DependsOn(CompilePlugin)
        .Executes(() =>
        {
            foreach (var plugin in Plugins)
                RepackPluginAssembly(plugin);
        });

    Target Uninstall => _ => _
        .Description("Uninstalls the plugins from the specified profile.")
        .Requires(() => ProfileName)
        .Executes(() =>
        {
            var pluginsDirectory = GetVatSysPluginsDirectory(ProfileName);
            foreach (var plugin in Plugins)
            {
                AbsolutePath[] pluginDirectories =
                [
                    pluginsDirectory / GetPluginDirectoryName(plugin, Configuration.Debug),
                    pluginsDirectory / GetPluginDirectoryName(plugin, Configuration.Release)
                ];

                foreach (var pluginDirectory in pluginDirectories)
                {
                    pluginDirectory.DeleteDirectory();
                    Log.Information("Plugin uninstalled from {Directory}", pluginDirectory);
                }
            }
        });

    Target Install => _ => _
        .Description("Installs the plugins to the specified profile.")
        .Requires(() => ProfileName)
        .DependsOn(RepackPlugin)
        .DependsOn(Uninstall)
        .Executes(() =>
        {
            var pluginsDirectory = GetVatSysPluginsDirectory(ProfileName);
            Log.Information("Installing plugins to {TargetDirectory}", pluginsDirectory);

            if (!pluginsDirectory.Exists())
                pluginsDirectory.CreateDirectory();

            foreach (var plugin in Plugins)
            {
                var pluginDirectory = pluginsDirectory / GetPluginDirectoryName(plugin, Configuration);
                pluginDirectory.CreateOrCleanDirectory();
                foreach (var absolutePath in GetPluginBuildOutputDirectory(plugin).GetFiles())
                {
                    absolutePath.CopyToDirectory(pluginDirectory, ExistsPolicy.MergeAndOverwrite);
                }

                Log.Information("Plugin installed to {PluginDirectory}", pluginDirectory);
            }
        });

    Target PackagePlugin => _ => _
        .Description("Bundles each plugin into its own zip archive.")
        .DependsOn(RepackPlugin)
        .Requires(() => Configuration == Configuration.Release)
        .Executes(() =>
        {
            foreach (var plugin in Plugins)
            {
                var packageDirectory = GetPluginPackageDirectory(plugin);
                var zipPath = GetPluginZipPath(plugin);
                packageDirectory.CreateOrCleanDirectory();

                var files = GetPluginBuildOutputDirectory(plugin).GetFiles().Where(f => f.Extension != ".pdb");
                foreach (var absolutePath in files)
                {
                    absolutePath.CopyToDirectory(packageDirectory, ExistsPolicy.MergeAndOverwrite);
                }

                if (zipPath.FileExists())
                    zipPath.DeleteFile();

                Log.Information("Packaging {OutputDirectory} to {ZipPath}", packageDirectory, zipPath);
                packageDirectory.ZipTo(zipPath);
            }
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
        .Description("Creates a GitHub release with the NuGet package and the plugins attached.")
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

            foreach (var plugin in Plugins)
            {
                var zipPath = GetPluginZipPath(plugin);
                using var pluginZipStream = File.OpenRead(zipPath);
                var pluginAssetUpload = new ReleaseAssetUpload
                {
                    FileName = zipPath.Name,
                    ContentType = "application/zip",
                    RawData = pluginZipStream
                };

                var pluginAsset = await githubClient.Repository.Release.UploadAsset(release, pluginAssetUpload);
                Log.Information("Plugin asset uploaded: {AssetUrl}", pluginAsset.BrowserDownloadUrl);
            }
        });

    void RepackPluginAssembly(string plugin)
    {
        var outputDirectory = GetPluginBuildOutputDirectory(plugin);
        var mainAssembly = outputDirectory / $"{plugin}.dll";
        if (!mainAssembly.FileExists())
            throw new Exception($"Main assembly not found: {mainAssembly}");

        // Harmony stays a separate file. vatSys loads only one copy of it, and the plugins that use Harmony share it.
        var assembliesToMerge = outputDirectory
            .GlobFiles("*.dll")
            .Where(a => a != mainAssembly && a.Name != HarmonyAssemblyFileName)
            .ToArray();

        if (assembliesToMerge.Length == 0)
        {
            Log.Information("No assemblies found to repack for {Plugin}, skipping", plugin);
            return;
        }

        var settings = new ILRepackSettings()
            .SetAssemblies([mainAssembly.ToString(), ..assembliesToMerge.Select(a => a.ToString())])
            .SetInternalize(false)
            .SetParallel(true)
            .SetOutput(mainAssembly.ToString())
            .SetLib(outputDirectory.ToString());

        Log.Information("Repacking {Count} assemblies into {MainAssembly}", assembliesToMerge.Length, mainAssembly);
        foreach (var assembly in assembliesToMerge)
            Log.Information("  - {Assembly}", assembly.Name);

        ILRepackTasks.ILRepack(settings);

        foreach (var assembly in assembliesToMerge)
            assembly.DeleteFile();
    }

    // Each plugin has its own directories, so that each plugin has its own binary and zip archive
    AbsolutePath GetPluginBuildOutputDirectory(string plugin) => TemporaryDirectory / "build-plugin" / plugin;

    AbsolutePath GetPluginPackageDirectory(string plugin) => TemporaryDirectory / "package-plugin" / plugin;

    AbsolutePath GetPluginZipPath(string plugin) => TemporaryDirectory / $"{plugin}.{GetSemanticVersion()}.zip";

    // The Debug and the Release plugins have different directories, so that both can be installed
    static string GetPluginDirectoryName(string plugin, Configuration configuration)
    {
        return configuration == Configuration.Debug ? $"{plugin} - Debug" : plugin;
    }

    static AbsolutePath GetVatSysPluginsDirectory(string profileName)
    {
        return GetVatSysProfilePath(profileName) / "Plugins";
    }

    static AbsolutePath GetVatSysProfilePath(string profileName)
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, "vatSys Files", "Profiles", profileName);
    }

    // The version is the tag on the current commit (for example, v1.2.3 gives 1.2.3). A commit with no tag is a local
    // build.
    string GetSemanticVersion()
    {
        var exactTag = GitTasks.Git("tag --points-at HEAD")
            .Select(o => o.Text.Trim())
            .FirstOrDefault(t => t.StartsWith("v"));

        return exactTag != null ? exactTag.TrimStart('v') : "0.0.0-local";
    }

    string GetMajorMinorPatch() => GetSemanticVersion().Split('-')[0];
}
