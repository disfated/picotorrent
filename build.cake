#r "./tools/Cake.CMake/lib/net45/Cake.CMake.dll"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target        = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var platform      = Argument("platform", "x64");

// Parameters
var BuildDirectory     = Directory("./build-" + platform) + Directory(configuration);
var ResourceDirectory  = Directory("./res");
var SigningCertificate = EnvironmentVariable("PICO_SIGNING_CERTIFICATE");
var SigningPassword    = EnvironmentVariable("PICO_SIGNING_PASSWORD");
var Version            = System.IO.File.ReadAllText("VERSION").Trim();
var Installer          = string.Format("PicoTorrent-{0}-x64.msi", Version);
var InstallerBundle    = string.Format("PicoTorrent-{0}-x64.exe", Version);

public void SignTool(FilePath file)
{
    var signTool = "C:\\Program Files (x86)\\Windows Kits\\8.1\\bin\\x64\\signtool.exe";
    var argsBuilder = new ProcessArgumentBuilder();

    argsBuilder.Append("sign");
    argsBuilder.Append("/d {0}", "PicoTorrent");
    argsBuilder.Append("/f {0}", SigningCertificate);
    argsBuilder.AppendSecret("/p {0}", SigningPassword);
    argsBuilder.Append("/t http://timestamp.digicert.com");
    argsBuilder.AppendQuoted("{0}", file);

    int exitCode = StartProcess(signTool, new ProcessSettings
    {
        Arguments = argsBuilder
    });

    if(exitCode != 0)
    {
        throw new CakeException("SignTool exited with error: " + exitCode);
    }
}

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(BuildDirectory);
});

Task("Generate-Project")
    .IsDependentOn("Clean")
    .Does(() =>
{
    CreateDirectory("build");

    var generator = "Visual Studio 14 Win64";

    if(platform == "x86")
    {
        generator = "Visual Studio 14";
    }

    CMake("./", new CMakeSettings {
      OutputPath = "./build-" + platform,
      Generator = generator,
      Toolset = "v140"
    });
});

Task("Build")
    .IsDependentOn("Generate-Project")
    .Does(() =>
{
    MSBuild("./build-" + platform + "/PicoTorrent.sln",
        settings => settings.SetConfiguration(configuration));
});

Task("Build-Installer")
    .IsDependentOn("Build")
    .Does(() =>
{
    WiXCandle("./installer/PicoTorrent.wxs", new CandleSettings
    {
        Architecture = Architecture.X64,
        Defines = new Dictionary<string, string>
        {
            { "BuildDirectory", BuildDirectory },
            { "ResourceDirectory", ResourceDirectory },
            { "Version", Version }
        },
        OutputDirectory = BuildDirectory
    });

    WiXLight(BuildDirectory + File("PicoTorrent.wixobj"), new LightSettings
    {
        OutputFile = BuildDirectory + File(Installer)
    });
});

Task("Build-Installer-Bundle")
    .IsDependentOn("Build")
    .Does(() =>
{
    WiXCandle("./installer/PicoTorrentBundle.wxs", new CandleSettings
    {
        Architecture = Architecture.X64,
        Extensions = new [] { "WixBalExtension", "WixUtilExtension" },
        Defines = new Dictionary<string, string>
        {
            { "PicoTorrentInstaller", BuildDirectory + File(Installer) },
            { "Version", Version }
        },
        OutputDirectory = BuildDirectory
    });

    WiXLight(BuildDirectory + File("PicoTorrentBundle.wixobj"), new LightSettings
    {
        Extensions = new [] { "WixBalExtension", "WixUtilExtension" },
        OutputFile = BuildDirectory + File(InstallerBundle)
    });
});

Task("Build-Chocolatey-Package")
    .IsDependentOn("Build-Installer")
    .Does(() =>
{
    TransformTextFile("./chocolatey/tools/chocolateyinstall.ps1.template", "%{", "}")
        .WithToken("Installer", InstallerBundle)
        .WithToken("Version", Version)
        .Save("./chocolatey/tools/chocolateyinstall.ps1");

    var currentDirectory = MakeAbsolute(Directory("."));
    var cd = MakeAbsolute(BuildDirectory);
    var nuspec = MakeAbsolute(File("./chocolatey/picotorrent.nuspec"));

    System.IO.Directory.SetCurrentDirectory(cd.ToString());

    ChocolateyPack(nuspec, new ChocolateyPackSettings
    {
        Version = Version
    });

    System.IO.Directory.SetCurrentDirectory(currentDirectory.ToString());
});

Task("Sign")
    .IsDependentOn("Build")
    .WithCriteria(() => SigningCertificate != null && SigningPassword != null)
    .Does(() =>
{
    var file = BuildDirectory + File("PicoTorrent.exe");
    SignTool(file);
});

Task("Sign-Installer")
    .IsDependentOn("Build-Installer")
    .WithCriteria(() => SigningCertificate != null && SigningPassword != null)
    .Does(() =>
{
    var file = BuildDirectory + File(Installer);
    SignTool(file);
});

Task("Sign-Installer-Bundle")
    .IsDependentOn("Build-Installer-Bundle")
    .WithCriteria(() => SigningCertificate != null && SigningPassword != null)
    .Does(() =>
{
    var bundle = BuildDirectory + File(InstallerBundle);
    var insignia = Directory("tools")
                   + Directory("WiX.Toolset")
                   + Directory("tools")
                   + Directory("wix")
                   + File("insignia.exe");

    // Detach Burn engine
    StartProcess(insignia, "-ib \"" + bundle + "\" -o build/BurnEngine.exe");
    SignTool("build/BurnEngine.exe");
    StartProcess(insignia, "-ab build/BurnEngine.exe \"" + bundle + "\" -o \"" + bundle + "\"");

    // Sign the bundle
    SignTool(bundle);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build-Installer")
    .IsDependentOn("Build-Installer-Bundle")
    .IsDependentOn("Build-Chocolatey-Package");

Task("Publish")
    .IsDependentOn("Build")
    .IsDependentOn("Sign")
    .IsDependentOn("Build-Installer")
    .IsDependentOn("Build-Installer-Bundle")
    .IsDependentOn("Sign-Installer")
    .IsDependentOn("Sign-Installer-Bundle")
    .IsDependentOn("Build-Chocolatey-Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
