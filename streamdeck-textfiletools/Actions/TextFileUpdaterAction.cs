using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.TextFileUpdater.Actions
{

    //---------------------------------------------------
    //          BarRaider's Hall Of Fame
    // CSSGuru : tobitege
    //---------------------------------------------------
    [PluginActionId("com.barraider.textfiletools.textfileupdater")]
    public class TextFileUpdaterAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    OutputFileName = String.Empty,
                    InputString = String.Empty,
                    AppendToFile = false
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "outputFileName")]
            public string OutputFileName { get; set; }

            [JsonProperty(PropertyName = "inputString")]
            public string InputString { get; set; }

            [JsonProperty(PropertyName = "appendToFile")]
            public bool AppendToFile { get; set; }
        }

        #region Private Members

        private readonly PluginSettings settings;

        #endregion
        public TextFileUpdaterAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            Connection.OnSendToPlugin += Connection_OnSendToPlugin;

        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
            SaveInputStringToFile();
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

       
        private void SaveInputStringToFile()
        {
            try
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Saving value: {settings.InputString} to file: {settings.OutputFileName}");
                if (!String.IsNullOrWhiteSpace(settings.OutputFileName))
                {
                    string output = settings.InputString.Replace(@"\n", "\n");
                    if (settings.AppendToFile)
                    {
                        File.AppendAllText(settings.OutputFileName, output);
                    }
                    else
                    {
                        File.WriteAllText(settings.OutputFileName, output);
                    }
                    Connection.ShowOk();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error Saving value: {settings.InputString} to file: {settings.OutputFileName} : {ex}");
                Connection.ShowAlert();
                settings.OutputFileName = "ACCESS DENIED";
                SaveSettings();
            }
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void Connection_OnSendToPlugin(object sender, SdTools.Wrappers.SDEventReceivedEventArgs<SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;
            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLowerInvariant())
                {
                    case "loadsavepicker":
                        string propertyName = (string)payload["property_name"];
                        string pickerTitle = (string)payload["picker_title"];
                        string pickerFilter = (string)payload["picker_filter"];
                        string fileName = PickersUtil.Pickers.SaveFilePicker(pickerTitle, null, pickerFilter);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            if (!PickersUtil.Pickers.SetJsonPropertyValue(settings, propertyName, fileName))
                            {
                                Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to save picker value to settings");
                            }
                            SaveSettings();
                        }
                        break;
                }
            }
        }


        #endregion
    }
}