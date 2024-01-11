using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernWindow.Structures
{
    internal interface ISettingsManipulator
    {
        public bool GetSettings(string setting);
        public void SetSettings(string setting, bool value);
    }
}
