using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace termulator.ViewModels;

public class Engine : IAsyncDisposable
{
    private readonly string _image = "alpine:latest";
    private readonly string _container;
    private bool _isRunning = false;
    private Process? _interruptShell;

    private Process? _shellProcess;

    private const string limit = "___CMD_END___";

    public Engine()
    {
        _container = $"termulator_env_{Guid.NewGuid().ToString()}";
    }

    public async Task Initialize(IProgress<(int percentage, string message)>? progress = null)
    {
        progress?.Report((10, "getting latest alpine image"));

        await Task.Delay(500);

        progress?.Report((40, $"starting container: {_container}..."));
        await RunBackgroundProcessAsync($"run -d -t --name {_container} {_image} sh");

        progress?.Report((70, "attaching shell..."));
        _shellProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec -i {_container} sh",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        _shellProcess.Start();

        _shellProcess.StandardInput.WriteLine("echo $$ > /tmp/shell_pid");

        _interruptShell = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec -i {_container} sh",
                RedirectStandardInput = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        _interruptShell.Start();

        _isRunning = true;

        progress?.Report((100, "Container ready"));
    }

    public async IAsyncEnumerable<string> StreamCommand(
        string command,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (!_isRunning || _shellProcess == null || _shellProcess.HasExited)
        {
            yield return "not running";
            yield break;
        }

        bool wroteOk = await TryWriteCommand(command);
        if (!wroteOk)
        {
            yield return "idk anywhere";
            yield break;
        }

        bool cancelAcknowledged = false;

        while (true)
        {
            var (line, brokenPipe) = await TryReadLine();

            if (brokenPipe)
            {
                _isRunning = false;
                yield return "broken pipe";
                yield break;
            }

            if (line == null || line.Trim() == limit)
            {
                yield break;
            }

            if (!cancelAcknowledged && cancellationToken.IsCancellationRequested)
            {
                cancelAcknowledged = true;
                yield return "^C";
            }

            if (!cancelAcknowledged)
            {
                yield return line;
            }
        }
    }

    private async Task<bool> TryWriteCommand(string command)
    {
        try
        {
            await _shellProcess!.StandardInput.WriteLineAsync($"{command} 2>&1");
            await _shellProcess!.StandardInput.WriteLineAsync($"echo {limit}");

            await _shellProcess!.StandardInput.FlushAsync();

            return true;
        }
        catch (System.IO.IOException)
        {
            _isRunning = false;
            return false;
        }
    }

    private async Task<(string? line, bool brokenPipe)> TryReadLine()
    {
        try
        {
            var line = await _shellProcess!.StandardOutput.ReadLineAsync();
            return (line, false);
        }
        catch (System.IO.IOException)
        {
            return (null, true);
        }
    }

    public void SendInterrupt()
    {
        if (!_isRunning || _interruptShell == null || _interruptShell.HasExited)
            return;

        try
        {
            _interruptShell.StandardInput.WriteLine(
                "pkill -INT -P $(cat /tmp/shell_pid) 2>/dev/null"
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task RunBackgroundProcessAsync(string args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
            },
        };
        process.Start();
        await process.WaitForExitAsync();
    }

    public async Task ShutOffAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        if (_shellProcess != null && !_shellProcess.HasExited)
        {
            _shellProcess.Kill();
            _shellProcess.Dispose();
        }

        await RunBackgroundProcessAsync($"rm -f {_container}");

        _isRunning = false;
    }

    public async ValueTask DisposeAsync()
    {
        await ShutOffAsync();
    }
}
