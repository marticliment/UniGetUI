using System.Security.Cryptography;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;

namespace UniGetUI.Core.IconEngine
{
    public enum IconValidationMethod
    {
        SHA256,
        FileSize,
        PackageVersion,
        UriSource
    }

    public readonly struct CacheableIcon
    {
        public readonly Uri Url;
        public readonly byte[] SHA256 = [];
        public readonly string Version = "";
        public readonly long Size = -1;
        public readonly IconValidationMethod ValidationMethod;

        /// <summary>
        /// Build a cacheable icon with SHA256 verification
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="Sha256"></param>
        public CacheableIcon(Uri uri, byte[] Sha256)
        {
            Url = uri;
            this.SHA256 = Sha256;
            ValidationMethod = IconValidationMethod.SHA256;
        }

        /// <summary>
        /// Build a cacheable icon with Version verification
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="version"></param>
        public CacheableIcon(Uri uri, string version)
        {
            Url = uri;
            Version = version;
            ValidationMethod = IconValidationMethod.PackageVersion;
        }

        /// <summary>
        /// Build a cacheable icon with Size verification
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="size"></param>
        public CacheableIcon(Uri uri, long size)
        {
            Url = uri;
            Size = size;
            ValidationMethod = IconValidationMethod.FileSize;
        }

        /// <summary>
        /// Build a cacheable icon with Uri verification
        /// </summary>
        /// <param name="icon"></param>
        public CacheableIcon(Uri icon)
        {
            Url = icon;
            ValidationMethod = IconValidationMethod.UriSource;
        }
    }

    public static class IconCacheEngine
    {
        /// <summary>
        /// Returns the path to the icon file, downloading it if necessary
        /// </summary>
        /// <param name="_icon">a CacheableIcon object representing the object</param>
        /// <param name="ManagerName">The name of the PackageManager</param>
        /// <param name="PackageId">the Id of the package</param>
        /// <returns>A path to a local icon file</returns>
        public static async Task<string?> GetCacheOrDownloadIcon(CacheableIcon? _icon, string ManagerName, string PackageId)
        {
            if (_icon is null)
            {
                return null;
            }

            var icon = (CacheableIcon)_icon;

            string extension = icon.Url.AbsolutePath[icon.Url.AbsolutePath.LastIndexOf('.')..][1..];

            if (extension.Length > 6)
            {
                Logger.Warn($"Extension {extension} for file url {icon.Url} seems to be invalid, defaulting to .png");
                extension = "png";
            }

            string cachedIconFile = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName, $"{PackageId}.{extension}");
            string iconVersionFile = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName, $"{PackageId}.{extension}.version");
            string iconUriFile = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName, $"{PackageId}.{extension}.uri");
            string iconLocation = Path.Join(CoreData.UniGetUICacheDirectory_Icons, ManagerName);
            if (!Directory.Exists(iconLocation))
            {
                Directory.CreateDirectory(iconLocation);
            }

            // Verify if the cached icon is valid
            bool isLocalCacheValid = false;
            bool localCacheExists = File.Exists(cachedIconFile);
            if (localCacheExists)
            {
                isLocalCacheValid = icon.ValidationMethod switch
                {
                    IconValidationMethod.FileSize => ValidateByImageSize(icon, cachedIconFile),
                    IconValidationMethod.SHA256 => await ValidateBySHA256(icon, cachedIconFile),
                    IconValidationMethod.PackageVersion => await ValidateByVersion(icon, iconVersionFile),
                    IconValidationMethod.UriSource => await ValidateByUri(icon, iconUriFile),
                    _ => throw new InvalidDataException("Invalid icon validation method"),
                };
            }

            // If a valid cache was found, return that cache
            if (isLocalCacheValid)
            {
                Logger.Debug($"Icon for package {PackageId} is VALID and won't be downloaded again (verification method is {icon.ValidationMethod})");
                return cachedIconFile;
                // Exit the function
            }
            else if (localCacheExists)
            {
                Logger.ImportantInfo($"Icon for Package={PackageId} Manager={ManagerName} Uri={icon.Url} is NOT VALID (verification method is {icon.ValidationMethod})");
            }
            else
            {
                Logger.Debug($"Icon for package {PackageId} on manager {ManagerName} was not found on cache, downloading it...");
            }

            // If the cache is determined to NOT be valid, delete cache
            DeteteCachedFiles(cachedIconFile, iconVersionFile, iconUriFile);

            // After discarding the cache, regenerate it
            using HttpClient client = new(CoreData.GenericHttpClientParameters);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
            HttpResponseMessage response = await client.GetAsync(icon.Url);
            response.EnsureSuccessStatusCode();
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using (FileStream fileStream = File.Create(cachedIconFile))
            {
                await stream.CopyToAsync(fileStream);
            }

            if (icon.ValidationMethod is IconValidationMethod.PackageVersion)
                await File.WriteAllTextAsync(iconVersionFile, icon.Version);

            if (icon.ValidationMethod is IconValidationMethod.UriSource)
                await File.WriteAllTextAsync(iconUriFile, icon.Url.ToString());

            Logger.Info($"Icon for package {PackageId} stored on {cachedIconFile}");

            // Ensure the new icon has been properly downloaded
            bool isNewCacheValid = icon.ValidationMethod switch
            {
                IconValidationMethod.FileSize => ValidateByImageSize(icon, cachedIconFile),
                IconValidationMethod.SHA256 => await ValidateBySHA256(icon, cachedIconFile),
                IconValidationMethod.PackageVersion => true, // The validation result would be always true
                IconValidationMethod.UriSource => true, // The validation result would be always true
                _ => throw new InvalidDataException("Invalid icon validation method"),
            };

            if (isNewCacheValid)
            {
                Logger.Info($"NEWLY DOWNLOADED Icon for Package={PackageId} Manager={ManagerName} Uri={icon.Url} is VALID (verification method is {icon.ValidationMethod})");
                return cachedIconFile;
            }
            else
            {
                Logger.Warn($"NEWLY DOWNLOADED Icon for Pacakge={PackageId} Manager={ManagerName} Uri={icon.Url} is NOT VALID and will be discarded (verification method is {icon.ValidationMethod})");
                DeteteCachedFiles(cachedIconFile, iconVersionFile, iconUriFile);
                return null;
            }
        }

        /// <summary>
        /// Checks whether a cached image is valid or not depending on the size (in bytes) of the image
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="cachedIconPath"></param>
        /// <returns></returns>
        private static bool ValidateByImageSize(CacheableIcon icon, string cachedIconPath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(cachedIconPath);
                return icon.Size == fileInfo.Length;
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to verify icon file size for {icon.Url} via FileSize with error {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks whether a cached image is valid or not depending on its SHA256 hash
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="cachedIconPath"></param>
        /// <returns></returns>
        private static async Task<bool> ValidateBySHA256(CacheableIcon icon, string cachedIconPath)
        {
            try
            {
                using FileStream stream = File.OpenRead(cachedIconPath);
                using SHA256 sha256 = SHA256.Create();
                return (await sha256.ComputeHashAsync(stream)).SequenceEqual(icon.SHA256);
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to verify icon file size for {icon.Url} via Sha256 with error {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks whether a cached image is valid or not depending on the package version it was pulled from
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="versionPath"></param>
        /// <returns></returns>
        private static async Task<bool> ValidateByVersion(CacheableIcon icon, string versionPath)
        {
            try
            {
                return File.Exists(versionPath) && (await File.ReadAllTextAsync(versionPath)) == icon.Version;
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to verify icon file size for {icon.Url} via PackageVersion with error {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks whether a cached image is valid or not depending on the URI it was pulled from
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="uriPath"></param>
        /// <returns></returns>
        private static async Task<bool> ValidateByUri(CacheableIcon icon, string uriPath)
        {
            try
            {
                return File.Exists(uriPath) && (await File.ReadAllTextAsync(uriPath)) == icon.Url.ToString();
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to verify icon file size for {icon.Url} via UriSource with error {e.Message}");
                return false;
            }
        }

        private static void DeteteCachedFiles(string iconFile, string versionFile, string uriFile)
        {
            try
            {
                if (File.Exists(iconFile)) File.Delete(iconFile);
                if (File.Exists(versionFile)) File.Delete(versionFile);
                if (File.Exists(uriFile)) File.Delete(uriFile);
            }
            catch (Exception e)
            {
                Logger.Warn($"An error occurred while deleting old icon cache: {e.Message}");
            }
        }
    }
}
