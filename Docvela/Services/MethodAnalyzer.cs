using Docvela.Models;
using Docvela.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Docvela.Services
{
    public class MethodAnalyzer
    {
        private HashSet<string> _rootNamespaces = new();

        public List<AnalyzerOutputData> AnalyzeProject(string path)
        {

            var output = new List<AnalyzerOutputData>();
            var workspace = MSBuildWorkspace.Create();

            // Open the project
            var project = workspace.OpenProjectAsync(path).Result;


            // Detect root namespaces dynamically from all source files
            DetectRootNamespacesAsync(project).Wait();

            foreach (var document in project.Documents)
            {
                if (string.IsNullOrEmpty(document.FilePath) || !File.Exists(document.FilePath))
                {
                    continue; // skip files that may have been deleted or moved
                }

                var fileName = document.Name ?? "";
                if (!fileName.Contains("Controller", StringComparison.OrdinalIgnoreCase)) continue;

                var tree = document.GetSyntaxTreeAsync().Result;
                var model = document.GetSemanticModelAsync().Result;
                if (tree == null || model == null) continue;

                var root = tree.GetRoot();
                var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classNode == null) continue;

                var controllerData = new AnalyzerOutputData
                {
                    ControllerName = classNode.Identifier.Text
                };

                foreach (var method in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                    if (symbol == null) continue;

                    var httpAttr = method.AttributeLists.SelectMany(x => x.Attributes)
                                      .FirstOrDefault(attr => attr.Name.ToString().StartsWith("Http"));
                    if (httpAttr == null) continue;

                    var xmlDoc = symbol.GetDocumentationCommentXml() ?? "";
                    var summary = RoslynHelpers.GetSummaryFromXmlDoc(xmlDoc);

                    var endpoint = new EndpointData
                    {
                        HttpMethod = httpAttr.Name.ToString(),
                        Route = method.AttributeLists.SelectMany(x => x.Attributes)
                                    .FirstOrDefault(a => a.ArgumentList != null)?.ArgumentList?.Arguments.ToString() ?? "",
                        MethodSignature = method.Identifier + "()",
                        Summary = summary  // add this line
                    };

                    foreach (var param in symbol.Parameters)
                    {
                        var type = RoslynHelpers.GetFullTypeName(param.Type);
                        endpoint.Parameters.Add(new ParameterData { Name = param.Name, Type = type });

                        if (type.Contains("Request"))
                        {
                            var dataModel = new DataModel { Name = param.Type.Name };
                            var members = param.Type.GetMembers().OfType<IPropertySymbol>();
                            foreach (var member in members)
                            {
                                dataModel.Properties.Add(new ParameterData
                                {
                                    Name = member.Name,
                                    Type = RoslynHelpers.GetFullTypeName(member.Type)
                                });
                            }

                            controllerData.DataModelsUsed.Add(dataModel);
                        }
                    }

                    // Collect return statements
                    var returnStatements = method.DescendantNodes().OfType<ReturnStatementSyntax>();
                    foreach (var r in returnStatements)
                    {
                        var returnText = r.ToString();
                        endpoint.ReturnStatements.Add(new ReturnData { Statement = returnText });
                    }

                    // Collect and filter service calls
                    var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    foreach (var invocation in invocations)
                    {
                        var calledSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                        if (calledSymbol != null)
                        {
                            var ns = calledSymbol.ContainingNamespace?.ToDisplayString() ?? "";

                            // Only include calls inside your root namespaces
                            if (_rootNamespaces.Any(rootNs => ns.StartsWith(rootNs)))
                            {
                                endpoint.ServiceCalls.Add(calledSymbol.ToDisplayString());
                            }
                        }
                    }

                    controllerData.Endpoints.Add(endpoint);
                }

                output.Add(controllerData);
            }

            return output;
        }

        private async Task DetectRootNamespacesAsync(Project project)
        {
            foreach (var document in project.Documents)
            {
                var tree = await document.GetSyntaxTreeAsync();
                if (tree == null) continue;

                var root = await tree.GetRootAsync();

                // Get the first namespace declaration in the document (if any)
                var namespaceDecl = root.DescendantNodes()
                                        .OfType<NamespaceDeclarationSyntax>()
                                        .FirstOrDefault();

                if (namespaceDecl != null)
                {
                    // Get the root part of the namespace (e.g. "FixRoutineAPI" from "FixRoutineAPI.Models.Entities")
                    var nsName = namespaceDecl.Name.ToString();
                    var rootNs = nsName.Split('.').First();

                    _rootNamespaces.Add(rootNs);
                }
            }
        }
    }
}
