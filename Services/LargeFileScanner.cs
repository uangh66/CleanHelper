using System.IO;
using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 大文件扫描服务 —— 扫描当前用户 6 个目录，仅读取元数据，不删除文件
/// 扫描范围：下载、桌面、文档、图片、视频、音乐
/// 不扫描：AppData、Windows、Program Files、其他用户目录
/// </summary>
public class LargeFileScanner : ILargeFileScanner
{
    private static readonly (string Path, string Category)[] ScanTargets;

    static LargeFileScanner()
    {
        var targets = new List<(string, string)>();

        void AddPath(string path, string category)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    targets.Add((path, category));
            }
            catch
            {
                // 目录不可用时跳过
            }
        }

        void AddSpecialFolder(Environment.SpecialFolder folder, string category)
        {
            try
            {
                var path = Environment.GetFolderPath(folder);
                AddPath(path, category);
            }
            catch
            {
                // 目录不可用时跳过
            }
        }

        // 下载目录（通过 UserProfile 拼接，因为 SpecialFolder 枚举不含 Downloads）
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            AddPath(Path.Combine(userProfile, "Downloads"), "下载");
        }
        catch { }

        AddSpecialFolder(Environment.SpecialFolder.Desktop, "桌面");
        AddSpecialFolder(Environment.SpecialFolder.MyDocuments, "文档");
        AddSpecialFolder(Environment.SpecialFolder.MyPictures, "图片");
        AddSpecialFolder(Environment.SpecialFolder.MyVideos, "视频");
        AddSpecialFolder(Environment.SpecialFolder.MyMusic, "音乐");

        ScanTargets = targets.ToArray();
    }

    public async Task<List<LargeFileInfo>> ScanAsync(long minSizeBytes, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var results = new List<LargeFileInfo>();

            foreach (var (rootPath, category) in ScanTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnumerateFilesRecursive(rootPath, rootPath, category, minSizeBytes, results, cancellationToken);
            }

            // 按大小降序排列
            results.Sort((a, b) => b.Size.CompareTo(a.Size));
            return results;
        }, cancellationToken);
    }

    /// <summary>
    /// 递归枚举目录中的大文件。跳过 ReparsePoint、无权限目录、异常文件。
    /// 不读取文件内容，只读取元数据。
    /// </summary>
    private static void EnumerateFilesRecursive(
        string rootPath,
        string directory,
        string category,
        long minSizeBytes,
        List<LargeFileInfo> results,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // 枚举文件
        try
        {
            foreach (var filePath in Directory.EnumerateFiles(directory))
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // 跳过 ReparsePoint
                    if (IsReparsePoint(filePath))
                        continue;

                    var fi = new FileInfo(filePath);

                    // 仅保留 >= 最小大小的文件
                    if (fi.Length < minSizeBytes)
                        continue;

                    results.Add(new LargeFileInfo
                    {
                        FileName = fi.Name,
                        FullPath = filePath,
                        Size = fi.Length,
                        LastWriteTime = fi.LastWriteTime,
                        Extension = fi.Extension.ToLowerInvariant(),
                        Category = category,
                        Suggestion = GetSuggestion(fi.Extension)
                    });
                }
                catch
                {
                    // 单个文件异常，跳过
                }
            }
        }
        catch (UnauthorizedAccessException) { return; }
        catch (DirectoryNotFoundException) { return; }
        catch (IOException) { return; }

        // 递归子目录
        try
        {
            foreach (var subDir in Directory.EnumerateDirectories(directory))
            {
                ct.ThrowIfCancellationRequested();

                // 跳过 ReparsePoint 目录（Junction / SymbolicLink / OneDrive 挂载点）
                if (IsReparsePoint(subDir))
                    continue;

                EnumerateFilesRecursive(rootPath, subDir, category, minSizeBytes, results, ct);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }
    }

    /// <summary>
    /// 根据扩展名给出建议说明（仅提示，不表示可自动删除）
    /// </summary>
    private static string GetSuggestion(string extension)
    {
        var ext = extension.ToLowerInvariant();

        return ext switch
        {
            ".zip" or ".rar" or ".7z" =>
                "压缩包，可能是历史下载文件，请用户确认是否需要保留",
            ".exe" or ".msi" =>
                "安装包，可能是已安装软件的安装文件，请用户确认是否需要保留",
            ".mp4" or ".mov" or ".avi" or ".mkv" =>
                "视频文件，占用较大，请用户确认是否需要保留",
            ".iso" =>
                "镜像文件，占用通常较大，请用户确认是否需要保留",
            _ => "大文件，请用户确认用途后再处理"
        };
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return true; // 无法获取属性，保守跳过
        }
    }
}
