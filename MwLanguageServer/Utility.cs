using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using MwLanguageServer.Localizable;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace MwLanguageServer
{
    static class Utility
    {

        public static readonly JsonSerializer CamelCaseJsonSerializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static void LogException(this ILogger logger, Exception ex, [CallerMemberName] string caller = null)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            logger.Error(ex, "Error in {method}.", caller);
        }

        public static string[] ProcessCommandlineArguments(IEnumerable<string> args)
        {
            return args.Select(a =>
                {
                    if (!a.Contains('=')) a = a + "=true";
                    return a;
                })
                .ToArray();
        }

        public static string ArgumentName(this TemplateArgument arg)
        {
            if (arg.Name != null) return MwParserUtility.NormalizeTemplateArgumentName(arg.Name);
            var parent = arg.ParentNode as Template;
            if (parent == null) return null;
            var unnamedCt = 0;
            foreach (var a in parent.Arguments)
            {
                if (a.Name == null) unnamedCt++;
                if (a == arg) return unnamedCt.ToString();
            }
            return null;
        }

        public static string EscapeMd(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var builder = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (c == '*' || c == '_' || c == '`' || c == '\\')
                    builder.Append('\\');
                builder.Append(c);
            }
            return builder.ToString();
        }

        private static string WikiTitleMd(Node node)
        {
            IWikitextSpanInfo span = node;
            if (node == null || span.HasSpanInfo && span.Length == 0) return "…";
            return EscapeMd(MwParserUtility.NormalizeTitle(node));
        }

        private static string WikiArgumentMd(Node node)
        {
            IWikitextSpanInfo span = node;
            if (node == null || span.HasSpanInfo && span.Length == 0) return "…";
            return EscapeMd(MwParserUtility.NormalizeTemplateArgumentName(node));
        }

        public static string NodeToMd(Node node)
        {
            string label;
            switch (node)
            {
                case Template t:
                    label = $"{{{{**{WikiTitleMd(t.Name)}**}}}}";
                    break;
                case TemplateArgument ta:
                    var tp = ta.ParentNode as Template;
                    label = $"{{{{{WikiTitleMd(tp?.Name)} | **{EscapeMd(ta.ArgumentName())}**=…}}}}";
                    break;
                case ArgumentReference ar:
                    label = $"{{{{{{**{WikiArgumentMd(ar.Name)}**}}}}}}";
                    break;
                case WikiLink wl:
                    label = $"[[{WikiTitleMd(wl.Target)}]]";
                    break;
                case ExternalLink el:
                    label = $"[{EscapeMd(el.Target?.ToString().Trim())}]";
                    break;
                case FormatSwitch fs:
                    label = fs.ToString();
                    break;
                case TagNode tn:
                    label = $"&lt;**{EscapeMd(tn.Name)}**&gt;";
                    break;
                case TagAttribute ta:
                    var tap = ta.ParentNode as TagNode;
                    label = $"&lt;{EscapeMd(tap?.Name?.Trim())} **{EscapeMd(ta.Name?.ToString().Trim())}**=… &gt;";
                    break;
                case Comment c:
                    label = "&lt;!-- … --&gt;";
                default:
                    label = node.GetType().Name;
                    break;
            }
            return label;
        }
    }
}
