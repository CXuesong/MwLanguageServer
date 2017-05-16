using System;
using System.Collections.Generic;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Localizable;
using D = MwLanguageServer.Localizable.Diagnostics;

namespace MwLanguageServer.Linter
{
    public class DiagnosticEmitter
    {
        public const string SourceName = "Wikitext";

        public ICollection<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        public void Warn(Range range, string message)
        {
            Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, range, SourceName, message));
        }

        public void Warn(Range range, string format, object arg0)
        {
            Warn(range, string.Format(format, arg0));
        }

        public void Warn(Range range, string format, object arg0, object arg1)
        {
            Warn(range, string.Format(format, arg0, arg1));
        }

        public void Info(Range range, string message)
        {
            Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Information, range, SourceName, message));
        }

        public void Info(Range range, string format, object arg0)
        {
            Info(range, string.Format(format, arg0));
        }

        public void Info(Range range, string format, object arg0, object arg1)
        {
            Info(range, string.Format(format, arg0, arg1));
        }

        public void OpenTagClosedByEndOfLine(Range range)
        {
            Warn(range, D.OpenTagClosedByEndOfLine);
        }

        public void DuplicateTemplateArgument(Range range, string name, string templateName)
        {
            Warn(range, D.DuplicateTemplateArgument, name, templateName);
        }

        public void DuplicateTagAttribute(Range range, string name, string tagName)
        {
            Warn(range, D.DuplicateTagAttribute, name, tagName);
        }

        public void TransclusionNotClosed(Range range, string name)
        {
            Warn(range, D.TransclusionNotClosed, name);
        }

        public void OpenTagNotClosed(Range range, string name)
        {
            Warn(range, D.OpenTagNotClosed, name);
        }

        public void EmptyTransclusionTarget(Range range)
        {
            Warn(range, D.EmptyTransclusionTarget);
        }

        public void EmptyWikilinkTarget(Range range)
        {
            Warn(range, D.EmptyWikilinkTarget);
        }

        public void HardCodedMagicLink(Range range)
        {
            Info(range, D.HardCodedMagicLink);
        }
    }
}
