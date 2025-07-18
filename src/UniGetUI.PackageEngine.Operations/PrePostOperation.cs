using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageOperations;

namespace UniGetUI.PackageEngine.Operations;

public class PrePostOperation : AbstractOperation
{
    private string Payload;
    public PrePostOperation(string payload) : base(true)
    {
        Payload = payload.Replace("\r", "\n").Replace("\n\n", "\n").Replace("\n", "&");
        Metadata.Status = $"Running custom operation {Payload}";
        Metadata.Title = $"Custom operation";
        Metadata.OperationInformation = " ";
        Metadata.SuccessTitle = $"Done!";
        Metadata.SuccessMessage = $"Done!";
        Metadata.FailureTitle = $"Custom operation failed";
        Metadata.FailureMessage = $"The custom operation {Payload} failed to run";

    }

    protected override void ApplyRetryAction(string retryMode)
    {
    }

    protected override async Task<OperationVeredict> PerformOperation()
    {
        Line($"Running command {Payload}", LineType.Information);
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C {Payload}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data is not null) Line(e.Data, LineType.Information);
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data is not null) Line(e.Data, LineType.Error);
        };
        await process.WaitForExitAsync();

        int exitCode = process.ExitCode;
        Line($"Exit code is {exitCode}", LineType.Information);
        return (exitCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure);
    }

    public override Task<Uri> GetOperationIcon()
        => Task.FromResult(new Uri("about:blank"));

}
