using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Localizable;
using Newtonsoft.Json;

namespace MwLanguageServer.Store
{

    public class PageInfo
    {

        public PageInfo(string fullName, string transclusionName, string summary, IReadOnlyList<TemplateArgumentInfo> arguments, bool isTemplate, bool isInferred)
        {
            FullName = fullName;
            TransclusionName = transclusionName;
            Summary = summary;
            Arguments = arguments;
            IsTemplate = isTemplate;
            IsInferred = isInferred;
        }

        public string FullName { get; }

        public string TransclusionName { get; }

        public string Summary { get; }

        public IReadOnlyList<TemplateArgumentInfo> Arguments { get; }

        public bool IsTemplate { get; }

        public bool IsInferred { get; }

        private volatile SignatureInformation signatureCache;

        /// <summary>
        /// To Template signature information.
        /// </summary>
        public SignatureInformation ToSignatureInformation()
        {
            if (signatureCache == null)
            {
                var labelBuilder = new StringBuilder("{{");
                labelBuilder.Append(TransclusionName);
                var sig = new SignatureInformation();
                if (Arguments.Count > 0)
                {
                    sig.Parameters = Arguments.Select(a => a.ToParameterInformation()).ToImmutableArray();
                    foreach (var a in Arguments)
                    {
                        labelBuilder.Append(" |");
                        labelBuilder.Append(a.Name);
                        labelBuilder.Append("=…");
                    }
                }
                labelBuilder.Append("}}");
                sig.Label = labelBuilder.ToString();
                sig.Documentation = Summary;
                signatureCache = sig;
            }
            return signatureCache;
        }
    }

    public class TemplateArgumentInfo
    {
        public TemplateArgumentInfo(string name, string summary) : this(name, summary, false)
        {
        }

        [JsonConstructor]
        public TemplateArgumentInfo(string name, string summary, bool forcePositional)
        {
            Name = name;
            Summary = summary;
            ForcePositional = forcePositional;
        }

        public string Name { get; }

        public string Summary { get; }

        public bool ForcePositional { get; }

        /// <inheritdoc />
        public override string ToString() => "{{{" + Name + "}}}";

        public ParameterInformation ToParameterInformation()
        {
            var sb = new StringBuilder(Summary);
            if (!string.IsNullOrEmpty(Summary)) sb.Append("\n\n");
            sb.Append(Prompts.UsageColon);
            var positional = false;
            if (ForcePositional || int.TryParse(Name, out _))
            {
                positional = true;
                sb.Append("| <…>");
            }
            if (!ForcePositional)
            {
                if (positional) sb.Append(Prompts.Or);
                sb.AppendFormat("|{0}= …", Name);
            }
            return new ParameterInformation(Name, sb.ToString());
        }
    }
}
