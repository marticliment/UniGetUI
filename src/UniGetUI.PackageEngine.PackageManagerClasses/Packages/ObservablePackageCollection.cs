using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.PackageClasses;
using Windows.Devices.Bluetooth.Advertisement;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

#pragma warning disable CS8524
        public bool ListIconShowHighlight
        {
            get => Package.Tag switch
            {
                PackageTag.Default => false,
                PackageTag.AlreadyInstalled => true,
                PackageTag.IsUpgradable => true,
                PackageTag.Pinned => false,
                PackageTag.OnQueue => false,
                PackageTag.BeingProcessed => false,
                PackageTag.Failed => true,
            };
        }

        public string ListedIconId
        {
            get => Package.Tag switch
            {
                PackageTag.Default => "install",
                PackageTag.AlreadyInstalled => "installed",
                PackageTag.IsUpgradable => "update",
                PackageTag.Pinned => "pin_fill",
                PackageTag.OnQueue => "sandclock",
                PackageTag.BeingProcessed => "gears",
                PackageTag.Failed => "stop",
            };
        }

        public string ListedNameTooltip
        {
            get => Package.Tag switch
            {
                PackageTag.Default => Package.Name,
                PackageTag.AlreadyInstalled => CoreTools.Translate("This package is already installed"),
                PackageTag.IsUpgradable => CoreTools.Translate("This package can be upgraded to version {0}", Package.NewVersion),
                PackageTag.Pinned => CoreTools.Translate("Updates for this package are ignored"),
                PackageTag.OnQueue => CoreTools.Translate("This package is on the queue"),
                PackageTag.BeingProcessed => CoreTools.Translate("This package is being processed"),
                PackageTag.Failed => CoreTools.Translate("An error occurred while processing this package"),
            } + " - " + Package.Name;
        }

        public float ListedOpacity
        {
            get => Package.Tag switch
            {
                PackageTag.Default => 1,
                PackageTag.AlreadyInstalled => 1,
                PackageTag.IsUpgradable => 1,
                PackageTag.Pinned => 1,
                PackageTag.OnQueue => .5F,
                PackageTag.BeingProcessed => .5F,
                PackageTag.Failed => 1,
            };
        }

        public int NewVersionLabelWidth { get => Package.IsUpgradable ? 125 : 0; }
        public int NewVersionIconWidth { get => Package.IsUpgradable ? 24 : 0; }

#pragma warning restore CS8524

        public int Index { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public Package Package { get; private set; }
        public PackageWrapper Self { get; private set; }
        public PackageWrapper(Package package)
        {
            Package = package;
            Self = this;
            Package.PropertyChanged += Package_PropertyChanged;
        }

        private void Package_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Package.Tag))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListedOpacity)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ListIconShowHighlight)));
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
    }

    /// <summary>
    /// A special ObservableCollection designed to work with Package objects
    /// </summary>
    public class ObservablePackageCollection : SortableObservableCollection<PackageWrapper>
    {
        public enum Sorter
        {
            Checked,
            Name,
            Id,
            Version,
            NewVersion,
            Source,
        }

        public ObservablePackageCollection()
            : base()
        {
            SortingSelector = x => x.Package.Name;
        }


        /// <summary>
        /// Add a package to the collection
        /// </summary>
        /// <param name="p"></param>
        public void Add(Package p)
        {
            base.Add(new PackageWrapper(p));
        }

        /// <summary>
        /// Sets the property with which to filter the package and sorts the collection
        /// </summary>
        /// <param name="field">The field with which to sort the collection</param>
        public void SetSorter(Sorter field)
        {
            switch (field)
            {
                case Sorter.Checked:
                    SortingSelector = x => x.Package.IsChecked;
                    break;

                case Sorter.Name:
                    SortingSelector = x => x.Package.Name;
                    break;

                case Sorter.Id:
                    SortingSelector = x => x.Package.Id;
                    break;

                case Sorter.Version:
                    SortingSelector = x => x.Package.VersionAsFloat;
                    break;

                case Sorter.NewVersion:
                    SortingSelector = x => x.Package.NewVersionAsFloat;
                    break;

                case Sorter.Source:
                    SortingSelector = x => x.Package.SourceAsString;
                    break;
            }
        }

        /// <summary>
        /// Clears the collection, deleting the wrapper objects in the process
        /// </summary>
        public new void Clear()
        {
            foreach(var wrapper in this)
            {
                wrapper.Dispose();
            }
            base.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Returns a list containing the packwges in this collection
        /// </summary>
        /// <returns></returns>
        public List<Package> GetPackages()
        {
            List<Package> packages = new List<Package>();
            foreach (var wrapper in this)
                    packages.Add(wrapper.Package);
            return packages;
        }

        /// <summary>
        /// Returns a list containing the checked packages on this collection
        /// </summary>
        /// <returns></returns>
        public List<Package> GetCheckedPackages()
        {
            List<Package> packages = new List<Package>();
            foreach (var wrapper in this)
            {
                if (wrapper.Package.IsChecked)
                    packages.Add(wrapper.Package);
            }
            return packages;
        }
        
        /// <summary>
        /// Mark all packages as checked
        /// </summary>
        public void SelectAll()
        {
            foreach (var wrapper in this)
                wrapper.Package.IsChecked = true;
        }
        
        /// <summary>
        /// Mark all packages as unchecked
        /// </summary>
        public void ClearSelection()
        {
            foreach (var wrapper in this)
                wrapper.Package.IsChecked = false;
        }
    }
}
