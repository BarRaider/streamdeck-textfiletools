﻿using BarRaider.SdTools;
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
    [PluginActionId("com.barraider.textfiletools.randomline")]
    public class RandomLineAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    FileName = String.Empty,
                    SendEnterAtEnd = false
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "fileName")]
            public string FileName { get; set; }

            [JsonProperty(PropertyName = "sendEnterAtEnd")]
            public bool SendEnterAtEnd { get; set; }
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
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
            string randomLine = ReadRandomLineFromFile();
            if (!string.IsNullOrEmpty(randomLine))
            {
                iis.Keyboard.TextEntry(randomLine);
            }

            if (settings.SendEnterAtEnd)
            {
                iis.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RETURN);
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

        private String ReadRandomLineFromFile()
        {
            if (String.IsNullOrEmpty(settings.FileName))
            {
                return null;
            }

            if (!File.Exists(settings.FileName))
            {
                return "No File";
            }

            string[] lines = File.ReadAllLines(settings.FileName);
            return lines[rand.Next(lines.Length)];
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

       #endregion
    }
}