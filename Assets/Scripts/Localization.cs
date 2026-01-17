using System;
using UnityEngine.Localization.Settings;

namespace ADC.Localization
{
    public static class Localization
    {
        public static string GetStrings(string table, string stringId)
        {
            string localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(table, stringId);
            if (localizedString.StartsWith("No translation found for")) localizedString = stringId;
            return localizedString;
        }
    }
}