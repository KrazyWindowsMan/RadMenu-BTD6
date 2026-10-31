using System;
using System.Globalization;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Map;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CheatMenu.Main), "RadMenu", "1.0.0", "silly")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace CheatMenu;

public class Main : BloonsTD6Mod
{
    public static readonly ModSettingHotkey MenuKey = new(KeyCode.F2)
    {
        displayName = "Cheat menu hotkey"
    };

    private bool menuOpen;
    private int tab;
    private string status = "";

    private bool freezeLives;
    private double frozenLives;
    private bool speedOn;
    private float speed = 1f;

    private bool noCooldowns;
    private bool freeStuff;
    private bool placeAnywhere;
    private bool damageOn;
    private float damageMult = 1f;

    public override void OnUpdate()
    {
        if (MenuKey.JustPressed())
        {
            menuOpen = !menuOpen;
        }

        if (InGame.instance == null) return;
        if (Game.instance != null && Game.instance.CanGetFlagged()) return;

        if (freezeLives)
        {
            InGame.instance.SetHealth(frozenLives);
        }

        // only touch the speed while the game isn't paused
        if (speedOn && Time.timeScale > 0f)
        {
            Time.timeScale = speed;
        }
    }

    // these read their toggles when a match starts
    public override void OnNewGameModel(GameModel result)
    {
        // stay out of modes that can get you flagged
        if (Game.instance != null && Game.instance.CanGetFlagged()) return;

        foreach (var tower in result.towers)
        {
            if (freeStuff) tower.cost = 0;
            if (placeAnywhere) MakePlaceable(tower);

            if (noCooldowns)
            {
                var abilities = tower.GetDescendants<AbilityModel>().ToList();
                for (var i = 0; i < abilities.Count; i++)
                {
                    abilities[i].cooldown = 0;
                }
            }

            if (damageOn)
            {
                var damages = tower.GetDescendants<DamageModel>().ToList();
                for (var i = 0; i < damages.Count; i++)
                {
                    damages[i].damage *= damageMult;
                }
            }
        }

        if (freeStuff)
        {
            foreach (var upgrade in result.upgrades)
            {
                upgrade.cost = 0;
            }
        }
    }

    public override void OnGUI()
    {
        if (!menuOpen) return;

        GUILayout.BeginArea(new Rect(40, 40, 430, 520), GUI.skin.box);
        GUILayout.Label("Cheat menu (F2 closes it)");

        if (Game.instance != null && Game.instance.CanGetFlagged())
        {
            GUILayout.Label("Cheats are off in this mode because it can get you flagged.");
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("In match")) tab = 0;
        if (GUILayout.Button("Profile")) tab = 1;
        GUILayout.EndHorizontal();

        if (tab == 0) MatchTab();
        else ProfileTab();

        GUILayout.Label(status);
        GUILayout.EndArea();
    }

    private void MatchTab()
    {
        if (GUILayout.Button("Set cash"))
        {
            AskNumber("Set cash", "Type how much cash you want", cash =>
            {
                if (InGame.instance == null) { status = "Start a match first"; return; }
                InGame.instance.SetCash(cash);
                status = "Cash is now " + cash;
            });
        }

        if (GUILayout.Button("Set lives"))
        {
            AskNumber("Set lives", "Type how many lives you want", lives =>
            {
                if (InGame.instance == null) { status = "Start a match first"; return; }
                InGame.instance.SetMaxHealth(lives);
                InGame.instance.SetHealth(lives);
                frozenLives = lives;
                status = "Lives are now " + lives;
            });
        }

        if (GUILayout.Button("Go to round"))
        {
            AskNumber("Go to round", "Type the round number", round =>
            {
                if (InGame.instance == null) { status = "Start a match first"; return; }
                if (round < 1) { status = "Round has to be 1 or higher"; return; }
                // rounds count from 0 internally
                InGame.instance.bridge.SetRound((int)round - 1);
                status = "Jumped to round " + (int)round;
            });
        }

        if (GUILayout.Button("Game speed (now x" + speed.ToString("0.##") + ")"))
        {
            AskNumber("Game speed", "Type a speed, 1 is normal", s =>
            {
                if (s <= 0) { status = "Speed has to be above 0"; return; }
                speed = (float)s;
                speedOn = speed != 1f;
                if (!speedOn) Time.timeScale = 1f;
                status = "Speed set to x" + speed;
            });
        }

        var wantFreeze = GUILayout.Toggle(freezeLives, "Freeze lives");
        if (wantFreeze != freezeLives)
        {
            freezeLives = wantFreeze;
            if (freezeLives && InGame.instance != null) frozenLives = InGame.instance.GetHealth();
        }

        if (GUILayout.Button("Pop all bloons"))
        {
            if (InGame.instance == null) status = "Start a match first";
            else
            {
                foreach (var bloon in InGame.instance.GetAllBloonToSim().ToList())
                {
                    bloon.GetSimBloon().Destroy();
                }
                status = "Popped everything on screen";
            }
        }

        GUILayout.Label("These kick in when a match starts, so restart after changing them:");
        noCooldowns = GUILayout.Toggle(noCooldowns, "No ability cooldowns");
        freeStuff = GUILayout.Toggle(freeStuff, "Free towers and upgrades");
        var wantAnywhere = GUILayout.Toggle(placeAnywhere, "Place towers anywhere");
        if (wantAnywhere != placeAnywhere)
        {
            placeAnywhere = wantAnywhere;
            ApplyPlaceAnywhereNow();
        }
        damageOn = GUILayout.Toggle(damageOn, "Damage multiplier: x" + damageMult.ToString("0.0"));
        damageMult = GUILayout.HorizontalSlider(damageMult, 1f, 20f);
    }

    private void ProfileTab()
    {
        GUILayout.Label("Profile changes may not save, and can get a real account flagged.");

        if (GUILayout.Button("Set Monkey Money"))
        {
            AskNumber("Set Monkey Money", "Type how much Monkey Money you want", money =>
            {
                Game.instance.SetMonkeyMoney(money);
                status = "Monkey Money is now " + money;
            });
        }

        if (GUILayout.Button("Set knowledge points"))
        {
            AskNumber("Set knowledge points", "Type how many points you want", points =>
            {
                Game.instance.GetBtd6Player().Data.knowledgePoints.Value = (int)points;
                status = "Knowledge points set to " + (int)points;
            });
        }

        if (GUILayout.Button("Unlock all heroes"))
        {
            var data = Game.instance.GetBtd6Player().Data;
            foreach (var tower in Game.instance.model.towers)
            {
                if (tower.IsHero()) data.UnlockTower(tower.baseId);
            }
            status = "Heroes unlocked";
        }
    }

    // lets a tower go on water, the track, on top of other towers, anywhere
    private static void MakePlaceable(TowerModel tower)
    {
        tower.ignoreBlockers = true;

        var areas = Enum.GetValues(typeof(AreaType)).Cast<AreaType>().ToArray();
        tower.areaTypes = new Il2CppStructArray<AreaType>(areas);

        if (tower.footprint != null)
        {
            tower.footprint.ignoresPlacementCheck = true;
            // placed monkeys also stop blocking whatever you place next
            tower.footprint.doesntBlockOtherTowers = true;
        }
    }

    // tries to apply the toggle to the match that's already running
    private void ApplyPlaceAnywhereNow()
    {
        if (!placeAnywhere)
        {
            status = "Place anywhere is off, restart the match to fully undo it";
            return;
        }

        if (InGame.instance == null)
        {
            status = "Place anywhere is on for your next match";
            return;
        }

        try
        {
            foreach (var tower in InGame.instance.GetGameModel().towers)
            {
                MakePlaceable(tower);
            }
            status = "Place anywhere is on. If it doesn't work yet, restart the match";
        }
        catch (Exception e)
        {
            MelonLogger.Warning("Place anywhere live apply failed: " + e.Message);
            status = "Place anywhere is on for your next match";
        }
    }

    // BTD6 strips Unity's text box code, so GUILayout.TextField crashes.
    // This opens the game's own popup to type into instead.
    private void AskNumber(string title, string hint, Action<double> apply)
    {
        PopupScreen.instance.SafelyQueue(screen =>
        {
            screen.ShowSetNamePopup(
                title,
                hint,
                new Action<string>(text =>
                {
                    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        apply(value);
                    }
                    else
                    {
                        status = "That wasn't a number";
                    }
                }),
                "");

            // plain text box, so numbers aren't filtered out
            screen.ModifyField(field =>
            {
                field.contentType = TMP_InputField.ContentType.Standard;
            });
        });
    }
}
