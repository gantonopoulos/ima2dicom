using Xunit;

namespace ImaToDicomConverter.Tests;

/// <summary>
/// Collection definition to prevent parallel execution of tests that modify global state
/// (e.g., current directory). Tests in this collection will run sequentially.
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialTestCollection
{
    // This class is never instantiated. It's just a marker for xUnit.
}

