# Docvela

**Docvela** is a .NET API documentation generator that uses Roslyn to analyze your MVC API projects and automatically produce detailed, human-readable Markdown documentation. It extracts controller endpoints, routes, data models, and service/processor method flows to help developers and teams keep their API documentation accurate and up-to-date — with minimal effort.

---

## Features

- Analyzes all controllers in your .NET MVC/WebAPI project  
- Extracts detailed endpoint info: HTTP methods, routes, parameters, return values, and service calls  
- Collects and documents data models used in controllers and services  
- Analyzes service/processor classes with method parameters, internal calls, and return statements  
- Outputs clean, structured Markdown files organized into folders (`controllers/`, `services/`, `datamodels/`)  
- Supports multi-project solutions and detects root namespaces dynamically  
- Designed for easy extension to support other languages, frameworks, and CI/CD integration  

---

## Getting Started

### Prerequisites

- .NET 6.0 SDK or later installed  
- Your target project should be a .NET MVC or WebAPI project  

### Usage

Clone the repository and build the tool:

```bash
git clone https://github.com/yourusername/docvela.git
cd docvela
dotnet build
