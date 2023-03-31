using HarmonyLib;
using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;

public class CustomGameOptionsHarmony
{
    private const string CustomAttrGamePrefSelectorId = "CustomAttrGamePrefSelectorId";
    private const string CustomAttrDefaultValue = "CustomAttrDefaultValue";
    private const string CustomNamePrefix = "Custom";
    public static int GamePrefValue = 500; // We can no longer use LAST as TFP decided to ignore Last and put enums past Last (Seriously WTF). So need to set a manual number for this

    private const string SCoreAssemblyName = "SCore,";
    private const string SCoreConfigurationClass = "Configuration";
    private const string SCoreCheckFeatureStatusMethod = "CheckFeatureStatus";

    public class CustomGameOptionsHarmony_Init : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out(" Loading Patch: " + this.GetType().ToString());

            // Reduce extra logging stuff
            Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(UnityEngine.LogType.Warning, StackTraceLogType.None);

            var harmony = new HarmonyLib.Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Need to manually initiate for dedicated servers but does no harm to initiate for MP/SP as well
            CustomGameOptions.LoadConfigs();
        }
    }

    [HarmonyPatch(typeof(XUiC_GamePrefSelector))]
    [HarmonyPatch("Init")]
    [HarmonyPatch(new Type[] { })]
    public class PatchXUiC_GamePrefSelectorInit
    {
        static bool Prefix(XUiC_GamePrefSelector __instance)
        {
            if (__instance.ViewComponent.ID.StartsWith(CustomNamePrefix))
            {
                // Store the ViewComonponent.ID in temp storage for use in Postfix and other Harmony methods below
                __instance.CustomAttributes.Set(CustomAttrGamePrefSelectorId, __instance.ViewComponent.ID);

                // Temporarily use enum 'Last'
                AccessTools.Field(typeof(XUiView), "id").SetValue(__instance.ViewComponent, "Last");
            }

            return true;
        }

        static void Postfix(XUiC_GamePrefSelector __instance,
            ref EnumGamePrefs ___gamePref,
            ref GamePrefs.EnumType ___valueType)
        {
            // If we are a Custom GameOption then get the correct ViewComponent ID from temp storage, then set the correct ViewComponent.ID and the GamePref to a new custom Enum value
            var id = __instance.CustomAttributes.GetString(CustomAttrGamePrefSelectorId);
            if (id.StartsWith(CustomNamePrefix))
            {
                GamePrefValue++;
                ___gamePref = (EnumGamePrefs)GamePrefValue;

                AccessTools.Field(typeof(XUiView), "id").SetValue(__instance.ViewComponent, id);

                // Set DefaultValue if nothing was loaded from configs
                if(!CustomGameOptions.HasValue(id))
                {
                    switch (___valueType)
                    {
                        case GamePrefs.EnumType.Int:
                            CustomGameOptions.SetValue(id, __instance.CustomAttributes.GetInt(CustomAttrDefaultValue));
                            break;
                        case GamePrefs.EnumType.String:
                            CustomGameOptions.SetValue(id, __instance.CustomAttributes.GetString(CustomAttrDefaultValue));
                            break;
                        case GamePrefs.EnumType.Bool:
                            CustomGameOptions.SetValue(id, __instance.CustomAttributes.GetBool(CustomAttrDefaultValue));
                            break;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(XUiC_GamePrefSelector))]
    [HarmonyPatch("SetVisible")]
    [HarmonyPatch(new Type[] { typeof(bool) })]
    public class PatchXUiC_GamePrefSelectorSetVisible
    {
        static bool Prefix(XUiC_GamePrefSelector __instance)
        {
            if (IsCustomGameOption(__instance))
            {
                __instance.ViewComponent.IsVisible = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(XUiC_GamePrefSelector))]
    [HarmonyPatch("ControlCombo_OnValueChanged")]
    [HarmonyPatch(new Type[] { typeof(XUiController), typeof(XUiC_GamePrefSelector.GameOptionValue), typeof(XUiC_GamePrefSelector.GameOptionValue) })]
    public class PatchXUiC_GamePrefSelectorControlCombo_OnValueChanged
    {
        static bool Prefix(XUiC_GamePrefSelector __instance,
            XUiC_GamePrefSelector.GameOptionValue _newValue,
            ref GamePrefs.EnumType ___valueType)
        {
            if (IsCustomGameOption(__instance))
            {
                var id = __instance.CustomAttributes.GetString(CustomAttrGamePrefSelectorId);

                switch (___valueType)
                {
                    case GamePrefs.EnumType.Int:
                        CustomGameOptions.SetValue(id, _newValue.IntValue);
                        break;
                    case GamePrefs.EnumType.String:
                        CustomGameOptions.SetValue(id, _newValue.StringValue);
                        break;
                    case GamePrefs.EnumType.Bool:
                        CustomGameOptions.SetValue(id, _newValue.IntValue == 1);
                        break;
                }

                AccessTools.Method(typeof(XUiC_GamePrefSelector), "CheckDefaultValue").Invoke(__instance, new object[] { });
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(XUiC_GamePrefSelector))]
    [HarmonyPatch("ControlText_OnChangeHandler")]
    [HarmonyPatch(new Type[] { typeof(XUiController), typeof(string), typeof(bool) })]
    public class PatchXUiC_GamePrefSelectorControlText_OnChangeHandler
    {
        static bool Prefix(XUiC_GamePrefSelector __instance,
            string _text,
            ref GamePrefs.EnumType ___valueType)
        {
            if (IsCustomGameOption(__instance))
            {
                var id = __instance.CustomAttributes.GetString(CustomAttrGamePrefSelectorId);

                switch (___valueType)
                {
                    case GamePrefs.EnumType.Int:
                        {
                            if (int.TryParse(_text, out int value))
                            {
                                CustomGameOptions.SetValue(id, value);
                            }
                            break;
                        }
                    case GamePrefs.EnumType.String:
                        CustomGameOptions.SetValue(id, _text);
                        break;
                }

                AccessTools.Method(typeof(XUiC_GamePrefSelector), "CheckDefaultValue").Invoke(__instance, new object[] { });
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(XUiC_GamePrefSelector))]
    [HarmonyPatch("SetCurrentValue")]
    [HarmonyPatch(new Type[] { })]
    public class PatchXUiC_GamePrefSelectorSetCurrentValue
    {
        static bool Prefix(XUiC_GamePrefSelector __instance,
            ref string[] ___valuesFromXml,
            ref GamePrefs.EnumType ___valueType,
            ref bool ___isTextInput,
            ref XUiC_ComboBoxList<XUiC_GamePrefSelector.GameOptionValue> ___controlCombo,
            ref XUiC_TextInput ___controlText)
        {
            if (IsCustomGameOption(__instance))
            {
                var id = __instance.CustomAttributes.GetString(CustomAttrGamePrefSelectorId);

                switch (___valueType)
                {
                    case GamePrefs.EnumType.Int:
                        {
                            if(___isTextInput)
                            {
                                ___controlText.Text = CustomGameOptions.GetInt(id).ToString();
                                break;
                            }

                            for (int i = 1; i < ___controlCombo.Elements.Count; i++)
                            {
                                if (___controlCombo.Elements[i].IntValue == CustomGameOptions.GetInt(id))
                                {
                                    ___controlCombo.SelectedIndex = i;
                                    break;
                                }
                            }

                            break;
                        }
                    case GamePrefs.EnumType.String:
                        {
                            if (___isTextInput)
                            {
                                ___controlText.Text = CustomGameOptions.GetString(id);
                                break;
                            }

                            for (int i = 1; i < ___controlCombo.Elements.Count; i++)
                            {
                                if (___controlCombo.Elements[i].StringValue == CustomGameOptions.GetString(id))
                                {
                                    ___controlCombo.SelectedIndex = i;
                                    break;
                                }
                            }
                            break;
                        }
                    case GamePrefs.EnumType.Bool:
                        ___controlCombo.SelectedIndex = CustomGameOptions.GetBool(id) ? 1 : 0;
                        break;
                }

                AccessTools.Method(typeof(XUiC_GamePrefSelector), "CheckDefaultValue").Invoke(__instance, new object[] { });
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(XUiC_GamePrefSelector))]
    [HarmonyPatch("IsDefaultValueForGameMode")]
    [HarmonyPatch(new Type[] { })]
    public class PatchXUiC_GamePrefSelectorIsDefaultValueForGameMode
    {
        static bool Prefix(XUiC_GamePrefSelector __instance,
            ref bool __result,
            ref GamePrefs.EnumType ___valueType,
            ref XUiC_ComboBoxList<XUiC_GamePrefSelector.GameOptionValue> ___controlCombo,
            ref XUiC_TextInput ___controlText,
            ref bool ___isTextInput)
        {
            if (IsCustomGameOption(__instance))
            {
                switch (___valueType)
                {
                    case GamePrefs.EnumType.Int:
                        {
                            int intValue;
                            if (___isTextInput)
                            {
                                StringParsers.TryParseSInt32(___controlText.Text, out intValue, 0, -1, NumberStyles.Integer);
                            }
                            else
                            {
                                intValue = ___controlCombo.Value.IntValue;
                            }
                            __result = intValue == __instance.CustomAttributes.GetInt(CustomAttrDefaultValue);
                            break;
                        }
                    case GamePrefs.EnumType.String:
                        {
                            if (___isTextInput)
                            {
                                __result = ___controlText.Text == __instance.CustomAttributes.GetString(CustomAttrDefaultValue);
                                break;
                            }
                            __result = ___controlCombo.Value.StringValue == __instance.CustomAttributes.GetString(CustomAttrDefaultValue);
                            break;
                        }
                    case GamePrefs.EnumType.Bool:
                        __result = ___controlCombo.Value.IntValue == 1 == __instance.CustomAttributes.GetBool(CustomAttrDefaultValue);
                        break;
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(XUiC_NewContinueGame))]
    [HarmonyPatch("SaveGameOptions")]
    [HarmonyPatch(new Type[] { })]
    public class PatchXUiC_NewContinueGameSaveGameOptions
    {
        static void Postfix(XUiC_NewContinueGame __instance)
        {
            Log.Out("SaveGameOptions");
            CustomGameOptions.SaveGameOptions();
        }
    }

    [HarmonyPatch(typeof(XUiC_GamePrefSelector))]
    [HarmonyPatch("ParseAttribute")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(XUiController) })]
    public class PatchXUiC_GamePrefSelectorParseAttribute
    {
        static bool Prefix(XUiC_GamePrefSelector __instance,
            ref string _name,
            ref string _value,
            ref XUiController _parent,
            ref GamePrefs.EnumType ___valueType)
        {
            // Load default values from windows.xml and save in CustomAttributes
            if (_name == "default_value" 
                && _value != "default_value")
            {
                __instance.CustomAttributes.Set(CustomAttrDefaultValue, _value);

                return false;
            }

            return true;
        }
    }

    private static bool IsCustomGameOption(XUiC_GamePrefSelector __instance)
    {
        var id = __instance.CustomAttributes.GetString(CustomAttrGamePrefSelectorId);

        return !string.IsNullOrEmpty(id) && id.StartsWith(CustomNamePrefix);
    }

    [HarmonyPatch()]
    public class PatchScoreConfigurationCheckFeatureStatus
    {
        [HarmonyPrepare]
        public static bool Prepare(MethodBase original)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly?.FullName.StartsWith(SCoreAssemblyName) != true) continue;

                foreach (Type type in assembly.GetTypes())
                {
                    if (type.FullName != SCoreConfigurationClass) continue;

                    return type.GetMethod(SCoreCheckFeatureStatusMethod, new Type[] { typeof(string), typeof(string) }) != null;
                }
            }

            return false;
        }


        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly?.FullName.StartsWith(SCoreAssemblyName) != true) continue;

                foreach (Type type in assembly.GetTypes())
                {
                    if (type?.FullName != SCoreConfigurationClass) continue;

                    return type.GetMethod(SCoreCheckFeatureStatusMethod, new Type[] { typeof(string), typeof(string) });
                }
            }

            return null; 
        }

        static bool Prefix(ref bool __result, string strClass, string strFeature)
        {
            var variableName = $"{CustomNamePrefix}{strClass}{strFeature}";

            if(CustomGameOptions.HasValue(variableName))
            {
                __result = CustomGameOptions.GetBool(variableName);
                return false;
            }

            return true;
        }
    }

    // EXAMPLE CODE 

    //private static int LastLoggedMinute = 0;

    //[HarmonyPatch(typeof(ChunkAreaBiomeSpawnData))]
    //[HarmonyPatch("IsSpawnNeeded")]
    //[HarmonyPatch(new Type[] { typeof(WorldBiomes), typeof(ulong) })]
    //public class PatchChunkAreaBiomeSpawnDataIsSpawnNeeded
    //{
    //    static bool Prefix()
    //    {
    //        if (DateTime.Now.Second == 0 && LastLoggedMinute != DateTime.Now.Minute)
    //        {
    //            Log.Out("CustomIntValue = " + CustomGameOptions.GetInt("CustomIntValue"));
    //            Log.Out("CustomIntInput = " + CustomGameOptions.GetInt("CustomIntInput"));
    //            Log.Out("CustomBool = " + CustomGameOptions.GetBool("CustomBool"));
    //            Log.Out("CustomBoolDefaulted = " + CustomGameOptions.GetBool("CustomBoolDefaulted"));
    //            Log.Out("CustomStringValue = " + CustomGameOptions.GetString("CustomStringValue"));
    //            Log.Out("CustomStringInput = " + CustomGameOptions.GetString("CustomStringInput"));
    //            Log.Out(" ------------------------------------------ ");
    //        }

    //        return true;
    //    }
    //}
}

