using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;

namespace BarRaider.TextFileUpdater.Actions
{
    [PluginActionId("com.barraider.textfiletools.randomline")]
    public class RandomLineAction : KeypadBase
    {

        public enum OutputAction
        {
            Type = 0,
            Clipboard = 1,
            SaveToFile = 2
        }

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    FileName = String.Empty,
                    SendEnterAtEnd = false,
                    OutputAction = OutputAction.Type,
                    OutputFileName = String.Empty
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "fileName")]
            public string FileName { get; set; }

            [JsonProperty(PropertyName = "sendEnterAtEnd")]
            public bool SendEnterAtEnd { get; set; }

            [JsonProperty(PropertyName = "outputAction")]
            public OutputAction OutputAction { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "outputFileName")]
            public string OutputFileName { get; set; }
        }

        #region Private Members

        private readonly PluginSettings settings;
        private readonly InputSimulator iis = new InputSimulator();
        private readonly Random rand = new Random();

        #endregion
        public RandomLineAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} Key Pressed");
            string randomLine = ReadRandomLineFromFile();
            if (!string.IsNullOrEmpty(randomLine))
            {
                if (HandleOutputAction(randomLine))
                {
                    await Connection.ShowOk();
                    return;
                }
            }
            await Connection.ShowAlert();
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick()
        {
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private String ReadRandomLineFromFile()
        {
            if (String.IsNullOrEmpty(settings.FileName))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} FileName is empty!");
                return null;
            }

            if (!File.Exists(settings.FileName))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} File not found {settings.FileName}");
                return null;
            }

            string[] lines = File.ReadAllLines(settings.FileName);
            return lines[rand.Next(lines.Length)];
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
                switch (payload["property_inspector"].ToString().ToLower())
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
                                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} Failed to save picker value to settings");
                            }
                            SaveSettings();
                        }
                        break;
                }
            }
        }

        private bool HandleOutputAction(string line)
        {
            try
            {
                switch (settings.OutputAction)
                {
                    case OutputAction.Type:
                        iis.Keyboard.TextEntry(line);
                        if (settings.SendEnterAtEnd)
                        {
                            iis.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                        }
                        return true;
                    case OutputAction.Clipboard:
                        if (settings.SendEnterAtEnd)
                        {
                            line = line + '\n';
                        }
                        SetClipboard(line);
                        return true;
                    case OutputAction.SaveToFile:
                        if (settings.SendEnterAtEnd)
                        {
                            line = line + '\n';
                        }
                        return SaveToOutputFile(line);
                    default:
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} Invalid action {settings.OutputAction}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} HandleOutputAction Exception: {ex}");
                return false;

            }
        }

        private bool SaveToOutputFile(string text)
        {
            try
            {
                if (String.IsNullOrEmpty(settings.OutputFileName))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} OutputFileName is empty!");
                    return false;
                }

                File.WriteAllText(settings.OutputFileName, text);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} SaveToOutputFile exception: {ex}");
            }
            return false;
        }

        private void SetClipboard(string text)
        {
            Thread staThread = new Thread(
                delegate ()
                {
                    try
                    {
                        Clipboard.SetText(text);
                    }

                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} SetClipboard exception: {ex}");
                    }
                });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        #endregion
    }
}