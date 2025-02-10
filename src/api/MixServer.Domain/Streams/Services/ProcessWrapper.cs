using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Streams.Models;

namespace MixServer.Domain.Streams.Services;

public class ProcessWrapper : Process
{
    private readonly ILogger<ProcessWrapper> _logger;
    private readonly ProcessSettings _processSettings;

    public ProcessWrapper(
        string key,
        ILogger<ProcessWrapper> logger,
        ProcessSettings processSettings)
    {
        Key = key;
        _logger = logger;
        _processSettings = processSettings;

        StartInfo.UseShellExecute = false;
        StartInfo.CreateNoWindow = true;
        StartInfo.RedirectStandardOutput = true;
        StartInfo.RedirectStandardError = true;

        if (!string.IsNullOrWhiteSpace(processSettings.WorkingDirectory))
        {
            StartInfo.WorkingDirectory = processSettings.WorkingDirectory;
        }

        OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                logger.Log(processSettings.StdOutLogLevel, "{Data}", args.Data);
            }
        };

        ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                logger.Log(processSettings.StdErrLogLevel, "{Data}", args.Data);
            }
        };
    }
    
    public string Key { get; }

    public bool Run(string command, string args) => Run(command, args, out _);
    
    public bool Run(string command, string args, out ICollection<string> stdOut)
        => Run(command, args, out stdOut, out _);

    public bool Run(string command, string args, out ICollection<string> stdOut, out ICollection<string> stdErr)
    {
        StartInfo.FileName = command;
        StartInfo.Arguments = args;

        void LogProgramExited(object? sender, EventArgs e)
        {
            if (_processSettings.OnExit is not null)
            {
                try
                {
                    _processSettings.OnExit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnExit callback");
                }
            }
            _logger.LogInformation("{Program} {Args} Exited", command, args);
        }

        var tempStdOut = new List<string>();
        void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                tempStdOut.Add(e.Data);
            }
        }

        var tempStdErr = new List<string>();
        void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                tempStdErr.Add(e.Data);
            }
        }

        OutputDataReceived += OnOutputDataReceived;
        ErrorDataReceived += OnErrorDataReceived;
        Exited += LogProgramExited;

        var start = Start();

        BeginOutputReadLine();
        BeginErrorReadLine();
        WaitForExit();

        OutputDataReceived -= OnOutputDataReceived;
        ErrorDataReceived -= OnErrorDataReceived;
        Exited -= LogProgramExited;

        stdOut = tempStdOut;
        stdErr = tempStdErr;
        return start;
    }
}