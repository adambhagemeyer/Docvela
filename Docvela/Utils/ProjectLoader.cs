using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace Docvela.Utilities;

public static class ProjectLoader
{
    public static async Task<Microsoft.CodeAnalysis.Project> LoadProjectAsync(string path)
    {
        MSBuildLocator.RegisterDefaults();
        var workspace = MSBuildWorkspace.Create();
        return await workspace.OpenProjectAsync(path);
    }
}
