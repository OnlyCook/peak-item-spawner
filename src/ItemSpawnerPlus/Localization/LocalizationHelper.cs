namespace ItemSpawnerPlus
{
    internal static class LocalizationHelper
    {
        // array order matches LocalizedText.Language, missing entries fall back to English
        public static string Resolve(string[] arr)
        {
            int idx = (int)LocalizedText.CURRENT_LANGUAGE;
            if (idx >= 0 && idx < arr.Length && !string.IsNullOrEmpty(arr[idx]))
                return arr[idx];
            return arr[0];
        }
    }
}
