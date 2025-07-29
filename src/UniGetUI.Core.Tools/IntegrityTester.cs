using System.Security.Cryptography;
using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.Tools;

public static class IntegrityTester
{
    public class MismatchedHash
    {
        public string Got;
        public string Expected;
    }

    public struct IntegrityResult
    {
        public bool Passed;
        public IReadOnlyList<string> MissingFiles;
        public Dictionary<string, MismatchedHash> CorruptedFiles;
    }

    private static async Task<string> GetMD5Async(string fullPath, bool canRetry = true)
    {
        try
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fullPath))
                {
                    var hashBytes = await md5.ComputeHashAsync(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "");
                }
            }
        }
        catch (Exception ex)
        {
            if (canRetry)
            {
                await Task.Delay(1000);
                return await GetMD5Async(fullPath, false);
            }

            return $"{ex.GetType()}: {ex.Message}";
        }
    }

    public static async Task<IntegrityResult> CheckIntegrityAsync()
    {
        string integrityTreePath = Path.Join(CoreData.UniGetUIExecutableDirectory, "IntegrityTree.json");
        if (!File.Exists(integrityTreePath))
        {
            Logger.Error("/IntegrityTree.json does not exist, integrity check will not be performed!");
            return new()
            {
                Passed = false,
                MissingFiles = ["/IntegrityTree.json"],
                CorruptedFiles = new Dictionary<string, MismatchedHash>(),
            };
        }

        string rawData = await File.ReadAllTextAsync(integrityTreePath);
        Dictionary<string, string>? data = null;

        try
        {
            data = JsonSerializer.Deserialize<Dictionary<string, string>>(rawData, SerializationHelpers.DefaultOptions);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to deserialize JSON object");
            Logger.Error(ex);
        }

        if (data is null)
        {
            return new()
            {
                Passed = false,
                MissingFiles = [],
                CorruptedFiles = new()
                { {"", new MismatchedHash() {Got = rawData, Expected = "A valid JSON"} } },
            };
        }

        Dictionary<string, MismatchedHash> mismatches = new();
        List<string> misses = new();

        foreach (var (file, expectedHash) in data)
        {
            var fullPath = Path.Join(CoreData.UniGetUIExecutableDirectory, file);
            if (!File.Exists(fullPath))
            {
                misses.Add($"/{file}");
                Logger.Error($"File {file} expected but did not exist");
                continue;
            }

            var currentMd5 = (await GetMD5Async(fullPath)).ToLower();
            if (currentMd5 != expectedHash.ToLower())
            {
                mismatches.Add($"/{file}", new() { Expected = expectedHash, Got = currentMd5 });
                Logger.Error($"File {file} expected to have md5 {expectedHash}, but had {currentMd5} insetad");
            }
        }

        IntegrityResult result = new()
        {
            Passed = !misses.Any() && !mismatches.Any(),
            MissingFiles = misses,
            CorruptedFiles = mismatches
        };

        if (result.Passed)
        {
            Logger.ImportantInfo("Integrity check passed successfully!");
        }

        return result;
    }
}