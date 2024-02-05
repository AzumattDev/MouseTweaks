using HarmonyLib;
using UnityEngine;

namespace MouseTweaks;

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.GetHoveredElement))]
static class InventoryGridGetHoveredElementPatch
{
    static void Postfix(InventoryGrid __instance, ref InventoryGrid.Element __result)
    {
        // If either of my move and drop keys are empty, return
        if (MouseTweaksPlugin.MoveNDrop.Value.MainKey == KeyCode.None && MouseTweaksPlugin.MoveNDrop2.Value.MainKey == KeyCode.None)
            return;
        if ((MouseTweaksPlugin.MoveNDrop.Value.IsKeyHeld() || MouseTweaksPlugin.MoveNDrop2.Value.IsKeyHeld()) && __result != null)
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