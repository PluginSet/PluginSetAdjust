using com.adjust.sdk;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEngine;

namespace PluginSet.Adjust.Editor
{
    [BuildChannelsParams("Adjust", "快手SDK参数")]
    [VisibleCaseBoolValue("SupportAndroid", true)]
    public class BuildAdjustParams: ScriptableObject
    {
        [Tooltip("是否启用Adjust SDK")]
        public bool Enable;
        
        [Tooltip("XCode支持iOS14")]
        [VisibleCaseBoolValue("Enable", true)]
        public bool IsiOS14ProcessingEnabled = true;

        [Tooltip("Adjust SDK提供的APPToken")]
        public string AppToken;
        
        [Tooltip("Adjust日志输出等级")]
        public AdjustLogLevel LogLevel;
        
        [Tooltip("后台允许发送日志")]
        public bool SendInBackground = true;
        
        [Tooltip("是否忽略没有token的事件")]
        public bool IgnoreEventWithoutToken;

        [Tooltip("注册事件名称")]
        public string RegisterEvent;
        
        [Tooltip("实名事件名称")]
        public string RealNameEvent;
        
        [Tooltip("充值事件名称")]
        public string PurchaseEvent;
        
        [Tooltip("事件名称与token对应表")]
        public DictStringString EventTokens;
    }
}