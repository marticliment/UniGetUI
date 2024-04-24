using System.Collections.Generic;
using UniGetUI.PackageEngine.Classes;
using Xunit;
using Assert = Xunit.Assert;

namespace ModernWindow.PackageEngine.Managers.Tests
{

    public abstract class PackageManagerTest<T> where T : IPackageManager, new()
    {
        private static readonly T _manager = new();

        protected virtual Dictionary<string, string> PackageDetails { get; } = new();

        [Fact()]
        public void ParsePackageDetailsTEst()
        {
            //arrange
            var subject = _manager;

            //act
            var actual = subject.Name;

            //assert
            Assert.NotNull(actual);
            Assert.NotEmpty(actual);
        }

        //[Theory()]
        //public void ParseTests(string packagename) { 
        //}
    }

    public class DummyPackageManager : IPackageManager
    {
        string IPackageManager.Name => "Dummy";
    }

    public class DummyPackageManagerTest : PackageManagerTest<DummyPackageManager> { 
    }

    //public class DotnetTests : PackageManagerTest<Dotnet>
    //{
    //    protected override Dictionary<string, string> PackageDetails => new() { { "dotnet-tools-outdated", """
    //            <?xml version="1.0" encoding="utf-8"?><entry xml:base="https://www.nuget.org/api/v2" xmlns="http://www.w3.org/2005/Atom" xmlns:d="http://schemas.microsoft.com/ado/2007/08/dataservices" xmlns:m="http://schemas.microsoft.com/ado/2007/08/dataservices/metadata" xmlns:georss="http://www.georss.org/georss" xmlns:gml="http://www.opengis.net/gml"><id>https://www.nuget.org/api/v2/Packages(Id='dotnet-tools-outdated',Version='0.1.0')</id><category term="NuGetGallery.OData.V2FeedPackage" scheme="http://schemas.microsoft.com/ado/2007/08/dataservices/scheme" /><link rel="edit" href="https://www.nuget.org/api/v2/Packages(Id='dotnet-tools-outdated',Version='0.1.0')" /><link rel="self" href="https://www.nuget.org/api/v2/Packages(Id='dotnet-tools-outdated',Version='0.1.0')" /><title type="text">dotnet-tools-outdated</title><updated>2020-01-14T20:59:44Z</updated><author><name>Mojmir Rychly</name></author><content type="application/zip" src="https://www.nuget.org/api/v2/package/dotnet-tools-outdated/0.1.0" /><m:properties><d:Id>dotnet-tools-outdated</d:Id><d:Version>0.1.0</d:Version><d:NormalizedVersion>0.1.0</d:NormalizedVersion><d:Authors>Mojmir Rychly</d:Authors><d:Copyright m:null="true" /><d:Created m:type="Edm.DateTime">2020-01-14T20:59:44.11+00:00</d:Created><d:Dependencies></d:Dependencies><d:Description>Checks if any of installed .NET Core CLI tools are outdated

    //            Example: dotnet-tools-outdated</d:Description><d:DownloadCount m:type="Edm.Int64">196290</d:DownloadCount><d:GalleryDetailsUrl>https://www.nuget.org/packages/dotnet-tools-outdated/0.1.0</d:GalleryDetailsUrl><d:IconUrl m:null="true" /><d:IsLatestVersion m:type="Edm.Boolean">false</d:IsLatestVersion><d:IsAbsoluteLatestVersion m:type="Edm.Boolean">false</d:IsAbsoluteLatestVersion><d:IsPrerelease m:type="Edm.Boolean">false</d:IsPrerelease><d:Language m:null="true" /><d:LastUpdated m:type="Edm.DateTime">2020-01-14T20:59:44.11+00:00</d:LastUpdated><d:Published m:type="Edm.DateTime">2020-01-14T20:59:44.11+00:00</d:Published><d:PackageHash>xh2sfq8ArMUI4nIeSmsOdSagU2VAJhezSxomfpDSoVzDZI/RPRynLwUxG+vNTcmYTxB8upmEsIH9MuqdXnG/bQ==</d:PackageHash><d:PackageHashAlgorithm>SHA512</d:PackageHashAlgorithm><d:PackageSize m:type="Edm.Int64">5493105</d:PackageSize><d:ProjectUrl>https://github.com/rychlym/dotnet-tools-outdated</d:ProjectUrl><d:ReportAbuseUrl>https://www.nuget.org/packages/dotnet-tools-outdated/0.1.0/ReportAbuse</d:ReportAbuseUrl><d:ReleaseNotes m:null="true" /><d:RequireLicenseAcceptance m:type="Edm.Boolean">false</d:RequireLicenseAcceptance><d:Summary></d:Summary><d:Tags>dotnet core tools outdated</d:Tags><d:Title>dotnet-tools-outdated</d:Title><d:VersionDownloadCount m:type="Edm.Int64">782</d:VersionDownloadCount><d:MinClientVersion m:null="true" /><d:LastEdited m:type="Edm.DateTime">2020-01-14T21:25:24.033+00:00</d:LastEdited><d:LicenseUrl>https://www.nuget.org/packages/dotnet-tools-outdated/0.1.0/license</d:LicenseUrl><d:LicenseNames m:null="true" /><d:LicenseReportUrl m:null="true" /></m:properties></entry>
    //            """ } };
    //}


}