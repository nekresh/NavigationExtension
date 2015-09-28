using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NavigationExtension.Services
{
    public interface IGoToImplementationService
    {
        bool TryGoToImplementation(Document document, int position, CancellationToken cancellationToken);
    }
}
