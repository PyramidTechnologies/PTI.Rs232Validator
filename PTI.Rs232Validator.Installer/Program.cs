using System.Diagnostics;
using System.Runtime.CompilerServices;
using WixSharp;
using File = WixSharp.File;

[assembly: InternalsVisibleTo(assemblyName: "PTI.Rs232Validator.Installer.aot")] // assembly name + '.aot suffix

internal class Program
{
    private static void Main(string[] args)
    {
        const string projectName = "RS232 Validator";
        var msiGuid = new Guid("C7154179-9B82-4016-AF33-E480F3E0F276");
        
        if (!TryGetStringArgument(args, "-BuildDir", out var buildDirectoryPath)
            || !TryGetStringArgument(args, "-Binary", out var guiBinaryPath))
        {
            throw new ArgumentException("Required arguments not provided. Usage: -BuildDir <path> -Binary <path>");
        }

        var guiPublishDir = Path.GetDirectoryName(guiBinaryPath);
        if (guiPublishDir is null)
        {
            throw new InvalidOperationException("Could not determine GUI publish directory.");
        }
        
        var installerDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
        var bannerPath = Path.Combine(installerDirectoryPath, "app.dialog_banner.bmp");
        var iconFilePath = Path.Combine(installerDirectoryPath, "icon.ico");
        
        var guiFileVersionInfo = FileVersionInfo.GetVersionInfo(guiBinaryPath);
        if (guiFileVersionInfo.FileVersion is null)
        {
            throw new InvalidOperationException("Could not get file version info for GUI binary.");
        }
        
        var guiBinaryName = Path.GetFileName(guiBinaryPath);

        var project =
            new ManagedProject(projectName,
                new Dir($@"%ProgramFiles%\Pyramid Technologies Inc\{projectName}",
                    new File(guiBinaryPath, new FileShortcut("RS232 Validator", "INSTALLDIR")),
                    new Files(Path.Combine(guiPublishDir, "*.dll"))),
                new Dir("%ProgramMenu%",
                    new ExeFileShortcut(projectName, $@"[INSTALLDIR]\{guiBinaryName}", "")
                    {
                        WorkingDirectory = "[INSTALLDIR]"
                    }
                ),
                new Dir("%Desktop%",
                    new ExeFileShortcut(projectName, $@"[INSTALLDIR]\{guiBinaryName}", "")
                    {
                        WorkingDirectory = "[INSTALLDIR]"
                    }
                ))
            {
                SourceBaseDir = guiPublishDir,
                ControlPanelInfo =
                {
                    Comments = "PTI RS232 Validator Application",
                    Manufacturer = "Pyramid Technologies Inc",
                    ProductIcon = iconFilePath,
                },
                GUID = msiGuid,
                Version = new Version(guiFileVersionInfo.FileVersion),
                MajorUpgradeStrategy = MajorUpgradeStrategy.Default,
                BannerImage = bannerPath
            };

        project.BuildMsi(Path.Combine(buildDirectoryPath, "PTI.Rs232Validator.Installer.msi"));
    }
    
    private static bool TryGetStringArgument(string[] args, string argName, out string argValue)
    {
        argValue = string.Empty;
        var argIndex = Array.IndexOf(args, argName);
        if (argIndex == -1)
        {
            return false;
        }

        if (argIndex + 1 >= args.Length)
        {
            return false;
        }

        argValue = args[argIndex + 1];
        return true;
    }
}