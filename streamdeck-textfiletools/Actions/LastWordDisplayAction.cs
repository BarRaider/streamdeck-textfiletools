using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;

namespace BarRaider.TextFileUpdater.Actions
{
    [PluginActionId("com.barraider.textfiletools.lastworddisplay")]
    public class LastWordDisplayAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    FileName = String.Empty,
                    AlertText = String.Empty,
                    AlertColor = DEFAULT_ALERT_COLOR,
                    BackgroundFile = String.Empty,
                    SplitLongWord = false,
                    TitlePrefix = String.Empty
                };
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "fileName")]
            public string FileName { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "backgroundFile")]
            public string BackgroundFile { get; set; }

            [JsonProperty(PropertyName = "alertText")]
            public string AlertText { get; set; }

            [JsonProperty(PropertyName = "alertColor")]
            public string AlertColor { get; set; }

            [JsonProperty(PropertyName = "splitLongWord")]
            public bool SplitLongWord { get; set; }

            [JsonProperty(PropertyName = "titlePrefix")]
            public string TitlePrefix { get; set; }

        }

        #region Private Members
        private const string DEFAULT_ALERT_COLOR = "#FF0000";
        private const int TOTAL_ALERT_STAGES = 4;
        private const double POINTS_TO_PIXEL_CONVERT = 1.3;

        private readonly PluginSettings settings;
        private readonly InputSimulator iis = new InputSimulator();
        private readonly System.Timers.Timer tmrAlert = new System.Timers.Timer();
        private SdTools.Wrappers.TitleParameters titleParameters = null;
        private string userTitle;
        private Color titleColor = Color.White;
        private bool isAlerting = false;
        private int alertStage = 0;

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
            Connection.OnTitleParametersDidChange += Connection_OnTitleParametersDidChange;
            tmrAlert.Interval = 200;
            tmrAlert.Elapsed += TmrAlert_Elapsed;
        }

        private void Connection_OnTitleParametersDidChange(object sender, SdTools.Wrappers.SDEventReceivedEventArgs<SdTools.Events.TitleParametersDidChange> e)
        {
            titleParameters = e.Event?.Payload?.TitleParameters;
            userTitle = e.Event?.Payload?.Title;
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
            tmrAlert.Stop();
            Connection.OnTitleParametersDidChange -= Connection_OnTitleParametersDidChange;
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed. Alert: {isAlerting}");

            if (isAlerting)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Stopping Alert");
                tmrAlert.Stop();
                isAlerting = false;
                await Connection.SetImageAsync((string)null);
                return;
            }

            iis.Keyboard.TextEntry(ReadLastWordFromFile());
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            string lastWord = ReadLastWordFromFile();
            if (String.IsNullOrEmpty(lastWord))
            {
                return;
            }

            if (!String.IsNullOrEmpty(settings.AlertText) && !isAlerting && settings.AlertText == lastWord)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Alerting, last word is {lastWord}");
                // Start the alert
                isAlerting = true;
                tmrAlert.Start();
            }

            if (isAlerting)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "OnTick - In Alert!");
                return;
            }

            if (settings.SplitLongWord)
            {
                lastWord = Tools.SplitStringToFit(lastWord, titleParameters);
            }

            // Add TitlePrefix
            lastWord = $"{settings.TitlePrefix?.Replace(@"\n", "\n") ?? ""}{lastWord}";

            if (String.IsNullOrEmpty(settings.BackgroundFile))
            {
                await Connection.SetImageAsync((string)null);
                await Connection.SetTitleAsync(lastWord);
            }
            else
            {
                await DrawImage(lastWord);
            }
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

            try
            {
                string[] words = File.ReadAllText(settings.FileName).Replace("*", "").Trim().Split(' ');
                return words.LastOrDefault();
            }
            catch (Exception ex)
            {

                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} ReadLastWordFromFile Exception: {ex}");
                return null;
            }
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private Color GenerateStageColor(string initialColor, int stage, int totalAmountOfStages)
        {
            Color color = ColorTranslator.FromHtml(initialColor);
            int a = color.A;
            double r = color.R;
            double g = color.G;
            double b = color.B;

            // Try and increase the color in the last stage;
            if (stage == totalAmountOfStages - 1)
            {
                stage = 1;
            }

            for (int idx = 0; idx < stage; idx++)
            {
                r /= 2;
                g /= 2;
                b /= 2;
            }

            return Color.FromArgb(a, (int)r, (int)g, (int)b);
        }

        private void TmrAlert_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            using (Bitmap img = Tools.GenerateGenericKeyImage(out Graphics graphics))
            {
                int height = img.Height;
                int width = img.Width;

                // Background
                var bgBrush = new SolidBrush(GenerateStageColor(settings.AlertColor, alertStage, TOTAL_ALERT_STAGES));
                graphics.FillRectangle(bgBrush, 0, 0, width, height);
                graphics.AddTextPath(titleParameters, img.Height, img.Width, settings.AlertText);
                Connection.SetImageAsync(img);

                alertStage = (alertStage + 1) % TOTAL_ALERT_STAGES;
                graphics.Dispose();
            }
        }

        private async Task DrawImage(string text)
        {
            await Connection.SetTitleAsync(null);
            using (Bitmap img = Tools.GenerateGenericKeyImage(out Graphics graphics))
            {
                int height = img.Height;
                int width = img.Width;

                // Background
                if (!String.IsNullOrEmpty(settings.BackgroundFile) && File.Exists(settings.BackgroundFile))
                {
                    using (Image backgroundImage = Image.FromFile(settings.BackgroundFile))
                    {
                        graphics.DrawImage(backgroundImage, 0, 0, width, height);
                    }
                }

                graphics.AddTextPath(titleParameters, img.Height, img.Width, text);
                await Connection.SetImageAsync(img);
                graphics.Dispose();
            }
        }

        #endregion
    }
}