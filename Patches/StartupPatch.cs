using HarmonyLib;

namespace ComputerPlusPlus.Patches
{
    [HarmonyPatch(typeof(GorillaTagger), "Start")]
    static class PostInitializedPatch
    {
        static bool initialized;

        private static void Postfix()
        {
            if (!initialized)
            {
                Plugin.Instance.Setup();
                initialized = true;
            }
        }
    }
}
