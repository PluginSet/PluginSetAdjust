using System.Xml;
using PluginSet.Core;
using PluginSet.Core.Editor;
using UnityEditor;

namespace PluginSet.Adjust.Editor
{
    [BuildTools]
    public static class BuildAdjustTools
    {
        [OnSyncEditorSetting]
        public static void OnSyncEditorSetting(BuildProcessorContext context)
        {
            switch (context.BuildTarget)
            {
                case BuildTarget.Android:
                case BuildTarget.iOS:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    break;
                default:
                    return;
            }
            
            var buildParams = context.BuildChannels.Get<BuildAdjustParams>();
            if (!buildParams.Enable)
                return;
            
            Global.CopyDependenciesInLib("com.pluginset.adjust");
            
            context.Symbols.Add("ENABLE_ADJUST");
            
            var pluginConfig = context.Get<PluginSetConfig>("pluginsConfig");
            var config = pluginConfig.AddConfig<PluginAdjustConfig>("Adjust");
            config.AppToken = buildParams.AppToken;
            config.LogLevel = buildParams.LogLevel;
            config.SendInBackground = buildParams.SendInBackground;
            config.RegisterEvent = buildParams.RegisterEvent;
            config.RealNameEvent = buildParams.RealNameEvent;
            config.PurchaseEvent = buildParams.PurchaseEvent;
            config.IgnoreEventWithoutToken = buildParams.IgnoreEventWithoutToken;
            config.EventTokens = buildParams.EventTokens;
            
            context.AddLinkAssembly("PluginSet.Adjust");
        }
        
        [AndroidProjectModify]
        public static void OnAndroidProjectModify(BuildProcessorContext context, AndroidProjectManager projectManager)
        {
            var buildParams = context.BuildChannels.Get<BuildAdjustParams>();
            if (!buildParams.Enable)
                return;

            var manifest = projectManager.LibraryManifest;
            manifest.AddUsePermission("android.permission.INTERNET", "adjust");
            manifest.AddUsePermission("android.permission.ACCESS_NETWORK_STATE", "adjust");
            manifest.AddUsePermission("com.google.android.finsky.permission.BIND_GET_INSTALL_REFERRER_SERVICE", "adjust");
            manifest.AddUsePermission("com.google.android.gms.permission.AD_ID", "adjust");

            // add receiver adjust Install Referrer
            const string path = "/manifest/application/receiver";
            const string attrName = "name";
            string attrValue = $"com.adjust.sdk.AdjustReferrerReceiver";

            var doc = projectManager.LibraryManifest;
            var list = doc.findElements(path, AndroidConst.NS_PREFIX, attrName, attrValue);
            XmlElement element;
            if (list.Count <= 0)
            {
                element = doc.createElementWithPath(path);
                element.SetAttribute(attrName, AndroidConst.NS_URI, attrValue);
            }
            else
            {
                element = list[0];
            }

            element.SetAttribute("permission", AndroidConst.NS_URI, "android.permission.INSTALL_PACKAGES");
            element.SetAttribute("exported"  , AndroidConst.NS_URI, "true");
            var intentFilter = element.FirstChild;
            if (intentFilter == null)
                intentFilter = element.createSubElement("intent-filter");

            var actionNode = intentFilter.FirstChild;
            if (actionNode == null)
                actionNode = intentFilter.createSubElement("action");

            ((XmlElement) actionNode).SetAttribute("name", AndroidConst.NS_URI, "com.android.vending.INSTALL_REFERRER");
        }
        
        
        [iOSXCodeProjectModify]
        public static void ModifyXCodeProject(BuildProcessorContext context, PBXProjectManager project)
        {
            var buildParams = context.BuildChannels.Get<BuildAdjustParams>();
            if (!buildParams.Enable)
                return;
            
            // The Adjust SDK will try to add following frameworks to your project:
            // - AdSupport.framework (needed for access to IDFA value)
            // - iAd.framework (needed in case you are running ASA campaigns)
            // - AdServices.framework (needed in case you are running ASA campaigns)
            // - CoreTelephony.framework (needed to get information about network type user is connected to)
            // - StoreKit.framework (needed for communication with SKAdNetwork framework)
            // - AppTrackingTransparency.framework (needed for information about user's consent to be tracked)

            // In case you don't need any of these, feel free to remove them from your app.

            
            var xcodeProject = project.Project;

            var target = project.UnityFramework;
            project.AddFrameworkToProject(target, "AdSupport.framework", true);
            project.AddFrameworkToProject(target, "iAd.framework", true);
            project.AddFrameworkToProject(target, "CoreTelephony.framework", true);

            if (buildParams.IsiOS14ProcessingEnabled)
            {
                project.AddFrameworkToProject(target, "StoreKit.framework", true);
                project.AddFrameworkToProject(target, "AppTrackingTransparency.framework", true);
            }

            // The Adjust SDK needs to have Obj-C exceptions enabled.
            // GCC_ENABLE_OBJC_EXCEPTIONS=YES

            project.AddBuildProperty(target, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");

            // The Adjust SDK needs to have -ObjC flag set in other linker flags section because of it's categories.
            // OTHER_LDFLAGS -ObjC
            
            project.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
            project.AddBuildProperty(target, "OTHER_LDFLAGS", "-force_load $(PROJECT_DIR)/Libraries/Adjust/iOS/AdjustSigSdk.a");
        }
    }
}