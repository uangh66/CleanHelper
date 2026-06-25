using System.IO;
using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 聊天软件缓存扫描服务 ——
/// 扫描微信/QQ/企业微信/钉钉的已知目录，递归统计文件数量和总大小。
/// 仅读取元数据，不读取聊天内容。
/// </summary>
public class ChatCacheScanner : IChatCacheScanner
{
    private static readonly List<ScanTarget> Targets;

    private record ScanTarget(string SoftwareName, string DirectoryType, string Path);

    static ChatCacheScanner()
    {
        Targets = BuildTargets();
    }

    private static List<ScanTarget> BuildTargets()
    {
        var list = new List<ScanTarget>();

        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 微信
            list.Add(new("微信", "用户文件目录", System.IO.Path.Combine(userProfile, "Documents", "WeChat Files")));
            list.Add(new("微信", "AppData缓存目录", System.IO.Path.Combine(appData, "Tencent", "WeChat")));
            list.Add(new("微信", "AppData缓存目录", System.IO.Path.Combine(localAppData, "Tencent", "WeChat")));

            // QQ
            list.Add(new("QQ", "用户文件目录", System.IO.Path.Combine(userProfile, "Documents", "Tencent Files")));
            list.Add(new("QQ", "AppData缓存目录", System.IO.Path.Combine(appData, "Tencent", "QQ")));
            list.Add(new("QQ", "AppData缓存目录", System.IO.Path.Combine(localAppData, "Tencent", "QQ")));

            // 企业微信
            list.Add(new("企业微信", "用户文件目录", System.IO.Path.Combine(userProfile, "Documents", "WXWork")));
            list.Add(new("企业微信", "AppData缓存目录", System.IO.Path.Combine(appData, "Tencent", "WXWork")));
            list.Add(new("企业微信", "AppData缓存目录", System.IO.Path.Combine(localAppData, "Tencent", "WXWork")));

            // 钉钉
            list.Add(new("钉钉", "AppData缓存目录", System.IO.Path.Combine(appData, "DingTalk")));
            list.Add(new("钉钉", "AppData缓存目录", System.IO.Path.Combine(localAppData, "DingTalk")));
            list.Add(new("钉钉", "用户文件目录", System.IO.Path.Combine(userProfile, "Documents", "DingTalk")));
        }
        catch
        {
            // 获取环境目录失败时保持空列表
        }

        return list;
    }

    public async Task<List<ChatCacheInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var results = new List<ChatCacheInfo>();

            foreach (var target in Targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(ScanDirectory(target, cancellationToken));
            }

            return results;
        }, cancellationToken);
    }

    /// <summary>
    /// 扫描单个目录，统计文件数量和总大小。
    /// 不读取文件内容。
    /// </summary>
    private static ChatCacheInfo ScanDirectory(ScanTarget target, CancellationToken ct)
    {
        var info = new ChatCacheInfo
        {
            SoftwareName = target.SoftwareName,
            DirectoryType = target.DirectoryType,
            FullPath = target.Path
        };

        if (!Directory.Exists(target.Path))
        {
            info.Exists = false;
            info.Suggestion = "未发现该目录，可能未安装或未登录该软件";
            return info;
        }

        info.Exists = true;

        try
        {
            var dirInfo = new DirectoryInfo(target.Path);
            info.LastWriteTime = dirInfo.LastWriteTime;

            var fileCount = 0;
            long totalSize = 0;

            EnumerateRecursive(dirInfo, ref fileCount, ref totalSize, ct);

            info.FileCount = fileCount;
            info.TotalSize = totalSize;

            // 按目录大小生成建议
            string sizeSuggestion;
            if (totalSize < 1L * 1024 * 1024 * 1024) // < 1 GB
                sizeSuggestion = "占用较小，通常无需处理";
            else if (totalSize < 5L * 1024 * 1024 * 1024) // 1-5 GB
                sizeSuggestion = "占用中等，可与用户确认是否需要整理";
            else
                sizeSuggestion = "占用较大，建议用户确认是否备份重要文件后再整理";

            // 按目录类型追加说明
            string typeNote = target.DirectoryType switch
            {
                "用户文件目录" when target.SoftwareName is "微信" or "QQ" =>
                    "可能包含聊天图片、视频、文件和接收文件，不能自动删除",
                "AppData缓存目录" =>
                    "可能包含软件缓存和运行数据，建议关闭软件后再由用户确认处理",
                _ => ""
            };

            info.Suggestion = string.IsNullOrEmpty(typeNote)
                ? sizeSuggestion
                : $"{sizeSuggestion}；{typeNote}";
        }
        catch (UnauthorizedAccessException)
        {
            info.Exists = false;
            info.Suggestion = "无权限访问该目录";
        }
        catch (Exception ex)
        {
            info.Exists = false;
            info.Suggestion = $"扫描异常: {ex.Message}";
        }

        return info;
    }

    /// <summary>
    /// 递归枚举目录中的文件，累加数量和大小。
    /// 跳过 ReparsePoint、无权限子目录。
    /// 不读取文件内容。
    /// </summary>
    private static void EnumerateRecursive(
        DirectoryInfo dir,
        ref int fileCount,
        ref long totalSize,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // 枚举文件
        try
        {
            foreach (var file in dir.EnumerateFiles())
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    // 跳过 ReparsePoint
                    if (IsReparsePoint(file.FullName))
                        continue;

                    fileCount++;
                    totalSize += file.Length;
                }
                catch
                {
                    // 单文件异常，跳过
                }
            }
        }
        catch (UnauthorizedAccessException) { return; }
        catch (DirectoryNotFoundException) { return; }
        catch (IOException) { return; }

        // 枚举子目录
        try
        {
            foreach (var subDir in dir.EnumerateDirectories())
            {
                ct.ThrowIfCancellationRequested();

                // 跳过 ReparsePoint（Junction / SymbolicLink）
                if (IsReparsePoint(subDir.FullName))
                    continue;

                EnumerateRecursive(subDir, ref fileCount, ref totalSize, ct);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return true; // 保守跳过
        }
    }
}
