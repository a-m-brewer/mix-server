using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Streams.Models;

namespace MixServer.Domain.Streams.Services;

public interface IProcessService
{
    bool ProcessExists(string key);
    void StartProcess(string key, string command, string args, ProcessSettings? settings = null);
}

public class ProcessService(ILoggerFactory loggerFactory) : IProcessService
{
    private readonly ConcurrentDictionary<string, ProcessWrapper> _processes = new();

    public bool ProcessExists(string key) => _processes.ContainsKey(key);
    
    public void StartProcess(string key, string command, string args, ProcessSettings? settings = null)
    {
        if (_processes.ContainsKey(key))
        {
            return;
        }

        var process = new ProcessWrapper(key, loggerFactory.CreateLogger<ProcessWrapper>(), settings ?? new ProcessSettings
        {
            StdErrLogLevel = LogLevel.Error,
            StdOutLogLevel = LogLevel.Information
        });
        
        process.EnableRaisingEvents = true;
        
        process.Exited += OnProcessExited;
        
        _processes.TryAdd(key, process);

        _ = Task.Run(() => process.Run(command, args));
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (sender is not ProcessWrapper process)
        {
            return;
        }

        process.Exited -= OnProcessExited;
        process.Dispose();
        _processes.TryRemove(process.Key, out _);
    }
}