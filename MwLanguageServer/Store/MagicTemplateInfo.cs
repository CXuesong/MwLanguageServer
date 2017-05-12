using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;

namespace MwLanguageServer.Store
{
    public class MagicTemplateInfo
    {
        public MagicTemplateInfo(string name, IReadOnlyCollection<string> aliases, string summary, string remarks, bool isCaseSensitive, IReadOnlyCollection<IReadOnlyList<TemplateArgumentInfo>> signatures)
        {
            Name = name;
            Aliases = aliases;
            Summary = summary;
            IsCaseSensitive = isCaseSensitive;
            Signatures = signatures;
            Remarks = remarks;
            if (Signatures == null || Signatures.Count == 0)
                signatureCache = ImmutableArray<SignatureInformation>.Empty;
        }

        public string Name { get; }

        public IReadOnlyCollection<string> Aliases { get; }

        public string Summary { get; }

        public string Remarks { get; }

        public bool IsCaseSensitive { get; }

        public IReadOnlyCollection<IReadOnlyList<TemplateArgumentInfo>> Signatures { get; }

        private volatile IList<SignatureInformation> signatureCache;

        /// <summary>
        /// To Template signature information.
        /// </summary>
        public IList<SignatureInformation> ToSignatureInformation()
        {
            // e.g. {{#if :expression |trueValue |falseValue }}
            if (signatureCache == null)
            {
                signatureCache = Signatures.DefaultIfEmpty().Select(arguments =>
                {
                    var labelBuilder = new StringBuilder("{{");
                    labelBuilder.Append(Name);
                    var sig = new SignatureInformation();
                    if (arguments != null && arguments.Count > 0)
                    {
                        sig.Parameters = arguments.Select(a => a.ToParameterInformation()).ToImmutableArray();
                        var isFirst = true;
                        foreach (var a in arguments)
                        {
                            labelBuilder.Append(isFirst ? " :" : " |");
                            labelBuilder.Append('<');
                            labelBuilder.Append(a.Name);
                            labelBuilder.Append('>');
                            isFirst = false;
                        }
                    }
                    labelBuilder.Append("}}");
                    sig.Label = labelBuilder.ToString();
                    sig.Documentation = Summary;
                    if (!string.IsNullOrEmpty(Remarks)) sig.Documentation += "\n" + Remarks;
                    return sig;
                }).ToImmutableArray();
            }
            return signatureCache;
        }
    }
}
