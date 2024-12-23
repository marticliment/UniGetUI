using Microsoft.UI.Xaml.Controls;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.Widgets
{
    public partial class PackageItemContainer : ItemContainer
    {
        public IPackage? Package { get; set; }
        public PackageWrapper Wrapper { get; set; } = null!;
    }
}
