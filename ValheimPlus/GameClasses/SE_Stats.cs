using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ValheimPlus.GameClasses {
    class BetterStatsTooltip {

		[HarmonyPatch(typeof(SE_Stats), "GetTooltipString")]
		public static class SE_Stats_GetTooltipString_Patch {
            private static void Postfix(SE_Stats __instance, ref string __result) {
				__result = "";
				if (__instance.m_tooltip.Length > 0)
				{
					__result = __result + __instance.m_tooltip + "\n";
				}
				if (__instance.m_jumpStaminaUseModifier != 0f)
				{
					__result = __result + $"$se_jumpstamina: <color=orange>{(__instance.m_jumpStaminaUseModifier * 100f).ToString("+0;-0") }%</color>\n";
				}
				if (__instance.m_runStaminaDrainModifier != 0f)
				{
					__result = __result + $"$se_runstamina: <color=orange>{(__instance.m_runStaminaDrainModifier * 100f).ToString("+0;-0")}%</color>\n";
				}
				if (__instance.m_healthOverTime != 0f)
				{
					__result = __result + $"$se_health: <color=orange>{__instance.m_healthOverTime.ToString()}</color>\n";
				}
				if (__instance.m_staminaOverTime != 0f)
				{
					__result = __result + $"$se_stamina: <color=orange>{__instance.m_staminaOverTime.ToString()}</color>\n";
				}
				if (__instance.m_healthRegenMultiplier != 1f)
				{
					__result = __result + $"$se_healthregen: <color=orange>{((__instance.m_healthRegenMultiplier - 1f) * 100f).ToString("+0;-0")}%</color>\n";
				}
				if (__instance.m_staminaRegenMultiplier != 1f)
				{
					__result = __result + $"$se_staminaregen: <color=orange>{((__instance.m_staminaRegenMultiplier - 1f) * 100f).ToString("+0;-0")}%</color>\n";
				}
				if (__instance.m_addMaxCarryWeight != 0f)
				{
					__result = __result + $"$se_max_carryweight: <color=orange>{__instance.m_addMaxCarryWeight.ToString("+0;-0")}</color>\n";
				}
				if (__instance.m_mods.Count > 0)
				{
					__result += SE_Stats.GetDamageModifiersTooltipString(__instance.m_mods).Substring(1) + "\n";
				}
				if (__instance.m_noiseModifier != 0f)
				{
					__result = __result + $"$se_noisemod: <color=orange>{(__instance.m_noiseModifier * 100f).ToString("+0;-0")}%</color>\n";
				}
				if (__instance.m_stealthModifier != 0f)
				{
					__result = __result + $"$se_sneakmod: <color=orange>{(-__instance.m_stealthModifier * 100f).ToString("+0;-0")}%</color>\n";
				}
			}
		}
    }
}
