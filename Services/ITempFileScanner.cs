using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 临时文件扫描服务接口
/// </summary>
public interface ITempFileScanner
{
    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);
}
