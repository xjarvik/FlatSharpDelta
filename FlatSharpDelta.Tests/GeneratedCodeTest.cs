using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

public abstract class GeneratedCodeTest : IDisposable
{
    protected abstract string[] FbsFiles { get; }
    private DirectoryInfo outputDirectory;

    private Assembly generatedAssembly;
    protected Assembly GeneratedAssembly { get => generatedAssembly; }

    protected GeneratedCodeTest()
    {
        Type type = this.GetType();
        string testName = type.Name;
        string currentDirectory = Path.GetDirectoryName(type.Assembly.Location);
        string inputFiles = String.Join(";", FbsFiles.Select(fbsFile => Path.Combine(currentDirectory, fbsFile)));
        outputDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "temp"));
        Directory.CreateDirectory(outputDirectory.FullName);
        ClearOutputDirectory();

        int exitCode = FlatSharpDelta.Compiler.Program.Main(new string[]
        {
            "-i", inputFiles,
            "-o", outputDirectory.FullName
        });
        Assert.Equal(0, exitCode);

        generatedAssembly = GetAssemblyFromOutputDirectory();
    }

    public void Dispose()
    {
        ClearOutputDirectory();
    }

    private void ClearOutputDirectory()
    {
        foreach (FileInfo file in outputDirectory.GetFiles())
        {
            file.Delete(); 
        }
    }

    private Assembly GetAssemblyFromOutputDirectory()
    {
        string assemblyName = Path.GetRandomFileName();

        List<MetadataReference> references = new List<MetadataReference>()
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(FlatSharp.IFlatBufferSerializable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(LinkedList<>).Assembly.Location),
        };

        foreach(var referencedAssembly in Assembly.GetEntryAssembly().GetReferencedAssemblies())
        {
            references.Add(MetadataReference.CreateFromFile(Assembly.Load(referencedAssembly).Location)); 
        }

        CSharpCompilation compilation = CSharpCompilation.Create
        (
            assemblyName,
            outputDirectory.GetFiles().Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file.FullName))),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using(var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            if(!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic => 
                    diagnostic.IsWarningAsError || 
                    diagnostic.Severity == DiagnosticSeverity.Error
                );

                string messages = String.Empty;

                foreach(Diagnostic diagnostic in failures)
                {
                    messages += diagnostic.Id + " " + diagnostic.GetMessage() + "\n";
                }

                throw new Exception(messages);
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(ms.ToArray());
            }
        }
    }
}