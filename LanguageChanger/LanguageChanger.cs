using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using KSP.UI.Screens;
using System.Text.RegularExpressions;

[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
public class LanguageChanger : MonoBehaviour
{
    private ApplicationLauncherButton button;
    private readonly string languagesRootPath = "GameData/LanguageChanger/Languages/";
    private readonly string squadLocPath = "GameData/Squad/Localization/";
    private readonly string serenityLocPath = "GameData/SquadExpansion/Serenity/Localization/";
    private readonly string buildIDFile = "buildID64.txt";

    private List<string> availableLanguages = new List<string>();
    private int selectedLanguageIndex = 0;
    private bool showGUI = false;
    private Rect windowRect = new Rect(300, 100, 350, 500);

    private string pendingLanguage = null;
    private bool showConfirmDialog = false;

    public void Start()
    {
        LoadLanguages();
        GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
    }

    private void OnGUIAppLauncherReady()
    {
        if (button == null)
        {
            button = ApplicationLauncher.Instance.AddModApplication(
                ToggleGUI, ToggleGUI,
                null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS,
                GameDatabase.Instance.GetTexture("LanguageChanger/Textures/icon", false)
            );
        }
    }

    private void LoadLanguages()
    {
        string squadPath = Path.Combine(KSPUtil.ApplicationRootPath, languagesRootPath, "Squad");
        if (Directory.Exists(squadPath))
        {
            availableLanguages = Directory.GetDirectories(squadPath)
                .Select(Path.GetFileName)
                .ToList();
        }
        else
        {
            Debug.LogWarning("[LanguageChanger] No Squad languages folder found at: " + squadPath);
        }
    }

    private void ToggleGUI()
    {
        showGUI = !showGUI;
    }

    private void OnGUI()
    {
        if (showGUI)
        {
            GUI.skin = HighLogic.Skin;
            windowRect = GUILayout.Window("LanguageChanger".GetHashCode(), windowRect, DrawWindow, "Language Changer");
        }
        if (showConfirmDialog)
        {
            Rect confirmRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150);
            GUI.ModalWindow("LangConfirmDialog".GetHashCode(), confirmRect, DrawConfirmDialog, "Apply Language Change");
        }
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label("Current Language: " + GetCurrentLanguage());
        GUILayout.Space(10);
        GUILayout.Label("Note: Changes require restarting the game to apply.", HighLogic.Skin.label);
        GUILayout.Space(10);
        GUILayout.Label("Select a Language:");

        foreach (string lang in availableLanguages)
        {
            if (GUILayout.Button(lang))
            {
                selectedLanguageIndex = availableLanguages.IndexOf(lang);
                pendingLanguage = lang;
                showConfirmDialog = true;
            }
        }

        GUI.DragWindow();
    }

    private void DrawConfirmDialog(int id)
    {
        GUILayout.Label("Apply language: " + pendingLanguage + "?");
        GUILayout.Space(10);

        GUILayout.Label("A restart is required.", HighLogic.Skin.label);
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply"))
        {
            ChangeLanguage(pendingLanguage);
            showConfirmDialog = false;
            showGUI = false;
        }
        if (GUILayout.Button("Cancel"))
        {
            showConfirmDialog = false;
        }
        GUILayout.EndHorizontal();
    }


    private string GetCurrentLanguage()
    {
        string path = Path.Combine(KSPUtil.ApplicationRootPath, buildIDFile);
        if (!File.Exists(path)) return "Unknown";

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Trim().StartsWith("language"))
                return line.Split('=')[1].Trim();
        }
        return "Unknown";
    }

    private void ChangeLanguage(string langFolder)
    {
        string squadLangPath = Path.Combine(languagesRootPath, "Squad", langFolder);
        string serenityLangPath = Path.Combine(languagesRootPath, "Serenity", langFolder);

        if (Directory.Exists(squadLangPath))
        {
            CopyLocalization(squadLangPath, squadLocPath);
        }
        if (Directory.Exists(serenityLangPath))
        {
            CopyLocalization(serenityLangPath, serenityLocPath);
        }

        UpdateBuildID(langFolder);
        ScreenMessages.PostScreenMessage("Language changed. A restart IS required.", 5f, ScreenMessageStyle.UPPER_CENTER);
    }

    private void CopyLocalization(string langPath, string targetPath)
    {
        string src = Path.Combine(langPath, "dictionary.cfg");
        string dst = Path.Combine(targetPath, "dictionary.cfg");

        if (File.Exists(src))
        {
            Directory.CreateDirectory(targetPath);
            File.Copy(src, dst, true);
        }
    }

    private void UpdateBuildID(string lang)
    {
        string path = Path.Combine(KSPUtil.ApplicationRootPath, buildIDFile);
        if (!File.Exists(path)) return;

        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith("language"))
            {
                lines[i] = "language = " + lang;
                break;
            }
        }
        File.WriteAllLines(path, lines);
    }

    public void OnDestroy()
    {
        if (button != null)
        {
            ApplicationLauncher.Instance.RemoveModApplication(button);
            button = null;
        }
        GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
    }
}

