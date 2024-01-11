using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernWindow.Structures
{
    internal interface IEmbeddableWindow
    {
        // Methods needed for Python to be able to embed the window to a custom QWidget
        public int GetHwnd();
        public void ShowWindow_SAFE();
    }
}
