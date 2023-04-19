using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Datadog.Unity.Editor
{
    public class DatadogConfigurationWindow : SettingsProvider
    {
        private DatadogConfigurationOptions _options;

        public DatadogConfigurationWindow(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {

        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _options = DatadogConfigurationOptions.GetOrCreate();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            GUILayout.Label("SDK Options", EditorStyles.boldLabel);

            _options.Enabled = EditorGUILayout.ToggleLeft(
                new GUIContent("Enable Datadog", "Whether the Datadog Plugin should be enabled or not."),
                _options.Enabled);

            _options.ClientToken = EditorGUILayout.TextField("Client Token", _options.ClientToken);
            _options.Site = (DatadogSite)EditorGUILayout.EnumPopup("Datadog Site", _options.Site);

            EditorGUILayout.Space();
            GUILayout.Label("Logging", EditorStyles.boldLabel);
            _options.DefaultLoggingLevel = (LogType)EditorGUILayout.EnumPopup("Default Logging Level", _options.DefaultLoggingLevel);
        }

        public override void OnDeactivate()
        {
            if (_options != null)
            {
                EditorUtility.SetDirty(_options);
                AssetDatabase.SaveAssets();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            var provider = new DatadogConfigurationWindow("Project/Datadog", SettingsScope.Project, new string[] { "Datadog" });
            return provider;
        }
    }
}
