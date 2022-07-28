using System;
using System.IO;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            string testName = this.GetType().Name;
            string currentDirectory = Path.GetDirectoryName(this.GetType().Assembly.Location);
            FileInfo inputFile = new FileInfo(Path.Combine(currentDirectory, testName + ".fbs"));
            DirectoryInfo outputDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "temp"));
            Directory.CreateDirectory(outputDirectory.FullName);

            int exitCode = FlatSharpDelta.Compiler.Program.Main(new string[]
            {
                "-i", inputFile.FullName,
                "-o", outputDirectory.FullName
            });
            Assert.Equal(0, exitCode);

            foreach (FileInfo file in outputDirectory.GetFiles())
            {
                file.Delete(); 
            }
        }
    }
}