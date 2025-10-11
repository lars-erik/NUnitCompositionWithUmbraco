using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System.Reflection;

namespace NUnitComposition.Extensions;

internal class EmptyPreFilter : IPreFilter
{
    // NUnit's internal PreFilter.Empty does this
    public bool IsMatch(Type type) => true;

    public bool IsMatch(Type type, MethodInfo method) => true;
}

[Serializable]
internal class EmptyFilter : TestFilter
{
    public override bool Match(ITest test)
    {
        return true;
    }

    public override bool Pass(ITest test, bool negated)
    {
        return true;
    }

    public override bool IsExplicitMatch(ITest test)
    {
        return false;
    }

    public override TNode AddToXml(TNode parentNode, bool recursive)
    {
        return parentNode.AddElement("filter");
    }
}
