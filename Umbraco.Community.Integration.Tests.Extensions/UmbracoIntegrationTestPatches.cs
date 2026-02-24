using HarmonyLib;
using System.Reflection;
using Castle.DynamicProxy;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Tests.Common.Testing;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace

public static class UmbracoIntegrationTestPatches
{
    private static readonly Type testOptionAttributeBaseType = typeof(TestOptionAttributeBase);
    private static readonly MethodInfo testOptionGetTestOptionsMethod = testOptionAttributeBaseType.GetMethod(nameof(TestOptionAttributeBase.GetTestOptions), [typeof(MethodInfo)])!;
    private static readonly MethodInfo testOptionGetMethod = testOptionAttributeBaseType.GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly Type referenceResolverType = Type.GetType("Umbraco.Cms.Core.Composing.ReferenceResolver, Umbraco.Core")!;
    private static readonly MethodInfo referenceResolverGetAssemblyFoldersMethod = referenceResolverType.GetMethod("GetAssemblyFolders", BindingFlags.Static | BindingFlags.NonPublic)!;

    public static void ApplyPatches(this GlobalSetupTeardown umbracoGlobalSetup)
    {
        var harmony = new Harmony(nameof(Umbraco.Community.Integration.Tests.Extensions));

        harmony.Patch(referenceResolverGetAssemblyFoldersMethod, postfix: new HarmonyMethod(ReplacedGetAssemblyLocation));
        harmony.Patch(testOptionGetTestOptionsMethod.MakeGenericMethod(typeof(UmbracoTestAttribute)), prefix: new HarmonyMethod(ScanReflectedTypeInsteadOfDeclaringType));
    }

    private static void ReplacedGetAssemblyLocation(ref IEnumerable<string?> __result)
    {
        __result = __result.Where(x => !String.IsNullOrWhiteSpace(x));
    }
    
    public static bool ScanReflectedTypeInsteadOfDeclaringType(MethodInfo method, TestOptionAttributeBase __instance, ref UmbracoTestAttribute __result)
    {
        var attr = ((UmbracoTestAttribute[])method.GetCustomAttributes(typeof(UmbracoTestAttribute), true)).FirstOrDefault();
        var type = method.ReflectedType;
        __result = (UmbracoTestAttribute)testOptionGetMethod.MakeGenericMethod(typeof(UmbracoTestAttribute)).Invoke(null, [type, attr])!;
        return false;
    }
}
