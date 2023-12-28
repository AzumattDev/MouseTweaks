using HarmonyLib;
using UnityEngine;

namespace MouseTweaks;

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.GetHoveredElement))]
static class InventoryGridGetHoveredElementPatch
{
    static void Postfix(InventoryGrid __instance, ref InventoryGrid.Element __result)
    {
        if ((ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl)) && __result != null)
        {
            // If my left click is down
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (__instance.m_onSelected == null)
                    return;
                var item = __instance.GetInventory().GetItemAt(__instance.GetButtonPos(__result.m_go).x, __instance.GetButtonPos(__result.m_go).y);
                if (item != null)
                {
                    __instance.m_onSelected(__instance, item, __result.m_pos, InventoryGrid.Modifier.Move);
                }
            }
        }
    }
}