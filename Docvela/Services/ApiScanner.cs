using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.MSBuild;
using Docvela.Models;

namespace Docvela.Services
{
    public class ApiScanner
    {
        private readonly string _projectFilePath;
        private readonly string _docsOutputFolder;
        private List<AnalyzerOutputData>? _controllerData;

        public ApiScanner(string inputPath)
        {
            if (File.Exists(inputPath) && inputPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                _projectFilePath = inputPath;
            }
            else if (Directory.Exists(inputPath))
            {
                var csproj = Directory.GetFiles(inputPath, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
                if (csproj == null)
                    throw new FileNotFoundException("No .csproj file found in the specified directory or its subdirectories.");
                _projectFilePath = csproj;
            }
            else
            {
                throw new FileNotFoundException("Input path must be a folder or a .csproj file.");
            }

            _docsOutputFolder = Path.Combine(Path.GetDirectoryName(_projectFilePath)!, "docs");
        }

        public List<AnalyzerOutputData> ScanControllers()
        {
            var analyzer = new MethodAnalyzer();
            _controllerData = analyzer.AnalyzeProject(_projectFilePath);
            return _controllerData;
        }

        // Return global distinct data models used across all controllers
        public List<DataModel> GetAllDataModels()
        {
            if (_controllerData == null)
                throw new InvalidOperationException("ScanControllers must be called before getting data models.");

            return _controllerData
                .SelectMany(c => c.DataModelsUsed)
                .GroupBy(dm => dm.Name)
                .Select(g => g.First())
                .ToList();
        }

        public List<ServiceData> ScanServices()
        {
            var serviceAnalyzer = new ServiceAnalyzer(_projectFilePath);
            return serviceAnalyzer.AnalyzeServices();
        }

        public string GetDocsOutputFolder() => _docsOutputFolder;
    }
}
