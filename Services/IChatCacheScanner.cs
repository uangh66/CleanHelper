using CleanHelper.Models;

namespace CleanHelper.Services;

/// <summary>
/// 聊天软件缓存扫描服务接口 —— 仅统计目录大小和文件数量，不读取内容
/// </summary>
public interface IChatCacheScanner
{
    /// <summary>
    /// 扫描微信/QQ/企业微信/钉钉的缓存目录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>每个候选目录的统计信息</returns>
    Task<List<ChatCacheInfo>> ScanAsync(CancellationToken cancellationToken = default);
}
