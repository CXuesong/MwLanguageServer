using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Localizable;
using MwLanguageServer.Store.Contracts;
using System.Linq;

namespace MwLanguageServer
{
    public static class StoreExtensions
    {
        public static ParameterInformation ToParameterInformation(this TemplateArgumentInfo argumentInfo, PageType pageType)
        {
            if (argumentInfo == null) throw new ArgumentNullException(nameof(argumentInfo));
            var sb = new StringBuilder(argumentInfo.Summary);
            if (!string.IsNullOrEmpty(argumentInfo.Summary)) sb.Append("\n\n");
            sb.Append(Prompts.UsageColon);
            var positional = int.TryParse(argumentInfo.Name, out _);
            if (positional || pageType == PageType.MagicWord)
            {
                sb.AppendFormat("| <{0}>", argumentInfo.Name);
            }
            if (pageType != PageType.MagicWord)
            {
                if (positional) sb.Append(Prompts.Or);
                sb.AppendFormat("|{0}= …", argumentInfo.Name);
            }
            return new ParameterInformation(argumentInfo.Name, sb.ToString());
        }

        /// <summary>
        /// To Template signature information.
        /// </summary>
        public static SignatureInformation ToSignatureInformation(this PageInfo pageInfo, PageInfo redirectSource)
        {
            if (pageInfo == null) throw new ArgumentNullException(nameof(pageInfo));
            var labelBuilder = new StringBuilder("{{");
            labelBuilder.Append(pageInfo.TransclusionName);
            var sig = new SignatureInformation();
            if (pageInfo.Arguments.Count > 0)
            {
                sig.Parameters = pageInfo.Arguments.Select(a => a.ToParameterInformation(pageInfo.Type))
                    .ToImmutableArray();
                foreach (var a in pageInfo.Arguments)
                {
                    if (pageInfo.Type == PageType.MagicWord)
                        labelBuilder.AppendFormat(" |<{0}>", a.Name);
                    else
                        labelBuilder.AppendFormat(" |{0}=…", a.Name);
                }
            }
            labelBuilder.Append("}}");
            sig.Label = labelBuilder.ToString();
            sig.Documentation = pageInfo.Summary;
            if (redirectSource != null)
            {
                sig.Documentation = string.Format("{0}{1} -> {2}\r\n", Prompts.RedirectColon, redirectSource, pageInfo)
                    + sig.Documentation;
            }
            return sig;
        }
    }
}

