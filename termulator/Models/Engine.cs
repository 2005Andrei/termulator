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
        _isRunning = true;

        progress?.Report((100, "Container ready"));
    }

    // public async Task<string> executeCommand(string command)
    // {
    //     if (!_isRunning || _shellProcess == null || _shellProcess.HasExited)
    //     {
    //         return "";
    //     }
    //
    //     var output = new StringBuilder();
    //
    //     using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    //
    //     try
    //     {
    //         await _shellProcess.StandardInput.WriteLineAsync($"{{ {command}; }} 2>&1");
    //         await _shellProcess.StandardInput.WriteLineAsync($"echo {limit}");
    //
    //         while (true)
    //         {
    //             var line = await _shellProcess.StandardOutput.ReadLineAsync(cts.Token);
    //
    //             if (line == null)
    //                 break;
    //             if (line.Trim() == limit)
    //                 break;
    //
    //             output.AppendLine(line);
    //         }
    //     }
    //     catch (System.IO.IOException)
    //     {
    //     }
    //     catch (OperationCanceledException)
    //     {
    //         try
    //         {
    //             await _shellProcess.StandardInput.WriteLineAsync("\x03");
    //         }
    //         catch { }
    //         return output.ToString() + "\nTimed out.";
    //     }
    //
    //     return output.ToString().TrimEnd();
    // }

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

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        bool wroteOk = await TryWriteCommand(command);
        if (!wroteOk)
        {
            yield return "idk anymore";
            yield break;
        }

        while (true)
        {
            var (line, timedOut, brokenPipe) = await TryReadLine(cts.Token);

            if (brokenPipe)
            {
                _isRunning = false;
                yield return "broien pipe";
                yield break;
            }

            if (timedOut)
            {
                try
                {
                    await _shellProcess.StandardInput.WriteLineAsync("\x03");
                }
                catch { }
                yield return "timed out";
                yield break;
            }

            if (line == null || line.Trim() == limit)
                yield break;

            yield return line;
        }
    }

    private async Task<bool> TryWriteCommand(string command)
    {
        try
        {
            await _shellProcess!.StandardInput.WriteLineAsync($"{{ {command}; }} 2>&1");
            await _shellProcess!.StandardInput.WriteLineAsync($"echo {limit}");
            return true;
        }
        catch (System.IO.IOException)
        {
            _isRunning = false;
            return false;
        }
    }

    private async Task<(string? line, bool timedOut, bool brokenPipe)> TryReadLine(
        CancellationToken token
    )
    {
        try
        {
            var line = await _shellProcess!.StandardOutput.ReadLineAsync(token);
            return (line, false, false);
        }
        catch (OperationCanceledException)
        {
            return (null, true, false);
        }
        catch (System.IO.IOException)
        {
            return (null, false, true);
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
