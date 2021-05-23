using System.IO;

static class IOUtil
{
    public static void CopyDirectory(string source, string dest, bool recursive = true) =>
        CopyDirectory(new DirectoryInfo(source), new DirectoryInfo(dest), recursive);

    public static void CopyDirectory(DirectoryInfo source, DirectoryInfo dest, bool recursive = true)
    {
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist: {source}");

        var dirs = source.GetDirectories();

        dest.Create();

        var files = source.GetFiles();
        foreach (var file in files)
            file.CopyTo(Path.Combine(dest.FullName, file.Name), false);

        if (recursive)
            foreach (var subdir in dirs)
                CopyDirectory(subdir.FullName, Path.Combine(dest.FullName, subdir.Name), recursive);
    }
}