using System.Runtime.CompilerServices;

namespace DBase.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyDiffPlex.Initialize();
        DerivePathInfo((_, projectDirectory, type, method) => new PathInfo(
            directory: Path.Combine(projectDirectory, type.Name),
            typeName: type.Name,
            methodName: method.Name));
    }
}
