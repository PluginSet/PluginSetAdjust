using com.adjust.sdk;
using PluginSet.Core;
using UnityEngine;

namespace PluginSet.Adjust
{
    public class PluginAdjustConfig: ScriptableObject
    {
        public string AppToken;
        public AdjustLogLevel LogLevel ;
        public bool SendInBackground = true;
        
        public bool IgnoreEventWithoutToken;

        public string PurchaseEvent;
        public string RegisterEvent;
        public string RealNameEvent;
        
        public DictStringString EventTokens;
    }
}