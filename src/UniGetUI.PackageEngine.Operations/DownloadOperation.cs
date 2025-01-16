using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageOperations;
using Windows.Foundation.Metadata;
using ExternalLibraries.Pickers;
using PhotoSauce.MagicScaler;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Managers.WingetManager;

namespace UniGetUI.PackageEngine.Operations;
public class DownloadOperation : AbstractOperation
{
    private IPackage _package;
    private string downloadLocation;
    public string DownloadLocation
    {
        get => downloadLocation;
    }
    private bool canceled;

    public DownloadOperation(IPackage package, string downloadPath): base(true)
    {
        downloadLocation = downloadPath;
        _package = package;

        Metadata.OperationInformation = "Downloading installer for Package=" + _package.Id + " with Manager=" + _package.Manager.Name;
        Metadata.Title = CoreTools.Translate("{package} installer download", new Dictionary<string, object?> { { "package", _package.Name } });
        Metadata.Status = CoreTools.Translate("{0} installer is being downloaded", _package.Name);
        Metadata.SuccessTitle = CoreTools.Translate("Download succeeded");
        Metadata.SuccessMessage = CoreTools.Translate("{package} installer was downloaded successfully", new Dictionary<string, object?> { { "package", _package.Name } });
        Metadata.FailureTitle = CoreTools.Translate("Download failed", new Dictionary<string, object?> { { "package", _package.Name } });
        Metadata.FailureMessage = CoreTools.Translate("{package} installer could not be downloaded", new Dictionary<string, object?> { { "package", _package.Name } });

        CancelRequested += (_, _) =>
        {
            canceled = true;
        };

    }

    public override Task<Uri> GetOperationIcon()
    {
        return Task.Run(_package.GetIconUrl);
    }

    protected override void ApplyRetryAction(string retryMode)
    {
        // Do nothing
    }

    private async Task<OperationVeredict> WinGetDownload()
    {
        if (_package.Manager is not WinGet) throw new InvalidDataException("How did we end up here?");
        if (!Directory.Exists(downloadLocation))
            throw new InvalidDataException("The output file must be a directory, if downloading a WinGet package");

        Process p = new Process()
        {
            StartInfo = new()
            {
                FileName = WinGet.Instance.Status.ExecutablePath,
                Arguments = $"download --id \"{_package.Id}\" --exact --source \"{_package.Source.Name}\" --disable-interactivity " +
                            $"--skip-license --accept-source-agreements --accept-package-agreements --download-directory \"{downloadLocation}\" --authentication-mode silentPreferred",
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        Line("Starting process " + p.StartInfo.FileName, LineType.VerboseDetails);
        Line("   with arguments " + p.StartInfo.Arguments, LineType.VerboseDetails);

        p.Start();

        string? line;
        while ((line = (await p.StandardOutput.ReadLineAsync())?.Trim()) != null)
        {
            if(canceled) p.Kill();
            Line(line, line.Length > 6? LineType.Information: LineType.ProgressIndicator);
        }

        await p.WaitForExitAsync();

        Line($"Process exited with output code {p.ExitCode}", LineType.Information);
        if (canceled)
        {
            Line("User has canceled the operation", LineType.Error);
            return OperationVeredict.Canceled;
        }
        else if (p.ExitCode == 0)
        {
            Line($"The file was saved to {downloadLocation}", LineType.Information);
            return OperationVeredict.Success;
        }
        else
        {
            Line("The download has failed.", LineType.Information);
            return OperationVeredict.Failure;
        }
    }

    protected override async Task<OperationVeredict> PerformOperation()
    {
        canceled = false;
        try
        {
            if (_package.Manager is WinGet && Directory.Exists(downloadLocation))
            {
                return await WinGetDownload();
            }

            Line($"Fetching download url for package {_package.Name} from {_package.Manager.DisplayName}...", LineType.Information);
            await _package.Details.Load();
            Uri? downloadUrl = _package.Details.InstallerUrl;
            if (downloadUrl is null)
            {
                Line($"UniGetUI was not able to find any installer for this package. " +
                     $"Please check that this package has an applicable installer and try again later", LineType.Error);
                return OperationVeredict.Failure;
            }

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes > 0;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(downloadLocation, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[4*1024*1024];
            long totalRead = 0;
            int bytesRead;

            int oldProgress = -1;
            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    var progress = (int)((totalRead * 100L) / totalBytes);
                    if (progress != oldProgress)
                    {
                        oldProgress = progress;
                        Line(CoreTools.TextProgressGenerator(
                            30, progress,
                            $"{CoreTools.FormatAsSize(totalRead)}/{CoreTools.FormatAsSize(totalBytes)}"
                        ), LineType.ProgressIndicator);
                    }
                }

                if (canceled)
                {
                    fileStream.Close();
                    File.Delete(downloadLocation);
                    Line("User has canceled the operation", LineType.Error);
                    return OperationVeredict.Canceled;
                }
            }

            Line($"The file was saved to {downloadLocation}", LineType.Information);
            return OperationVeredict.Success;
        }
        catch (Exception ex)
        {
            Line($"{ex.GetType()}: {ex.Message}", LineType.Error);
            Line($"{ex.StackTrace}", LineType.Error);
            return OperationVeredict.Failure;
        }
    }
}
