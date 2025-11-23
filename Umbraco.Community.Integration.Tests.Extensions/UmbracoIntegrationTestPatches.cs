using HarmonyLib;
using System.Reflection;
using Castle.DynamicProxy;
using Umbraco.Cms.Core.Composing;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace

public static class UmbracoIntegrationTestPatches
{
    private static readonly Type assemblyProviderType = typeof(DefaultUmbracoAssemblyProvider);
    private static readonly FieldInfo entrypointAssemblyField = assemblyProviderType.GetField("_entryPointAssembly", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static void ApplyPatches(this GlobalSetupTeardown umbracoGlobalSetup)
    {
        var harmony = new Harmony(nameof(Umbraco.Community.Integration.Tests.Extensions));

        var assemblyProviderCtor = assemblyProviderType.GetConstructors().Single(); // Let's break if this ever changes

        harmony.Patch(assemblyProviderCtor, postfix: new HarmonyMethod(ReplaceDynamicEntryAssembly));
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
}
