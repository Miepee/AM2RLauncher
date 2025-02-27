﻿using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace AM2RLauncherLib;

/// <summary>
/// Class that does operations that work cross-platform.
/// </summary>
public static class CrossPlatformOperations
{
    /// <summary>
    /// The logger for <see cref="Core"/>, used to write any caught exceptions.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(typeof(CrossPlatformOperations));

    /// <summary>
    /// Name of the Launcher executable.
    /// </summary>
    public static readonly string LauncherName = AppDomain.CurrentDomain.FriendlyName;

    /// <summary>
    /// Path to the Home Folder.
    /// </summary>
    public static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create);

    /// <summary>
    /// Config file path for *nix based systems. <br/>
    /// </summary>
    /// <remarks>
    /// Linux: Will point to XDG_CONFIG_HOME/AM2RLauncher/config.xml <br/>
    /// Mac: Will point to ~/Library/Preferences/AM2RLauncher/config.xml. <br/>
    /// Anything else: <see langword="null"/>
    /// </remarks>
    public static string NixLauncherConfigFilePath
    {
        get
        {
            switch (OS.Name)
            {
                case "Linux": return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/AM2RLauncher/config.xml";
                case "Mac": return $"{Home}/Library/Preferences/AM2RLauncher/config.xml";
                default: return null;
            }
        }
    }

    /// <summary>
    /// Current Path where the Launcher Data is located.
    /// </summary>
    public static readonly string CurrentPath = GenerateCurrentPath();

    /// <summary>
    /// Generates the mirror list, depending on the current Platform.
    /// </summary>
    /// <returns>A <see cref="List{String}"/> containing the mirror links.</returns>
    public static List<string> GenerateMirrorList()
    {
        if (OS.IsWindows)
        {
            return new List<string>
            {
                "https://github.com/AM2R-Community-Developers/AM2R-Autopatcher-Windows.git",
                "https://gitlab.com/am2r-community-developers/AM2R-Autopatcher-Windows.git"
            };
        }
        if (OS.IsLinux)
        {
            return new List<string>
            {
                "https://github.com/AM2R-Community-Developers/AM2R-Autopatcher-Linux.git",
                "https://gitlab.com/am2r-community-developers/AM2R-Autopatcher-Linux.git"
            };
        }
        if (OS.IsMac)
        {
            return new List<string>
            {
                "https://github.com/Miepee/AM2R-Autopatcher-Mac.git"
                //TODO: make mac official at some point:tm: and mirror it on gitlab
            };

        }

        // Should never occur, but...
        log.Error($"{OS.Name} has no mirror lists!");
        return new List<string>();
    }



    /// <summary>
    /// This open a website cross-platform.
    /// </summary>
    /// <param name="url">The URL of the website to be opened.</param>
    public static void OpenURL(string url)
    {
        if (OS.IsWindows) Process.Start(url);
        else if (OS.IsLinux) Process.Start("xdg-open", url);
        else if (OS.IsMac) Process.Start("open", url);
        else log.Error($"{OS.Name} can't open URLs!");
    }

    /// <summary>
    /// Opens <paramref name="path"/> in a file explorer. Creates the directory if it doesn't exist.
    /// </summary>
    /// <param name="path">Path to open.</param>
    public static void OpenFolder(string path)
    {
        // We have to replace forward slashes with backslashes here on windows because explorer.exe is picky...
        // And on Nix systems, we want to replace ~ with its corresponding env var
        string realPath = OS.IsWindows ? Environment.ExpandEnvironmentVariables(path).Replace("/", "\\")
            : path.Replace("~", Home);
        
        log.Info($"Creating {realPath} if it did not exist before");
        Directory.CreateDirectory(realPath);

        // Needs quotes otherwise paths with space wont open
        if (OS.IsWindows)
            // And we're using explorer.exe to prevent people from stuffing system commands in here wholesale. That would be bad.
            Process.Start("explorer.exe", $"\"{realPath}\"");
        else if (OS.IsLinux)
            Process.Start("xdg-open", $"\"{realPath}\"");
        else if (OS.IsMac)
            Process.Start("open", $"\"{realPath}\"");
        else
            log.Error($"{OS.Name} can't open folders!");
    }

    /// <summary>
    /// Opens <paramref name="path"/> and selects it in a file explorer.
    /// Only selects on Windows and Mac, on Linux it just opens the folder. Does nothing if file doesn't exist.
    /// </summary>
    /// <param name="path">Path to open.</param>
    public static void OpenFolderAndSelectFile(string path)
    {
        // We have to replace forward slashes with backslashes here on windows because explorer.exe is picky...
        // And on nix systems, we want to replace ~ with its corresponding env var
        string realPath = OS.IsWindows ? Environment.ExpandEnvironmentVariables(path).Replace("/", "\\")
            : path.Replace("~", Home);
        if (!File.Exists(realPath))
        {
            log.Error($"{realPath}did not exist, operation to open its folder and select it was cancelled!");
            return;
        }

        // Needs quotes otherwise paths with spaces wont open
        if (OS.IsWindows)
            // And we're using explorer.exe to prevent people from stuffing system commands in here wholesale. That would be bad.
            Process.Start("explorer.exe", $"/select, \"{realPath}\"");
        else if (OS.IsLinux)
            // Linux only opens the directory because opening and selecting a file requires some dbus stuff I don't want to bother for now
            // If anyone wants to do a PR, feel free to!
            Process.Start("xdg-open", $"\"{Path.GetDirectoryName(realPath)}\"");
        else if (OS.IsMac)
            Process.Start("open", $"-R \"{realPath}\"");
        else
            log.Error($"{OS.Name} can't open select files in file explorer!");
    }

    /// <summary>
    /// Checks if command-line Java is installed and located in PATH.
    /// </summary>
    /// <returns><see langword="true"/> if it is installed, <see langword="false"/> if not.</returns>
    public static bool IsJavaInstalled()
    {
        string process = "";
        string arguments = "";

        if (OS.IsWindows)
        {
            process = "cmd.exe";
            arguments = "/C java -version";
        }
        else if (OS.IsUnix)
        {
            process = "java";
            arguments = "-version";
        }
        else
            log.Error($"{OS.Name} has no java process/arguments");

        ProcessStartInfo javaStart = new ProcessStartInfo
        {
            FileName = process,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };


        Process java = new Process { StartInfo = javaStart };

        // This is primarily for unix, but could be happening on windows as well
        // This gets triggered, if "java" cannot be found.
        try
        {
            java.Start();
            java.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }

        return java.ExitCode == 0;
    }

    /// <summary>
    /// Checks if command-line xdelta is installed and located in PATH.
    /// </summary>
    /// <returns><see langword="true"/> if it is installed, <see langword="false"/> if not.</returns>
    public static bool CheckIfXdeltaIsInstalled()
    {
        const string process = "xdelta3";
        const string arguments = "-V";

        ProcessStartInfo xdeltaStart = new ProcessStartInfo
        {
            FileName = process,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };


        Process xdelta = new Process { StartInfo = xdeltaStart };

        try
        {
            xdelta.Start();
            xdelta.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }

        return xdelta.ExitCode == 0;
    }

    /// <summary>
    /// This applies an Xdelta Patch cross-platform.
    /// </summary>
    /// <param name="originalFile">Full Path to the original file.</param>
    /// <param name="patchFile">Full Path to the Xdelta patch to apply.</param>
    /// <param name="outputFile">Full Path to the output file.</param>
    /// <remarks>This method assumes that Xdelta is already installed and located in PATH, except
    /// for Windows, where it uses the provided one.</remarks>
    public static void ApplyXdeltaPatch(string originalFile, string patchFile, string outputFile)
    {
        // For *whatever reason* **sometimes** xdelta patching doesn't work, if outputFile = originalFile. So I'm fixing that here.
        string originalOutput = outputFile;
        if (originalFile == outputFile)
            outputFile += "_";

        // The reason why currentPath is taken out of all paths, is because xdelta (windows?) breaks if it has non-ascii characters
        // So for users who have a russian username for example, this would just throw.
        // By replacing the currentPath and setting the working directory to where we want it to be, we ensure to only have our ascii characters.
        string arguments = $"-f -d -s \"{originalFile.Replace($"{CurrentPath}/", "")}\" \"{patchFile.Replace($"{CurrentPath}/", "")}\" \"{outputFile.Replace($"{CurrentPath}/", "")}\"";

        ProcessStartInfo parameters = new ProcessStartInfo
        {
            FileName = OS.IsWindows ? $"{CurrentPath}/PatchData/utilities/xdelta/xdelta3.exe" : "xdelta3",
            WorkingDirectory = $"{CurrentPath}",
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = arguments
        };

        using Process proc = new Process { StartInfo = parameters };
        proc.Start();
        proc.WaitForExit();


        if ((originalOutput == outputFile) || !File.Exists(outputFile))
            return;

        File.Delete(originalOutput);
        File.Move(outputFile, originalOutput);
    }

    /// <summary>
    /// Runs a Java jar file cross-platform.
    /// </summary>
    /// <param name="arguments">The arguments for the jar file.</param>
    /// <param name="workingDirectory">The working directory for the jar process.
    /// If <see langword="null"/> then it will fallback to the users' Home directory</param>
    /// <remarks>This assumes that Java is installed and located in PATH.</remarks>
    public static void RunJavaJar(string arguments = null, string workingDirectory = null)
    {
        workingDirectory ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string proc = "",
               javaArgs = "";

        if (OS.IsWindows)
        {
            proc = "cmd";
            javaArgs = "/C java -jar";
        }
        else if (OS.IsUnix)
        {
            proc = "java";
            javaArgs = "-jar";
        }
        else
            log.Error($"{OS.Name} has no java process!");

        ProcessStartInfo jarStart = new ProcessStartInfo
        {
            FileName = proc,
            Arguments = $"{javaArgs} {arguments}",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process jarProcess = new Process
        {
            StartInfo = jarStart
        };

        jarProcess.Start();
        jarProcess.WaitForExit();
    }

    /// <summary>
    /// Figures out what the AM2RLauncher's <see cref="CurrentPath"/> should be.<br/>
    /// </summary>
    /// <remarks>
    /// Determination is as follows:
    /// <list type="number">
    ///     <item><b>$AM2RLAUNCHERDATA</b> environment variable is read and folders are recursively generated.</item>
    ///     <item>The current OS is checked. For Windows, the path where the executable is located will be returned.<br/>
    ///     For Linux, <b>$XDG_DATA_HOME/AM2RLauncher</b> will be returned.
    ///     Should <b>$XDG_DATA_HOME</b> be empty, it will default to <b>$HOME/.local/share</b>.<br/>
    ///     For Mac, <b>HOME/Library/Application Support/AM2RLauncher"</b> will be returned.</item>
    ///     <item>The path where the executable is located will be returned.</item>
    /// </list>
    /// Should any errors occur, it falls down to the next step.</remarks>
    /// <returns>The path where the AM2RLauncher can store its data.</returns>
    private static string GenerateCurrentPath()
    {
        // First, we check if the user has a custom AM2RLAUNCHERDATA env var
        string am2rLauncherDataEnvVar = Environment.GetEnvironmentVariable("AM2RLAUNCHERDATA");
        if (!String.IsNullOrWhiteSpace(am2rLauncherDataEnvVar))
        {
            try
            {
                // This will create the directories recursively if they don't exist
                Directory.CreateDirectory(am2rLauncherDataEnvVar);

                // Our env var is now set and directories exist
                log.Info($"CurrentPath is set to {am2rLauncherDataEnvVar}");
                return am2rLauncherDataEnvVar;
            }
            catch (Exception ex)
            {
                log.Error($"There was an error with '{am2rLauncherDataEnvVar}'!\n{ex.Message} {ex.StackTrace}. Falling back to defaults.");
            }
        }

        if (OS.IsWindows)
        {
            log.Info("Using default Windows CurrentPath.");
            // Windows has the path where the exe is located as default
            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        }
        else if (OS.IsLinux)
        {

            // Linux has the Path at XDG_DATA_HOME/AM2RLauncher
            string linuxPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify)}/AM2RLauncher";

            try
            {
                Directory.CreateDirectory(linuxPath);
                log.Info($"CurrentPath is set to {linuxPath}");
                return linuxPath;
            }
            catch (Exception ex)
            {
                log.Error($"There was an error with '{linuxPath}'!\n{ex.Message} {ex.StackTrace}. Falling back to defaults.");
            }
        }
        else if (OS.IsMac)
        {
            // Cannot use SpecialFolders here, as the current .NET version returns them wrongly.
            // Mac has the Path at HOME/Application Support/Library/AM2RLauncher
            string macPath = $"{Home}/Library/Application Support/AM2RLauncher";
            try
            {
                Directory.CreateDirectory(macPath);
                log.Info("Using default Mac CurrentPath.");
                return macPath;
            }
            catch (Exception ex)
            {
                log.Error($"There was an error with '{macPath}'!\n{ex.Message} {ex.StackTrace}. Falling back to defaults.");
            }
        }
        else
            log.Error($"{OS.Name} has no current path!");

        log.Info("Something went wrong, falling back to the default CurrentPath.");
        return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
    }
}