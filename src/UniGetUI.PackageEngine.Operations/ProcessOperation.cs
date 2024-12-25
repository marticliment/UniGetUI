using System.Diagnostics;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageOperations;

public abstract class AbstractProcessOperation : AbstractOperation
{
    protected Process process { get; private set; }
    protected AbstractProcessOperation(bool queue_enabled) : base(queue_enabled)
    {
        process = new();
        CancelRequested += (_, _) => process.Kill();
        OperationStarting += async (_, _) =>
        {
            process = new();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "lol";
            process.StartInfo.Arguments = "lol";
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                string data = e.Data.ToString().Trim();
                var lineType = LineType.StdOUT;
                if (data.Length < 6 || data.Contains("Waiting for another install..."))
                {
                    lineType = LineType.Progress;
                }

                Line(data, lineType);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                string data = e.Data.ToString().Trim();
                var lineType = LineType.StdERR;
                if (data.Length < 6 || data.Contains("Waiting for another install..."))
                {
                    lineType = LineType.Progress;
                }

                Line(data, lineType);
            };
            await PrepareProcessStartInfo();
        };
    }

    protected override async Task<OperationVeredict> PerformOperation()
    {
        if(process.StartInfo.UseShellExecute) throw new InvalidOperationException("UseShellExecute must be set to false");
        if(!process.StartInfo.RedirectStandardOutput) throw new InvalidOperationException("RedirectStandardOutput must be set to true");
        if(!process.StartInfo.RedirectStandardInput) throw new InvalidOperationException("RedirectStandardInput must be set to true");
        if(!process.StartInfo.RedirectStandardError) throw new InvalidOperationException("RedirectStandardError must be set to true");
        if (process.StartInfo.FileName == "lol") throw new InvalidOperationException("StartInfo.FileName has not been set");
        if (process.StartInfo.Arguments == "lol") throw new InvalidOperationException("StartInfo.Arguments has not been set");

        Line($"Executing process with StartInfo:", LineType.OperationInfo);
        Line($" - FileName: \"{process.StartInfo.FileName.Trim()}\"", LineType.OperationInfo);
        Line($" - Arguments: \"{process.StartInfo.Arguments.Trim()}\"", LineType.OperationInfo);
        Line($"Start Time: \"{DateTime.Now}\"", LineType.OperationInfo);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        Line($"End Time: \"{DateTime.Now}\"", LineType.OperationInfo);
        Line($"Process return value: \"{process.ExitCode}\" (0x{process.ExitCode:X})", LineType.OperationInfo);

        return await GetProcessVeredict(process.ExitCode, []);
    }

    protected abstract Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output);
    protected abstract Task PrepareProcessStartInfo();
}
