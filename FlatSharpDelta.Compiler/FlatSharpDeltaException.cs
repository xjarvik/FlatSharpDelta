using System;
using System.IO;
using System.Text.RegularExpressions;
using FlatSharp;
using CommandLine;
using reflection;

namespace FlatSharpDelta.Compiler
{
    public class FlatSharpDeltaException : Exception
    {
        public FlatSharpDeltaException(string message) : base(message)
        {
        }
    }
}