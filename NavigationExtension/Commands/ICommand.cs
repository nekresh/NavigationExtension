using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationExtension.Commands
{
    public interface ICommand
    {
        void Initialize(IServiceProvider serviceProvider);
    }
}
