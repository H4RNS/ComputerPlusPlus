using GorillaNetworking;
using HarmonyLib;

namespace ComputerPlusPlus.Patches
{
    [HarmonyPatch(typeof(GorillaComputer), "PressButton")]
    class KeyPressPatches
    {
        private static bool Prefix(GorillaKeyboardButton buttonPressed)
        {
            ComputerManager.Instance.OnKeyPressed(buttonPressed);
            return false;
        }
    }
}
