using PluginSet.Core.Editor;
using UnityEditor;

namespace PluginSet.Adjust.Editor
{
    [InitializeOnLoad]
    public static class PluginsAdjustFilter
    {
        static PluginsAdjustFilter()
        {
            var filter = PluginFilter.IsBuildParamsEnable<BuildAdjustParams>();
            PluginFilter.RegisterFilter("com.pluginset.adjust/Plugins/Android", filter);
        }
    }
}