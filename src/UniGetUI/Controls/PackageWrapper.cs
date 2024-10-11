using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.System.RemoteSystems;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    /// <summary>
    /// A wrapper for packages to be able to show in ItemCollections
    /// </summary>
    public class PackageWrapper : IIndexableListItem, INotifyPropertyChanged, IDisposable
    {
        public bool IsChecked
        {
            get => Package.IsChecked;
            set => Package.IsChecked = value;
        }

        public IconType ListedComplementaryIconId = IconType.Empty;
        private IconType ListedIconId = IconType.Package;
        private IconSourceElement? MainIcon;
        private bool? PackageHasCustomIcon;
        private Uri? PackageIconIfAny;

        public string ListedNameTooltip = "";
        public float ListedOpacity = 1.0f;

        public int NewVersionLabelWidth { get => Package.IsUpgradable ? 125 : 0; }
        public int NewVersionIconWidth { get => Package.IsUpgradable ? 24 : 0; }

        public int Index { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public IPackage Package { get; private set; }
        public PackageWrapper Self { get; private set; }

        public PackageWrapper(IPackage package)
        {
            Package = package;
            Self = this;
            WhenTagHasChanged();
            Package.PropertyChanged += Package_PropertyChanged;
        }

        public void Package_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(Package.Tag))
                {
                    WhenTagHasChanged();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedOpacity)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedComplementaryIconId)));
                    // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedIconId)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedNameTooltip)));
                }
                else if (e.PropertyName == nameof(Package.IsChecked))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
                else
                {
                    PropertyChanged?.Invoke(this, e);
                }
            } catch (COMException)
            {
                // ignore
            }
        }

        public void Dispose()
        {
            Package.PropertyChanged -= Package_PropertyChanged;
        }

        /// <summary>
        /// Updates the fields that change how the item template is rendered.
        /// </summary>
        public void WhenTagHasChanged()
        {
            ListedIconId = Package.Tag switch
            {
                PackageTag.Default => IconType.Package,
                PackageTag.AlreadyInstalled => IconType.Installed,
                PackageTag.IsUpgradable => IconType.Upgradable,
                PackageTag.Pinned => IconType.Pin,
                PackageTag.OnQueue => IconType.SandClock,
                PackageTag.BeingProcessed => IconType.Loading,
                PackageTag.Failed => IconType.Warning,
                PackageTag.Unavailable => IconType.Help,
                _ => throw new ArgumentException($"Unknown tag {Package.Tag}"),
            };
            SetPackageIcon();

            ListedComplementaryIconId = Package.Tag switch
            {
                PackageTag.Default => IconType.Empty,
                PackageTag.AlreadyInstalled => IconType.Installed_Filled,
                PackageTag.IsUpgradable => IconType.Upgradable_Filled,
                PackageTag.Pinned => IconType.Pin_Filled,
                PackageTag.OnQueue => IconType.Empty,
                PackageTag.BeingProcessed => IconType.Loading_Filled,
                PackageTag.Failed => IconType.Warning_Filled,
                PackageTag.Unavailable => IconType.Empty,
                _ => throw new ArgumentException($"Unknown tag {Package.Tag}"),
            };

            ListedNameTooltip = Package.Tag switch
            {
                PackageTag.Default => Package.Name,
                PackageTag.AlreadyInstalled => CoreTools.Translate("This package is already installed"),
                PackageTag.IsUpgradable => CoreTools.Translate("This package can be upgraded to version {0}",
                    Package.GetUpgradablePackage()?.NewVersion ?? "-1"),
                PackageTag.Pinned => CoreTools.Translate("Updates for this package are ignored"),
                PackageTag.OnQueue => CoreTools.Translate("This package is on the queue"),
                PackageTag.BeingProcessed => CoreTools.Translate("This package is being processed"),
                PackageTag.Failed => CoreTools.Translate("An error occurred while processing this package"),
                PackageTag.Unavailable => CoreTools.Translate("This package is not available"),
                _ => throw new ArgumentException($"Unknown tag {Package.Tag}"),
            } + " - " + Package.Name;

            ListedOpacity = Package.Tag switch
            {
                PackageTag.Default => 1,
                PackageTag.AlreadyInstalled => 1,
                PackageTag.IsUpgradable => 1,
                PackageTag.Pinned => 1,
                PackageTag.OnQueue => .5F,
                PackageTag.BeingProcessed => .5F,
                PackageTag.Failed => 1,
                PackageTag.Unavailable => .5F,
                _ => throw new ArgumentException($"Unknown tag {Package.Tag}"),
            };
#pragma warning restore CS8524

        }

        public void SetMainGrid(Grid grid)
        {
            try
            {
                MainIcon = (IconSourceElement)grid.Children[2];
                _ = SetPackageIcon();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public async Task SetPackageIcon()
        {
            if (MainIcon is null) return;


            // Load the package icon information, if any
            if (PackageHasCustomIcon is null)
            {
                if (Settings.Get("DisableIconOnListsSupport"))
                {
                    PackageHasCustomIcon = false;
                }
                else
                {
                    PackageIconIfAny = await Task.Run(Package.GetIconUrlIfAny);
                    PackageHasCustomIcon = PackageIconIfAny is null;
                }
            }

            // Display the appropiate icon
            if (PackageHasCustomIcon is true)
            {
                MainIcon.IconSource = new BitmapIconSource()
                {
                    UriSource = PackageIconIfAny,
                    ShowAsMonochrome = false
                };
            }
            else
            {
                MainIcon.IconSource = new LocalIconSource(ListedIconId);
            }
        }
    }
}
