using System;
using System.Collections.Generic;
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
using UniGetUI.PackageEngine.Managers.PowerShellManager;

namespace UniGetUI.PackageEngine.Operations;
public class DownloadOperation : AbstractOperation
{
    private IPackage _package;
    private string downloadLocation;
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

    protected override async Task<OperationVeredict> PerformOperation()
    {
        canceled = false;
        try
        {
            Line($"Fetching download url for package {_package.Id} on manager {_package.Manager.DisplayName}...", LineType.StdOUT);
            await _package.Details.Load();
            Uri? downloadUrl = _package.Details.InstallerUrl;
            if (downloadUrl is null)
            {
                Line($"We couldn't find any installer for this package. " +
                     $"Please check that this package has an applicable installer and try again later", LineType.StdERR);
                return OperationVeredict.Failure;
            }

            /*FileSavePicker savePicker = new(hWnd);
            string extension = _package.Manager is BaseNuGet
                ? "nupkg"
                : downloadUrl.ToString().Split('.').Last();
            string suggestedName = _package.Id + " installer." + extension;

            List<string> extensions = new();
            extensions.Add($"*.{extension}");
            if (downloadUrl.ToString().Split('.')[^1] == "nupkg") extensions.Add("*.zip");
            extensions.Add("*.*");

            string name = await Task.Run(() => savePicker.Show(extensions, suggestedName));
            if (name == String.Empty)
            {
                Line("User has canceled the operation");
                return OperationVeredict.Canceled;
            }*/

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
                            30,
                            progress,
                            $"{CoreTools.FormatAsSize(totalRead)}/{CoreTools.FormatAsSize(totalBytes)}"
                        ), LineType.Progress);
                    }
                }

                if (canceled)
                {
                    fileStream.Close();
                    File.Delete(downloadLocation);
                    Line("User has canceled the operation", LineType.StdERR);
                    return OperationVeredict.Canceled;
                }
            }

            Line($"The file was saved to {downloadLocation}", LineType.Progress);
            return OperationVeredict.Success;
        }
        catch (Exception ex)
        {
            Line($"{ex.GetType()}: {ex.Message}", LineType.StdERR);
            Line($"{ex.StackTrace}", LineType.StdERR);
            return OperationVeredict.Failure;
        }
    }
}