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
    [PluginActionId("com.barraider.textfiletools.nextline")]
    public class NextLineAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    FileName = String.Empty,
                    SendEnterAtEnd = false,
                    UseClipboard = false
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "fileName")]
            public string FileName { get; set; }

            [JsonProperty(PropertyName = "sendEnterAtEnd")]
            public bool SendEnterAtEnd { get; set; }

            [JsonProperty(PropertyName = "useClipboard")]
            public bool UseClipboard { get; set; }
        }
        

        #region Private Members

        private readonly PluginSettings settings;
        private readonly InputSimulator iis = new InputSimulator();
        int currentLine = 0;

        #endregion
        public NextLineAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            string randomLine = ReadNextLineFromFile();
            if (!string.IsNullOrEmpty(randomLine))
            {
                if (settings.UseClipboard)
                {
                    SetClipboard(randomLine);
                }
                else
                {
                    iis.Keyboard.TextEntry(randomLine);
                    if (settings.SendEnterAtEnd)
                    {
                        iis.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
                    }
                }
            }          
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

        private String ReadNextLineFromFile()
        {
            if (String.IsNullOrEmpty(settings.FileName))
            {
                return null;
            }

            if (!File.Exists(settings.FileName))
            {

                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} ReadNextLineFromFile: File not found {settings.FileName}");
                return null;
            }

            string[] lines = File.ReadAllLines(settings.FileName);
            if (currentLine >= lines.Length)
            {
                currentLine = 0;
            }
            return lines[currentLine++];
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
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
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetSongInfo ReadFromClipboard exception: {ex}");
                    }
                });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        #endregion
    }
}