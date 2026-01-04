using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppChatBot.Common;

namespace WhatsAppChatBot.Services;

public class NgrokTunnel : IUnifiedAction
{
    public Task<string> PerformAsync()
    {
        var dict = new Dictionary<string, string>
        {
            ["file"] = "src/WhatsAppChatBot/Services/NgrokTunnel.cs",
            ["class"] = "NgrokTunnel",
            ["result"] = "ok"
        };
        return Task.FromResult(JsonSerializer.Serialize(dict));
    }
}
using System.Diagnostics;
using System.Text.Json;

namespace WhatsAppChatBot.Services;

public interface INgrokTunnel
{
    Task<string> CreateAsync(int port, int maxRetries = 3);
    Task KillAsync();
    bool IsAvailable();
    void RegisterShutdownHandler();
}

public class NgrokTunnel : INgrokTunnel
{
    private readonly string _authToken;
    private readonly ILogger<NgrokTunnel> _logger;
    private Process? _ngrokProcess;
    private string? _tunnelUrl;

    public NgrokTunnel(string authToken, ILogger<NgrokTunnel> logger)
    {
        _authToken = authToken;
        _logger = logger;
    }

    public async Task<string> CreateAsync(int port, int maxRetries = 3)
    {
        var retries = maxRetries;

        while (retries > 0)
        {
            retries--;
            try
            {
                await KillAsync();
                await Task.Delay(1000);

                var ngrokPath = GetNgrokPath();
                if (string.IsNullOrEmpty(ngrokPath))
                {
                    throw new FileNotFoundException("Ngrok executable not found. Please install ngrok.");
                }

                // Set auth token
                await RunNgrokCommand(ngrokPath, $"config add-authtoken {_authToken}");

                // Start tunnel
                var startInfo = new ProcessStartInfo
                {
                    FileName = ngrokPath,
                    Arguments = $"http {port} --log=stdout --log-format=json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _ngrokProcess = Process.Start(startInfo);
                if (_ngrokProcess == null)
                {
                    throw new InvalidOperationException("Failed to start ngrok process");
                }

                // Wait for tunnel URL
                _tunnelUrl = await WaitForTunnelUrl();
                if (!string.IsNullOrEmpty(_tunnelUrl))
                {
                    _logger.LogInformation("Ngrok tunnel created: {Url}", _tunnelUrl);
                    return _tunnelUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Ngrok tunnel. Retries left: {Retries}", retries);
                await KillAsync();

                if (retries > 0)
                {
                    await Task.Delay(1000);
                }
            }
        }

        throw new InvalidOperationException("Failed to create Ngrok tunnel after multiple retries");
    }

    public async Task KillAsync()
    {
        try
        {
            if (_ngrokProcess != null && !_ngrokProcess.HasExited)
            {
                _ngrokProcess.Kill();
                await _ngrokProcess.WaitForExitAsync();
                _ngrokProcess.Dispose();
                _ngrokProcess = null;
            }

            // Kill any existing ngrok processes
            await KillExistingProcesses();
            _tunnelUrl = null;
            _logger.LogInformation("Ngrok tunnel killed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to kill Ngrok tunnel");
        }
    }

    public bool IsAvailable()
    {
        try
        {
            var ngrokPath = GetNgrokPath();
            return !string.IsNullOrEmpty(ngrokPath) && File.Exists(ngrokPath);
        }
        catch
        {
            return false;
        }
    }

    public void RegisterShutdownHandler()
    {
        AppDomain.CurrentDomain.ProcessExit += async (_, _) => await KillAsync();
        Console.CancelKeyPress += async (_, _) => await KillAsync();
    }

    private static string? GetNgrokPath()
    {
        // Check common locations for ngrok
        var possiblePaths = new[]
        {
            "/usr/local/bin/ngrok",
            "/opt/homebrew/bin/ngrok",
            "/usr/bin/ngrok",
            "ngrok" // Assume it's in PATH
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                if (path == "ngrok")
                {
                    // Check if ngrok is in PATH
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = "ngrok",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    });

                    if (process != null)
                    {
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            return "ngrok";
                        }
                    }
                }
                else if (File.Exists(path))
                {
                    return path;
                }
            }
            catch
            {
                // Continue checking other paths
            }
        }

        return null;
    }

    private static async Task RunNgrokCommand(string ngrokPath, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = ngrokPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process != null)
        {
            await process.WaitForExitAsync();
            process.Dispose();
        }
    }

    private async Task<string?> WaitForTunnelUrl()
    {
        if (_ngrokProcess == null) return null;

        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout && !_ngrokProcess.HasExited)
        {
            try
            {
                var line = await _ngrokProcess.StandardOutput.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                // Parse JSON log output from ngrok
                if (line.Contains("\"url\":\"https://") && line.Contains("\"msg\":\"started tunnel\""))
                {
                    var jsonDoc = JsonDocument.Parse(line);
                    if (jsonDoc.RootElement.TryGetProperty("url", out var urlElement))
                    {
                        return urlElement.GetString();
                    }
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(100);
        }

        return null;
    }

    private static async Task KillExistingProcesses()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "pkill",
                Arguments = "-f ngrok",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process != null)
            {
                await process.WaitForExitAsync();
                process.Dispose();
            }

            await Task.Delay(1000);
        }
        catch
        {
            // Best effort - ignore errors
        }
    }
}
