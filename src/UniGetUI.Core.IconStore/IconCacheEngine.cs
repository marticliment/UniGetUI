using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using Windows.Foundation.Collections;

namespace UniGetUI.Core.IconEngine
{
    public enum CachedIconVerificationMethod
    {
        None,
        Sha256Checksum,
        FileSize,
        PackageVersion,
        UriSource
    }

    public readonly struct CacheableIcon
    {
        public readonly Uri Url;
        public readonly byte[] Sha256 = {};
        public readonly string Version = "";
        public readonly long Size = -1;
        public readonly CachedIconVerificationMethod VerificationMethod;

        public CacheableIcon(Uri uri, byte[] Sha256)
        {
            Url = uri;
            this.Sha256 = Sha256;
            VerificationMethod = CachedIconVerificationMethod.Sha256Checksum;
        }
        
        public CacheableIcon(Uri uri, string version)
        {
            Url = uri;
            this.Version = version;
            VerificationMethod = CachedIconVerificationMethod.PackageVersion;
        }

        public CacheableIcon(Uri uri, long size)
        {
            Url = uri;
            this.Size = size;
            VerificationMethod = CachedIconVerificationMethod.FileSize;
        }

        public CacheableIcon(Uri icon)
        {
            Url = icon;
            VerificationMethod = CachedIconVerificationMethod.UriSource;
        }
    }

    public static class IconCacheEngine
    {
        /// <summary>
        /// Returns the path to the icon file, downloading it if necessary
        /// </summary>
        /// <param name="icon">a CacheableIcon object representing the object</param>
        /// <param name="ManagerName">The name of the PackageManager</param>
        /// <param name="PackageId">the Id of the package</param>
        /// <returns>A path to a local icon file</returns>
        public static async Task<string> DownloadIconOrCache(CacheableIcon? _icon, string ManagerName, string PackageId)
        {
            if (_icon is null) return "";
            CacheableIcon icon = _icon ?? new CacheableIcon();

            var extension = icon.Url.AbsolutePath.Substring(icon.Url.AbsolutePath.LastIndexOf('.'))[1..];
            
            if (extension.Length > 6)
            {
                Logger.Warn($"Extension {extension} for file url {icon.Url} seems to be invalid, defaulting to .png");
                extension = "png";
            }

            var FilePath = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName, $"{PackageId}.{extension}");
            var VersionPath = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName, $"{PackageId}.{extension}.version");
            var UriPath = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName, $"{PackageId}.{extension}.uri");
            var FileDirectory = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName);
            if (!Directory.Exists(FileDirectory))
                Directory.CreateDirectory(FileDirectory);

            var FileExists = File.Exists(FilePath);
            bool IsFileValid = false;
            if(FileExists)
                switch (icon.VerificationMethod)
                {
                    case CachedIconVerificationMethod.FileSize:
                        try
                        {
                            long size = await CoreTools.GetFileSizeAsyncAsLong(icon.Url);
                            IsFileValid = (size == icon.Size);
                        } catch (Exception e)
                        {
                            Logger.Warn($"Failed to verify icon file size for {icon.Url} through FileSize with error {e.Message}");
                        }
                        break;

                    case CachedIconVerificationMethod.Sha256Checksum:
                        try
                        {
                            byte[] hash = await CalculateFileHashAsync(FilePath);
                            IsFileValid = hash.SequenceEqual(icon.Sha256);
                        }
                        catch (Exception e)
                        {
                            Logger.Warn($"Failed to verify icon file size for {icon.Url} through Sha256 with error {e.Message}");
                        }
                        break;

                    case CachedIconVerificationMethod.PackageVersion:
                        try
                        {
                            if(File.Exists(VersionPath))
                            {
                                var localVersion = File.ReadAllText(VersionPath);
                                IsFileValid = (localVersion == icon.Version);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Warn($"Failed to verify icon file size for {icon.Url} through PackageVersion with error {e.Message}");
                        }
                        break;

                    case CachedIconVerificationMethod.UriSource:
                        try
                        {
                            if (File.Exists(UriPath))
                            {
                                var localVersion = File.ReadAllText(UriPath);
                                IsFileValid = (localVersion == icon.Url.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Warn($"Failed to verify icon file size for {icon.Url} through UriSource with error {e.Message}");
                        }
                        break;

                    default:
                        Logger.ImportantInfo($"Icon {icon.Url} for package {PackageId} on manager {ManagerName} does not use a valid cache verification method");
                        IsFileValid = true;
                        break;
                }

            Logger.Debug($"Icon for package {PackageId} on manager {ManagerName} with Uri={icon.Url} has been determined to be {(IsFileValid? "VALID": "INVALID")} through verification method {icon.VerificationMethod}");

            if(!IsFileValid)
                using (var client = new HttpClient())
                {
                    if(File.Exists(FilePath)) File.Delete(FilePath);
                    HttpResponseMessage response = await client.GetAsync(icon.Url);
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                        using(FileStream fileStream = File.Create(FilePath))
                            await stream.CopyToAsync(fileStream);
                    if (icon.VerificationMethod == CachedIconVerificationMethod.PackageVersion)
                        await File.WriteAllTextAsync(VersionPath, icon.Version);

                    if (icon.VerificationMethod == CachedIconVerificationMethod.UriSource)
                        await File.WriteAllTextAsync(UriPath, icon.Url.ToString());
                }

            Logger.Info($"Icon for package {PackageId} stored on {FilePath}");
            return FilePath;
        }


        private static async Task<byte[]> CalculateFileHashAsync(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
                using (var sha256 = SHA256.Create())
                    return await sha256.ComputeHashAsync(stream);
        }
    }
}
