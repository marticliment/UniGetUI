using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Interface.Enums;

namespace UniGetUI.Controls.OperationWidgets;
public class OperationBadge
{
    public string Tooltip;
    public string PrimaryBanner;
    public string SecondaryBanner;
    public bool SecondaryBannerVisible;
    public IconType Icon;

    public OperationBadge(string tooltip, IconType icon, string primaryBanner, string? secondaryBanner = null)
    {
        Tooltip = tooltip;
        Icon = icon;
        PrimaryBanner = primaryBanner;
        if(secondaryBanner != null)
        {
            SecondaryBanner = secondaryBanner;
            SecondaryBannerVisible = true;
        }
        else
        {
            SecondaryBanner = "";
            SecondaryBannerVisible = false;
        }
    }
}
