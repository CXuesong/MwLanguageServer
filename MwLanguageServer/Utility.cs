using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Configuration;
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
    }
}
