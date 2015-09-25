using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NavigationExtension
{
    public interface IGoToImplementationService
    {
        bool TryGoToImplementation(Document document, int position, CancellationToken cancellationToken);
    }

    [Export(typeof(IGoToImplementationService))]
    internal sealed class GoToImplementationService : IGoToImplementationService
    {
        [Import] public INavigationService NavigationService { get; set; }

        public bool TryGoToImplementation(Document document, int position, CancellationToken cancellationToken)
        {
            var project = document.Project;

            var symbol = WaitAndGetResult(SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken), cancellationToken);
            if (symbol != null)
            {
                if (symbol.IsAbstract && (symbol is IMethodSymbol || symbol is IPropertySymbol))
                {
                    if (symbol.ContainingType.TypeKind == TypeKind.Interface)
                    {
                        var implementations = WaitAndGetResult(SymbolFinder.FindImplementationsAsync(symbol, project.Solution, cancellationToken: cancellationToken), cancellationToken);

                        if (implementations.Count() == 1)
                        {
                            var implementation = implementations.First();
                            return TryGoToImplementation(implementation, project, cancellationToken);
                        }
                    }
                    else if (symbol.ContainingType.TypeKind == TypeKind.Class)
                    {
                        var implementations = WaitAndGetResult(SymbolFinder.FindOverridesAsync(symbol, project.Solution, cancellationToken: cancellationToken), cancellationToken);

                        if (implementations.Count() == 1)
                        {
                            var implementation = implementations.First();
                            return TryGoToImplementation(implementation, project, cancellationToken);
                        }
                    }
                }
                else
                    return TryGoToImplementation(symbol, project, cancellationToken);
            }

            return false;
        }

        private bool TryGoToImplementation(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            symbol = symbol.OriginalDefinition;

            var location = symbol.Locations.Where(loc => loc.IsInSource).FirstOrDefault();
            if (location != null)
            {
                var targetDocument = project.Solution.GetDocument(location.SourceTree);
                var documentId = targetDocument.Id;
                return NavigationService.TryNavigate(project.Solution.Workspace, documentId, location.SourceSpan, cancellationToken);
            }

            return false;
        }

        static TResult WaitAndGetResult<TResult>(Task<TResult> task, CancellationToken cancellationToken)
        {
            task.Wait(cancellationToken);
            return task.Result;
        }
    }
}
