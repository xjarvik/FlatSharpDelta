/*
 * Copyright 2023 William Söder
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class AssemblyFixture<T> : IDisposable where T : ITestConfiguration, new()
    {
        private DirectoryInfo outputDirectory;
        private Assembly generatedAssembly;
        public Assembly GeneratedAssembly { get => generatedAssembly; }
        public Exception CompilerException { get; private set; }

        public AssemblyFixture()
        {
            T configuration = new T();
            string testClassName = configuration.TestType.Name;
            string currentDirectory = Path.GetDirectoryName(configuration.TestType.Assembly.Location);
            string inputFiles = String.Join(";", configuration.FbsFiles.Select(fbsFile => Path.Combine(currentDirectory, "fixtures", testClassName, fbsFile)));
            outputDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "fixtures", testClassName, "output"));
            Directory.CreateDirectory(outputDirectory.FullName);
            ClearOutputDirectory();

            int exitCode = -1;

            try
            {
                exitCode = FlatSharpDelta.Compiler.Program.Main(new string[]
                {
                    "-i", inputFiles,
                    "-o", outputDirectory.FullName,
                    "--debug"
                });
            }
            catch (Exception exception)
            {
                CompilerException = exception;

                if (configuration.CatchCompilerException)
                {
                    return;
                }

                throw;
            }

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

            foreach (var referencedAssembly in Assembly.GetEntryAssembly().GetReferencedAssemblies())
            {
                references.Add(MetadataReference.CreateFromFile(Assembly.Load(referencedAssembly).Location));
            }

#if NET7_0_OR_GREATER
            CSharpParseOptions parseOptions = new CSharpParseOptions()
                .WithLanguageVersion(LanguageVersion.CSharp11)
                .WithPreprocessorSymbols(new [] { "NET7_0_OR_GREATER" });
#else
            CSharpParseOptions parseOptions = new CSharpParseOptions();
#endif

            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName,
                outputDirectory.GetFiles().Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file.FullName), parseOptions)),
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error
                    );

                    string messages = String.Empty;

                    foreach (Diagnostic diagnostic in failures)
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
}