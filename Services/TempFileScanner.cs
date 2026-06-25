using System.IO;
using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 临时文件扫描服务 —— 递归扫描 %TEMP%
/// 风险分级：优先按最后修改时间 + 路径安全判断，扩展名仅做辅助展示
/// </summary>
public class TempFileScanner : ITempFileScanner
{
    private readonly ILogger _logger;
    private readonly string _tempPath;

    public TempFileScanner(ILogger logger)
    {
        _logger = logger;
        _tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var result = new ScanResult();

            // 获取扫描前 C 盘可用空间
            try
            {
                var cDrive = new DriveInfo("C");
                result.AvailableSpaceBefore = cDrive.AvailableFreeSpace;
            }
            catch (Exception ex)
            {
                _logger.LogAsync($"获取C盘空间信息失败: {ex.Message}");
            }

            var files = new List<FileScanInfo>();

            EnumerateFilesRecursive(_tempPath, files, result, cancellationToken);

            result.Files = files;
            result.FileCount = files.Count;
            result.TotalSize = files.Sum(f => f.Size);

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// 递归遍历目录，收集所有文件信息。
    /// 遇到 ReparsePoint 目录或无权访问目录则跳过该目录。
    /// </summary>
    private void EnumerateFilesRecursive(
        string directory,
        List<FileScanInfo> files,
        ScanResult result,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // 枚举当前目录下的文件
        try
        {
            foreach (var filePath in Directory.EnumerateFiles(directory))
            {
                ct.ThrowIfCancellationRequested();
                var info = CreateFileScanInfo(filePath);
                if (info != null)
                    files.Add(info);
                else
                    result.SkippedCount++;
            }
        }
        catch (UnauthorizedAccessException)
        {
            result.SkippedCount++;
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }
        catch (Exception)
        {
            result.SkippedCount++;
            return;
        }

        // 递归枚举子目录：检查 ReparsePoint 再进入
        try
        {
            foreach (var subDir in Directory.EnumerateDirectories(directory))
            {
                ct.ThrowIfCancellationRequested();

                // ReparsePoint 目录（Junction/SymbolicLink）跳过
                if (IsReparsePoint(subDir))
                {
                    result.SkippedCount++;
                    continue;
                }

                EnumerateFilesRecursive(subDir, files, result, ct);
            }
        }
        catch (UnauthorizedAccessException)
        {
            result.SkippedCount++;
        }
        catch (DirectoryNotFoundException)
        {
            // 目录在扫描过程中被删除
        }
        catch (Exception)
        {
            result.SkippedCount++;
        }
    }

    /// <summary>
    /// 为单个文件创建 FileScanInfo，同时判定风险等级。
    /// 返回 null 表示该文件应跳过（ReparsePoint / 无权限 / 路径不安全）。
    /// </summary>
    private FileScanInfo? CreateFileScanInfo(string filePath)
    {
        try
        {
            // ---- 路径安全校验 ----
            if (!IsPathUnderTemp(filePath))
            {
                return null;
            }

            // ---- 检查 ReparsePoint ----
            if (IsReparsePoint(filePath))
            {
                return null;
            }

            var fi = new FileInfo(filePath);
            var info = new FileScanInfo
            {
                FilePath = filePath,
                Size = fi.Length,
                LastWriteTime = fi.LastWriteTime
            };

            // ---- 风险分级：按最后修改时间 ----
            var age = DateTime.Now - fi.LastWriteTime;

            if (age.TotalDays > 7)
            {
                // 7 天前修改 → 低风险，默认勾选
                info.RiskLevel = "低风险";
                info.RiskReason = $"最后修改于 {fi.LastWriteTime:yyyy-MM-dd HH:mm}（超过7天）";
                info.IsChecked = true;
                info.CanClean = true;
            }
            else
            {
                // 1～7 天内 或 24 小时内 → 中风险，默认不勾选
                info.RiskLevel = "中风险";
                if (age.TotalDays >= 1)
                    info.RiskReason = $"最后修改于 {fi.LastWriteTime:yyyy-MM-dd HH:mm}（{age.Days}天内）";
                else
                    info.RiskReason = $"最后修改于 {fi.LastWriteTime:yyyy-MM-dd HH:mm}（24小时内）";
                info.IsChecked = false;
                info.CanClean = true;
            }

            return info;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 检查路径是否仍然位于当前用户 %TEMP% 目录下
    /// </summary>
    private bool IsPathUnderTemp(string filePath)
    {
        try
        {
            var normalized = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return normalized.StartsWith(_tempPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                   || normalized.Equals(_tempPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查文件或目录是否具有 ReparsePoint 属性（Junction / SymbolicLink）
    /// </summary>
    private static bool IsReparsePoint(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            return attrs.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            // 无法获取属性，保守处理——视为 ReparsePoint 跳过
            return true;
        }
    }
}
