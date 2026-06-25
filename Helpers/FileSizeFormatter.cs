namespace CleanHelper.Helpers;

/// <summary>
/// 文件大小格式化工具，将字节数转为人类可读字符串（B/KB/MB/GB/TB）
/// </summary>
public static class FileSizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    /// <summary>
    /// 将字节数格式化为可读字符串。
    /// 小于 0 返回 "0 B"。
    /// </summary>
    public static string Format(long bytes)
    {
        if (bytes < 0) return "0 B";

        int unitIndex = 0;
        double size = bytes;

        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{size:F0} {Units[unitIndex]}"
            : $"{size:F2} {Units[unitIndex]}";
    }
}
