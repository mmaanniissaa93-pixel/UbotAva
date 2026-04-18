using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace UBot.Updater;

internal static class Program
{
    private const string DisplayName = "UBot";

    private static void Main()
    {
        try
        {
            var directory = Environment.CurrentDirectory;

            Console.WriteLine($"Starting {DisplayName} Updater...");

            // Find process working on this directory and kill it
            var processNames = new[] { "UBot", "UBot" };
            foreach (var processName in processNames)
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    if (process.StartInfo.WorkingDirectory == directory)
                    {
                        Console.WriteLine($"Killing process {process.Id}...");
                        process.Kill();
                        process.WaitForExit();
                    }
                }
            }

            var tempDirectory = Path.Combine(directory, "update_temp");
            var zipFilePath = tempDirectory + "\\update.zip";

            if (Directory.Exists(tempDirectory) && File.Exists(zipFilePath))
            {
                ZipFile.ExtractToDirectory(zipFilePath, tempDirectory);

                File.Delete(zipFilePath);
                CopyDir(tempDirectory, directory);
                Directory.Delete(tempDirectory, true);
            }

            Console.WriteLine($"Update applied successfully. Starting {DisplayName}...");
            var executablePath = Path.Combine(directory, "UBot.exe");
            if (!File.Exists(executablePath))
                executablePath = Path.Combine(directory, "UBot.exe");

            if (File.Exists(executablePath))
                Process.Start(executablePath);
            else
                Console.WriteLine("No bot executable found to start.");
        }
        catch (Exception ex)
        {
            File.WriteAllText("updater_error.log", ex.ToString());
        }

        Environment.Exit(0);
    }

    /// <summary>
    /// Copy directory to destination directory
    /// </summary>
    /// <param name="sourceFolder">The source folder</param>
    /// <param name="destFolder">The Destination folder</param>
    private static void CopyDir(string sourceFolder, string destFolder)
    {
        if (!Directory.Exists(destFolder))
            Directory.CreateDirectory(destFolder);

        // Get Files & Copy
        var files = Directory.GetFiles(sourceFolder);
        foreach (var file in files)
            File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true);

        // Get dirs recursively and copy files
        var folders = Directory.GetDirectories(sourceFolder);
        foreach (var folder in folders)
            CopyDir(folder, Path.Combine(destFolder, Path.GetFileName(folder)));
    }
}
