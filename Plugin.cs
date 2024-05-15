using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace MouseTweaks
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class MouseTweaksPlugin : BaseUnityPlugin
    {
        internal const string ModName = "MouseTweaks";
        internal const string ModVersion = "1.0.2";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private static string ConfigFileName = $"{ModGUID}.cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource MouseTweaksLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        internal static ConfigEntry<KeyboardShortcut> InventoryUseItemKeyCodeConfig = null!;
        internal static ConfigEntry<KeyboardShortcut> MoveNDrop = null!;
        internal static ConfigEntry<KeyboardShortcut> MoveNDrop2 = null!;

        public void Awake()
        {
            InventoryUseItemKeyCodeConfig = Config.Bind("1 - Input", "InventoryUseItem", new KeyboardShortcut(KeyCode.Mouse1), "Keybinding for using/consuming/equipping an item in the inventory (Mouse1 = right mouse button).\nIf this is set to anything other than Mouse1, you no longer have to hold shift to split a stack when right clicking.\nAvailable KeyCodes can be found here: https://docs.unity3d.com/ScriptReference/KeyCode.html");
            MoveNDrop = Config.Bind("1 - Input", "Move & Drop Key1", new KeyboardShortcut(KeyCode.LeftControl), "Keybinding that is held down to quick move or drop items from the player inventory.\nIf this is blank (None), the behaviour will not run and moving/dropping items quickly won't happen.\nAvailable KeyCodes can be found here: https://docs.unity3d.com/ScriptReference/KeyCode.html");
            MoveNDrop2 = Config.Bind("1 - Input", "Move & Drop Key2", new KeyboardShortcut(KeyCode.RightControl), "Alternative Keybinding that is held down to quick move or drop items from the player inventory. This is just defining a secondary key that you can also hold down.\nIf this is blank (None), the behaviour will not run and moving/dropping items quickly won't happen.\nAvailable KeyCodes can be found here: https://docs.unity3d.com/ScriptReference/KeyCode.html");
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                MouseTweaksLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                MouseTweaksLogger.LogError($"There was an issue loading your {ConfigFileName}");
                MouseTweaksLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
    }

    public static class KeyboardExtensions
    {
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }
    }
}