using NUnitComposition.Extensions;

namespace NUnitComposition.SampleScope;

[ScopedSetupFixture]
public class SampleScopedSetupFixture : IInjectionSource
{
    public static object? State { get; private set; }

    private static IInjectionSource? instance;

    public static bool HasInstance => instance != null;

    public static IInjectionSource Instance
    {
        get
        {
            if (instance == null) throw new Exception($"{nameof(SampleScopedSetupFixture)} instance not available in current context");
            return instance;
        }
    }

    private readonly Dictionary<Type, object> injectables = new();

    public T Get<T>()
    {
        return (T)Get(typeof(T));
    }

    public object Get(Type type)
    {
        if (injectables.TryGetValue(type, out var value))
        {
            return value;
        }
        throw new InvalidOperationException($"No injectable of type {type} found.");
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        if (instance != null) throw new Exception($"Cannot have sibling or deeper scope when {nameof(SampleScopedSetupFixture)} already have an instance.");
        instance = this;
        
        injectables.Add(typeof(string), "An injectable string");
        injectables.Add(typeof(int), 42);
        
        Root.Log.Add($"{nameof(SampleScopedSetupFixture)} {nameof(OneTimeSetup)} called.");
    }

    [SetUp]
    public void SetUp()
    {
        Root.Log.Add($"{nameof(SampleScopedSetupFixture)} {nameof(SetUp)} called (should be mutated to onetime).");

        State = "Some state from setup";
    }

    [TearDown]
    public void TearDown()
    {
        State = null;

        Root.Log.Add($"{nameof(SampleScopedSetupFixture)} {nameof(TearDown)} called (should be mutated to onetime).");
    }

    [TearDown]
    public void OneTimeTearDown()
    {
        Root.Log.Add($"{nameof(SampleScopedSetupFixture)} {nameof(OneTimeTearDown)} called.");

        instance = null;
    }
}