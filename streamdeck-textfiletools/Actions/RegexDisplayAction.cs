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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsInput;

namespace BarRaider.TextFileUpdater.Actions
{
    [PluginActionId("com.barraider.textfiletools.regexdisplay")]
    public class RegexDisplayAction : KeypadBase
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
                    TitlePrefix = String.Empty,
                    AutoStopAlert = false,
                    Regex = String.Empty,
                    RegexFetch = String.Empty
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

            [JsonProperty(PropertyName = "autoStopAlert")]
            public bool AutoStopAlert { get; set; }

            [JsonProperty(PropertyName = "regex")]
            public string Regex { get; set; }

            [JsonProperty(PropertyName = "regexFetch")]
            public string RegexFetch { get; set; }
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
        public RegexDisplayAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
                await StopAlert();
                return;
            }

            var result = ReadRegexWordFromFile(true);
            if (String.IsNullOrEmpty(result))
            {
                await Connection.ShowAlert();
                return;
            }

            iis.Keyboard.TextEntry(result);
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            string regexWord = ReadRegexWordFromFile(false);
            if (String.IsNullOrEmpty(regexWord))
            {
                await Connection.SetTitleAsync(null);
                return;
            }

            if (!String.IsNullOrEmpty(settings.AlertText) && !isAlerting && settings.AlertText == regexWord)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Alerting, regex word is {regexWord}");
                await Connection.SetTitleAsync(null);
                // Start the alert
                isAlerting = true;
                tmrAlert.Start();
            }

            if (isAlerting)
            {
                if (settings.AutoStopAlert && settings.AlertText != regexWord)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Stopping alert, word changed to: {regexWord}");
                    await StopAlert();
                }
                return;
            }

            if (settings.SplitLongWord)
            {
                regexWord = regexWord.SplitToFitKey(titleParameters);
            }

            // Add TitlePrefix
            regexWord = $"{settings.TitlePrefix?.Replace(@"\n", "\n") ?? ""}{regexWord}";

            if (String.IsNullOrEmpty(settings.BackgroundFile))
            {
                await Connection.SetImageAsync((string)null);
                await Connection.SetTitleAsync(regexWord);
            }
            else
            {
                await DrawImage(regexWord);
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private String ReadRegexWordFromFile(bool logErrors)
        {
            if (String.IsNullOrEmpty(settings.FileName))
            {
                return null;
            }

            if (String.IsNullOrWhiteSpace(settings.Regex))
            {
                return null;
            }

            if (String.IsNullOrWhiteSpace(settings.RegexFetch))
            {
                return null;
            }

            if (!File.Exists(settings.FileName))
            {
                return "No File";
            }

            try
            {
                string text = File.ReadAllText(settings.FileName);
                string matchedText = MatchRegex(text, settings.Regex, settings.RegexFetch, logErrors);
                return matchedText;
            }
            catch (Exception ex)
            {

                if (logErrors)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} ReadLastWordFromFile Exception: {ex}");
                }
                return null;
            }
        }

        private string MatchRegex(string text, string regex, string regexFetch, bool logErrors)
        {
            // Regex Parse
            if (String.IsNullOrEmpty(regex) || String.IsNullOrEmpty(regexFetch))
            {
                if (logErrors)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} MatchRegex Regex or RegexFetch are null!");
                return null;
            }

            if (!IsRegexPatternValid(regex))
            {
                if (logErrors)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Invalid Regex: {regex}");
            }

            ParseMatchAndGroup(regexFetch, logErrors, out int matchIndex, out int groupIndex);
            if (matchIndex >= 0)
            {
                var matchRegex = new Regex(regex);
                var matches = matchRegex.Matches(text);
                if (matches.Count <= matchIndex)
                {
                    if (logErrors)
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "Match Index is out of bounds for result");
                }
                else
                {
                    var match = matches[matchIndex];
                    if (groupIndex >= 0)
                    {
                        if (match.Groups.Count <= groupIndex)
                        {
                            if (logErrors)
                                Logger.Instance.LogMessage(TracingLevel.ERROR, "Group Index is out of bounds for result");
                        }
                        else
                        {
                            return match.Groups[groupIndex].Value;
                        }
                    }
                    else
                    {
                        return match.Value;
                    }
                }
            }

            return null;
        }

        private bool IsRegexPatternValid(String pattern)
        {
            try
            {
                new Regex(pattern);
                return true;
            }
            catch { }
            return false;
        }

        private void ParseMatchAndGroup(string regexFetch, bool logErrors, out int matchIndex, out int groupIndex)
        {
            matchIndex = -1;
            groupIndex = -1;

            if (string.IsNullOrWhiteSpace(regexFetch))
            {
                if (logErrors)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "RegexFetch is empty");
                return;
            }

            regexFetch = regexFetch.ToUpperInvariant();
            if (regexFetch[0] != 'M')
            {
                if (logErrors)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"RegexFetch must start with a 'M'. {regexFetch}");
                return;
            }

            int position = 1;
            matchIndex = ExtractIndex(regexFetch, ref position);
            if (matchIndex < 0)
            {
                if (logErrors)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"RegexFetch has invalid format. {regexFetch}");
                return;
            }

            if (position < regexFetch.Length && regexFetch[position] != 'G')
            {
                if (logErrors)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"RegexFetch has invalid format,  'G' expected.  {regexFetch}");
                return;
            }

            if (position < regexFetch.Length)
            {
                position++;
                groupIndex = ExtractIndex(regexFetch, ref position);
            }
        }

        private int ExtractIndex(string str, ref int position)
        {
            int startPosition = position;
            int index = 0;
            while (position < str.Length)
            {
                if (!Int32.TryParse(str[position].ToString(), out int currDigit))
                {
                    break;
                }
                index *= 10;
                index += currDigit;
                position++;
            }

            if (position == startPosition) // No numbers
            {
                return -1;
            }
            return index;
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

        private async Task StopAlert()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Stopping Alert");
            tmrAlert.Stop();
            isAlerting = false;
            await Connection.SetImageAsync((string)null);
        }

        #endregion
    }
}