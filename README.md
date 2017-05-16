# MwLanguageServer

A work in (slow) progress .NET [Language Server](https://github.com/Microsoft/language-server-protocol) and client-side plugin of [Wikitext](https://en.wikipedia.org/wiki/Wiki_markup) for VSCode. The client-side plugin for VSCode is named Wikitext. Both the Language Server and the client-side plugin are at their very early stage.

![Snapshot-TemplateHint2](E:\My Files\Visual Studio 2017\Projects\WikiClient\MwLanguageServer\README.resource\Snapshot-TemplateHint1.gif)

Emm, in fact I only copied documentations for a small number of magic words by hand… That's an issue to resolve.

For now, the plugin supports

*   Basic syntax highlight (depends on Jake Boone's [Mediawiki](https://marketplace.visualstudio.com/items?itemName=jakeboone02.mediawiki) plugin)
*   Limited template auto-completion and parameter hints
*   Basic linting for possible syntax errors

Currently, the plugin only supports local files, but later (maybe in the coming months) it might support real-time template and page information fetching, which will allow for more accurate hints.

See [README.resource](README.resource) folder for more snapshots.