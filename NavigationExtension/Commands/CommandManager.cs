using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationExtension.Commands
{
    [Export]
    public class CommandManager
    {
        [ImportMany]
        public IEnumerable<ICommand> Commands { get; set; }

        public void Initialize(IServiceProvider serviceProvider)
        {
            foreach (var cmd in Commands)
                cmd.Initialize(serviceProvider);
        }
    }
}
