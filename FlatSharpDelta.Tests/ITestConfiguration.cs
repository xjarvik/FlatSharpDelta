using System;

public interface ITestConfiguration
{
    Type TestType { get; }
    string[] FbsFiles { get; }
}