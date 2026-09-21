using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using UnturnedDat.Data.Diagnostics;
using UnturnedDat.Data.Parsing;
using UnturnedDat.Data.Properties;
using UnturnedDat.Data.Utility;
using UnturnedDat.LanguageServer.Files;
using UnturnedDat.LanguageServer.Handlers;
using UnturnedDat.LanguageServer.Utility;

namespace UnturnedDat.LanguageServer.Diagnostics;

/// <summary>
/// Handles keeping track of active files and publishing diagnostics.
/// </summary>
internal class DiagnosticsManager : IDisposable
{
    private readonly OpenedFileTracker _fileTracker;
    private readonly UnturnedAssetFileSyncHandler _fileSync;
    private readonly LspWorkspaceEnvironment _workspaceEnvironment;
    private readonly ILanguageServerFacade _languageServer;

    private readonly ConcurrentQueue<DiagnosticsWorkItem> _workQueue;
    private readonly Lock _workerThreadLock = new Lock();
    private Thread? _diagnosticProcessorThread;
    private bool _isWorkerThreadRunning;
    private DateTime _lastInlineProcess = DateTime.MinValue;

    private readonly ConcurrentDictionary<string, FileDiagnostics> _diagnostics;

    private ConcurrentQueue<FileDiagnostics>? _startupQueue;

    internal IParsingServices Services;
    private readonly StartupWaitUtility _startupWait;
    internal IFileRelationalModelProvider RelationalModelProvider;

    public DiagnosticsManager(
        OpenedFileTracker fileTracker,
        UnturnedAssetFileSyncHandler fileSync,
        LspWorkspaceEnvironment workspaceEnvironment,
        IFileRelationalModelProvider relationalModelProvider,
        ILanguageServerFacade languageServer,
        IParsingServices parsingServices,
        StartupWaitUtility startupWait)
    {
        _workQueue = new ConcurrentQueue<DiagnosticsWorkItem>();
        _diagnostics = new ConcurrentDictionary<string, FileDiagnostics>(OSPathHelper.PathComparer);

        _fileTracker = fileTracker;
        _fileSync = fileSync;
        _workspaceEnvironment = workspaceEnvironment;
        _languageServer = languageServer;

        RelationalModelProvider = relationalModelProvider;
        Services = parsingServices;
        _startupWait = startupWait;

        _workspaceEnvironment.FileCreated += OnFileCreated;
        _workspaceEnvironment.FileDeleted += OnFileDeleted;
        _workspaceEnvironment.FileUpdated += OnFileUpdated;
        _workspaceEnvironment.FileRenamed += OnFileRenamed;
        _workspaceEnvironment.WorkspaceFolderAdded += OnWorkspaceFolderAdded;
        _workspaceEnvironment.WorkspaceFolderRemoved += OnWorkspaceFolderRemoved;
        _fileSync.FileAdded += OnFileOpened;
        _fileSync.FileRemoved += OnFileClosed;
        _fileSync.ContentUpdated += OnContentUpdated;

        foreach (WorkspaceFolderTracker folder in _workspaceEnvironment.WorkspaceFolders.Values)
        {
            OnWorkspaceFolderAdded(folder);
        }

        Task.Run(async () =>
        {
            await _startupWait.WaitForStartupAsync();

            try
            {
                OnStartupFinished();
            }
            catch (Exception ex)
            {
                Services.CreateLogger<DiagnosticsManager>().LogError(ex, "Error running OnStartupFinished.");
            }
        });
    }

    private void OnStartupFinished()
    {
        ConcurrentQueue<FileDiagnostics>? queue = Interlocked.Exchange(ref _startupQueue, null);
        if (queue == null)
            return;
        
        while (queue.TryDequeue(out FileDiagnostics? diag))
        {
            diag.Recalculate();
        }
    }

    private void OnFileOpened(OpenedFile obj)
    {
        ReclaculateDiagnostics(obj.File);
        if (_diagnostics.TryGetValue(obj.File, out FileDiagnostics? file))
        {
            file.SetOpenedFile(obj);
        }
    }

    private void OnFileClosed(OpenedFile obj)
    {
        foreach (FileDiagnostics file in _diagnostics.Values)
        {
            file.SetOpenedFile(null, obj);
        }
    }

    public void PushDiagnostics(FileDiagnostics file, Container<Diagnostic> diagnostics)
    {
        _languageServer.SendNotification(new PublishDiagnosticsParams
        {
            Diagnostics = diagnostics,
            Uri = file.Uri,
            Version = null
        });
    }

    public Diagnostic CreateDiagnostic(in DatDiagnosticMessage msg)
    {
        return new Diagnostic
        {
            Code = new DiagnosticCode(msg.Diagnostic.ErrorId),
            Source = UnturnedDatLanguageServer.DiagnosticSource,
            Message = msg.Message,
            Range = msg.Range.ToRange(),
            Tags = msg.Diagnostic == DatDiagnostics.UNT1018 ? new Container<DiagnosticTag>(DiagnosticTag.Deprecated) : null,
            Severity = (DiagnosticSeverity)msg.Diagnostic.Severity
        };
    }

    public void Dispose()
    {
        _workspaceEnvironment.FileCreated -= OnFileCreated;
        _workspaceEnvironment.FileDeleted -= OnFileDeleted;
        _workspaceEnvironment.FileUpdated -= OnFileUpdated;
        _workspaceEnvironment.FileRenamed -= OnFileRenamed;
        _workspaceEnvironment.WorkspaceFolderAdded -= OnWorkspaceFolderAdded;
        _workspaceEnvironment.WorkspaceFolderRemoved -= OnWorkspaceFolderRemoved;
        _fileSync.FileAdded -= OnFileOpened;
        _fileSync.FileRemoved -= OnFileClosed;
        _fileSync.ContentUpdated -= OnContentUpdated;
    }

    public FileDiagnostics GetOrAddFile(string filePath, DocumentUri? uri)
    {
        if (Path.DirectorySeparatorChar == '\\')
            filePath = filePath.Replace('/', '\\');

        if (_diagnostics.TryGetValue(filePath, out FileDiagnostics? d))
            return d;

        FileDiagnostics newDiagnostics = new FileDiagnostics(filePath, uri ?? DocumentUri.File(filePath), this, Services.Database);
        d = _diagnostics.GetOrAdd(filePath, newDiagnostics);
        if (ReferenceEquals(d, newDiagnostics) && _fileTracker.Files.TryGetValue(newDiagnostics.Uri, out OpenedFile? openedFile))
        {
            newDiagnostics.SetOpenedFile(openedFile, null);
            if (!_fileTracker.Files.ContainsKey(newDiagnostics.Uri))
                newDiagnostics.SetOpenedFile(null, openedFile);
        }

        return d;
    }

    private void RemoveDiagnosticsForFiles(string directoryPath)
    {
        StringComparison c = OSPathHelper.PathComparison;
        foreach (string key in _diagnostics.Keys)
        {
            if (!key.StartsWith(directoryPath, c))
                continue;

            RemoveDiagnostics(key);
        }
    }

    private void OnContentUpdated(OpenedFile obj)
    {
        int version = obj.ChangeVersion;
        Task.Run(async () =>
        {
            await Task.Delay(500);
            if (version == obj.ChangeVersion)
            {
                _workQueue.Enqueue(new DiagnosticsWorkItem(obj.File));
                MaybeStartWorkerThread();
            }
        });
    }

    private void ReclaculateDiagnostics(string filePath)
    {
        if (Path.DirectorySeparatorChar == '\\')
            filePath = filePath.Replace('/', '\\');

        RecalculateOrQueue(GetOrAddFile(filePath, null));
    }

    private void RecalculateOrQueue(FileDiagnostics diagnostics)
    {
        if (_startupWait.HasStartedUp)
        {
            diagnostics.Recalculate();
        }
        else
        {
            ConcurrentQueue<FileDiagnostics> queue;
            if (_startupQueue == null)
            {
                ConcurrentQueue<FileDiagnostics>? old = Interlocked.CompareExchange(ref _startupQueue, queue = new ConcurrentQueue<FileDiagnostics>(), null);
                if (old != null)
                    queue = old;
            }
            else
            {
                queue = _startupQueue;
            }

            queue.Enqueue(diagnostics);
        }
    }

    private void TransferDiagnostics(string from, string to)
    {
        if (!_diagnostics.TryRemove(from, out FileDiagnostics? fileDiags))
            return;

        fileDiags.UpdateFileName(to, DocumentUri.File(to));
        _diagnostics[to] = fileDiags;
        RecalculateOrQueue(fileDiags);
    }

    private void RemoveDiagnostics(string filePath)
    {
        if (_diagnostics.TryRemove(filePath, out FileDiagnostics? file))
        {
            file.Clear();
        }
    }

    private void OnWorkspaceFolderAdded(WorkspaceFolderTracker obj)
    {
        string filePath = obj.FilePath;

        if (Path.DirectorySeparatorChar == '\\')
            filePath = filePath.Replace('/', '\\');

        _workQueue.Enqueue(new DiagnosticsWorkItem(filePath, type: DiagnosticsWorkItemType.DiscoverAll));
        MaybeStartWorkerThread(forceRunOnWorkerThread: true);
    }

    private void OnWorkspaceFolderRemoved(WorkspaceFolderTracker obj)
    {
        string filePath = obj.FilePath;

        if (Path.DirectorySeparatorChar == '\\')
            filePath = filePath.Replace('/', '\\');

        _workQueue.Enqueue(new DiagnosticsWorkItem(filePath, type: DiagnosticsWorkItemType.DeleteAll));
        MaybeStartWorkerThread(forceRunOnWorkerThread: true);
    }

    private void OnFileCreated(WorkspaceFolderTracker tracker, string fullPath)
    {
        _workQueue.Enqueue(new DiagnosticsWorkItem(fullPath));
        MaybeStartWorkerThread();
    }

    private void OnFileDeleted(WorkspaceFolderTracker tracker, string fullPath)
    {
        _workQueue.Enqueue(new DiagnosticsWorkItem(fullPath, type: DiagnosticsWorkItemType.Delete));
    }

    private void OnFileUpdated(WorkspaceFolderTracker tracker, string fullPath)
    {
        _workQueue.Enqueue(new DiagnosticsWorkItem(fullPath));
        MaybeStartWorkerThread();
    }

    private void OnFileRenamed(WorkspaceFolderTracker tracker, string oldFullPath, string newFullPath)
    {
        _workQueue.Enqueue(new DiagnosticsWorkItem(newFullPath, oldFullPath));
        MaybeStartWorkerThread();
    }

    private int _queuedMaybeStart;

    private void MaybeStartWorkerThread(bool forceRunOnWorkerThread = false)
    {
        if (!Services.Database.IsInitialized && Interlocked.Exchange(ref _queuedMaybeStart, 1) == 0)
        {
            Services.Database.OnInitialize(_ =>
            {
                MaybeStartWorkerThread(true);
                return Task.CompletedTask;
            });
            return;
        }

        const int maxInlineProcessCt = 3;
        bool exited = false;
        if (!forceRunOnWorkerThread)
        {
            _workerThreadLock.Enter();
            try
            {
                // try to quickly process a small amout of items without spinning up a new thread
                // only spin up a new thread when there are a lot of items added
                if (_workQueue.Count <= maxInlineProcessCt && (DateTime.UtcNow - _lastInlineProcess).TotalSeconds > 0.5)
                {
                    _lastInlineProcess = DateTime.UtcNow;
                    _workerThreadLock.Exit();
                    exited = true;

                    for (int ct = 0; _workQueue.Count + ct < maxInlineProcessCt && _workQueue.TryDequeue(out DiagnosticsWorkItem workItem); ++ct)
                    {
                        ProcessWorkItem(in workItem, false);
                    }
                }
            }
            finally
            {
                if (!exited)
                    _workerThreadLock.Exit();
            }
        }

        if (_workQueue.Count == 0)
            return;

        _workerThreadLock.Enter();
        try
        {
            if (_isWorkerThreadRunning)
                return;

            _isWorkerThreadRunning = true;
            _diagnosticProcessorThread = new Thread(WorkerThread);
            _diagnosticProcessorThread.Start();
        }
        finally
        {
            _workerThreadLock.Exit();
        }
    }

    private void WorkerThread()
    {
        while (_workQueue.TryDequeue(out DiagnosticsWorkItem workItem))
        {
            ProcessWorkItem(in workItem, true);
        }

        _workerThreadLock.Enter();
        _isWorkerThreadRunning = false;
        _workerThreadLock.Exit();

        if (_workQueue.Count > 0 && _diagnosticProcessorThread == null)
        {
            MaybeStartWorkerThread();
        }
    }

    private void ProcessWorkItem(in DiagnosticsWorkItem workItem, bool isInWorkerThread)
    {
        if (workItem.Type == DiagnosticsWorkItemType.DeleteAll)
        {
            RemoveDiagnosticsForFiles(workItem.FilePath);
            return;
        }

        if (workItem.Type == DiagnosticsWorkItemType.Delete
            || workItem.Type != DiagnosticsWorkItemType.DiscoverAll && !File.Exists(workItem.FilePath))
        {
            RemoveDiagnostics(workItem.FilePath);
            return;
        }

        if (workItem.Type == DiagnosticsWorkItemType.DiscoverAll)
        {
            if (!Directory.Exists(workItem.FilePath))
            {
                RemoveDiagnosticsForFiles(workItem.FilePath);
                return;
            }

            PatternMatchingResult result = UnturnedDatLanguageServer.FileWatcherMatcher.Execute(
                new DirectoryInfoWrapper(new DirectoryInfo(workItem.FilePath))
            );

            if (!result.HasMatches)
            {
                RemoveDiagnosticsForFiles(workItem.FilePath);
                return;
            }

            foreach (FilePatternMatch file in result.Files)
            {
                _workQueue.Enqueue(new DiagnosticsWorkItem(Path.Combine(workItem.FilePath, file.Path)));
            }

            if (!isInWorkerThread)
                MaybeStartWorkerThread();
            return;
        }

        if (workItem.RenamedFrom != null)
        {
            TransferDiagnostics(workItem.RenamedFrom, workItem.FilePath);
            return;
        }

        ReclaculateDiagnostics(workItem.FilePath);
    }

    private struct DiagnosticsWorkItem
    {
        public DiagnosticsWorkItemType Type { get; }
        public string FilePath { get; }
        public string? RenamedFrom { get; }

        public DiagnosticsWorkItem(string filePath, string? renamedFrom = null, DiagnosticsWorkItemType type = DiagnosticsWorkItemType.Recalculate)
        {
            Type = type;
            FilePath = filePath;
            RenamedFrom = renamedFrom;
        }
    }

    public enum DiagnosticsWorkItemType
    {
        Recalculate,
        DiscoverAll,
        Delete,
        DeleteAll
    }
}
