using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using UniGetUI.Core;

namespace UniGetUI.PackageEngine.Classes
{

    public class InvalidBundledPackage : BundledPackage
    {
        string __source;
        string __manager;

        public override string SourceAsString
        {
            get
            {
                if (__source == "")
                    return __manager;
                return __manager + ": " + __source;
            }
        }

        public override string IconId
        {
            get
            {
                return "help";
            }
        }

        public InvalidBundledPackage(string name, string id, string version, string source, string manager) : this(new Package(name, id, version, AppTools.Instance.App.Winget.MainSource, AppTools.Instance.App.Winget))
        {
            IsValid = false;
            DrawOpacity = 0.5;
            __source = source;
            __manager = manager;
        }
        public InvalidBundledPackage(Package package) : base(package, new InstallationOptions(package, reset: true), new SerializableUpdatesOptions_v1())
        {
            IsValid = false;
            DrawOpacity = 0.5;
            if (!package.Manager.Capabilities.SupportsCustomSources)
            {
                __source = "";
                __manager = package.Manager.Name;
            }
            else if (package.Source.IsVirtualManager)
            {
                __source = "";
                __manager = package.Source.Name;
            }
            else
            {
                __source = package.Source.Name;
                __manager = package.Manager.Name;
            }
        }
        public async override void ShowOptions(object sender, RoutedEventArgs e)
        {
            await Task.Delay(0);
        }

        public override SerializableValidPackage_v1 AsSerializable()
        {
            throw new System.Exception("Cannot serialize an invalid package as a bundled package. Call Serialized_Incompatible() instead ");
        }
        public override SerializableIncompatiblePackage_v1 AsSerializable_Incompatible()
        {
            SerializableIncompatiblePackage_v1 Serializable = new();
            Serializable.Id = Id;
            Serializable.Name = Name;
            Serializable.Version = version;
            Serializable.Source = SourceAsString;
            return Serializable;
        }


    }

}
