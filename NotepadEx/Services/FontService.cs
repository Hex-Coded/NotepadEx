using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NotepadEx.MVVM.View;
using NotepadEx.Properties;
using NotepadEx.Services.Interfaces;
using NotepadEx.Util;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;

namespace NotepadEx.Services
{
    public class FontService : IFontService
    {
        public FontSettings CurrentFont { get; private set; }
        public ObservableCollection<FontFamily> AvailableFonts { get; private set; }

        private readonly Application application;
        private FontEditorWindow fontEditorWindow;

        public FontService(Application application)
        {
            this.application = application;
            AvailableFonts = new ObservableCollection<FontFamily>();
            LoadAvailableFonts();
            CurrentFont = new FontSettings();
        }

        private void LoadAvailableFonts()
        {
            AvailableFonts.Clear();
            foreach(var font in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
            {
                AvailableFonts.Add(font);
            }
        }

        public void LoadCurrentFont()
        {
            ProcessSync.RunSynchronized(() =>
            {
                var fontSettings = new FontSettings
                {
                    FontFamily = Settings.Default.FontFamily ?? "Consolas",
                    FontSize = Settings.Default.FontSize > 0 ? Settings.Default.FontSize : 12,
                    FontStyle = ParseFontStyle(Settings.Default.FontStyle),
                    FontWeight = ParseFontWeight(Settings.Default.FontWeight),
                };
                // We only need to apply the font, not save settings back again.
                InternalApplyFont(fontSettings);
            });
        }

        public void ApplyFont(FontSettings fontSettings)
        {
            // Use the mutex to ensure saving settings is process-safe.
            ProcessSync.RunSynchronized(() =>
            {
                InternalApplyFont(fontSettings);

                // Persist the new settings
                Settings.Default.FontFamily = fontSettings.FontFamily;
                Settings.Default.FontSize = fontSettings.FontSize;
                Settings.Default.FontStyle = fontSettings.FontStyle.ToString();
                Settings.Default.FontWeight = fontSettings.FontWeight.ToString();
                Settings.Default.Save();
            });
        }

        /// <summary>
        /// Applies font settings to the application's resources without saving them.
        /// This is the non-synchronized part of the logic.
        /// </summary>
        private void InternalApplyFont(FontSettings fontSettings)
        {
            try
            {
                CurrentFont = fontSettings;
                AppResourceUtil<FontFamily>.TrySetResource(application, UIConstants.Font_Family, new FontFamily(fontSettings.FontFamily));
                AppResourceUtil<double>.TrySetResource(application, UIConstants.Font_Size, fontSettings.FontSize);
                AppResourceUtil<FontStyle>.TrySetResource(application, UIConstants.Font_Style, fontSettings.FontStyle);
                AppResourceUtil<FontWeight>.TrySetResource(application, UIConstants.Font_Weight, fontSettings.FontWeight);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error applying font settings. Reverting to default.\r\nException Message: {ex.Message}");
                // Revert to a safe default if parsing fails
                InternalApplyFont(new FontSettings());
            }
        }

        private FontStyle ParseFontStyle(string fontStyle)
        {
            if(string.IsNullOrEmpty(fontStyle)) return FontStyles.Normal;

            return fontStyle switch
            {
                "Italic" => FontStyles.Italic,
                "Oblique" => FontStyles.Oblique,
                _ => FontStyles.Normal
            };
        }

        private FontWeight ParseFontWeight(string fontWeight)
        {
            if(string.IsNullOrEmpty(fontWeight)) return FontWeights.Normal;

            return fontWeight switch
            {
                "Thin" => FontWeights.Thin,
                "ExtraLight" => FontWeights.ExtraLight,
                "Light" => FontWeights.Light,
                "Regular" => FontWeights.Regular,
                "Medium" => FontWeights.Medium,
                "SemiBold" => FontWeights.SemiBold,
                "Bold" => FontWeights.Bold,
                "ExtraBold" => FontWeights.ExtraBold,
                "Black" => FontWeights.Black,
                _ => FontWeights.Normal
            };
        }

        public void OpenFontEditor()
        {
            if(fontEditorWindow == null || !fontEditorWindow.IsLoaded)
                fontEditorWindow = new FontEditorWindow(this);

            fontEditorWindow.Show();
            fontEditorWindow.Activate();
        }
    }
}