using System.IO;
using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 临时文件清理服务 —— 选择性删除用户勾选的文件
/// 删除前会做路径安全二次校验，确保文件仍在当前用户 %TEMP% 目录下
/// </summary>
public class TempFileCleaner : ITempFileCleaner
{
    private readonly ILogger _logger;
    private readonly string _tempPath;

    public TempFileCleaner(ILogger logger)
    {
        _logger = logger;
        _tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public async Task<CleanResult> CleanAsync(
        IEnumerable<FileScanInfo> filesToClean,
        long availableSpaceBefore,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            var result = new CleanResult
            {
                AvailableSpaceBefore = availableSpaceBefore
            };

            foreach (var file in filesToClean)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 只清理用户勾选且可清理的文件
                if (!file.IsChecked || !file.CanClean)
                    continue;

                // ---- 删除前路径安全二次校验 ----
                if (!IsPathUnderTemp(file.FilePath))
                {
                    result.FailedCount++;
                    result.FailedFiles.Add($"{file.FilePath} - 路径安全校验失败（不在当前用户Temp目录下）");
                    continue;
                }

                try
                {
                    var fileInfo = new FileInfo(file.FilePath);

                    if (!fileInfo.Exists)
                    {
                        // 文件已不存在（可能已被其他程序删除），视为成功
                        result.DeletedCount++;
                        result.SuccessfullyProcessedSize += file.Size;
                        continue;
                    }

                    // 去除只读属性
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                    }

                    fileInfo.Delete();
                    result.DeletedCount++;
                    result.SuccessfullyProcessedSize += file.Size;
                }
                catch (IOException)
                {
                    result.FailedCount++;
                    result.FailedFiles.Add($"{file.FilePath} - 文件被占用");
                }
                catch (UnauthorizedAccessException)
                {
                    result.FailedCount++;
                    result.FailedFiles.Add($"{file.FilePath} - 权限不足");
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedFiles.Add($"{file.FilePath} - {ex.Message}");
                }
            }

            // 清理后重新获取 C 盘可用空间，计算真实释放空间
            try
            {
                var cDrive = new DriveInfo("C");
                result.AvailableSpaceAfter = cDrive.AvailableFreeSpace;
                result.ActualSpaceFreed = result.AvailableSpaceAfter - availableSpaceBefore;
                if (result.ActualSpaceFreed < 0) result.ActualSpaceFreed = 0;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"清理后获取C盘空间信息失败: {ex.Message}");
            }

            return result;
        }, cancellationToken);
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
}
