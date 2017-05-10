using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string NodeToMd(Node node)
        {
            string type = null, label = null;
            switch (node)
            {
                case Template t:
                    type = Prompts.TemplateNode;
                    label = t.Name == null ? null : MwParserUtility.NormalizeTemplateArgumentName(t.Name);
                    break;
                case TemplateArgument ta:
                    type = Prompts.TemplateArgumentNode;
                    label = ta.Name?.ToPlainText().Trim();
                    break;
                case ArgumentReference ar:
                    type = Prompts.TemplateArgumentNode;
                    label = ar.Name?.ToPlainText().Trim();
                    break;
                case WikiLink wl:
                    type = Prompts.WikiLinkNode;
                    label = wl.Target == null ? null : MwParserUtility.NormalizeTitle(wl.Target);
                    break;
                case ExternalLink el:
                    type = Prompts.ExternalLinkNode;
                    label = el.Target?.ToPlainText().Trim();
                    break;
                case FormatSwitch fs:
                    type = Prompts.FormatSwitchNode;
                    if (fs.SwitchBold) label = Prompts.Bold;
                    if (fs.SwitchItalics) label += " " + Prompts.Italics;
                    break;
                default:
                    type = node.GetType().Name;
                    break;
            }
            if (label == null) return "**" + type + "**";
            return $"**{type}** {label}";
        }
    }
}
