using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 临时文件清理服务接口
/// </summary>
public interface ITempFileCleaner
{
    /// <summary>
    /// 清理指定的文件列表。
    /// 内部会再次做路径安全校验，只删除 IsChecked=true 且 CanClean=true 且路径安全的文件。
    /// </summary>
    /// <param name="filesToClean">待清理的文件列表（通常来自扫描结果中用户勾选的文件）</param>
    /// <param name="availableSpaceBefore">清理前 C 盘可用空间（字节），用于计算真实释放空间</param>
    /// <param name="cancellationToken"></param>
    Task<CleanResult> CleanAsync(IEnumerable<FileScanInfo> filesToClean, long availableSpaceBefore, CancellationToken cancellationToken = default);
}
