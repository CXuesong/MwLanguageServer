using JsonRpc.Standard.Contracts;
using LanguageServer.VsCode.Contracts;

namespace MwLanguageServer.Services
{
    [JsonRpcScope(MethodPrefix = "completionItem/")]
    public class CompletionItemService : LanguageServiceBase
    {

    }
}
