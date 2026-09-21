namespace SmartCraftStorage.Shared
{
    internal static class DebugLog
    {
        public static void Log(string message)
        {
            if (Config.ModConfig.DebugLogging.Value)
            {
                UnityEngine.Debug.Log(message);
            }
        }
    }
}
