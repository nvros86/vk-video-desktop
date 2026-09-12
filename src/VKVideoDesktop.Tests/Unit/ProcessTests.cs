using System.ComponentModel;
using System.Diagnostics;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class ProcessTests : IDisposable
{
    private readonly List<Process> _processes = new();

    [Fact]
    public void ExternalProcess_ExitCodeZero_Succeeds()
    {
        using var process = StartProcess("cmd.exe", "/c exit 0");
        process.WaitForExit();

        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void ExternalProcess_ExitCodeNonZero_ReturnsCode()
    {
        using var process = StartProcess("cmd.exe", "/c exit 1");
        process.WaitForExit();

        Assert.Equal(1, process.ExitCode);
    }

    [Fact]
    public void ExternalProcess_ExitCodeHigh_ReturnsCode()
    {
        using var process = StartProcess("cmd.exe", "/c exit 255");
        process.WaitForExit();

        Assert.Equal(255, process.ExitCode);
    }

    [Fact]
    public async Task ExternalProcess_Timeout_KillsProcess()
    {
        using var process = StartProcess("cmd.exe", "/c ping 127.0.0.1 -n 30");
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }

        Assert.True(process.HasExited);
    }

    [Fact]
    public void ExternalProcess_InvalidPath_ThrowsFileNotFound()
    {
        var info = new ProcessStartInfo
        {
            FileName = "nonexistent_program_12345.exe",
            Arguments = "/c exit 0",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Assert.Throws<Win32Exception>(() => Process.Start(info));
    }

    [Fact]
    public void ExternalProcess_OutputIsCaptured()
    {
        using var process = StartProcess("cmd.exe", "/c echo hello");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        Assert.Contains("hello", output);
    }

    [Fact]
    public async Task ExternalProcess_ConcurrentProcesses_AllComplete()
    {
        var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(() =>
        {
            using var process = StartProcess("cmd.exe", $"/c exit {i}");
            process.WaitForExit();
            return process.ExitCode;
        }));

        var results = await Task.WhenAll(tasks);

        for (int i = 0; i < 5; i++)
            Assert.Equal(i, results[i]);
    }

    [Fact]
    public async Task ExternalProcess_Cancellation_TerminatesProcess()
    {
        using var process = StartProcess("cmd.exe", "/c ping 127.0.0.1 -n 30");
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }

        Assert.True(process.HasExited);
    }

    public void Dispose()
    {
        foreach (var p in _processes)
        {
            try
            {
                if (!p.HasExited)
                    p.Kill(entireProcessTree: true);
            }
            catch { }
            p.Dispose();
        }
    }

    private Process StartProcess(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        _processes.Add(process);
        return process;
    }
}
