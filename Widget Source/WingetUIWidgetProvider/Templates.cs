using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Printing.PrintSupport;

namespace WingetUIWidgetProvider
{

    public class Verbs
    {
        public const string Reload = "reload";
        public const string OpenWingetUI = "openwingetui";
        public const string ViewUpdatesOnWingetUI = "viewupdatesonwingetui";
        public const string UpdateAll = "updateall";
        public const string UpdatePackage = "updateindex";
    }

    public class Widgets
    {
        public const string All = "updates_all";
        public const string Winget = "updates_winget";
        public const string Scoop = "updates_scoop";
        public const string Chocolatey = "updates_chocolatey";
        public const string Pip = "updates_pip";
        public const string Npm = "updates_npm";
        public const string Dotnet = "updates_dotnet";
    }

    public class Templates
    {

        public static string GetData_NoWingetUI()
        {
            return "{ \"NoWingetUI\": true }";
        }
        private const string NoWingetUI = @"
            {
                ""type"": ""Container"",
                ""items"": [
                    {
                        ""type"": ""Container"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Could not communicate with WingetUI"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""fontType"": ""Default"",
                                ""size"": ""Default"",
                                ""weight"": ""Bolder""
                            },
                            {
                                ""type"": ""Image"",
                                ""url"": ""https://marticliment.com/resources/wingetui.png"",
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Medium"",
                                ""$when"": ""${$host.widgetSize!=\""small\""}""
                            },
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""WingetUI is required for this widget to work.\nPlease make sure that WingetUI is installed and running on the background"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Small""
                            }
                        ],
                        ""height"": ""stretch""
                    },
                    {
                        ""type"": ""ActionSet"",
                        ""actions"": [
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""Retry"",
                                ""verb"": """ + Verbs.Reload + @"""
                            }
                        ],
                        ""horizontalAlignment"": ""Center"",
                        ""$when"": ""${$host.widgetSize!=\""small\""}""
                    }
                ],
                ""verticalContentAlignment"": ""Center"",
                ""style"": ""default"",
                ""id"": ""NoWingetUIDiv"",
                ""height"": ""stretch"",
                ""$when"": ""${$root.NoWingetUI}""
            }";

        public static string GetData_IsLoading()
        {
            return "{ \"IsLoading\": true }";
        }
        private const string IsLoading = @"
            {
                ""type"": ""Container"",
                ""items"": [
                    {
                        ""type"": ""Container"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Checking for updates..."",
                                ""wrap"": true,
                                ""fontType"": ""Default"",
                                ""size"": ""Default"",
                                ""weight"": ""Bolder"",
                                ""horizontalAlignment"": ""Center""
                            },
                            {
                                ""type"": ""Image"",
                                ""url"": ""https://marticliment.com/resources/widgets/sandclock.png"",
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Medium"",
                                ""$when"": ""${$host.widgetSize!=\""small\""}""
                            },
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""This won't take long"",
                                ""wrap"": true,
                                ""size"": ""Small"",
                                ""horizontalAlignment"": ""Center""
                            }
                        ],
                        ""verticalContentAlignment"": ""Center"",
                        ""height"": ""stretch""
                    }
                ],
                ""id"": ""LoadingDiv"",
                ""height"": ""stretch"",
                ""verticalContentAlignment"": ""Center"",
                ""$when"": ""${$root.IsLoading}""
            }";


        public static string GetData_NoUpdatesFound()
        {
            return "{ \"NoUpdatesFound\": true }";
        }
        private const string NoUpdatesFound = @"
            {
                ""type"": ""Container"",
                ""items"": [
                    {
                        ""type"": ""Container"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Hooray! No updates were found!"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""fontType"": ""Default"",
                                ""size"": ""Default"",
                                ""weight"": ""Bolder""
                            },
                            {
                                ""type"": ""Image"",
                                ""url"": ""https://marticliment.com/resources/widgets/laptop_checkmark.png"",
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Medium"",
                                ""$when"": ""${$host.widgetSize!=\""small\""}""
                            },
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Everything seems to be up-to-date"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Small"",
                                ""$when"": ""${$host.widgetSize!=\""small\""}""
                            }
                        ],
                        ""height"": ""stretch""
                    },
                    {
                        ""type"": ""ActionSet"",
                        ""actions"": [
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""Check again"",
                                ""verb"": """ + Verbs.Reload + @"""
                            },
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""Show WingetUI"",
                                ""verb"": """ + Verbs.OpenWingetUI + @"""
                            }
                        ],
                        ""horizontalAlignment"": ""Center""
                    }
                ],
                ""verticalContentAlignment"": ""Center"",
                ""style"": ""default"",
                ""id"": ""NoUpdatesDiv"",
                ""$when"": ""${$root.NoUpdatesFound}"",
                ""height"": ""stretch""
            }";


        private static string GeneratePackageStructure(int index)
        {
            string package = @"
                        {
                            ""type"": ""TableRow"",
                            ""cells"": [
                                {
                                    ""type"": ""TableCell"",
                                    ""minHeight"": ""2px"",
                                    ""items"": [
                                        {
                                            ""type"": ""Image"",
                                            ""url"": ""${Icon"+index.ToString()+ @"}"",
                                            ""horizontalAlignment"": ""Center"",
                                            ""width"": ""24px"",
                                            ""spacing"": ""Padding""
                                        }
                                    ],
                                    ""backgroundImage"": {
                                        ""verticalAlignment"": ""Center""
                                    },
                                    ""verticalContentAlignment"": ""Center""
                                },
                                {
                                    ""type"": ""TableCell"",
                                    ""items"": [
                                        {
                                            ""type"": ""Container"",
                                            ""items"": [
                                                {
                                                    ""type"": ""TextBlock"",
                                                    ""text"": ""${PackageName" + index.ToString()+ @"}"",
                                                    ""wrap"": false,
                                                    ""spacing"": ""None"",
                                                    ""fontType"": ""Default"",
                                                    ""size"": ""Small"",
                                                    ""weight"": ""Bolder"",
                                                    ""color"": ""Accent""
                                                },
                                                {
                                                    ""type"": ""TextBlock"",
                                                    ""text"": ""From ${Version"+index.ToString()+@"} to ${NewVersion"+index.ToString()+ @"}"",
                                                    ""fontType"": ""Default"",
                                                    ""size"": ""Small"",
                                                    ""weight"": ""Lighter"",
                                                    ""isSubtle"": true,
                                                    ""spacing"": ""None"",
                                                    ""wrap"": false,
                                                    ""style"": ""default""
                                                }
                                            ]
                                        }
                                    ]
                                },
                                {
                                    ""type"": ""TableCell"",
                                    ""items"": [
                                        {
                                            ""type"": ""ActionSet"",
                                            ""actions"": [
                                                {
                                                    ""type"": ""Action.Execute"",
                                                    ""title"": ""↻"",
                                                    ""verb"": """ + Verbs.UpdatePackage + index.ToString() + @""",
                                                    ""data"": {},
                                                    ""tooltip"": ""Update this package""
                                                }
                                            ],
                                            ""horizontalAlignment"": ""Center""
                                        }
                                    ],
                                    ""verticalContentAlignment"": ""Center"",
                                    ""minHeight"": ""36px""
                                }
                            ],
                        ""$when"": ""${$root.Package" + index.ToString()+@"Visisble}""
                        }";
            return package;
        }

        public static string GetData_UpdatesList(int count, Package[] upgradablePackages)
        {
            string data = @"
                { 
                    ""UpdatesList"": true,  
                    ""count"": """ + count.ToString() + @"""";
            int maxPos = 0;
            for(int i = 0; i<upgradablePackages.Length; i++)
            {
                if (upgradablePackages[i] != null)
                {
                    data += @",
                        ""Package"+i.ToString()+ @"Visisble"": true,
                        ""PackageName"+i.ToString()+@""": """ + upgradablePackages[i].Name + @""",
                        ""Version"+i.ToString()+@""": """ + upgradablePackages[i].Version + @""",
                        ""Icon"+i.ToString()+@""": """ + upgradablePackages[i].Icon + @""",
                        ""NewVersion"+i.ToString()+@""": """ + upgradablePackages[i].NewVersion + @"""";
                    maxPos = i;
                }
            }

            if ((upgradablePackages.GetLength(0) - maxPos) > 1)
            {
                data += ",\"upgradablePackages\": \"" + (upgradablePackages.GetLength(0) - maxPos - 1) + " packages more can be updated\"}";
            }
            else
            {
                data += ",\"upgradablePackages\": \"\\n\"}";
            }

            return data;
        }

        private static string UpdatesList = @"
            {
                ""type"": ""Container"",
                ""items"": [
                    {
                        ""type"": ""TextBlock"",
                        ""text"": ""Available Updates: ${count}"",
                        ""wrap"": true,
                        ""weight"": ""Bolder"",
                        ""size"": ""Medium""
                    },
                    {
                        ""type"": ""Table"",
                        ""columns"": [
                            {
                                ""width"": ""32px""
                            },
                            {
                                ""width"": 1
                            },
                            {
                                ""width"": ""34px""
                            }
                        ],
                        ""rows"": [
                            " + GeneratePackageStructure(0) + @",
                            " + GeneratePackageStructure(1) + @",
                            " + GeneratePackageStructure(2) + @",
                            " + GeneratePackageStructure(3) + @",
                            " + GeneratePackageStructure(4) + @",
                            " + GeneratePackageStructure(5) + @",
                            " + GeneratePackageStructure(6) + @",
                            " + GeneratePackageStructure(7) + @"
                        ]
                    },

                    {
                        ""type"": ""Container"",
                        ""verticalContentAlignment"": ""Center"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""${upgradablePackages}"",
                                ""horizontalAlignment"": ""Center"",
                                ""spacing"": ""None"",
                                ""weight"": ""Lighter"",
                                ""isSubtle"": true
                            }
                        ],
                        ""height"": ""stretch"",
                        ""spacing"": ""None""
                    },
                    {
                        ""type"": ""ActionSet"",
                        ""actions"": [
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""Update all"",
                                ""verb"": """ + Verbs.UpdateAll + @"""
                            },
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""Reload"",
                                ""verb"": """ + Verbs.Reload + @"""
                            },
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""WingetUI"",
                                ""verb"": """ + Verbs.ViewUpdatesOnWingetUI + @"""
                            }
                        ],
                        ""id"": ""buttons"",
                        ""horizontalAlignment"": ""Center"",
                        ""spacing"": ""None""
                    }
                ],
                ""height"": ""stretch"",
                ""$when"": ""${$root.UpdatesList}""
            }";

        public static string GetData_ErrorOccurred(string error)
        {
            return "{ \"ErrorOccurred\": true, \"errorcode\": \""+ error + "\"}";
        }
        private const string ErrorOccurred = @"
            {
                ""type"": ""Container"",
                ""items"": [
                    {
                        ""type"": ""Container"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Woops! Something went wrong!"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""fontType"": ""Default"",
                                ""size"": ""Default"",
                                ""weight"": ""Bolder""
                            },
                            {
                                ""type"": ""Image"",
                                ""url"": ""https://marticliment.com/resources/error.png"",
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Medium"",
                                ""$when"": ""${$host.widgetSize!=\""small\""}""
                            },
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""An error occurred with this widget: ${errorcode}"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Small"",
                                ""$when"": ""${$host.widgetSize!=\""small\""}""
                            }
                        ],
                        ""height"": ""stretch""
                    },
                    {
                        ""type"": ""ActionSet"",
                        ""actions"": [
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""Try again"",
                                ""verb"": """ + Verbs.Reload + @"""
                            }
                        ],
                        ""horizontalAlignment"": ""Center""
                    }
                ],
                ""verticalContentAlignment"": ""Center"",
                ""style"": ""default"",
                ""id"": ""ErrorOccurredDiv"",
                ""$when"": ""${$root.ErrorOccurred}"",
                ""height"": ""stretch""
            }";

        public static string GetData_UpdatesInCourse()
        {
            return "{ \"UpdatesInCourse\": true }";
        }
        private const string UpdatesInCourse = @"
            {
                ""type"": ""Container"",
                ""items"": [
                    {
                        ""type"": ""Container"",
                        ""items"": [
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""Your packages are being updated!"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""fontType"": ""Default"",
                                ""size"": ""Default"",
                                ""weight"": ""Bolder""
                            },
                            {
                                ""type"": ""Image"",
                                ""url"": ""https://marticliment.com/resources/widgets/laptop_checkmark.png"",
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Medium"",
                                ""$when"": ""${$host.widgetSize!=\""small\""}""
                            },
                            {
                                ""type"": ""TextBlock"",
                                ""text"": ""The updates will be ready soon. You can check their progress on WingetUI"",
                                ""wrap"": true,
                                ""horizontalAlignment"": ""Center"",
                                ""size"": ""Small""
                            }
                        ],
                        ""height"": ""stretch""
                    },
                    {
                        ""type"": ""ActionSet"",
                        ""actions"": [
                            {
                                ""type"": ""Action.Execute"",
                                ""title"": ""Refresh"",
                                ""verb"": """ + Verbs.Reload + @"""
                            }
                        ],
                        ""horizontalAlignment"": ""Center"",
                        ""$when"": ""${$host.widgetSize!=\""small\""}""
                    }
                ],
                ""verticalContentAlignment"": ""Center"",
                ""style"": ""default"",
                ""id"": ""UpdatesOnTheGo"",
                ""height"": ""stretch"",
                ""$when"": ""${$root.UpdatesInCourse}""
            }";


        public const string BaseTemplate = @"
            {
                ""type"": ""AdaptiveCard"",
                ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
                ""version"": ""1.5"",
                ""body"": [
                    " + NoWingetUI + @",
                    " + IsLoading + @",
                    " + NoUpdatesFound + @",
                    " + UpdatesInCourse + @",
                    " + ErrorOccurred + @"
                ],
                ""rtl"": false,
                ""refresh"": {
                    ""action"": {
                        ""type"": ""Action.Execute"",
                        ""verb"": """ + Verbs.Reload + @"""
                    }
                }
            }";

        public static string UpdatesTemplate = @"
            {
                ""type"": ""AdaptiveCard"",
                ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
                ""version"": ""1.5"",
                ""body"": [
                    " + UpdatesList + @"
                ],
                ""rtl"": false,
                ""refresh"": {
                    ""action"": {
                        ""type"": ""Action.Execute"",
                        ""verb"": """ + Verbs.Reload + @"""
                    }
                }
            }";


    }
}
