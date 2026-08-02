using MegaCrit.Sts2.Core.Models;

using static HideDetailsMod.HideDetailsModCode.AlternateArts;
namespace HideDetailsMod.HideDetailsModCode.Patches;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

// TODO: put in proper location
public static class HarmonyPatchHelpers
{
    /// <summary>
    /// Finds all implementations and overrides of the given MethodInfo across specified assemblies.
    /// </summary>
    /// <param name="baseMethod">The MethodInfo representing the target method signature and declaring type.</param>
    /// <param name="assemblies">Optional list of assemblies to search in. Defaults to all loaded non-dynamic assemblies.</param>
    /// <returns>An enumerable of MethodBase targets for [HarmonyTargetMethods].</returns>
    public static IEnumerable<MethodBase> GetMethodImplementations(
        MethodInfo baseMethod,
        IEnumerable<Assembly>? assemblies = null)
    {
        if (baseMethod == null) throw new ArgumentNullException(nameof(baseMethod));

        Type declaringType = baseMethod.DeclaringType
            ?? throw new ArgumentException("Base method must have a valid DeclaringType.", nameof(baseMethod));

        string methodName = baseMethod.Name;
        Type[] paramTypes = baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        Type[]? genericArgs = baseMethod.IsGenericMethod ? baseMethod.GetGenericArguments() : null;

        assemblies ??= AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types)
            {
                // Must be a subtype, implement the interface, or be the declaring type itself
                if (!declaringType.IsAssignableFrom(type))
                    continue;

                // Look for declared method matching the exact signature on this subtype
                MethodInfo method = AccessTools.DeclaredMethod(type, methodName, paramTypes, genericArgs);

                // Make sure the method exists on this class specifically and isn't abstract
                if (method != null && !method.IsAbstract)
                {
                    yield return method;
                }
            }
        }
    }
}
[HarmonyPatch]
public static class ArtPatch
{
    [HarmonyPatch]
    public static class AllPortraitPaths
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Simply pass the target base method directly
            MethodInfo baseMethod = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.AllPortraitPaths));
            var impls = HarmonyPatchHelpers.GetMethodImplementations(baseMethod);
            MainFile.Logger.Info($"Found ");
            return impls;
        }
        [HarmonyPostfix]
        internal static void PostFix(CardModel __instance, ref IEnumerable<string> __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            try
            {
                List<string> result = [.. __result];

                var found = GetAltsFor(__instance);

                result.AddRange(found.SelectMany(alt => alt.All).Select(Img => Img.PortraitPath));

                var BaseImg = new CardImg(__instance);
                if (BaseImg.Exists()) result.Add(BaseImg.PortraitPath);
                var UpgradedImg = BaseImg.Upgraded();
                if (UpgradedImg.Exists()) result.Add(UpgradedImg.PortraitPath);
                __result = result;
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Error in AllPortraitPaths: {e}");
            }
        }
    }

    static CardImg? ImgFor(CardModel card)
    {
        var factories = GetAltsFor(card);
        foreach (var factory in factories)
        {
            var img = factory.Get(card);
            if (img == null) continue;
            if (card.IsUpgraded && img.Upgraded().Exists()) return img.Upgraded();
            if (img.Exists()) return img;
        }
        var Img = new CardImg(card);
        if (card.IsUpgraded && Img.Upgraded().Exists()) return Img.Upgraded();
        if (Img.Exists()) return Img;
        return null;
    }
    [HarmonyPatch]
    public static class PortraitPath
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Simply pass the target base method directly
            MethodInfo baseMethod = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.PortraitPath));
            var impls = HarmonyPatchHelpers.GetMethodImplementations(baseMethod);
            MainFile.Logger.Info($"Found ");
            return impls;
        }
        [HarmonyPostfix]
        internal static void PostFix(CardModel __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            try
            {
                var Img = ImgFor(__instance);
                if (Img != null && Img.Exists()) __result = Img.PortraitPath;
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPath: {e}"); }
        }

    }
    [HarmonyPatch]
    public static class PortraitPngPath
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // Simply pass the target base method directly
            MethodInfo baseMethod = AccessTools.PropertyGetter(typeof(CardModel), "PortraitPngPath");
            var impls = HarmonyPatchHelpers.GetMethodImplementations(baseMethod);
            MainFile.Logger.Info($"Found ");
            return impls;
        }
        [HarmonyPostfix]
        internal static void PostFix(CardModel __instance, ref string __result)
        {
            if (!MyModConfig.UseCustomArt) return;
            if (__instance == null) return;
            try
            {
                var Img = ImgFor(__instance);
                if (Img != null && Img.Exists()) __result = Img.PortraitPngPath;
            }
            catch (Exception e)
            { MainFile.Logger.Error($"Error in PortraitPngPath: {e}"); }
        }
    }
}
