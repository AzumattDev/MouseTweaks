using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MouseTweaks.DropSingleItem;

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateItemDrag))]
internal class InventoryGui_UpdateItemDrag
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo method = typeof(ZInput).GetMethod("GetMouseButton", new System.Type[1]
        {
            typeof(int)
        });
        List<CodeInstruction> source = new(instructions);
        for (int index = 1; index < source.Count; ++index)
        {
            if (source[index - 1].opcode == OpCodes.Ldc_I4_1 && source[index].Calls(method))
            {
                source[index - 1] = new CodeInstruction(OpCodes.Nop);
                source[index] = new CodeInstruction(OpCodes.Ldc_I4_S, 0);
            }
        }

        return source.AsEnumerable<CodeInstruction>();
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
internal class InventoryGui_OnRightClickItem
{
    public static bool Prefix(InventoryGui __instance, ref int ___m_dragAmount, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
    {
        bool flag1 = __instance.m_dragGo != null;
        bool flag2 = MouseTweaksPlugin.InventoryUseItemKeyCodeConfig.Value.MainKey == KeyCode.Mouse1;
        bool flag3 = flag2 && Input.GetKey(KeyCode.LeftShift);
        if (!flag2 | flag3)
        {
            if (!flag1 && item is { m_stack: > 1 })
            {
                __instance.SetupDragItem(item, grid.GetInventory(), item.m_stack / 2);
                return false;
            }

            if (flag3)
                return false;
        }

        if (!flag1)
            return flag2;
        ItemDrop.ItemData a = __instance.m_dragItem;
        if (!Utils.CanStackItem(a, item))
            return false;
        Inventory inventory = __instance.m_dragInventory;
        int num = __instance.m_dragAmount;
        int stack = a.m_stack;
        ___m_dragAmount = 1;
        __instance.OnSelectedItem(grid, item, pos, InventoryGrid.Modifier.Select);
        int amount = num - (stack - a.m_stack);
        if (amount > 0)
            __instance.SetupDragItem(a, inventory, amount);
        return false;
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
internal class InventoryGui_Awake
{
    public static void Postfix(InventoryGui __instance, Button ___m_dropButton) => ___m_dropButton.gameObject.AddComponent<RightClickButtonComponent>().OnRightClick.AddListener((UnityAction)(() => OnDropSingleOutside(__instance)));

    private static void OnDropSingleOutside(InventoryGui __instance)
    {
        if (!(bool)(Object)__instance.m_dragGo)
            return;
        ItemDrop.ItemData itemData = __instance.m_dragItem;
        Inventory inventory = __instance.m_dragInventory;
        int num = __instance.m_dragAmount;
        int stack = itemData.m_stack;
        MouseTweaksPlugin.MouseTweaksLogger.LogDebug(("Drop single item " + itemData.m_shared.m_name));
        if (!inventory.ContainsItem(itemData))
        {
            __instance.SetupDragItem(null, null, 1);
        }
        else
        {
            if (!Player.m_localPlayer.DropItem(inventory, itemData, 1))
                return;
            __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity);
            int amount = num - (stack - itemData.m_stack);
            if (inventory.ContainsItem(itemData) && amount > 0)
                __instance.SetupDragItem(itemData, inventory, amount);
            else
                __instance.SetupDragItem(null, null, 1);
            __instance.UpdateCraftingPanel();
        }
    }
}

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateInventory))]
internal class InventoryGrid_UpdateInventory
{
    public static void Postfix(InventoryGrid __instance, Inventory inventory, ItemDrop.ItemData dragItem)
    {
        if (MouseTweaksPlugin.InventoryUseItemKeyCodeConfig.Value.MainKey == KeyCode.Mouse1 || dragItem != null || !MouseTweaksPlugin.InventoryUseItemKeyCodeConfig.Value.IsKeyDown())
            return;
        ItemDrop.ItemData itemData = __instance.GetItem(new Vector2i(Input.mousePosition));
        if (itemData == null || !(bool)(Object)Player.m_localPlayer)
            return;
        Player.m_localPlayer.UseItem(inventory, itemData, true);
    }
}

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.OnLeftClick))]
internal class InventoryGrid_OnLeftClick
{
    private const float DoubleClickThreshold = 0.2f;
    private static float LastItemClickedTime;

    public static bool Prefix(ref State __state)
    {
        __state.DoubleClicked = Time.unscaledTime - (double)LastItemClickedTime < 0.200000002980232;
        LastItemClickedTime = Time.unscaledTime;
        InventoryGui instance = InventoryGui.instance;
        if (!__state.DoubleClicked || !(bool)(Object)instance.m_dragGo)
            return true;
        __state.DoubleClicked = false;
        GrabFullStackItemDrag(instance);
        return false;
    }

    public static void Postfix(State __state)
    {
        InventoryGui instance = InventoryGui.instance;
        if (!__state.DoubleClicked || !(bool)(Object)instance.m_dragGo)
            return;
        GrabFullStackItemDrag(instance);
    }

    private static void GrabFullStackItemDrag(InventoryGui __instance)
    {
        int num = __instance.m_dragAmount;
        Container container = __instance.m_currentContainer;
        if (container != null)
            GrabFullStackItemDrag(__instance, container.GetInventory());
        GrabFullStackItemDrag(__instance, Player.m_localPlayer.GetInventory());
        ItemDrop.ItemData itemData = __instance.m_dragItem;
        int stack = itemData.m_stack;
        if (num == stack)
            return;
        __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity);
        __instance.SetupDragItem(itemData, __instance.m_dragInventory, itemData.m_stack);
        __instance.UpdateCraftingPanel();
    }

    private static void GrabFullStackItemDrag(InventoryGui __instance, Inventory inventory)
    {
        ItemDrop.ItemData a = __instance.m_dragItem;
        Inventory inventory1 = __instance.m_dragInventory;
        List<ItemDrop.ItemData> itemDataList = new();
        inventory.GetAllItems(a.m_shared.m_name, itemDataList);
        while (itemDataList.Any<ItemDrop.ItemData>() && a.m_stack < a.m_shared.m_maxStackSize)
        {
            ItemDrop.ItemData b = itemDataList.Last<ItemDrop.ItemData>();
            itemDataList.RemoveAt(itemDataList.Count - 1);
            if (Utils.CanStackItem(a, b))
                inventory1.MoveItemToThis(inventory, b, b.m_stack, a.m_gridPos.x, a.m_gridPos.y);
        }
    }

    public struct State
    {
        public bool DoubleClicked;
    }
}

[HarmonyPatch(typeof(Button), "OnPointerClick")]
internal class Button_OnPointerClick
{
    public static void Postfix(Button __instance, PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || !__instance.IsActive() || !__instance.IsInteractable())
            return;
        RightClickButtonComponent component = __instance.GetComponent<RightClickButtonComponent>();
        if (!(bool)(Object)component)
            return;
        component.OnRightClick.Invoke();
    }
}

internal static class InventoryGui_Extensions
{
    public static void SetupDragItem(this InventoryGui instance, ItemDrop.ItemData item, Inventory inventory, int amount)
    {
        Traverse.Create(instance).Method(nameof(SetupDragItem), new System.Type[3]
        {
            typeof(ItemDrop.ItemData),
            typeof(Inventory),
            typeof(int)
        }, null).GetValue(item, inventory, amount);
    }

    public static void UpdateCraftingPanel(this InventoryGui instance, bool focusView = false) => Traverse.Create(instance).Method(nameof(UpdateCraftingPanel), new System.Type[1]
    {
        typeof(bool)
    }, null).GetValue(focusView);

    public static void OnSelectedItem(this InventoryGui instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
    {
        Traverse.Create(instance).Method(nameof(OnSelectedItem), new System.Type[4]
        {
            typeof(InventoryGrid),
            typeof(ItemDrop.ItemData),
            typeof(Vector2i),
            typeof(InventoryGrid.Modifier)
        }, null).GetValue(grid, item, pos, mod);
    }
}