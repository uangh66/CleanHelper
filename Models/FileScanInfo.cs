namespace CleanHelper.Models;

/// <summary>
/// 扫描到的单个文件信息
/// </summary>
public class FileScanInfo
{
    public string FilePath { get; set; } = string.Empty;
    public long Size { get; set; }

    /// <summary>文件最后修改时间</summary>
    public DateTime LastWriteTime { get; set; }

    /// <summary>用户是否勾选要清理</summary>
    public bool IsChecked { get; set; }

    /// <summary>是否可以清理（ReparsePoint/无权限/路径不安全 = false）</summary>
    public bool CanClean { get; set; } = true;

    /// <summary>风险等级：低风险 / 中风险 / 无法清理</summary>
    public string RiskLevel { get; set; } = "低风险";

    /// <summary>风险判定原因（如"7天前修改""24小时内修改""ReparsePoint""无权限访问"）</summary>
    public string RiskReason { get; set; } = string.Empty;

    /// <summary>修改时间显示文本（用于 DataGrid 列绑定）</summary>
    public string LastWriteTimeDisplay => LastWriteTime.ToString("yyyy-MM-dd HH:mm");

    /// <summary>大小显示文本（用于 DataGrid 列绑定）</summary>
    public string SizeDisplay => Helpers.FileSizeFormatter.Format(Size);
}
