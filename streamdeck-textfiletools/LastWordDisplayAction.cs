using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;

namespace BarRaider.TextFileUpdater
{
    [PluginActionId("com.barraider.textfiletools.lastworddisplay")]
    public class LastWordDisplayAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.FileName = String.Empty;
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "fileName")]
            public string FileName { get; set; }
        }

        #region Private Members

        private PluginSettings settings;
        private InputSimulator iis = new InputSimulator();

        #endregion
        public LastWordDisplayAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
            iis.Keyboard.TextEntry(ReadLastWordFromFile());
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            await Connection.SetTitleAsync(ReadLastWordFromFile());
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private String ReadLastWordFromFile()
        {
            if (String.IsNullOrEmpty(settings.FileName))
            {
                return null;
            }

            if (!File.Exists(settings.FileName))
            {
                return "No File";
            }

            string[] words = File.ReadAllText(settings.FileName).Split(' ');
            return words.LastOrDefault();
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

       #endregion
    }
}