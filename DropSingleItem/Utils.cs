namespace MouseTweaks.DropSingleItem;

internal class Utils
{
    public static bool CanStackItem(ItemDrop.ItemData a, ItemDrop.ItemData b) => a == null || b == null || a != b && !(a.m_shared.m_name != b.m_shared.m_name) && a.m_shared.m_maxStackSize != 1 && b.m_shared.m_maxStackSize != 1 && (a.m_shared.m_maxQuality <= 1 && b.m_shared.m_maxQuality <= 1 || a.m_quality == b.m_quality);
}