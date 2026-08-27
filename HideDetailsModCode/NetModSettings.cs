using System.Reflection;
using Godot;
using HarmonyLib;
using HideDetailsMod.HideDetailsModCode.Patches;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace HideDetailsMod.HideDetailsModCode;

public static class StringExtensions
{
    /// <summary>
    /// Indents every line of a string block by a specified number of spaces.
    /// </summary>
    public static string ToIndentedString(this string input, int spaces)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string indent = new(' ', spaces);

        // Prepend indent to the first line, then inject it after every line ending
        return indent + input.ReplaceLineEndings("\n" + indent);
    }
}

internal class MSPainNetConfigCmd : AbstractConsoleCmd
{
    public override string CmdName => "mspainnetconfigs";

    public override string Args => "";

    public override string Description => "Shows what other MSPain users are using";

    public override bool IsNetworked => false;

    public override bool DebugOnly => false;//!MainFile.IsCanary;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        IEnumerable<(ulong? NetId, NetModSettings? NetSettings)> configs = [
            (LocalContext.NetId, new NetModSettings()),
            .. NetModSettingsPatch.Configs.Select(kv => (kv.Key, kv.Value))
         ];
        IEnumerable<string> values = configs.Select(v => $"{GetPlayerName(v.NetId)} -- {v.NetSettings.ToString() ?? "Does not have the mod"}");
        return new(true, string.Join('\n', values));
    }

    public static string GetPlayerName(ulong? NetId)
    {
        if (NetId is not { } netId) return "YOU";
        string? name = null;
        try
        {
            PlatformType platformType = RunManager.Instance.NetService.Platform;
            name = PlatformUtil.GetPlayerNameRaw(platformType, netId) + $" ({netId})";
        }
        catch (Exception)
        {
            MainFile.Logger.Error("Failed to get name for " + netId);
        }
        return name ?? netId.ToString();
    }

}

public readonly struct NetModSettings
{
    // 1. ADD NEW TOGGLES HERE
    public bool Canary => EnabledFlags.Contains("C");
    public bool BetaShiv => EnabledFlags.Contains("Shiv");
    public bool BetaSoul => EnabledFlags.Contains("Soul");

    // Constructor that builds automatically from the local config settings
    public NetModSettings()
    {
        var args = OS.GetCmdlineArgs();
        var settings = args.FirstOrDefault(f => f.StartsWith("--settings="));
        if (!string.IsNullOrWhiteSpace(settings))
        {
            var configs = settings["--settings=".Length..].Split(",");
            EnabledFlags = [.. EnabledFlags.Union(configs)];
        }
        if (args.Contains("--skip-settings")) return;
        if (MainFile.IsCanary) EnabledFlags.Add("C");
        if (MyModConfig.UseBetaShivArt) EnabledFlags.Add("Shiv");
        if (MyModConfig.UseBetaSoulArt) EnabledFlags.Add("Soul");
    }

    public override string ToString()
    {
        return string.Join(":", EnabledFlags.RemoveWhere(string.IsNullOrWhiteSpace));
        // var sb = new StringBuilder();
        // sb.AppendLine($"{nameof(NetModSettings)} {{");

        // // Dynamically fetch all public instance properties
        // var properties = typeof(NetModSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // foreach (var prop in properties)
        // {
        //     // Filter out the collection property
        //     if (prop.Name == nameof(EnabledFlags))
        //         continue;

        //     var value = prop.GetValue(this);

        //     // Format lower-case boolean strings for clarity
        //     string formattedValue = value is bool b ? b.ToString().ToLower() : value?.ToString() ?? "null";

        //     sb.AppendLine($"  {prop.Name} = {formattedValue},");
        // }

        // sb.Append('}');
        // return sb.ToString();
    }

    // Container for holding the synchronized string flags
    public HashSet<string> EnabledFlags { get; } = [];

    // Constructor to build from a raw token set (used for remote players)
    internal NetModSettings(IEnumerable<string> enabledFlags)
    {
        EnabledFlags.UnionWith(enabledFlags);
    }


    // Automatically bundles active configs into your mod list string
    public static string BuildPackedString()
    {
        var localSettings = new NetModSettings();
        return localSettings.EnabledFlags.Count > 0
            ? ":" + string.Join(":", localSettings.EnabledFlags)
            : "";
    }


    public static NetModSettings? GetPlayerConfig(Player? player)
    {
        if (LocalContext.IsMe(player)) return new();
        return GetPlayerConfig(player?.NetId);
    }

    public static NetModSettings? GetPlayerConfig(ulong? NetId)
    {
        if (LocalContext.NetId == NetId) return new();
        return NetModSettingsPatch.Read(NetId);
    }
}

[HarmonyPatch]
static class NetModSettingsPatch
{
    [HarmonyTargetMethods]
    static internal IEnumerable<MethodInfo> Methods()
    {
        var method = AccessTools.Method("MegaCrit.Sts2.Core.Multiplayer.PeerVersionInfo:LocalDefault");
        if (method is { } real) yield return real;
    }
    [HarmonyPostfix]
    // [HarmonyPatch(typeof(PeerVersionInfo), nameof(PeerVersionInfo.LocalDefault))]
    // beta main compat
    // TODO: update when main
    static internal void PeerVersionInfo_LocalDefault(object __result)
    {
        var mods = Traverse.Create(__result).Property("otherMods").GetValue<List<string>?>();
        if (mods == null) return;
        mods.Add(Prefix + NetModSettings.BuildPackedString());
    }
    // static internal void PeerVersionInfo_LocalDefault(PeerVersionInfo __result)
    // {
    //     var mods = __result.otherMods;
    //     if (mods == null) return;
    //     mods.Add(Prefix + NetModSettings.BuildPackedString() + ":" + LocalContext.NetId);
    // }
    [HarmonyPatch]
    static class NetPatch
    {
        // IHandshakeHandler
        [HarmonyTargetMethods]
        static internal IEnumerable<MethodBase> Targets()
        {
            // HandshakeManager;
            MethodInfo baseMethod = AccessTools.Method("MegaCrit.Sts2.Core.Multiplayer.Connection.IHandshakeHandler:HandshakeSucceeded");
            return HarmonyPatchHelpers.GetMethodImplementations(baseMethod);
        }

        [HarmonyPostfix]
        // static void HandshakeSuccess(ulong peerId, PeerVersionInfo versionInfo)
        // static internal void HandshakeSuccess(object[] __args)
        // {
        //     if (__args is not [IConvertible num, PeerVersionInfo versionInfo])
        //     {
        //         MainFile.Logger.Error($"Failed to get args: {string.Join(',', __args)}");
        //         return;
        //     }
        //     ulong peerId = num.ToUInt64(null);
        //     MainFile.Logger.Warn($"NETCONTENT FOR {peerId}: " + string.Join('\t', versionInfo.otherMods ?? []));

        //     Configs[peerId] = Read(versionInfo) ?? new([]);
        // }
        static internal void HandshakeSuccess(object[] __args)
        {
            if (__args is not [IConvertible num, object versionInfo])
            {
                MainFile.Logger.Error($"Failed to get args: {string.Join(',', __args)}");
                return;
            }
            ulong peerId = num.ToUInt64(null);
            // MainFile.Logger.Warn($"NETCONTENT FOR {peerId}: " + string.Join('\t', versionInfo.otherMods ?? []));
            Configs[peerId] = Read(Traverse.Create(versionInfo)) ?? new([]);
        }
    }
    // public static readonly Dictionary<ulong, PeerVersionInfo> GameInfos = [];
    static public readonly Dictionary<ulong, NetModSettings> Configs = [];
    static public IEnumerable<ulong> AllPlayers()
    {
        if (LocalContext.NetId is { } self) yield return self;
        foreach (var item in Configs.Keys)
        {
            yield return item;
        }
    }

    private const string Prefix = "__mspainsettings__:";
    // TODO: beta main compat
    // static NetModSettings? Read(PeerVersionInfo info)
    // {
    //     var item = info.otherMods?.FirstOrDefault(value => value.StartsWith(Prefix));
    //     if (item == null) return null;
    //     return Read(item);
    // }
    static NetModSettings? Read(Traverse info)
    {
        var item = info.Property("otherMods").GetValue<List<string>?>()?.FirstOrDefault(value => value.StartsWith(Prefix));
        if (item == null) return null;
        return Read(item);
    }

    static NetModSettings Read(string item)
    {
        return new(item.Split(':').Skip(1).ToHashSet());
    }

    public static NetModSettings? Read(ulong? peer)
    {
        if (peer is not { } Peer) return null;
        if (!Configs.TryGetValue(Peer, out var value)) return null;
        return value;
    }
}