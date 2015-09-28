//------------------------------------------------------------------------------
// <copyright file="GoToImplementationCommands.cs" company="Richemont">
//     Copyright (c) Richemont.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using NavigationExtension.Services;
using System.Composition;

namespace NavigationExtension.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    [Export(typeof(ICommand))]
    public class GoToImplementationCommand : ICommand
    {
        /// <summary>
        /// Go to implementation command ID
        /// </summary>
        public const int cmdidGoToImplementationCommands = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8d22bd8a-9d2a-4f17-ac2b-95bdab02aed7");

        private IServiceProvider serviceProvider;

        [Import]
        public Lazy<IGoToImplementationService> GoToImplementationService { get; set; }

        public void Initialize(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var commandService = serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                commandService.AddCommand(new OleMenuCommand(GoToImplementationExecuted,
                    new CommandID(CommandSet, cmdidGoToImplementationCommands)));
            }
        }

        private void GoToImplementationExecuted(object sender, EventArgs e)
        {
            var view = GetActiveTextView();
            if (view == null)
                return;

            try
            {
                var textView = GetTextViewFromVsTextView(view);
                var sourceText = textView.TextBuffer.CurrentSnapshot.AsText();
                var caretPos = GetCaretPoint(textView, textView.TextBuffer);

                if (caretPos.HasValue)
                {
                    var document = getDocument(sourceText);
                    if (document != null)
                    {
                        if (GoToImplementationService.Value.TryGoToImplementation(document, caretPos.Value, CancellationToken.None))
                        {
                            return;
                        }
                    }
                }
            }
            catch (InvalidOperationException) { }
        }

        static SnapshotPoint? GetCaretPoint(ITextView textView, ITextBuffer subjectBuffer)
        {
            var caret = textView.Caret.Position;
            return MapUpOrDownToBuffer(textView.BufferGraph, caret.BufferPosition, subjectBuffer);
        }

        static SnapshotPoint? MapUpOrDownToBuffer(IBufferGraph bufferGraph, SnapshotPoint point, ITextBuffer destinationBuffer)
        {
            var startBuffer = point.Snapshot.TextBuffer;

            if (startBuffer == destinationBuffer)
            {
                return point;
            }

            // Are we trying to map down or up?
            var startProjBuffer = startBuffer as IProjectionBufferBase;
            if (startProjBuffer != null && IsSourceBuffer(startProjBuffer, destinationBuffer))
            {
                return bufferGraph.MapDownToInsertionPoint(point, PointTrackingMode.Positive, s => s == destinationBuffer.CurrentSnapshot);
            }

            var destProjBuffer = destinationBuffer as IProjectionBufferBase;
            if (destProjBuffer != null && IsSourceBuffer(destProjBuffer, startBuffer))
            {
                return bufferGraph.MapUpToBuffer(point, PointTrackingMode.Positive, PositionAffinity.Predecessor, destinationBuffer);
            }

            return null;
        }

        static bool IsSourceBuffer(IProjectionBufferBase top, ITextBuffer bottom)
        {
            return top.SourceBuffers.Contains(bottom) ||
                top.SourceBuffers.OfType<IProjectionBufferBase>().Any(b => IsSourceBuffer(b, bottom));
        }

        private Document getDocument(SourceText sourceText)
        {
            Workspace workspace;

            if (Workspace.TryGetWorkspace(sourceText.Container, out workspace))
            {
                var id = workspace.GetDocumentIdInCurrentContext(sourceText.Container);
                if (id == null || !workspace.CurrentSolution.ContainsDocument(id))
                    return null;

                var sol = workspace.CurrentSolution.WithDocumentText(id, sourceText, PreservationMode.PreserveIdentity);
                return sol.GetDocument(id);
            }

            return null;
        }

        private IVsTextView GetActiveTextView()
        {
            var selection = this.serviceProvider.GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
            object frameObj = null;

            ErrorHandler.ThrowOnFailure(selection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out frameObj));

            var frame = frameObj as IVsWindowFrame;
            if (frame == null)
                return null;

            return GetActiveView(frame);
        }

        private static IVsTextView GetActiveView(IVsWindowFrame windowFrame)
        {
            if (windowFrame == null)
                throw new ArgumentNullException("windowFrame");

            object pvar;
            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));

            var textView = pvar as IVsTextView;
            if (textView == null)
            {
                var codeWin = pvar as IVsCodeWindow;
                if (codeWin != null)
                    ErrorHandler.ThrowOnFailure(codeWin.GetLastActiveView(out textView));
            }

            return textView;
        }

        private static IWpfTextView GetTextViewFromVsTextView(IVsTextView view)
        {
            if (view == null)
                throw new ArgumentNullException("view");

            var userData = view as IVsUserData;
            if (userData == null)
                throw new InvalidOperationException();

            object objTextViewHost;
            ErrorHandler.ThrowOnFailure(userData.GetData(Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost, out objTextViewHost));

            var textViewHost = objTextViewHost as IWpfTextViewHost;
            if (textViewHost == null)
                throw new InvalidOperationException();

            return textViewHost.TextView;
        }
    }
}
