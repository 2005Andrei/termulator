using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace termulator.ViewModels;

public class Engine : IAsyncDisposable
{
    private readonly string _image = "apline:latest";
    private readonly string _container;
    private bool _isRunning = false;

    private Process? _shellProcess;

    private const string limit = "___CMD_END___";

    public Engine()
    {
        _container = $"termulator_env_{Guid.NewGuid().ToString()}";
    }

    public async Task Initialize()
    {
        await RunBackgroundProcessAsync($"run -d -t --name {_container} {_image} sh");

        _shellProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec -i {_container} sh",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // technically taken care of 2>&1
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        _shellProcess.Start();
        _isRunning = true;
    }

    public async Task<string> executeCommand(string command)
    {
        if (!_isRunning || _shellProcess == null)
        {
            return "not running or doesn't exist";
        }

        var output = new StringBuilder();

        await _shellProcess.StandardInput.WriteLineAsync($"{{ {command}; }} 2>&1");
        await _shellProcess.StandardInput.WriteLineAsync($"echo {limit}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            while (true)
            {
                var line = await _shellProcess.StandardOutput.ReadLineAsync(cts.Token);

                if (line == null)
                    break;
                if (line.Trim() == limit)
                    break;

                output.AppendLine(line);
            }
        }
        catch (OperationCanceledException)
        {
            await _shellProcess.StandardInput.WriteLineAsync("\x03");
            return output.ToString() + "\n[Process timed out. Avoid blocking commands.]";
        }

        return output.ToString().TrimEnd();
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
