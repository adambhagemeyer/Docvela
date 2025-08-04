using Docvela.Models;
using Docvela.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Docvela.Services;

public class ServiceAnalyzer
{
    private readonly string _projectPath;
    private List<ServiceData> _serviceData = new();

    public ServiceAnalyzer(string projectPath)
    {
        _projectPath = projectPath;
    }

    public List<ServiceData> AnalyzeServices()
    {
        var workspace = MSBuildWorkspace.Create();
        var projectFile = Path.Combine(_projectPath, $"{Path.GetFileName(_projectPath)}.csproj");
        var project = workspace.OpenProjectAsync(projectFile).Result;

        foreach (var document in project.Documents)
        {
            if (!document.Name.EndsWith("Service.cs") && !document.Name.EndsWith("Processor.cs"))
                continue;

            var tree = document.GetSyntaxTreeAsync().Result;
            var model = document.GetSemanticModelAsync().Result;
            if (tree == null || model == null) continue;

            var root = tree.GetRoot();
            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classNode == null) continue;

            var service = new ServiceData
            {
                ServiceName = classNode.Identifier.Text
            };

            foreach (var method in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var symbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
                if (symbol == null) continue;

                var xmlDoc = symbol.GetDocumentationCommentXml() ?? "";
                var summary = RoslynHelpers.GetSummaryFromXmlDoc(xmlDoc);

                var methodData = new ServiceMethodData
                {
                    MethodName = method.Identifier.Text,
                    ReturnType = RoslynHelpers.GetFullTypeName(symbol.ReturnType),
                    Summary = summary  // add this line
                };

                // Parameters
                foreach (var param in symbol.Parameters)
                {
                    var paramType = RoslynHelpers.GetFullTypeName(param.Type);
                    methodData.Parameters.Add(new ParameterData { Name = param.Name, Type = paramType });

                    // If parameter is a DTO-like object, capture its properties
                    if (paramType.Contains("Request") || paramType.Contains("Dto") || paramType.Contains("DTO"))
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
                        if (!service.DataModelsUsed.Any(dm => dm.Name == dataModel.Name))
                            service.DataModelsUsed.Add(dataModel);
                    }
                }

                // Analyze method body for internal calls and return statements
                if (method.Body != null)
                {
                    var invocations = method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    foreach (var invocation in invocations)
                    {
                        var calledSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                        if (calledSymbol != null)
                        {
                            methodData.InternalCalls.Add(calledSymbol.ToDisplayString());
                        }
                    }

                    var returns = method.Body.DescendantNodes().OfType<ReturnStatementSyntax>();
                    foreach (var ret in returns)
                    {
                        methodData.ReturnStatements.Add(ret.ToString());
                    }
                }
                else if (method.ExpressionBody != null)
                {
                    // For expression-bodied methods (=> ...)
                    var exprBody = method.ExpressionBody.Expression;
                    var retStr = exprBody.ToString();
                    methodData.ReturnStatements.Add($"return {retStr};");
                }

                service.Methods.Add(methodData);
            }

            _serviceData.Add(service);
        }

        return _serviceData;
    }
}
