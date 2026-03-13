using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net.Http;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.Avalonia.Models;

public class PackageWrapper : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public IPackage Package { get; }
    private bool _disposed;

    public bool IsChecked
    {
        get => Package.IsChecked;
        set
        {
            Package.IsChecked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        }
    }

    public string VersionComboString { get; }
    public string ExtendedTooltip { get; }
    public float ListedOpacity { get; private set; } = 1.0f;
    public string ListedNameTooltip { get; private set; } = "";
    public string StatusIcon { get; private set; } = "";

    // ─── Icon support ───────────────────────────────────────────────────

    private Bitmap? _iconBitmap;
    private bool _iconLoaded;

    // Limits concurrent icon loads to avoid flooding the network
    private static readonly SemaphoreSlim _iconSemaphore = new(6, 6);

    // Shared HTTP client
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8),
        DefaultRequestHeaders = { { "User-Agent", "UniGetUI" } },
    };

    // In-memory cache: cache key → Bitmap (shared across all wrappers)
    private static readonly ConcurrentDictionary<string, Bitmap?> _iconMemCache = new();

    // Disk cache directory
    private static readonly string _iconCacheDir = Path.Combine(
        CoreData.UniGetUICacheDirectory_Icons, "_avalonia");

    private static Bitmap? _fallbackIcon;
    private static Bitmap FallbackIcon
    {
        get
        {
            if (_fallbackIcon is null)
            {
                try
                {
                    var uri = new Uri("avares://UniGetUI.Avalonia/Assets/package_color.png");
                    using var stream = global::Avalonia.Platform.AssetLoader.Open(uri);
                    _fallbackIcon = new Bitmap(stream);
                }
                catch
                {
                    _fallbackIcon = null!;
                }
            }
            return _fallbackIcon!;
        }
    }

    public Bitmap? IconBitmap
    {
        get
        {
            if (!_iconLoaded)
            {
                _iconLoaded = true;
                _iconBitmap = FallbackIcon;
                _ = LoadIconAsync();
            }
            return _iconBitmap;
        }
        private set
        {
            _iconBitmap = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconBitmap)));
        }
    }

    private async Task LoadIconAsync()
    {
        await _iconSemaphore.WaitAsync();
        try
        {
            if (_disposed) return;

            // Step 1: Check if backend has a cached icon (disk cache from IconDatabase)
            var uri = await Task.Run(() => Package.GetIconUrlIfAny());
            if (_disposed) return;

            if (uri is not null && uri.Scheme == "file" && File.Exists(uri.LocalPath))
            {
                var bitmap = new Bitmap(uri.LocalPath);
                if (!_disposed)
                    Dispatcher.UIThread.Post(() => IconBitmap = bitmap);
                return;
            }

            // Step 2: Get favicon from the package registry domain
            // Derived from manager name — no details.Load() needed
            string? domain = GetRegistryDomain(Package);
            if (domain is not null)
            {
                var bitmap = await FetchFaviconAsync(domain);
                if (_disposed) return;
                if (bitmap is not null)
                {
                    Dispatcher.UIThread.Post(() => IconBitmap = bitmap);
                    return;
                }
            }

            // Step 3: If details happen to be already loaded, try homepage favicon
            if (Package.Details.IsPopulated && Package.Details.HomepageUrl is { Host: { Length: > 0 } host })
            {
                if (host != domain) // Don't retry the same domain
                {
                    var bitmap = await FetchFaviconAsync(host);
                    if (_disposed) return;
                    if (bitmap is not null)
                    {
                        Dispatcher.UIThread.Post(() => IconBitmap = bitmap);
                        return;
                    }
                }
            }
        }
        catch
        {
            // Keep fallback icon
        }
        finally
        {
            _iconSemaphore.Release();
        }
    }

    /// <summary>
    /// Get the favicon domain for a package based on its manager.
    /// No network call needed — just maps manager name to its registry website.
    /// </summary>
    private static string? GetRegistryDomain(IPackage package)
    {
        string managerName = package.Manager.Name.ToLowerInvariant();
        return managerName switch
        {
            "npm" => "www.npmjs.com",
            "pip" => "pypi.org",
            "homebrew" => "formulae.brew.sh",
            "cargo" => "crates.io",
            ".net tool" or "dotnet" => "www.nuget.org",
            "vcpkg" => "vcpkg.io",
            "winget" => "apps.microsoft.com",
            "scoop" => "scoop.sh",
            "chocolatey" => "community.chocolatey.org",
            _ => null,
        };
    }

    /// <summary>
    /// Fetch a favicon for a domain. Checks memory cache, disk cache, then network.
    /// </summary>
    private static async Task<Bitmap?> FetchFaviconAsync(string domain)
    {
        if (string.IsNullOrEmpty(domain)) return null;

        // Memory cache
        if (_iconMemCache.TryGetValue(domain, out var cached))
            return cached;

        // Disk cache
        string diskPath = Path.Combine(_iconCacheDir, domain + ".png");
        if (File.Exists(diskPath))
        {
            try
            {
                var bitmap = new Bitmap(diskPath);
                _iconMemCache[domain] = bitmap;
                return bitmap;
            }
            catch { /* corrupted, re-fetch */ }
        }

        // Network: Google favicon service (fast, reliable, 64px)
        try
        {
            var data = await _httpClient.GetByteArrayAsync(
                $"https://www.google.com/s2/favicons?domain={domain}&sz=64");

            if (data.Length < 100)
            {
                // Too small = default/empty favicon
                _iconMemCache[domain] = null;
                return null;
            }

            using var stream = new MemoryStream(data);
            var bitmap = new Bitmap(stream);

            // Cache to disk
            Directory.CreateDirectory(_iconCacheDir);
            await File.WriteAllBytesAsync(diskPath, data);

            _iconMemCache[domain] = bitmap;
            return bitmap;
        }
        catch
        {
            _iconMemCache[domain] = null;
            return null;
        }
    }

    // ─── Constructor ────────────────────────────────────────────────────

    public PackageWrapper(IPackage package)
    {
        Package = package;
        Package.PropertyChanged += OnPackagePropertyChanged;

        VersionComboString = package.IsUpgradable
            ? $"{package.VersionString} -> {package.NewVersionString}"
            : package.VersionString;

        ExtendedTooltip = package.Name.Equals(package.Id, StringComparison.OrdinalIgnoreCase)
            ? $"{package.Name} (from {package.Source.AsString_DisplayName})"
            : $"{package.Name} ({package.Id} from {package.Source.AsString_DisplayName})";

        UpdateFromTag();
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Package.Tag))
        {
            UpdateFromTag();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedOpacity)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedNameTooltip)));
        }
        else if (e.PropertyName == nameof(Package.IsChecked))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        }
    }

    private void UpdateFromTag()
    {
        ListedOpacity = Package.Tag switch
        {
            PackageTag.OnQueue or PackageTag.BeingProcessed or PackageTag.Unavailable => 0.5f,
            _ => 1.0f,
        };

        StatusIcon = Package.Tag switch
        {
            PackageTag.AlreadyInstalled => "✓",
            PackageTag.IsUpgradable => "↑",
            PackageTag.Pinned => "📌",
            PackageTag.OnQueue => "⏳",
            PackageTag.BeingProcessed => "⟳",
            PackageTag.Failed => "⚠",
            PackageTag.Unavailable => "?",
            _ => "",
        };

        ListedNameTooltip = Package.Tag switch
        {
            PackageTag.AlreadyInstalled => CoreTools.Translate("This package is already installed") + " - ",
            PackageTag.IsUpgradable => CoreTools.Translate("This package can be upgraded to version {0}",
                Package.GetUpgradablePackage()?.NewVersionString ?? "-1") + " - ",
            PackageTag.Pinned => CoreTools.Translate("Updates for this package are ignored") + " - ",
            PackageTag.OnQueue => CoreTools.Translate("This package is on the queue") + " - ",
            PackageTag.BeingProcessed => CoreTools.Translate("This package is being processed") + " - ",
            PackageTag.Failed => CoreTools.Translate("An error occurred while processing this package") + " - ",
            PackageTag.Unavailable => CoreTools.Translate("This package is not available") + " - ",
            _ => "",
        } + Package.Name;
    }

    public void Dispose()
    {
        _disposed = true;
        Package.PropertyChanged -= OnPackagePropertyChanged;
    }
}
