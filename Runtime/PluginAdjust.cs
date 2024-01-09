#if UNITY_EDITOR || ENABLE_ADJUST
using System;
using System.Collections.Generic;
using System.Text;
using com.adjust.sdk;
using PluginSet.Core;
using UnityEngine;
using Logger = PluginSet.Core.Logger;

namespace PluginSet.Adjust
{
#if !UNITY_EDITOR
    [PluginRegister]
#endif
    public partial class PluginAdjust: PluginBase, IPrivacyAuthorizationCallback, IAnalytics, IReport, ICustomPlugin
    {
        private static readonly Logger Logger = LoggerManager.GetLogger("Adjust");
        private static StringBuilder sb = new StringBuilder();

        private struct EventRecord
        {
            public string name;
            public Dictionary<string, object> @params;
        }

        private const string EventAttributeCallback = "adjust_attribute_callback";
        private const string EventDeferredDeeplink = "adjust_deferred_deep_link";
        
        public override string Name => "Adjust";

        private string _appToken;
        private AdjustLogLevel _logLevel;
        
        private string _registerEvent;
        private string _realNameEvent;
        private string _purchaseEvent;

        private bool _sendInBackground;

        private bool _ignoreEventWithoutToken;

        private string _adjustAttributionJson;
        private string _deeplink;

        private bool _sdkInited;
        
        private readonly Dictionary<string, string> _eventTokens = new Dictionary<string, string>();

        private readonly List<EventRecord> _unTrackEvents = new List<EventRecord>();

        protected override void Init(PluginSetConfig config)
        {
            var data = config.Get<PluginAdjustConfig>();
            _appToken = data.AppToken;
            _logLevel = data.LogLevel;
            _registerEvent = data.RegisterEvent;
            _realNameEvent = data.RealNameEvent;
            _purchaseEvent = data.PurchaseEvent;
            _sendInBackground = data.SendInBackground;
            _ignoreEventWithoutToken = data.IgnoreEventWithoutToken;

            var pairs = data.EventTokens.Pairs;
            if (pairs != null && pairs.Length > 0)
            {
                foreach (var kv in pairs)
                {
                    var key = kv.Key;
                    if (_eventTokens.ContainsKey(key))
                        continue;
                    
                    _eventTokens.Add(key, kv.Value);
                }
            }
        }

        private void InitSdk()
        {
#if DEBUG
            var config = new AdjustConfig(_appToken, AdjustEnvironment.Sandbox);
#else
            var config = new AdjustConfig(_appToken, AdjustEnvironment.Production);
#endif
            config.setLogLevel(_logLevel);
            config.setLogDelegate(msg => Logger.Debug(msg));
            config.setSendInBackground(_sendInBackground);
            config.setEventSuccessDelegate(OnEventSuccessCallback);
            config.setEventFailureDelegate(OnEventFailureCallback);
            config.setSessionSuccessDelegate(OnSessionSuccessCallback);
            config.setSessionFailureDelegate(OnSessionFailureCallback);
            config.setDeferredDeeplinkDelegate(OnDeferredDeeplinkCallback);
            config.setAttributionChangedDelegate(OnAttributeChangedCallback);
#if !UNITY_IOS
            //ios的adjust启动做一个延时 给应用一个充裕的时候获取各种信息
            config.setDelayStart(5);
#endif
            var obj = new GameObject("Adjust");
            obj.AddComponent<com.adjust.sdk.Adjust>();
            com.adjust.sdk.Adjust.start(config);
            _sdkInited = true;

            foreach (var info in _unTrackEvents)
            {
                CustomEvent(info.name, info.@params);
            }
            _unTrackEvents.Clear();
        }

        private void OnEventSuccessCallback(AdjustEventSuccess obj)
        {
            Logger.Debug($"OnEventSuccessCallback {obj.EventToken}: {obj.CallbackId}");
        }

        private void OnEventFailureCallback(AdjustEventFailure obj)
        {
            Logger.Debug($"OnEventFailureCallback {obj.EventToken}: {obj.CallbackId} ({obj.Message})");
        }

        private void OnSessionSuccessCallback(AdjustSessionSuccess obj)
        {
            Logger.Debug($"OnSessionSuccessCallback {obj.Message}");
        }

        private void OnSessionFailureCallback(AdjustSessionFailure obj)
        {
            Logger.Debug($"OnSessionFailureCallback {obj.Message}");
        }

        private void OnDeferredDeeplinkCallback(string obj)
        {
            _deeplink = obj;
            Logger.Debug($"OnDeferredDeeplinkCallback {obj}");
            SendNotification(EventDeferredDeeplink, obj);
        }

        private void OnAttributeChangedCallback(AdjustAttribution obj)
        {
            _adjustAttributionJson = AdjustHelper.ConvertToString(obj);
            Logger.Debug($"OnAttributeChangedCallback {_adjustAttributionJson}");
            SendNotification(EventAttributeCallback, _adjustAttributionJson);
        }

        public void OnPrivacyAuthorization()
        {
            InitSdk();
        }
        
        public void CustomEvent(string customEventName, Dictionary<string, object> eventData = null)
        {
            if (!_sdkInited)
            {
                _unTrackEvents.Add(new EventRecord()
                {
                    name = customEventName,
                    @params = eventData
                });
                return;
            }
            
            if (!_eventTokens.TryGetValue(customEventName, out var eventToken))
            {
                if (_ignoreEventWithoutToken)
                {
                    Logger.Warn("Cannot find event token in adjust with eventName: {0}", customEventName);
                    return;
                }

                eventToken = customEventName;
            }

#if DEBUG
            sb.Clear();
            sb.Append("Adjust: trackEvent. ");
            sb.Append(customEventName);
            sb.Append(' ');
#endif
            AdjustEvent adjustEvent = new AdjustEvent(eventToken);
            if (eventData != null)
            {
                foreach (var kv in eventData)
                {
                    var key = kv.Key;
                    if (PluginUtil.FilterAnalyticsEventName(key))
                        continue;

                    var value = kv.Value.ToString();
                    adjustEvent.addCallbackParameter(key, value);
#if DEBUG
                    sb.Append(key);
                    sb.Append('=');
                    sb.Append(value);
                    sb.Append(' ');
#endif
                }
            }
#if DEBUG
            sb.Append("[EOL]");
            Logger.Debug(sb.ToString());
#endif
            com.adjust.sdk.Adjust.trackEvent(adjustEvent);
        }
        
        public void OnUserRegister(string method = "Indefinite")
        {
            if (string.IsNullOrEmpty(_registerEvent))
                return;
            
            CustomEvent(_registerEvent, new Dictionary<string, object>()
            {
                {"method", method}
            });
        }

        public void OnUserRealName()
        {
            if (string.IsNullOrEmpty(_realNameEvent))
                return;
            
            CustomEvent(_realNameEvent);
        }

        public void OnUserPurchase(bool success, string currency, float price, string paymentMethod, int amount = 1,
            string productId = null, string productName = null, string productType = null)
        {
            if (string.IsNullOrEmpty(_purchaseEvent))
                return;
            
            if (!_eventTokens.TryGetValue(_purchaseEvent, out var eventToken))
            {
                if (_ignoreEventWithoutToken)
                {
                    Logger.Warn("Cannot find event token in adjust with eventName: {0}", _purchaseEvent);
                    return;
                }

                eventToken = _purchaseEvent;
            }
            
            AdjustEvent adjustEvent = new AdjustEvent(eventToken);
            adjustEvent.setRevenue(price, currency);
            adjustEvent.addCallbackParameter("method", paymentMethod);
            adjustEvent.addCallbackParameter("productId", productId);
            com.adjust.sdk.Adjust.trackEvent(adjustEvent);
        }
        
        public void CustomCall(string func, Action<Result> callback = null, string json = null)
        {
            switch (func)
            {
                case "GetDeepLink":
                    if (string.IsNullOrEmpty(_deeplink))
                    {
                        callback?.Invoke(new Result()
                        {
                            Success = false,
                            PluginName = Name,
                            Code = PluginConstants.FailDefaultCode,
                            Error = "No deep link callback"
                        });
                    }
                    else
                    {
                        callback?.Invoke(new Result()
                        {
                            Success = true,
                            PluginName = Name,
                            Code = PluginConstants.SuccessCode,
                            Data = _deeplink
                        });
                    }
                    break;
                case "GetAttribution":
                    if (string.IsNullOrEmpty(_adjustAttributionJson))
                    {
                        callback?.Invoke(new Result()
                        {
                            Success = false,
                            PluginName = Name,
                            Code = PluginConstants.FailDefaultCode,
                            Error = "No attribute callback"
                        });
                    }
                    else
                    {
                        callback?.Invoke(new Result()
                        {
                            Success = true,
                            PluginName = Name,
                            Code = PluginConstants.SuccessCode,
                            Data = _adjustAttributionJson
                        });
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
#endif