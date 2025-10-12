using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Interfaces;

namespace NUnitComposition.Extensions
{
    internal interface IExtendableLifecycle
    {
        IMethodInfo[] SetUpMethods { get; set; }
        IMethodInfo[] TearDownMethods { get; set; }
        IMethodInfo[] OneTimeSetUpMethods { get; set; }
        IMethodInfo[] OneTimeTearDownMethods { get; set; }
    }
}
