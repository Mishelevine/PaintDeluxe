using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PluginInterface
{
    public interface IFilterPlugin
    {
        string Name { get; }
        string Author { get; }
        void Transform(ref InkCanvas canvas);
    }
}
