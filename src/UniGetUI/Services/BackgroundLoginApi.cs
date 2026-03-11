using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using UniGetUI.Core.Logging;

namespace UniGetUI.Services;

public class GHAuthApiRunner : IDisposable
{
    public event EventHandler<string>? OnLogin;
    private IHost? _host;
    public GHAuthApiRunner() { }

    public async Task Start()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseKestrel();
            webBuilder.SuppressStatusMessages(true);
            webBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", LOGIN_CollectGitHubToken);
                });
            });
            webBuilder.UseUrls("http://localhost:58642");
        });
        _host = builder.Build();
        await _host.StartAsync();
        Logger.Info("Api running on http://localhost:58642");
    }

    private async Task LOGIN_CollectGitHubToken(HttpContext context)
    {
        var code = context.Request.Query["code"];
        if (string.IsNullOrEmpty(code))
        {
            context.Response.StatusCode = 400;
            return;
        }

        await context.Response.WriteAsync(
            """
            <html><style>
                div {
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    justify-content: center;
                    height: 100vh;
                    font-family: sans-serif;
                    text-align: center;
                }
            </style><script>
                window.close();
            </script><div>
                <title>UniGetUI authentication</title>
                <h1>Authentication successful</h1>
                <p>You can now close this window and return to UniGetUI</p>
            </div></html>
            """);

        Logger.ImportantInfo($"[AUTH API] Received authentication token {code} from GitHub");
        OnLogin?.Invoke(this, code.ToString());
    }

    public async Task Stop()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_host);
            await _host.StopAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}
