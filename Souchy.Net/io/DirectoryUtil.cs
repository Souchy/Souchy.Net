using System;
using System.Collections.Generic;
using System.Text;

namespace Souchy.Net.io;

public static class DirectoryUtil
{
    public static DirectoryInfo? FindDirectory(string path, string name)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
            return null;
        var dir = new DirectoryInfo(path);
        if (dir.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return dir;
        foreach (var subDir in dir.GetDirectories())
        {
            var found = FindDirectory(subDir.FullName, name);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Moves files and combines directories from source to target.
    /// Old files are deleted if overwrite is true.
    /// Old directories are not deleted so you can combine directories.
    /// Source path is deleted.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target">Replaces source path by target path for each file</param>
    /// <param name="overwrite"></param>
    public static void MoveDirectory(string source, string target, bool overwrite = true)
    {
        var sourcePath = source.Replace("/", "\\").TrimEnd('\\', ' ');
        var targetPath = target.Replace("/", "\\").TrimEnd('\\', ' ');
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
        var sourcePath = source.Replace("/", "\\").TrimEnd('\\', ' ');
        var targetPath = target.Replace("/", "\\").TrimEnd('\\', ' ');
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
