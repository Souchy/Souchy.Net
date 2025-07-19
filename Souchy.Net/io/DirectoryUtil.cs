using System;
using System.Collections.Generic;
using System.Text;

namespace Souchy.Net.io;

public static class DirectoryUtil
{
    public static string? FindDirectory(string path, string name)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
            return null;
        var dir = new DirectoryInfo(path);
        if (dir.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return dir.FullName;
        foreach (var subDir in dir.GetDirectories())
        {
            var found = FindDirectory(subDir.FullName, name);
            if (found != null)
                return found;
        }
        return null;
    }

    public static void MoveDirectoryRec(string source, string target, bool overwrite = true)
    {
        var sourcePath = source.TrimEnd('\\', ' ');
        var targetPath = target.TrimEnd('\\', ' ');
        var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                             .GroupBy(Path.GetDirectoryName);
        foreach (var folder in files)
        {
            var targetFolder = folder.Key.Replace(sourcePath, targetPath);
            Directory.CreateDirectory(targetFolder);
            foreach (var file in folder)
            {
                var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                bool exists = File.Exists(targetFile);

                if (overwrite && exists)
                {
                    File.Delete(targetFile);
                }
                if (overwrite || !exists)
                {
                    File.Move(file, targetFile);
                }

            }
        }
        Directory.Delete(source, true);
    }

    public static void CopyDirectory(string source, string target, bool overwrite = true)
    {
        var sourcePath = source.TrimEnd('\\', ' ');
        var targetPath = target.TrimEnd('\\', ' ');
        var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                             .GroupBy(Path.GetDirectoryName);
        foreach (var folder in files)
        {
            var targetFolder = folder.Key.Replace(sourcePath, targetPath);
            Directory.CreateDirectory(targetFolder);
            foreach (var file in folder)
            {
                var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                File.Copy(file, targetFile, overwrite);
            }
        }
    }

}
