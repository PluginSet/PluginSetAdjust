using com.adjust.sdk;

namespace PluginSet.Adjust
{
    public static class AdjustHelper
    {
        public static string ConvertToString(AdjustAttribution obj)
        {
            var json = new JSONClass();
            json.SafeAddString("adid", obj.adid);
            json.SafeAddString("adgroup", obj.adgroup);
            json.SafeAddString("clickLabel", obj.clickLabel);
            json.SafeAddString("campaign", obj.campaign);
            json.SafeAddString("creative", obj.creative);
            json.SafeAddString("network", obj.network);
            json.SafeAddString("costCurrency", obj.costCurrency);
            json.SafeAddString("costType", obj.costType);
            json.SafeAddString("trackerName", obj.trackerName);
            json.SafeAddString("trackerToken", obj.trackerToken);
            json.SafeAddString("fbInstallReferrer", obj.fbInstallReferrer);
            return json.ToString();
        }

        public static void SafeAddString(this JSONClass json, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            
            json.Add(key, value);
        }
        
    }
}