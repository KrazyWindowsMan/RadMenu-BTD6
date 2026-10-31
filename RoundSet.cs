using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(RoundSet.RoundSet), "RoundSet", "1.0.0", "silly")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace RoundSet;

public class RoundSet : BloonsTD6Mod
{
    public static readonly ModSettingInt TargetRound = new(100)
    {
        displayName = "Target Round",
        min = 1,
        max = 999999999999999999999
    };

    public static readonly ModSettingHotkey SetRoundKey = new(KeyCode.F6)
    {
        displayName = "Set Round Hotkey"
    };

    public override void OnUpdate()
    {
        if (InGame.instance == null) return;

        if (SetRoundKey.JustPressed())
        {
            // Rounds are 0-indexed internally, so round 40 on screen is 39
            InGame.instance.bridge.SetRound(TargetRound - 1);
        }
    }
}