using System.IO;

namespace CleanHelper.Services;

/// <summary>
/// 日志服务 —— 将扫描和清理日志写入程序目录下的 logs 文件夹
/// </summary>
public class Logger : ILogger
{
    private readonly string _logDirectory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public string LogDirectory => _logDirectory;

    public Logger()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        try
        {
            Directory.CreateDirectory(_logDirectory);
        }
        catch
        {
            // 无法创建日志目录时静默失败
        }
    }

    public async Task LogAsync(string message)
    {
        try
        {
            await _semaphore.WaitAsync();
            var logFile = Path.Combine(_logDirectory, $"clean_{DateTime.Now:yyyyMMdd}.log");
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            await File.AppendAllTextAsync(logFile, logLine + Environment.NewLine);
        }
        catch
        {
            // 写日志失败时静默处理
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
