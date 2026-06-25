using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 大文件扫描服务接口 —— 仅扫描，不删除
/// </summary>
public interface ILargeFileScanner
{
    /// <summary>
    /// 扫描用户目录中的大文件
    /// </summary>
    /// <param name="minSizeBytes">最小文件大小（字节）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>大文件列表，按大小降序排列</returns>
    Task<List<LargeFileInfo>> ScanAsync(long minSizeBytes, CancellationToken cancellationToken = default);
}
