using UnityEngine;
using UnityEngine.UI;

namespace MouseTweaks.DropSingleItem;

internal class RightClickButtonComponent : MonoBehaviour
{
    public Button.ButtonClickedEvent OnRightClick = new();
}