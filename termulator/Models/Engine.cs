using System;
using System.Diagnostics;
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
        progress?.Report((10, "Getting latest alpine image..."));

        await Task.Delay(500);

        progress?.Report((40, $"Starting container: {_container}..."));
        await RunBackgroundProcessAsync($"run -d -t --name {_container} {_image} sh");

        progress?.Report((70, "Attaching shell..."));
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

        progress?.Report((100, "Container ready."));
    }

    public async Task<string> executeCommand(string command)
    {
        // Check if the process exists AND hasn't exited
        if (!_isRunning || _shellProcess == null || _shellProcess.HasExited)
        {
            return "[Error: Shell process is not running or has terminated]";
        }

        var output = new StringBuilder();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            // Try writing to the process. If it died unexpectedly, this will throw an IOException.
            await _shellProcess.StandardInput.WriteLineAsync($"{{ {command}; }} 2>&1");
            await _shellProcess.StandardInput.WriteLineAsync($"echo {limit}");

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
        catch (System.IO.IOException)
        {
            _isRunning = false; // Mark engine as dead
            return "[Connection to the terminal was lost (Broken Pipe).]";
        }
        catch (OperationCanceledException)
        {
            // If we time out, try to send a Ctrl+C (\x03) to unblock it
            try
            {
                await _shellProcess.StandardInput.WriteLineAsync("\x03");
            }
            catch { }
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
