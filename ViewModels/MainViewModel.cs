using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CleanHelper.Models;
using CleanHelper.Services;

namespace CleanHelper.ViewModels;

/// <summary>
/// 主窗口 ViewModel —— 临时文件清理 + 空间体检 + 聊天缓存分析
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    public const string AppVersion = "v1.0.0";

    private readonly ITempFileScanner _scanner;
    private readonly ITempFileCleaner _cleaner;
    private readonly ILargeFileScanner _largeFileScanner;
    private readonly IChatCacheScanner _chatCacheScanner;
    private readonly ILogger _logger;

    private ScanResult? _scanResult;
    private CleanResult? _cleanResult;

    // ---- 临时文件清理：文件列表 ----

    public ObservableCollection<FileScanInfo> FileList { get; } = new();

    private string _selectedCountText = "-";
    public string SelectedCountText
    {
        get => _selectedCountText;
        set { _selectedCountText = value; OnPropertyChanged(); }
    }

    // ---- 临时文件清理：绑定属性 ----

    private string _availableSpace = "计算中...";
    public string AvailableSpace
    {
        get => _availableSpace;
        set { _availableSpace = value; OnPropertyChanged(); }
    }

    private string _scanStatus = "就绪";
    public string ScanStatus
    {
        get => _scanStatus;
        set { _scanStatus = value; OnPropertyChanged(); }
    }

    private string _fileCountText = "-";
    public string FileCountText
    {
        get => _fileCountText;
        set { _fileCountText = value; OnPropertyChanged(); }
    }

    private string _estimatedSizeText = "-";
    public string EstimatedSizeText
    {
        get => _estimatedSizeText;
        set { _estimatedSizeText = value; OnPropertyChanged(); }
    }

    private string _skippedCountText = "-";
    public string SkippedCountText
    {
        get => _skippedCountText;
        set { _skippedCountText = value; OnPropertyChanged(); }
    }

    private string _deletedCountText = "-";
    public string DeletedCountText
    {
        get => _deletedCountText;
        set { _deletedCountText = value; OnPropertyChanged(); }
    }

    private string _failedCountText = "-";
    public string FailedCountText
    {
        get => _failedCountText;
        set { _failedCountText = value; OnPropertyChanged(); }
    }

    private string _successSizeText = "-";
    public string SuccessSizeText
    {
        get => _successSizeText;
        set { _successSizeText = value; OnPropertyChanged(); }
    }

    private string _actualSpaceFreedText = "-";
    public string ActualSpaceFreedText
    {
        get => _actualSpaceFreedText;
        set { _actualSpaceFreedText = value; OnPropertyChanged(); }
    }

    private string _availableSpaceAfter = "-";
    public string AvailableSpaceAfter
    {
        get => _availableSpaceAfter;
        set { _availableSpaceAfter = value; OnPropertyChanged(); }
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        set { _isScanning = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private bool _isCleaning;
    public bool IsCleaning
    {
        get => _isCleaning;
        set { _isCleaning = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private bool _hasScanResult;
    public bool HasScanResult
    {
        get => _hasScanResult;
        set { _hasScanResult = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private bool _hasCleanResult;
    public bool HasCleanResult
    {
        get => _hasCleanResult;
        set { _hasCleanResult = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private string _statusBarText = "就绪";
    public string StatusBarText
    {
        get => _statusBarText;
        set { _statusBarText = value; OnPropertyChanged(); }
    }

    // ---- 临时文件清理：按钮启用状态 ----

    private bool Busy => IsScanning || IsCleaning || IsLargeFileScanning || IsChatCacheScanning;
    private bool CanScan => !Busy;
    private bool CanClean => HasScanResult && FileList.Any(f => f.IsChecked && f.CanClean) && !Busy;
    private bool CanExport => HasCleanResult;
    private bool HasFileList => HasScanResult && FileList.Count > 0;

    // ---- 临时文件清理：命令 ----

    public ICommand ScanCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand SelectLowRiskCommand { get; }
    public ICommand CleanCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ViewLogCommand { get; }

    // ============================================================
    //  空间体检：大文件分析
    // ============================================================

    private CancellationTokenSource? _largeFileCts;

    public ObservableCollection<LargeFileInfo> LargeFiles { get; } = new();

    public List<int> MinSizeOptions { get; } = new() { 50, 100, 500, 1000 };

    private int _selectedMinSizeMb = 100;
    public int SelectedMinSizeMb
    {
        get => _selectedMinSizeMb;
        set { _selectedMinSizeMb = value; OnPropertyChanged(); }
    }

    private long MinSizeBytes => _selectedMinSizeMb * 1024L * 1024L;

    private string _minSizeDisplay = "100 MB";
    public string MinSizeDisplay
    {
        get => _minSizeDisplay;
        set { _minSizeDisplay = value; OnPropertyChanged(); }
    }

    private bool _isLargeFileScanning;
    public bool IsLargeFileScanning
    {
        get => _isLargeFileScanning;
        set { _isLargeFileScanning = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private string _largeFileStatusText = "就绪";
    public string LargeFileStatusText
    {
        get => _largeFileStatusText;
        set { _largeFileStatusText = value; OnPropertyChanged(); }
    }

    private string _largeFileTotalCountText = "-";
    public string LargeFileTotalCountText
    {
        get => _largeFileTotalCountText;
        set { _largeFileTotalCountText = value; OnPropertyChanged(); }
    }

    private string _largeFileTotalSizeText = "-";
    public string LargeFileTotalSizeText
    {
        get => _largeFileTotalSizeText;
        set { _largeFileTotalSizeText = value; OnPropertyChanged(); }
    }

    private string _largeFileCategorySummary = "-";
    public string LargeFileCategorySummary
    {
        get => _largeFileCategorySummary;
        set { _largeFileCategorySummary = value; OnPropertyChanged(); }
    }

    private bool _hasLargeFileResults;
    public bool HasLargeFileResults
    {
        get => _hasLargeFileResults;
        set { _hasLargeFileResults = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private bool CanScanLargeFiles => !Busy;
    private bool CanCancelLargeFileScan => IsLargeFileScanning;
    private bool CanOpenLargeFileLocation => SelectedLargeFile != null;
    private bool CanExportSpaceReport => HasLargeFileResults && LargeFiles.Count > 0;

    private LargeFileInfo? _selectedLargeFile;
    public LargeFileInfo? SelectedLargeFile
    {
        get => _selectedLargeFile;
        set { _selectedLargeFile = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    public ICommand ScanLargeFilesCommand { get; }
    public ICommand CancelLargeFileScanCommand { get; }
    public ICommand OpenLargeFileLocationCommand { get; }
    public ICommand ExportSpaceReportCommand { get; }

    // ============================================================
    //  聊天缓存分析
    // ============================================================

    private CancellationTokenSource? _chatCacheCts;

    public ObservableCollection<ChatCacheInfo> ChatCaches { get; } = new();

    private bool _isChatCacheScanning;
    public bool IsChatCacheScanning
    {
        get => _isChatCacheScanning;
        set { _isChatCacheScanning = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private string _chatCacheStatusText = "就绪";
    public string ChatCacheStatusText
    {
        get => _chatCacheStatusText;
        set { _chatCacheStatusText = value; OnPropertyChanged(); }
    }

    private string _chatCacheDirCountText = "-";
    public string ChatCacheDirCountText
    {
        get => _chatCacheDirCountText;
        set { _chatCacheDirCountText = value; OnPropertyChanged(); }
    }

    private string _chatCacheTotalSizeText = "-";
    public string ChatCacheTotalSizeText
    {
        get => _chatCacheTotalSizeText;
        set { _chatCacheTotalSizeText = value; OnPropertyChanged(); }
    }

    private bool _hasChatCacheResults;
    public bool HasChatCacheResults
    {
        get => _hasChatCacheResults;
        set { _hasChatCacheResults = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private ChatCacheInfo? _selectedChatCache;
    public ChatCacheInfo? SelectedChatCache
    {
        get => _selectedChatCache;
        set { _selectedChatCache = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private bool CanScanChatCaches => !Busy;
    private bool CanCancelChatCacheScan => IsChatCacheScanning;
    private bool CanOpenChatCacheLocation => _selectedChatCache != null && _selectedChatCache.Exists;
    private bool CanExportChatCacheReport => _hasChatCacheResults;

    public ICommand ScanChatCachesCommand { get; }
    public ICommand CancelChatCacheScanCommand { get; }
    public ICommand OpenChatCacheLocationCommand { get; }
    public ICommand ExportChatCacheReportCommand { get; }

    // ---- 工具按钮 ----

    public ICommand AboutCommand { get; }
    public ICommand OpenReportDirCommand { get; }

    // ============================================================
    //  构造函数
    // ============================================================

    public MainViewModel()
    {
        _logger = new Logger();
        _scanner = new TempFileScanner(_logger);
        _cleaner = new TempFileCleaner(_logger);
        _largeFileScanner = new LargeFileScanner();
        _chatCacheScanner = new ChatCacheScanner();

        // 临时文件清理
        ScanCommand = new RelayCommand(async () => await ScanAsync(), () => CanScan);
        SelectAllCommand = new RelayCommand(SelectAll, () => HasFileList);
        DeselectAllCommand = new RelayCommand(DeselectAll, () => HasFileList);
        SelectLowRiskCommand = new RelayCommand(SelectLowRisk, () => HasFileList);
        CleanCommand = new RelayCommand(async () => await CleanAsync(), () => CanClean);
        ExportReportCommand = new RelayCommand(async () => await ExportReportAsync(), () => CanExport);
        ViewLogCommand = new RelayCommand(ViewLog, () => true);

        // 空间体检
        ScanLargeFilesCommand = new RelayCommand(async () => await ScanLargeFilesAsync(), () => CanScanLargeFiles);
        CancelLargeFileScanCommand = new RelayCommand(CancelLargeFileScan, () => CanCancelLargeFileScan);
        OpenLargeFileLocationCommand = new RelayCommand(OpenLargeFileLocation, () => CanOpenLargeFileLocation);
        ExportSpaceReportCommand = new RelayCommand(async () => await ExportSpaceReportAsync(), () => CanExportSpaceReport);

        // 聊天缓存分析
        ScanChatCachesCommand = new RelayCommand(async () => await ScanChatCachesAsync(), () => CanScanChatCaches);
        CancelChatCacheScanCommand = new RelayCommand(CancelChatCacheScan, () => CanCancelChatCacheScan);
        OpenChatCacheLocationCommand = new RelayCommand(OpenChatCacheLocation, () => CanOpenChatCacheLocation);
        ExportChatCacheReportCommand = new RelayCommand(async () => await ExportChatCacheReportAsync(), () => CanExportChatCacheReport);

        // 工具按钮
        AboutCommand = new RelayCommand(About, () => true);
        OpenReportDirCommand = new RelayCommand(OpenReportDir, () => true);

        _ = RefreshAvailableSpaceAsync();
    }

    // ---- C 盘空间 ----

    private async Task RefreshAvailableSpaceAsync()
    {
        try
        {
            var freeSpace = await Task.Run(() => new DriveInfo("C").AvailableFreeSpace);
            AvailableSpace = FormatSize(freeSpace);
        }
        catch
        {
            AvailableSpace = "无法获取";
        }
    }

    // ============================================================
    //  临时文件清理
    // ============================================================

    private async Task ScanAsync()
    {
        IsScanning = true;
        HasScanResult = false;
        HasCleanResult = false;
        ScanStatus = "正在扫描...";
        StatusBarText = "正在扫描临时文件，请稍候...";

        FileList.Clear();
        FileCountText = "-";
        EstimatedSizeText = "-";
        SkippedCountText = "-";
        SelectedCountText = "-";
        DeletedCountText = "-";
        FailedCountText = "-";
        SuccessSizeText = "-";
        ActualSpaceFreedText = "-";
        AvailableSpaceAfter = "-";

        await _logger.LogAsync("========== 开始扫描 ==========");
        await _logger.LogAsync($"扫描目录: {Path.GetTempPath()}");

        try
        {
            _scanResult = await _scanner.ScanAsync();

            foreach (var file in _scanResult.Files)
                FileList.Add(file);

            FileCountText = $"{_scanResult.FileCount} 个";
            EstimatedSizeText = FormatSize(_scanResult.TotalSize);
            SkippedCountText = $"{_scanResult.SkippedCount} 个";
            UpdateSelectedCount();

            await _logger.LogAsync($"扫描完成 - 文件数量: {_scanResult.FileCount}, " +
                $"预计大小: {FormatSize(_scanResult.TotalSize)}, 跳过: {_scanResult.SkippedCount}");

            if (_scanResult.FileCount == 0)
            {
                ScanStatus = "扫描完成 - 没有发现可清理的临时文件";
                StatusBarText = "扫描完成 - 没有发现可清理的临时文件";
                HasScanResult = false;
            }
            else
            {
                ScanStatus = "扫描完成";
                StatusBarText = $"扫描完成 - 发现 {_scanResult.FileCount} 个文件，" +
                    $"已勾选 {FileList.Count(f => f.IsChecked)} 个低风险文件";
                HasScanResult = true;
            }
        }
        catch (OperationCanceledException)
        {
            ScanStatus = "扫描已取消";
            StatusBarText = "扫描已取消";
        }
        catch (Exception ex)
        {
            ScanStatus = "扫描失败";
            StatusBarText = $"扫描出错: {ex.Message}";
            await _logger.LogAsync($"扫描异常: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void SelectAll()
    {
        foreach (var f in FileList)
        {
            if (f.CanClean) f.IsChecked = true;
        }
        UpdateSelectedCount();
        StatusBarText = "已全选可清理文件";
        CommandManager.InvalidateRequerySuggested();
    }

    private void DeselectAll()
    {
        foreach (var f in FileList)
            f.IsChecked = false;
        UpdateSelectedCount();
        StatusBarText = "已取消全选";
        CommandManager.InvalidateRequerySuggested();
    }

    private void SelectLowRisk()
    {
        foreach (var f in FileList)
            f.IsChecked = f.RiskLevel == "低风险" && f.CanClean;
        UpdateSelectedCount();
        StatusBarText = "已仅勾选低风险文件";
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateSelectedCount()
    {
        var checkedCount = FileList.Count(f => f.IsChecked);
        SelectedCountText = $"{checkedCount} / {FileList.Count} 个";
    }

    private async Task CleanAsync()
    {
        if (_scanResult == null || !FileList.Any(f => f.IsChecked && f.CanClean))
            return;

        var filesToClean = FileList.Where(f => f.IsChecked && f.CanClean).ToList();
        var estimatedSize = filesToClean.Sum(f => f.Size);

        var confirmResult = MessageBox.Show(
            $"确认要删除选中的 {filesToClean.Count} 个文件吗？\n\n" +
            $"预计处理大小: {FormatSize(estimatedSize)}\n" +
            $"扫描目录: {Path.GetTempPath()}\n\n" +
            "此操作不可撤销，是否继续？",
            "确认清理",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.Yes) return;

        IsCleaning = true;
        HasCleanResult = false;
        StatusBarText = "正在清理选中文件，请稍候...";

        await _logger.LogAsync("========== 开始清理 ==========");
        await _logger.LogAsync($"待清理文件数: {filesToClean.Count}, 预计大小: {FormatSize(estimatedSize)}");

        try
        {
            _cleanResult = await _cleaner.CleanAsync(filesToClean, _scanResult.AvailableSpaceBefore);

            DeletedCountText = $"{_cleanResult.DeletedCount} 个";
            FailedCountText = $"{_cleanResult.FailedCount} 个";
            SuccessSizeText = FormatSize(_cleanResult.SuccessfullyProcessedSize);
            ActualSpaceFreedText = FormatSize(_cleanResult.ActualSpaceFreed);
            AvailableSpaceAfter = FormatSize(_cleanResult.AvailableSpaceAfter);
            HasCleanResult = true;

            await _logger.LogAsync($"清理完成 - 成功: {_cleanResult.DeletedCount}, 失败: {_cleanResult.FailedCount}, " +
                $"成功处理大小: {FormatSize(_cleanResult.SuccessfullyProcessedSize)}, " +
                $"真实释放空间: {FormatSize(_cleanResult.ActualSpaceFreed)}");

            if (_cleanResult.FailedCount > 0)
            {
                foreach (var failed in _cleanResult.FailedFiles)
                    await _logger.LogAsync($"  失败: {failed}");
            }

            StatusBarText = $"清理完成 - 成功删除 {_cleanResult.DeletedCount} 个文件，" +
                $"真实释放 {FormatSize(_cleanResult.ActualSpaceFreed)}";
        }
        catch (Exception ex)
        {
            StatusBarText = $"清理出错: {ex.Message}";
            await _logger.LogAsync($"清理异常: {ex.Message}");
        }
        finally
        {
            IsCleaning = false;
            await RefreshAvailableSpaceAsync();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task ExportReportAsync()
    {
        if (_scanResult == null || _cleanResult == null || _cleanResult.DeletedCount + _cleanResult.FailedCount == 0)
            return;

        try
        {
            var logDir = _logger.LogDirectory;
            Directory.CreateDirectory(logDir);
            var reportFile = Path.Combine(logDir, $"CleanReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            await Task.Run(() =>
            {
                using var sw = new StreamWriter(reportFile, false, System.Text.Encoding.UTF8);

                sw.WriteLine("============================================================");
                sw.WriteLine("  临时文件清理报告");
                sw.WriteLine("============================================================");
                sw.WriteLine("【隐私提醒】本报告包含本机文件路径信息，请勿公开传播。");
                sw.WriteLine();
                sw.WriteLine($"软件名称: CleanHelper / 临时文件清理助手");
                sw.WriteLine($"版本:     {AppVersion}");
                sw.WriteLine($"报告类型: 临时文件清理报告");
                sw.WriteLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"计算机名: {Environment.MachineName}");
                sw.WriteLine($"用户名:   {Environment.UserName}");
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  清理前");
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine($"C盘可用空间: {FormatSize(_scanResult.AvailableSpaceBefore)}");
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  清理详情");
                sw.WriteLine("------------------------------------------------------------");

                foreach (var file in FileList.Where(f => f.IsChecked && f.CanClean))
                {
                    var failed = _cleanResult.FailedFiles.FirstOrDefault(x => x.StartsWith(file.FilePath));
                    if (failed != null)
                    {
                        sw.WriteLine($"[失败] {file.FilePath}  ({FormatSize(file.Size)})");
                        sw.WriteLine($"       原因: {failed[(file.FilePath.Length + 3)..]}");
                    }
                    else
                    {
                        sw.WriteLine($"[成功] {file.FilePath}  ({FormatSize(file.Size)})");
                    }
                }
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  清理汇总");
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine($"成功删除:     {_cleanResult.DeletedCount} 个");
                sw.WriteLine($"删除失败:     {_cleanResult.FailedCount} 个");
                sw.WriteLine($"成功处理大小: {FormatSize(_cleanResult.SuccessfullyProcessedSize)}");
                sw.WriteLine($"真实释放空间: {FormatSize(_cleanResult.ActualSpaceFreed)}  (C盘清理后可用 - 清理前可用)");
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  清理后");
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine($"C盘可用空间: {FormatSize(_cleanResult.AvailableSpaceAfter)}");
                sw.WriteLine();
                sw.WriteLine("============================================================");
                sw.WriteLine("  报告结束");
                sw.WriteLine("============================================================");
            });

            Process.Start("notepad.exe", reportFile);
            StatusBarText = $"报告已生成: {reportFile}";
            await _logger.LogAsync($"报告已导出: {reportFile}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出报告失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewLog()
    {
        try
        {
            var logDir = _logger.LogDirectory;
            if (Directory.Exists(logDir))
            {
                Process.Start("explorer.exe", logDir);
            }
            else
            {
                MessageBox.Show("日志目录尚不存在，请先执行扫描操作。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开日志目录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============================================================
    //  空间体检：扫描大文件
    // ============================================================

    private async Task ScanLargeFilesAsync()
    {
        IsLargeFileScanning = true;
        HasLargeFileResults = false;
        LargeFileStatusText = "正在扫描...";
        StatusBarText = "正在扫描用户目录中的大文件，请稍候...";

        LargeFiles.Clear();
        LargeFileTotalCountText = "-";
        LargeFileTotalSizeText = "-";
        LargeFileCategorySummary = "-";
        SelectedLargeFile = null;

        _largeFileCts = new CancellationTokenSource();

        try
        {
            var files = await _largeFileScanner.ScanAsync(MinSizeBytes, _largeFileCts.Token);

            foreach (var f in files)
                LargeFiles.Add(f);

            LargeFileTotalCountText = $"{files.Count} 个";
            LargeFileTotalSizeText = FormatSize(files.Sum(f => f.Size));

            var catGroups = files.GroupBy(f => f.Category)
                .OrderByDescending(g => g.Sum(x => x.Size))
                .Select(g => $"{g.Key}: {g.Count()} 个, 共 {FormatSize(g.Sum(x => x.Size))}");
            LargeFileCategorySummary = string.Join(" | ", catGroups);

            HasLargeFileResults = true;
            LargeFileStatusText = $"扫描完成 - 发现 {files.Count} 个大文件";
            StatusBarText = $"空间体检完成 - 发现 {files.Count} 个大文件，共 {FormatSize(files.Sum(f => f.Size))}";
        }
        catch (OperationCanceledException)
        {
            LargeFileStatusText = "扫描已取消";
            StatusBarText = "大文件扫描已取消";
        }
        catch (Exception ex)
        {
            LargeFileStatusText = $"扫描失败: {ex.Message}";
            StatusBarText = $"大文件扫描出错: {ex.Message}";
        }
        finally
        {
            IsLargeFileScanning = false;
            _largeFileCts?.Dispose();
            _largeFileCts = null;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void CancelLargeFileScan()
    {
        _largeFileCts?.Cancel();
        LargeFileStatusText = "正在停止...";
        StatusBarText = "正在停止大文件扫描...";
    }

    private void OpenLargeFileLocation()
    {
        if (_selectedLargeFile == null) return;

        try
        {
            var path = _selectedLargeFile.FullPath;
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
                StatusBarText = $"已打开文件位置: {path}";
            }
            else
            {
                MessageBox.Show("文件已不存在。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开文件位置失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportSpaceReportAsync()
    {
        if (LargeFiles.Count == 0) return;

        try
        {
            var logDir = _logger.LogDirectory;
            Directory.CreateDirectory(logDir);
            var reportFile = Path.Combine(logDir, $"SpaceReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var files = LargeFiles.ToList();
            var totalSize = files.Sum(f => f.Size);

            await Task.Run(() =>
            {
                using var sw = new StreamWriter(reportFile, false, System.Text.Encoding.UTF8);

                sw.WriteLine("============================================================");
                sw.WriteLine("  C盘空间体检报告");
                sw.WriteLine("============================================================");
                sw.WriteLine("【隐私提醒】本报告包含本机文件名和路径信息，请勿公开传播。");
                sw.WriteLine();
                sw.WriteLine($"软件名称: CleanHelper / 临时文件清理助手");
                sw.WriteLine($"版本:     {AppVersion}");
                sw.WriteLine($"报告类型: C盘空间体检报告");
                sw.WriteLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"计算机名: {Environment.MachineName}");
                sw.WriteLine($"当前用户: {Environment.UserName}");
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  扫描设置");
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine($"扫描目录: 下载、桌面、文档、图片、视频、音乐");
                sw.WriteLine($"最小文件大小阈值: {_selectedMinSizeMb} MB");
                sw.WriteLine($"发现大文件数量: {files.Count} 个");
                sw.WriteLine($"大文件总大小:   {FormatSize(totalSize)}");
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  按目录汇总");
                sw.WriteLine("------------------------------------------------------------");
                var catGroups = files.GroupBy(f => f.Category)
                    .OrderByDescending(g => g.Sum(x => x.Size));
                foreach (var g in catGroups)
                {
                    sw.WriteLine($"  {g.Key}: {g.Count()} 个, 共 {FormatSize(g.Sum(x => x.Size))}");
                }
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  大文件列表（按大小降序）");
                sw.WriteLine("------------------------------------------------------------");
                foreach (var f in files)
                {
                    sw.WriteLine($"  文件名:   {f.FileName}");
                    sw.WriteLine($"  大小:     {f.SizeDisplay}");
                    sw.WriteLine($"  修改时间: {f.LastWriteTimeDisplay}");
                    sw.WriteLine($"  所属目录: {f.Category}");
                    sw.WriteLine($"  扩展名:   {f.Extension}");
                    sw.WriteLine($"  路径:     {f.FullPath}");
                    sw.WriteLine($"  建议:     {f.Suggestion}");
                    sw.WriteLine();
                }
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  安全说明");
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  本次扫描仅读取文件元数据（路径、大小、修改时间、扩展名）。");
                sw.WriteLine("  未读取任何文件内容。");
                sw.WriteLine("  未删除、移动、修改任何用户文件。");
                sw.WriteLine("  是否删除或转移文件需由用户本人确认。");
                sw.WriteLine();
                sw.WriteLine("============================================================");
                sw.WriteLine("  报告结束");
                sw.WriteLine("============================================================");
            });

            Process.Start("notepad.exe", reportFile);
            StatusBarText = $"空间体检报告已生成: {reportFile}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出报告失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============================================================
    //  聊天缓存分析
    // ============================================================

    private async Task ScanChatCachesAsync()
    {
        IsChatCacheScanning = true;
        HasChatCacheResults = false;
        ChatCacheStatusText = "正在扫描...";
        StatusBarText = "正在分析聊天软件缓存目录，请稍候...";

        ChatCaches.Clear();
        ChatCacheDirCountText = "-";
        ChatCacheTotalSizeText = "-";
        SelectedChatCache = null;

        _chatCacheCts = new CancellationTokenSource();

        try
        {
            var results = await _chatCacheScanner.ScanAsync(_chatCacheCts.Token);

            foreach (var r in results)
                ChatCaches.Add(r);

            var existing = results.Where(r => r.Exists).ToList();
            var totalSize = existing.Sum(r => r.TotalSize);

            ChatCacheDirCountText = $"{existing.Count} 个（共扫描 {results.Count} 个候选目录）";
            ChatCacheTotalSizeText = FormatSize(totalSize);

            HasChatCacheResults = true;
            ChatCacheStatusText = $"扫描完成 - 发现 {existing.Count} 个聊天缓存目录";
            StatusBarText = $"聊天缓存分析完成 - {existing.Count} 个目录，共 {FormatSize(totalSize)}";
        }
        catch (OperationCanceledException)
        {
            ChatCacheStatusText = "扫描已取消";
            StatusBarText = "聊天缓存扫描已取消";
        }
        catch (Exception ex)
        {
            ChatCacheStatusText = $"扫描失败: {ex.Message}";
            StatusBarText = $"聊天缓存扫描出错: {ex.Message}";
        }
        finally
        {
            IsChatCacheScanning = false;
            _chatCacheCts?.Dispose();
            _chatCacheCts = null;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void CancelChatCacheScan()
    {
        _chatCacheCts?.Cancel();
        ChatCacheStatusText = "正在停止...";
        StatusBarText = "正在停止聊天缓存扫描...";
    }

    private void OpenChatCacheLocation()
    {
        if (_selectedChatCache == null || !_selectedChatCache.Exists) return;

        try
        {
            var path = _selectedChatCache.FullPath;
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path);
                StatusBarText = $"已打开目录: {path}";
            }
            else
            {
                MessageBox.Show("目录已不存在。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开目录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportChatCacheReportAsync()
    {
        if (ChatCaches.Count == 0) return;

        try
        {
            var logDir = _logger.LogDirectory;
            Directory.CreateDirectory(logDir);
            var reportFile = Path.Combine(logDir, $"ChatCacheReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var caches = ChatCaches.ToList();
            var existingDirs = caches.Where(c => c.Exists).ToList();
            var totalSize = existingDirs.Sum(c => c.TotalSize);

            await Task.Run(() =>
            {
                using var sw = new StreamWriter(reportFile, false, System.Text.Encoding.UTF8);

                sw.WriteLine("============================================================");
                sw.WriteLine("  聊天软件缓存分析报告");
                sw.WriteLine("============================================================");
                sw.WriteLine("【隐私提醒】本报告包含本机软件目录路径和占用信息，请勿公开传播。");
                sw.WriteLine();
                sw.WriteLine($"软件名称: CleanHelper / 临时文件清理助手");
                sw.WriteLine($"版本:     {AppVersion}");
                sw.WriteLine($"报告类型: 聊天软件缓存分析报告");
                sw.WriteLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"计算机名: {Environment.MachineName}");
                sw.WriteLine($"当前用户: {Environment.UserName}");
                sw.WriteLine();
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  扫描设置");
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine($"扫描软件: 微信、QQ、企业微信、钉钉");
                sw.WriteLine($"候选目录数: {caches.Count} 个");
                sw.WriteLine($"发现目录数: {existingDirs.Count} 个");
                sw.WriteLine($"总占用大小: {FormatSize(totalSize)}");
                sw.WriteLine();

                // 按软件汇总
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  按软件汇总");
                sw.WriteLine("------------------------------------------------------------");
                foreach (var g in caches.GroupBy(c => c.SoftwareName))
                {
                    var exist = g.Where(c => c.Exists).ToList();
                    sw.WriteLine($"  {g.Key}: {exist.Count}/{g.Count()} 个目录存在, 共 {FormatSize(exist.Sum(c => c.TotalSize))}");
                }
                sw.WriteLine();

                // 每个目录详情
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  目录详情");
                sw.WriteLine("------------------------------------------------------------");
                foreach (var c in caches)
                {
                    sw.WriteLine($"  软件:       {c.SoftwareName}");
                    sw.WriteLine($"  目录类型:   {c.DirectoryType}");
                    sw.WriteLine($"  是否存在:   {c.ExistsDisplay}");
                    sw.WriteLine($"  路径:       {c.FullPath}");
                    if (c.Exists)
                    {
                        sw.WriteLine($"  文件数量:   {c.FileCount} 个");
                        sw.WriteLine($"  目录大小:   {c.SizeDisplay}");
                        sw.WriteLine($"  修改时间:   {c.LastWriteTimeDisplay}");
                    }
                    sw.WriteLine($"  建议:       {c.Suggestion}");
                    sw.WriteLine();
                }

                // 安全说明
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  安全说明");
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine("  本次仅统计目录大小和文件数量。");
                sw.WriteLine("  未读取聊天内容。");
                sw.WriteLine("  未打开图片、视频、文档。");
                sw.WriteLine("  未删除、移动、修改任何聊天文件。");
                sw.WriteLine("  是否清理或转移聊天文件需由用户本人确认。");
                sw.WriteLine();
                sw.WriteLine("============================================================");
                sw.WriteLine("  报告结束");
                sw.WriteLine("============================================================");
            });

            Process.Start("notepad.exe", reportFile);
            StatusBarText = $"聊天缓存报告已生成: {reportFile}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出报告失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ============================================================
    //  工具按钮
    // ============================================================

    private void About()
    {
        MessageBox.Show(
            "软件名称: CleanHelper / 临时文件清理助手\n" +
            $"版本: {AppVersion}\n\n" +
            "工具定位: 个人电脑垃圾清理工具\n" +
            "运行方式: 在本地电脑运行，无需联网\n\n" +
            "当前功能:\n" +
            "  1. 临时文件安全清理\n" +
            "  2. 用户目录大文件空间体检\n" +
            "  3. 微信/QQ/企业微信/钉钉缓存占用分析\n\n" +
            "安全承诺:\n" +
            "  * 不联网\n" +
            "  * 不上传文件\n" +
            "  * 不后台常驻\n" +
            "  * 不自启动\n" +
            "  * 不安装服务\n" +
            "  * 不清理注册表\n" +
            "  * 不清理系统关键目录\n" +
            "  * 聊天缓存分析不读取聊天内容\n" +
            "  * 空间体检不读取文件内容\n" +
            "  * 只有「临时文件清理」会删除文件，且只删除\n" +
            "    用户勾选的当前用户 %TEMP% 文件",
            "关于与安全说明",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenReportDir()
    {
        try
        {
            var logDir = _logger.LogDirectory;
            Directory.CreateDirectory(logDir);
            Process.Start("explorer.exe", logDir);
            StatusBarText = $"已打开报告目录: {logDir}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开报告目录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---- 格式化 ----

    private static string FormatSize(long bytes) => Helpers.FileSizeFormatter.Format(bytes);

    // ---- INotifyPropertyChanged ----

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
