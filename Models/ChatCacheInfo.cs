namespace CleanHelper.Models;

/// <summary>
/// 聊天软件缓存目录信息（仅扫描不删除）
/// </summary>
public class ChatCacheInfo
{
    /// <summary>软件名称：微信 / QQ / 企业微信 / 钉钉</summary>
    public string SoftwareName { get; set; } = string.Empty;

    /// <summary>目录类型：用户文件目录 / AppData缓存目录</summary>
    public string DirectoryType { get; set; } = string.Empty;

    /// <summary>目录完整路径</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>目录是否存在</summary>
    public bool Exists { get; set; }

    /// <summary>目录总大小（字节），不存在时为 0</summary>
    public long TotalSize { get; set; }

    /// <summary>文件数量，不存在时为 0</summary>
    public int FileCount { get; set; }

    /// <summary>目录最后修改时间，不存在时为 null</summary>
    public DateTime? LastWriteTime { get; set; }

    /// <summary>建议说明</summary>
    public string Suggestion { get; set; } = string.Empty;

    // ---- 显示属性 ----

    public string ExistsDisplay => Exists ? "存在" : "未发现";

    public string LastWriteTimeDisplay => LastWriteTime?.ToString("yyyy-MM-dd HH:mm") ?? "-";

    public string FileCountDisplay => Exists ? $"{FileCount} 个" : "-";

    public string SizeDisplay => Exists && TotalSize >= 0
        ? Helpers.FileSizeFormatter.Format(TotalSize)
        : "-";
}
