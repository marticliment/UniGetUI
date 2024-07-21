using System.ComponentModel;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;

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

        public string ListedComplementaryIconId = "";
        public string ListedIconId = "";
        public string ListedNameTooltip = "";
        public float ListedOpacity;

        public int NewVersionLabelWidth { get => Package.IsUpgradable ? 125 : 0; }
        public int NewVersionIconWidth { get => Package.IsUpgradable ? 24 : 0; }

        public int Index { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public Package Package { get; private set; }
        public PackageWrapper Self { get; private set; }
        public PackageWrapper(Package package)
        {
            Package = package;
            Self = this;
            WhenTagHasChanged();
            Package.PropertyChanged += Package_PropertyChanged;
        }

        private void Package_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Package.Tag))
            {
                WhenTagHasChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedOpacity)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedComplementaryIconId)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedIconId)));
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
#pragma warning disable CS8524

            ListedComplementaryIconId = Package.Tag switch
            {
                PackageTag.Default => "empty",
                PackageTag.AlreadyInstalled => "installed_filled",
                PackageTag.IsUpgradable => "upgradable_filled",
                PackageTag.Pinned => "pin_filled",
                PackageTag.OnQueue => "empty",
                PackageTag.BeingProcessed => "loading_filled",
                PackageTag.Failed => "warning_filled",
            };

            ListedIconId = Package.Tag switch
            {
                PackageTag.Default => "package",
                PackageTag.AlreadyInstalled => "installed",
                PackageTag.IsUpgradable => "upgradable",
                PackageTag.Pinned => "pin",
                PackageTag.OnQueue => "sandclock",
                PackageTag.BeingProcessed => "loading",
                PackageTag.Failed => "warning",
            };

            ListedNameTooltip = Package.Tag switch
            {
                PackageTag.Default => Package.Name,
                PackageTag.AlreadyInstalled => CoreTools.Translate("This package is already installed"),
                PackageTag.IsUpgradable => CoreTools.Translate("This package can be upgraded to version {0}", Package.GetUpgradablePackage()?.NewVersion ?? "-1"),
                PackageTag.Pinned => CoreTools.Translate("Updates for this package are ignored"),
                PackageTag.OnQueue => CoreTools.Translate("This package is on the queue"),
                PackageTag.BeingProcessed => CoreTools.Translate("This package is being processed"),
                PackageTag.Failed => CoreTools.Translate("An error occurred while processing this package"),
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
            };
#pragma warning restore CS8524
        }
    }
}
