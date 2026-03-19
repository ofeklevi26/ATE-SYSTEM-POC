using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Commands;

public sealed class CommandInvoker
{
    private readonly ConcurrentQueue<IAteCommand> _queue = new ConcurrentQueue<IAteCommand>();
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
    private readonly object _gate = new object();
    private readonly ILogger _logger;

    private CancellationTokenSource? _lifetimeCts;
    private CancellationTokenSource? _currentCommandCts;
    private Task? _workerTask;
    private volatile bool _paused;

    public CommandInvoker(ILogger logger)
    {
        _logger = logger;
        State = "Stopped";
    }

    public string State { get; private set; }

    public string? CurrentCommand { get; private set; }

    public string? LastError { get; private set; }

    public int QueueLength => _queue.Count;

    public void Enqueue(IAteCommand command)
    {
        _queue.Enqueue(command);
        _signal.Release();
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_workerTask != null && !_workerTask.IsCompleted)
            {
                return;
            }

            _lifetimeCts = new CancellationTokenSource();
            State = "Running";
            _workerTask = Task.Run(() => WorkerLoopAsync(_lifetimeCts.Token));
        }
    }

    public async Task StopAsync()
    {
        Task? worker;
        lock (_gate)
        {
            if (_lifetimeCts == null)
            {
                State = "Stopped";
                return;
            }

            State = "Stopping";
            _lifetimeCts.Cancel();
            AbortCurrent();
            worker = _workerTask;
        }

        if (worker != null)
        {
            try
            {
                await worker.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                _logger.Info($"Command worker stopped due to cancellation: {ex.Message}");
            }
        }

        State = "Stopped";
    }

    public void Pause()
    {
        _paused = true;
        State = "Paused";
    }

    public void Resume()
    {
        _paused = false;
        State = "Running";
        _signal.Release();
    }

    public void ClearPending()
    {
        while (_queue.TryDequeue(out _))
        {
        }
    }

    public void AbortCurrent()
    {
        _currentCommandCts?.Cancel();
    }

    public void ReportError(string error)
    {
        LastError = error;
        _logger.Error(error);
    }

    private async Task WorkerLoopAsync(CancellationToken stopToken)
    {
        while (!stopToken.IsCancellationRequested)
        {
            await _signal.WaitAsync(stopToken).ConfigureAwait(false);

            if (_paused)
            {
                await Task.Delay(200, stopToken).ConfigureAwait(false);
                _signal.Release();
                continue;
            }

            if (!_queue.TryDequeue(out var command))
            {
                continue;
            }

            CurrentCommand = command.Name;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            _currentCommandCts = linked;
            try
            {
                await command.ExecuteAsync(linked.Token).ConfigureAwait(false);
                LastError = null;
            }
            catch (OperationCanceledException)
            {
                LastError = $"Command '{command.Name}' was cancelled.";
                _logger.Error(LastError);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _logger.Error($"Command '{command.Name}' failed.", ex);
            }
            finally
            {
                CurrentCommand = null;
                _currentCommandCts = null;
            }
        }
    }
}
