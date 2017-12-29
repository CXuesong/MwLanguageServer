"use strict";
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below

import * as vscode from "vscode";
import * as languageClient from "vscode-languageclient";
import * as path from "path";
import * as fs from "fs";
import * as util from './common';
import { Logger } from "./logger";
import { ExtensionDownloader } from "./ExtensionDownloader";

let _channel: vscode.OutputChannel = null;

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export async function activate(context: vscode.ExtensionContext) {
	console.log('wikitext extension is now activated.');
	
	const extensionId = 'lextudio.restructuredtext';
    const extension = vscode.extensions.getExtension(extensionId);

	util.setExtensionPath(extension.extensionPath);
	
	_channel = vscode.window.createOutputChannel("Wikitext");

    let logger = new Logger(text => _channel.append(text));

	// Uncomment this before shipping.
    // let runtimeDependenciesExist = await ensureRuntimeDependencies(extension, logger);

	activateLanguageServer(context);

	// The command has been defined in the package.json file
	let disposable = vscode.commands.registerCommand('extension.sayHello', () => {
		// The code you place here will be executed every time your command is executed

		// Display a message box to the user
		vscode.window.showInformationMessage('Hello World!');
	});

	context.subscriptions.push(disposable);
}

function ensureRuntimeDependencies(extension: vscode.Extension<any>, logger: Logger): Promise<boolean> {
    return util.installFileExists(util.InstallFileType.Lock)
        .then(exists => {
            if (!exists) {
                const downloader = new ExtensionDownloader(_channel, logger, extension.packageJSON);
                return downloader.installRuntimeDependencies();
            } else {
                return true;
            }
        });
}

const languageServerPaths = [
	".server/MwLanguageServer.exe",
	".server/MwLanguageServer",
	"../../../MwLanguageServer/bin/Debug/netcoreapp2.0/MwLanguageServer.dll"
]

function activateLanguageServer(context: vscode.ExtensionContext) {
	// The server is implemented in an executable application.
	let serverModule: string = null;
	for (let p of languageServerPaths)
	{
		p = context.asAbsolutePath(p);
		// console.log(p);
		if (fs.existsSync(p))
		{
			serverModule = p;
			break;
		}
	}
	if (!serverModule) throw new URIError("Cannot find MwLanguageServer.");
	let workPath = path.dirname(serverModule);
	console.log(`Use ${serverModule} as server module.`);
	console.log(`Work path: ${workPath}.`);
	

	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	let serverOptions: languageClient.ServerOptions = {
		run: { command: serverModule, args: [], options: { cwd: workPath } },
		debug: { command: serverModule, args: ["--debug"], options: { cwd: workPath } }
	}

	// For debugging only.
	if (serverModule.indexOf(".dll") > -1)
	{
		serverOptions = {
			run: { command: "dotnet", args: [serverModule], options: { cwd: workPath } },
			debug: { command: "dotnet", args: [serverModule, "--debug"], options: { cwd: workPath } }
		}
	}

	// Options to control the language client
	let clientOptions: languageClient.LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: ["wikitext", "mediawiki"],
		synchronize: {
			// Synchronize the setting section 'languageServerExample' to the server
			configurationSection: 'wikitextLanguageServer',
			// Notify the server about file changes to '.clientrc files contain in the workspace
			fileEvents: vscode.workspace.createFileSystemWatcher('**/.clientrc')
		},
	}

	// Create the language client and start the client.
	let client = new languageClient.LanguageClient('wikitextLanguageServer', 'Wikitext Language Server', serverOptions, clientOptions);
	let disposable = client.start();

	// Push the disposable to the context's subscriptions so that the 
	// client can be deactivated on extension deactivation
	context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() {
}