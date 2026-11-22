#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// While the Unity editor is open, asks Windows not to put the laptop to sleep (the way a video player does), so
    /// remote work on the project can carry on overnight. The screen may still switch off. Nothing is changed in the
    /// power settings: the request ends the moment Unity closes, or untick ASBO/Keep Laptop Awake.
    /// </summary>
    [InitializeOnLoad]
    public static class ASBOAwake
    {
        const uint ES_CONTINUOUS = 0x80000000, ES_SYSTEM_REQUIRED = 0x00000001, ES_AWAYMODE_REQUIRED = 0x00000040;
        const string Key = "ASBO.KeepAwake", MenuPath = "ASBO/Keep Laptop Awake";

        [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint esFlags);

        static ASBOAwake() { EditorApplication.delayCall += Apply; }

        static bool On { get => EditorPrefs.GetBool(Key, true); set => EditorPrefs.SetBool(Key, value); }

        static void Apply()
        {
            uint r = SetThreadExecutionState(On ? ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED : ES_CONTINUOUS);
            if (r == 0) r = SetThreadExecutionState(On ? ES_CONTINUOUS | ES_SYSTEM_REQUIRED : ES_CONTINUOUS); // away mode isn't on every laptop
            Menu_Check();
            Debug.Log("[ASBO] keep awake " + (On ? "on" : "off") + (r == 0 ? " (Windows refused)" : ""));
        }

        [MenuItem(MenuPath)] static void Toggle() { On = !On; Apply(); }
        static void Menu_Check() { UnityEditor.Menu.SetChecked(MenuPath, On); }
    }
}
#endif
