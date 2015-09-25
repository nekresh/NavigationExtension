using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TextManager.Interop;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace NavigationExtension
{
    public interface INavigationService
    {
        bool TryNavigate(Workspace workspace, DocumentId documentId, TextSpan span, CancellationToken cancellationToken);
    }

    [Export(typeof(INavigationService))]
    internal sealed class NavigationService : INavigationService
    {
        [Import] public SVsServiceProvider ServiceProvider { get; set; }
        [Import] public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }
        
        public  bool TryNavigate(Workspace workspace, DocumentId documentId, TextSpan span, CancellationToken cancellationToken)
        {
            // Ensure that the document is open in the Editor or the navigation will not be possible
            workspace.OpenDocument(documentId);
            var document = workspace.CurrentSolution.GetDocument(documentId);

            var text = WaitAndGetResult(document.GetTextAsync(cancellationToken), cancellationToken);
            var textBuffer = text.Container.GetTextBuffer();

            var vsTextBuffer = EditorAdaptersFactoryService.GetBufferAdapter(textBuffer);

            var linePosition = text.Lines.GetLinePositionSpan(span);
            var vsTextSpan = new VsTextSpan
            {
                iStartLine = linePosition.Start.Line,
                iStartIndex = linePosition.Start.Character,
                iEndLine = linePosition.End.Line,
                iEndIndex = linePosition.End.Character
            };

            var textManager = (IVsTextManager2)ServiceProvider.GetService(typeof(SVsTextManager));
            if (textManager != null)
                return ErrorHandler.Succeeded(
                    textManager.NavigateToLineAndColumn2(vsTextBuffer, VSConstants.LOGVIEWID.TextView_guid,
                        vsTextSpan.iStartLine, vsTextSpan.iStartIndex, vsTextSpan.iEndLine, vsTextSpan.iEndIndex,
                        (uint)_VIEWFRAMETYPE.vftCodeWindow)
                );

            return false;
        }

        static TResult WaitAndGetResult<TResult>(Task<TResult> task, CancellationToken cancellationToken)
        {
            task.Wait(cancellationToken);
            return task.Result;
        }
    }
}
