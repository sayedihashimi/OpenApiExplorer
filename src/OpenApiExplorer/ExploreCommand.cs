﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Spectre.Console;

namespace SayedHa.OpenAPIExplorer.ConsoleRunner; 
public class ExploreCommand : CommandBase {
    private IReporter _reporter;
    public ExploreCommand(IReporter reporter) {
        _reporter = reporter;
    }
    public override Command CreateCommand() =>
        new Command(name: "explore", description: "Explorer an OpenAPI spec") {
            CommandHandler.Create<string,bool>((openApiFilePath,verbose) => {
                _reporter.EnableVerbose = verbose;

				var explorer = new Explorer(openApiFilePath);
				var endpoints = explorer.GetEndpointsWithOperation();

                PrintApiInfo(explorer.Document!);

				bool cancelRequested = false;
				// Register the event handler for CTRL+C
				Console.CancelKeyPress += (sender, e) => {
					e.Cancel = true;
					cancelRequested = true;
				};

                // wait for the user to select an endpoint
				while (true) {
					if (cancelRequested) {
						break;
					}
					Console.WriteLine();
					var endpoint = PromptForEndpoint(endpoints);

					var endpointWithInfo = explorer.GetEndpointFor(endpoint.OperationType, endpoint.Path);
					PrintEndpointWithInfo(endpointWithInfo);
				}

				return 0;
            }),
            ArgumentOpenApiFilePath(),
            OptionVerbose(),
        };
	protected Option OptionPackages() =>
        new Option(new string[] { "--paramname" }, "TODO: update param description") {
            Argument = new Argument<string>(name: "paramname")
        };

    protected Argument ArgumentOpenApiFilePath() =>
        new Argument<string>(
            name: "openApiFilePath",
				description: "The path to the OpenAPI file to explore"
			);

	protected void PrintApiInfo(OpenApiDocument document) {
		if(document is null) {
			throw new ArgumentNullException(nameof(document));
		}
		//_reporter.WriteLine($"Title: {document.Info.Title}");
		//WriteLineOnlyIf(_reporter, document.Info.Title, !string.IsNullOrWhiteSpace(document.Info.Title));
		//_reporter.WriteLine($"Version: {document.Info.Version}");
		//_reporter.WriteLine($"Description: {document.Info.Description}");
		//_reporter.WriteLine($"Terms of Service: {document.Info.TermsOfService}");
		//_reporter.WriteLine($"Contact: {document.Info.Contact.Name} - {document.Info.Contact.Email}");
		//_reporter.WriteLine($"License: {document.Info.License.Name} - {document.Info.License.Url}");
		//_reporter.WriteLine($"Host: {document.Servers[0].Url}");
		//_reporter.WriteLine($"External Docs: {document.ExternalDocs.Url}");
		//_reporter.WriteLine($"Tags: {string.Join(", ", document.Tags.Select(t => t.Name))}");
		//_reporter.WriteLine($"Servers: {document.Servers.Count} servers");
		//_reporter.WriteLine($"Info: {document.Info.Title} - {document.Info.Version} - {document.Info.Description}");
		//_reporter.WriteLine($"External Docs: {document.ExternalDocs.Url} - {document.ExternalDocs.Description}");

		if (document.Info is null) {
			return;
		}
		var sb = new StringBuilder();
		if(!string.IsNullOrWhiteSpace(document.Info.Title)) {
			// sb.AppendLine($"Title: {document.Info.Title}");
			sb.Append(document.Info.Title);
			if (!string.IsNullOrWhiteSpace(document.Info.Version)) {
				sb.Append(" Version: ");
				sb.Append(document.Info.Version);
			}
			sb.AppendLine();
		}
		if (document.Info.Contact is not null && !string.IsNullOrWhiteSpace(document.Info.Contact.Name)) {
			sb.Append("Contact: ");
			sb.Append(document.Info.Contact.Name);
			if (!string.IsNullOrWhiteSpace(document.Info.Contact.Email)) {
				sb.Append(" ");
				sb.Append(document.Info.Contact.Email);
			}
			sb.AppendLine();
		}
		if (document.Info.License is not null) {
			sb.Append("License: ");
			sb.Append(document.Info.License.Name);
			sb.Append(" ");
			sb.Append(document.Info.License.Url);
			sb.AppendLine();
		}
		if (document.Info.TermsOfService is not null) {
			sb.Append("Terms: ");
			sb.AppendLine(document.Info.TermsOfService.ToString());
		}
		if (!string.IsNullOrWhiteSpace(document.Info.Description)) {
			sb.AppendLine();
			sb.AppendLine(document.Info.Description);
		}
		if(document.Components.SecuritySchemes is not null && document.Components.SecuritySchemes.Count > 0) {
			sb.AppendLine();
			sb.AppendLine("Security schemes: ");

			foreach(var sec in document.Components.SecuritySchemes) {
				sb.AppendLine($"  {sec.Value.Type}");

				if(!string.IsNullOrEmpty(sec.Value.Name)) {
					sb.AppendLine($"    Name: {sec.Value.Name}");
				}
				sb.AppendLine($"    In: {sec.Value.In}");
				if (!string.IsNullOrEmpty(sec.Value.BearerFormat)) {
					sb.AppendLine($"    Bearer Format: {sec.Value.BearerFormat}");
				}
				if(!string.IsNullOrEmpty(sec.Value.Description)) {
					sb.AppendLine($"    Description: {sec.Value.Description}");
				}
				if(sec.Value.OpenIdConnectUrl != null) {
					sb.AppendLine($"    OIDC URL: {sec.Value.OpenIdConnectUrl}");
				}

				if (sec.Value.Reference is not null && sec.Value.Reference is OpenApiReference secRef) {
					if(!string.IsNullOrEmpty(secRef.Id)) {
						sb.AppendLine($"    Id: {secRef.Id}");
					}
					if (secRef.ExternalResource != null) {
						sb.AppendLine($"    ExternalResource: {secRef.ExternalResource}");
					}
					if(secRef.HostDocument != null) {
						sb.AppendLine($"    HostDocument: {secRef.HostDocument}");
					}
					sb.AppendLine($"    IsExternal: {secRef.IsExternal}");
					sb.AppendLine($"    IsFragment: {secRef.IsFragrament}");
					sb.AppendLine($"    IsLocal: {secRef.IsLocal}");
					if (!string.IsNullOrEmpty(secRef.ReferenceV2)) {
						sb.AppendLine($"    ReferenceV2: {secRef.ReferenceV2}");
					}
					if (!string.IsNullOrEmpty(secRef.ReferenceV3)) {
						sb.AppendLine($"    ReferenceV3: {secRef.ReferenceV3}");
					}
					if(secRef.Type != null) {
						sb.AppendLine($"    Type: {secRef.Type}");
					}
				}

				if (sec.Value.Flows != null) {
					sb.AppendLine($"    Flows:");
					if(sec.Value.Flows.AuthorizationCode != null) {
						sb.AppendLine($"      AuthorizationCode: {sec.Value.Flows.AuthorizationCode}");
					}
					if(sec.Value.Flows.ClientCredentials != null) {
						sb.AppendLine($"      ClientCredentials: {sec.Value.Flows.ClientCredentials}");
					}
					if(sec.Value.Flows.Implicit != null) {
						sb.AppendLine($"      Authorization URL: {sec.Value.Flows.Implicit.AuthorizationUrl}");
						if (sec.Value.Flows.Implicit.RefreshUrl != null) {
							sb.AppendLine($"      RefreshUrl: {sec.Value.Flows.Implicit.RefreshUrl}");
						}
						if (sec.Value.Flows.Implicit.Scopes?.Keys?.Count > 0) {
							sb.AppendLine("      Scopes: ");
							sec.Value.Flows.Implicit.Scopes.ToList().ForEach(scope => {
								sb.AppendLine($"        {scope.Key}: {scope.Value}");
							});
						}
						if(sec.Value.Flows.Implicit.TokenUrl != null) {
							sb.AppendLine($"      TokenUrl: {sec.Value.Flows.Implicit.TokenUrl}");
						}
					}
				}

			}
		}

		if(document.Servers.Count > 0) {
			sb.AppendLine();
			sb.Append("Servers: ");
			foreach(var server in document.Servers) {
				sb.Append(server.Url);
				sb.Append(" ");
			}
		}

		_reporter.WriteLine(sb.ToString());
	}
	protected void PrintoutEndpoints(List<DocPathWithOperation> endpoints) {
		_reporter.WriteLine("Endpoints:");
		foreach (var item in endpoints) {
            // var opString = new 
            _reporter.WriteLine($"{item.OperationType.ToString().PadLeft(8)} {item.Path}");
		}
	}
	protected void WriteLineOnlyIf(IReporter reporter, string message, bool condition) {
		if (condition) {
			reporter.WriteLine(message);
		}
	}
	protected DocPathWithOperation PromptForEndpoint(List<DocPathWithOperation> endpoints) =>
		AnsiConsole.Prompt(
			new SelectionPrompt<DocPathWithOperation>()
				.Title("Select endpoint")
				.AddChoices(endpoints)
		);
    protected void ExplorePath(DocPathWithOperation pathAndOperation) {
        
    }
    protected void PrintEndpointWithInfo(EndpointWithInfo endpoint) {
		var sb = new StringBuilder();
        sb.AppendLine();
        sb.Append($"{endpoint.OperationType.ToString()}");
        sb.Append($" {endpoint.Path}");
        if (!string.IsNullOrWhiteSpace(endpoint.Summary)) {
            sb.Append($" - {endpoint.Summary}");
        }

		if (endpoint.Operation.Deprecated) {
			sb.Append(" *DEPRECATED*");
		}

		sb.AppendLine();

        if(endpoint.Security.Count > 0) {
			sb.Append("  Security: ");
			foreach (var sec in endpoint.Security) {
				foreach (var secItem in sec) {
					sb.AppendLine($" {secItem.Key.Type}");
					if (secItem.Value.Any()) {
						sb.AppendLine("    Scopes");
					}
					foreach (var scope in secItem.Value) {
						sb.AppendLine($"      {scope}");
					}
				}
			}
		}
        else {
			sb.AppendLine("    Security: None");
		}

        // print parameters
        if (endpoint.Parameters.Any()) {
			sb.AppendLine("  Parameters: ");
			foreach (var param in endpoint.Parameters) {
				sb.Append($"    {param.Name}: {param.Schema.Type}");
				if (!string.IsNullOrWhiteSpace(param.Schema.Format)) {
					sb.Append($" ({param.Schema.Format})");
				}
				if (!string.IsNullOrWhiteSpace(param.Description)) {
					sb.Append($" - {param.Description.TrimEnd()}");
				}
				if (param.Required) {
					sb.Append(" (required)");
				}
				if (param.In.HasValue) {
					sb.Append($" [From: {param.In}]");
				}

				sb.AppendLine();
			}
        }
        else {
            sb.AppendLine("  Parameters: None");
		}

        // print responses
        if (endpoint.ResponsesWithKey.Any()) {
			sb.AppendLine("  Responses: ");
			foreach (var (key, response) in endpoint.ResponsesWithKey) {
                sb.AppendLine($"     {key} - {response.Description}");
			}
		}
        else {
			sb.AppendLine("  Responses: None");
		}

		_reporter.WriteLine(sb.ToString());
	}
}
