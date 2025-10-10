# Copilot Instructions

## Project Context

This project demonstrates a technique for creating suite-scoped singletons in NUnit by:

- Tagging a class with [SetUp] and [TearDown] as a "suite" using a custom [ScopedSuite] attribute.
- Moving [SetUp] and [TearDown] methods to OneTimeSetUp/OneTimeTearDown on a custom TestSuite node.
- Keeping the suite instance alive for the duration of all child fixtures, allowing shared resources (e.g., DI containers, seeded databases) to be passed to child test fixtures.
- Passing the suite instance to child fixtures via constructor arguments or TestContext properties.

## Key Patterns

- Use a custom attribute (ScopedSuiteAttribute) implementing IFixtureBuilder to create a parent TestSuite node.
- Use reflection to move [SetUp] and [TearDown] methods to OneTimeSetUp/OneTimeTearDown.
- Store the suite instance in suite.Properties or TestContext for access by child fixtures.
- Pass the suite instance to child fixtures' constructors using NUnitTestFixtureBuilder.BuildFrom.
- Child fixtures should accept the suite instance as a constructor parameter.

## Best Practices

- Avoid parallel execution for suite-scoped tests: set [Parallelizable(ParallelScope.None)] on the suite to prevent concurrent access to shared resources.
- Be aware that this approach uses NUnit internals (e.g., MethodInfoAdapter via reflection) and may break with future NUnit changes.
- IDE test runners will show the suite as an extra node above your real fixtures.
- Use this pattern to share expensive resources (e.g., DI containers, seeded databases) across multiple test fixtures in a controlled way.

## Example Usage

```csharp
[ScopedSuite]
public class SharedScope
{
    [SetUp] public void SetUp() { ... }
    [TearDown] public void TearDown() { ... }

    [TestFixture]
    public class TestClassA
    {
        public TestClassA(SharedScope scope) { ... }
        [Test] public void TestA() { ... }
    }

    [TestFixture]
    public class TestClassB
    {
        public TestClassB(SharedScope scope) { ... }
        [Test] public void TestB() { ... }
    }
}
```

## Caveats

- This is a "gray-box" approach and may require maintenance if NUnit internals change.
- Test filtering and parallelism require careful handling.
- Only use this pattern when you need true suite-level resource sharing.

---
