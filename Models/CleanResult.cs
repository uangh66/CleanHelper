namespace CleanHelper.Models;

/// <summary>
/// 清理结果
/// </summary>
public class CleanResult
{
    /// <summary>成功删除的文件数</summary>
    public int DeletedCount { get; set; }

    /// <summary>删除失败的文件数</summary>
    public int FailedCount { get; set; }

    /// <summary>成功删除文件的 Size 累加（字节）——即"成功处理大小"</summary>
    public long SuccessfullyProcessedSize { get; set; }

    /// <summary>真实释放空间（字节）= 清理后 C 盘可用空间 - 清理前 C 盘可用空间</summary>
    public long ActualSpaceFreed { get; set; }

    /// <summary>清理后 C 盘可用空间（字节）</summary>
    public long AvailableSpaceAfter { get; set; }

    /// <summary>删除失败的文件路径及原因</summary>
    public List<string> FailedFiles { get; set; } = new();

    /// <summary>清理前 C 盘可用空间（字节）——由调用方在清理前设置</summary>
    public long AvailableSpaceBefore { get; set; }
}
