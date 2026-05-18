using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum NVGMode
{
    GreenPhosphor,
    WhitePhosphor,
    Monochrome,
    FullColor
}

[BepInPlugin("com.Domiyaa.NVGConfig", "NVG Config", "1.0.0")]
public class NVGConfigPlugin : BaseUnityPlugin
{
    public static ConfigEntry<NVGMode> SelectedNVGMode;
    public static BepInEx.Logging.ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;

        SelectedNVGMode = Config.Bind(
            "Settings",
            "Filter Mode",
            NVGMode.GreenPhosphor
        );

        var harmony = new Harmony("com.Domiyaa.NVGConfigNVG");
        harmony.PatchAll();

        Logger.LogInfo("White Phosphor NVG loaded");
    }
}

[HarmonyPatch(typeof(NightVision), "Start")]
public class NightVision_Start_Patch
{
    static void Postfix(NightVision __instance)
    {;
        Volume postProcessing = Traverse.Create(__instance).Field("postProcessing").GetValue<Volume>();

        if (postProcessing == null)
            return;

        if (!postProcessing.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            return;

        NightVision_Update_Patch.origSaturation = colorAdjustments.saturation.value;
        NightVision_Update_Patch.origContrast = colorAdjustments.contrast.value;
        NightVision_Update_Patch.origColorFilter = colorAdjustments.colorFilter.value;
        NightVision_Update_Patch.origHueShift = colorAdjustments.hueShift.value;
        NightVision_Update_Patch.cached = true;
    }
}

[HarmonyPatch(typeof(NightVision), "Update")]
public class NightVision_Update_Patch
{
    public static bool cached = false;
    public static float origSaturation;
    public static float origContrast;
    public static Color origColorFilter;
    public static float origHueShift;

    static void Postfix(NightVision __instance)
    {
        if (!cached)
            return;

        NVGMode mode = NVGConfigPlugin.SelectedNVGMode.Value;

        bool nightVisActive = Traverse.Create(__instance).Field("nightVisActive").GetValue<bool>();
        Volume postProcessing = Traverse.Create(__instance).Field("postProcessing").GetValue<Volume>();

        if (postProcessing == null)
            return;

        if (!postProcessing.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            return;

        if (nightVisActive)
        {
            if (mode == NVGMode.GreenPhosphor)
            {
                colorAdjustments.saturation.value = origSaturation;
                colorAdjustments.contrast.value = origContrast;
                colorAdjustments.colorFilter.value = origColorFilter;
                colorAdjustments.hueShift.value = origHueShift;
            }
            else if (mode == NVGMode.WhitePhosphor)
            {
                colorAdjustments.saturation.value = -80f;
                colorAdjustments.contrast.value = 40f;
                colorAdjustments.colorFilter.value = new Color(0.0f, 0.8f, 0.8f);
                colorAdjustments.hueShift.value = origHueShift;
            }
            else if (mode == NVGMode.Monochrome)
            {
                colorAdjustments.saturation.value = -100f;
                colorAdjustments.contrast.value = 40f;
                colorAdjustments.colorFilter.value = Color.white;
                colorAdjustments.hueShift.value = origHueShift;
            }
            else if (mode == NVGMode.FullColor)
            {
                colorAdjustments.saturation.value = 40f;
                colorAdjustments.contrast.value = 15f;
                colorAdjustments.colorFilter.value = Color.white;
                colorAdjustments.hueShift.value = origHueShift;
            }
        }
        else
        {
            colorAdjustments.saturation.value = origSaturation;
            colorAdjustments.contrast.value = origContrast;
            colorAdjustments.colorFilter.value = origColorFilter;
            colorAdjustments.hueShift.value = origHueShift;
        }
    }
}
