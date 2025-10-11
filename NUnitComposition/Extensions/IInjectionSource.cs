namespace NUnitComposition.Extensions;

public interface IInjectionSource
{
    static abstract bool HasInstance { get; }
    static abstract IInjectionSource Instance { get; }

    T Get<T>();
    object Get(Type type);
}