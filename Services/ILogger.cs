namespace CleanHelper.Services;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILogger
{
    Task LogAsync(string message);
    string LogDirectory { get; }
}
