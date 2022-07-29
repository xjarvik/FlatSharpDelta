using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace FlatSharpDelta.Tests
{
    public class GetDeltaOptimizationTests : GeneratedCodeTests
    {
        protected override string[] FbsFiles
        {
            get => new string[]
            {
                "GetDeltaOptimizationTests.fbs"
            };
        }

        
    }
}