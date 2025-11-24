using HarmonyLib;
using System.Reflection;
using Castle.DynamicProxy;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Tests.Common.Testing;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace

public static class UmbracoIntegrationTestPatches
{
    private static readonly Type assemblyProviderType = typeof(DefaultUmbracoAssemblyProvider);
    private static readonly FieldInfo entrypointAssemblyField = assemblyProviderType.GetField("_entryPointAssembly", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly Type testOptionAttributeBaseType = typeof(TestOptionAttributeBase);
    private static readonly MethodInfo testOptionGetTestOptionsMethod = testOptionAttributeBaseType.GetMethod(nameof(TestOptionAttributeBase.GetTestOptions), [typeof(MethodInfo)])!;
    private static readonly MethodInfo testOptionGetMethod = testOptionAttributeBaseType.GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static)!;

    public static void ApplyPatches(this GlobalSetupTeardown umbracoGlobalSetup)
    {
        var harmony = new Harmony(nameof(Umbraco.Community.Integration.Tests.Extensions));

        var assemblyProviderCtor = assemblyProviderType.GetConstructors().Single(); // Let's break if this ever changes

        harmony.Patch(assemblyProviderCtor, postfix: new HarmonyMethod(ReplaceDynamicEntryAssembly));

        harmony.Patch(testOptionGetTestOptionsMethod.MakeGenericMethod(typeof(UmbracoTestAttribute)), prefix: new HarmonyMethod(ScanReflectedTypeInsteadOfDeclaringType));
    }

    public static void ReplaceDynamicEntryAssembly(DefaultUmbracoAssemblyProvider __instance)
    {
        var entryPointAssembly = (Assembly)entrypointAssemblyField.GetValue(__instance)!;
        if (entryPointAssembly.GetName().Name == ModuleScope.DEFAULT_ASSEMBLY_NAME)
        {
            var firstNonDelegateType = entryPointAssembly.ExportedTypes.FirstOrDefault(type => !type.IsAssignableTo(typeof(MulticastDelegate)));
            if (firstNonDelegateType != null)
            {
                var baseType = firstNonDelegateType.BaseType;
                var baseTypeAssembly = baseType!.Assembly;
                entrypointAssemblyField.SetValue(__instance, baseTypeAssembly);
            }
        }
    }

    public static bool ScanReflectedTypeInsteadOfDeclaringType(MethodInfo method, TestOptionAttributeBase __instance, ref UmbracoTestAttribute __result)
    {
        var attr = ((UmbracoTestAttribute[])method.GetCustomAttributes(typeof(UmbracoTestAttribute), true)).FirstOrDefault();
        var type = method.ReflectedType;
        __result = (UmbracoTestAttribute)testOptionGetMethod.MakeGenericMethod(typeof(UmbracoTestAttribute)).Invoke(null, [type, attr])!;
        return false;
    }


}
