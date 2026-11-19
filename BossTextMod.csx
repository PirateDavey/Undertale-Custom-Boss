// ============================================================================
// BossTextMod.csx - UndertaleModTool script
// Text/dialogue mod + a custom boss ("Captain Crumb") built by reskinning
// an existing early enemy's text and stats.
//
// HOW TO USE
//  1. Back up your own copy of Undertale's data.win first.
//  2. Open UndertaleModTool (github.com/UnderminersTeam/UndertaleModTool).
//  3. File > Open > your data.win
//  4. Scripts > Run other script... > pick this file
//  5. File > Save, then run the game.
//
// NOTE: This script is untested against your game version. Internal object
// and code names can differ between versions, so every edit is guarded:
// anything not found is skipped and reported at the end instead of crashing.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

EnsureDataLoaded();

// ---------------------------------------------------------------------------
// CONFIG - edit these to taste
// ---------------------------------------------------------------------------
string bossName = "Captain Crumb";
int bossHp = 120;       // HP of the new boss
int bossAttack = 6;     // damage per hit
int bossDefense = 2;

// Which existing enemy to reskin as the boss (object name in the game)
string hostEnemy = "obj_froggit";

// Boss dialogue shown in the battle bubble (one entry per turn, loops)
string[] bossLines = new string[] {
    "I AM CAPTAIN CRUMB!\nTERROR OF THE BREAKFAST TABLE!",
    "YOU DARE DODGE MY\nCRUMBS OF DOOM?",
    "MY BAKERY FLEET WILL\nSTOP AT NOTHING!",
    "...NOT BAD, SMALL ONE.",
    "FINE! I'LL USE MY\nSECRET ATTACK!!",
    "...I MAY HAVE OVERBAKED\nTHAT ONE."
};

// Flavor text shown in the menu each turn
string[] flavorLines = new string[] {
    "Captain Crumb blocks the way.",
    "Smells like toast.",
    "Captain Crumb adjusts his tiny hat.",
    "Crumbs rain from the ceiling.",
    "Captain Crumb looks a little tired."
};

// Global find/replace across all game strings (the "text mod" part).
// Keys are matched as exact substrings of game text.
var textSwaps = new Dictionary<string, string>() {
    { "* You feel your sins crawling on your back.", "* You feel crumbs crawling on your back." },
    { "* But nobody came.",                          "* But the toast did not arrive." },
    { "Stay determined...",                          "Stay toasty..." }
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
var report = new List<string>();

string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");

string ArrayLiteral(string[] items) =>
    "[" + string.Join(", ", items.Select(x => "\"" + Esc(x) + "\"")) + "]";

// ---------------------------------------------------------------------------
// 1) Global text swaps
// ---------------------------------------------------------------------------
int swapped = 0;
foreach (var str in Data.Strings)
{
    if (str?.Content == null) continue;
    foreach (var kv in textSwaps)
    {
        if (str.Content.Contains(kv.Key))
        {
            str.Content = str.Content.Replace(kv.Key, kv.Value);
            swapped++;
        }
    }
}
report.Add("Text swaps applied: " + swapped);

// ---------------------------------------------------------------------------
// 2) Boss: patch the host enemy's Create event (name, stats, text)
// ---------------------------------------------------------------------------
string createName = "gml_Object_" + hostEnemy + "_Create_0";
try
{
    string original = GetDecompiledText(createName);
    if (string.IsNullOrEmpty(original))
        throw new Exception("code entry not found");

    // Appended after the original Create code so it overrides stats/text.
    string patch =
        "\n// ---- BossTextMod overrides ----\n" +
        "name = \"" + Esc(bossName) + "\";\n" +
        "hp = " + bossHp + ";\n" +
        "maxhp = " + bossHp + ";\n" +
        "atk = " + bossAttack + ";\n" +
        "def = " + bossDefense + ";\n" +
        "bossLines = " + ArrayLiteral(bossLines) + ";\n" +
        "flavorLines = " + ArrayLiteral(flavorLines) + ";\n" +
        "bossTurn = 0;\n";

    ImportGMLString(createName, original + patch);
    report.Add("Boss stats/text patched on " + hostEnemy);
}
catch (Exception e)
{
    report.Add("SKIPPED boss Create patch (" + createName + "): " + e.Message);
}

// ---------------------------------------------------------------------------
// 3) Boss: cycle the dialogue each turn (Step event hook)
// ---------------------------------------------------------------------------
string stepName = "gml_Object_" + hostEnemy + "_Step_0";
try
{
    string original = GetDecompiledText(stepName);
    if (string.IsNullOrEmpty(original))
        throw new Exception("code entry not found");

    string hook =
        "\n// ---- BossTextMod dialogue cycling ----\n" +
        "if (variable_instance_exists(id, \"bossLines\"))\n" +
        "{\n" +
        "    if (variable_instance_exists(id, \"mytalk\"))\n" +
        "        msg = bossLines[bossTurn mod array_length_1d(bossLines)];\n" +
        "}\n";

    ImportGMLString(stepName, original + hook);
    report.Add("Boss dialogue hook added on " + hostEnemy);
}
catch (Exception e)
{
    report.Add("SKIPPED boss Step hook (" + stepName + "): " + e.Message);
}

// ---------------------------------------------------------------------------
// Done
// ---------------------------------------------------------------------------
ScriptMessage("BossTextMod finished:\n\n" + string.Join("\n", report) +
              "\n\nRemember to File > Save.");
