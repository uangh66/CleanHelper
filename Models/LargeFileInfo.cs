namespace CleanHelper.Models;

/// <summary>
/// 大文件信息（空间体检用，仅扫描不删除）
/// </summary>
public class LargeFileInfo
{
    /// <summary>文件名（不含路径）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>完整路径</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>文件大小（字节）</summary>
    public long Size { get; set; }

    /// <summary>最后修改时间</summary>
    public DateTime LastWriteTime { get; set; }

    /// <summary>扩展名（含点，小写，如 ".zip"）</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>所属目录分类：下载 / 桌面 / 文档 / 图片 / 视频 / 音乐</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>建议说明</summary>
    public string Suggestion { get; set; } = string.Empty;

    /// <summary>修改时间显示文本</summary>
    public string LastWriteTimeDisplay => LastWriteTime.ToString("yyyy-MM-dd HH:mm");

    /// <summary>大小显示文本</summary>
    public string SizeDisplay => Helpers.FileSizeFormatter.Format(Size);
}
