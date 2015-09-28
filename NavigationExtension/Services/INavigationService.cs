using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NavigationExtension.Services
{
    public interface INavigationService
    {
        bool TryNavigate(Workspace workspace, DocumentId documentId, TextSpan span, CancellationToken cancellationToken);
    }
}
