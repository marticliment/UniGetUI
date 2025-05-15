using UniGetUI.Core.Classes;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    /// <summary>
    /// A special ObservableCollection designed to work with Package objects
    /// </summary>
    public partial class ObservablePackageCollection : SortableObservableCollection<PackageWrapper>
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
        public Sorter CurrentSorter { get; private set; }

        public ObservablePackageCollection()
        {
            CurrentSorter = Sorter.Name;
            SortingSelector = x => x.Package.Name;
        }

        public void FromRange(IReadOnlyList<PackageWrapper> packages)
        {
            BlockSorting = true;

            // Clear the list
            Clear();

            // Add all packages
            foreach (var w in packages)
                Add(w);

            BlockSorting = false;
            Sort();
        }


        /// <summary>
        /// Sets the property with which to filter the package and sorts the collection
        /// </summary>
        /// <param name="field">The field with which to sort the collection</param>
        ///
        public void SetSorter(Sorter field)
        {
            CurrentSorter = field;
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
                    SortingSelector = x => x.Package.NormalizedVersion;
                    break;

                case Sorter.NewVersion:
                    SortingSelector = x => x.Package.NormalizedNewVersion;
                    break;

                case Sorter.Source:
                    SortingSelector = x => x.Package.Source.AsString_DisplayName;
                    break;
            }
        }

        /// <summary>
        /// Returns a list containing the packages in this collection
        /// </summary>
        public List<IPackage> GetPackages()
        {
            List<IPackage> packages = [];
            foreach (PackageWrapper wrapper in this)
            {
                packages.Add(wrapper.Package);
            }

            return packages;
        }

        /// <summary>
        /// Returns a list containing the checked packages on this collection
        /// </summary>
        public List<IPackage> GetCheckedPackages()
        {
            List<IPackage> packages = [];
            foreach (PackageWrapper wrapper in this)
            {
                if (wrapper.Package.IsChecked)
                {
                    packages.Add(wrapper.Package);
                }
            }
            return packages;
        }

        /// <summary>
        /// Mark all packages as checked
        /// </summary>
        public void SelectAll()
        {
            foreach (PackageWrapper wrapper in this)
            {
                wrapper.IsChecked = true;
            }
        }

        /// <summary>
        /// Mark all packages as unchecked
        /// </summary>
        public void ClearSelection()
        {
            foreach (PackageWrapper wrapper in this)
            {
                wrapper.IsChecked = false;
            }
        }
    }
}
