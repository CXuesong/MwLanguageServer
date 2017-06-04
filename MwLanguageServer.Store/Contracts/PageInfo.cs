using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MwLanguageServer.Store.Contracts
{

    public enum PageType
    {
        Page = 0,
        MagicWord = 1,
        Template = 2,
    }

    public enum CaseSensitivity
    {
        CaseSensitive = 0,
        CaseInsensitive,
        InitialCaseSensitive
    }

    public class PageInfo
    {

        private static readonly TemplateArgumentInfo[] EmptyTemplates = { };

        public PageInfo(string fullName, string transclusionName, string redirectTarget, string summary, IReadOnlyList<TemplateArgumentInfo> arguments, PageType type, bool isInferred)
        {
            FullName = fullName;
            TransclusionName = transclusionName;
            RedirectTarget = redirectTarget;
            Summary = summary;
            Arguments = arguments ?? EmptyTemplates;
            Type = type;
            IsInferred = isInferred;
        }

        /// <summary>
        /// Full name of the page.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Page name used for transclusion.
        /// </summary>
        public string TransclusionName { get; }

        /// <summary>
        /// Full name of the redirect target.
        /// </summary>
        public string RedirectTarget { get; }

        /// <summary>
        /// Page summary.
        /// </summary>
        public string Summary { get; }

        public IReadOnlyList<TemplateArgumentInfo> Arguments { get; }

        public PageType Type { get; }

        /// <summary>
        /// Determines whether the page information is inferred from usage.
        /// </summary>
        public bool IsInferred { get; }

        public override string ToString()
        {
            switch (Type)
            {
                case PageType.Template:
                case PageType.MagicWord:
                    return "{{" + TransclusionName + "}}";
                default:
                    return "[[" + FullName + "]]";
            }
        }

    }

    public class TemplateArgumentInfo
    {

        [JsonConstructor]
        public TemplateArgumentInfo(string name, string summary)
        {
            Name = name;
            Summary = summary;
        }

        /// <summary>
        /// Argument name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Argument summary.
        /// </summary>
        public string Summary { get; }

        /// <inheritdoc />
        public override string ToString() => "{{{" + Name + "}}}";
    }
}
