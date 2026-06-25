namespace CleanHelper.Models;

/// <summary>
/// 扫描结果
/// </summary>
public class ScanResult
{
    /// <summary>扫描到的文件列表</summary>
    public List<FileScanInfo> Files { get; set; } = new();

    /// <summary>文件总数</summary>
    public int FileCount { get; set; }

    /// <summary>文件总大小（字节）</summary>
    public long TotalSize { get; set; }

    /// <summary>跳过的文件数（无权限/被占用）</summary>
    public int SkippedCount { get; set; }

    /// <summary>扫描前 C 盘可用空间（字节）</summary>
    public long AvailableSpaceBefore { get; set; }
}
