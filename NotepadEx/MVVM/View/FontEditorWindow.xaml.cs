using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using NotepadEx.MVVM.View.UserControls;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Services.Interfaces;
using NotepadEx.Util;
using FontFamily = System.Windows.Media.FontFamily;

namespace NotepadEx.MVVM.View;

public partial class FontEditorWindow : Window
{
    readonly IFontService fontService;
    FontSettings workingCopy;
    CustomTitleBarViewModel titleBarViewModel;
    private WindowChrome _windowChrome;

    public CustomTitleBarViewModel TitleBarViewModel => titleBarViewModel;
    public FontSettings CurrentFont
    {
        get => workingCopy;
        set
        {
            workingCopy = value;
            OnPropertyChanged(nameof(CurrentFont));
        }
    }

    public ObservableCollection<FontFamily> AvailableFonts { get; }

    public FontEditorWindow(IFontService fontService)
    {
        InitializeComponent();
        DataContext = this;
        titleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "Font Settings", showMaximize: false);

        this.fontService = fontService;

        workingCopy = new FontSettings
        {
            FontFamily = this.fontService.CurrentFont.FontFamily,
            FontSize = this.fontService.CurrentFont.FontSize,
            FontStyle = this.fontService.CurrentFont.FontStyle,
            FontWeight = this.fontService.CurrentFont.FontWeight,
        };

        AvailableFonts = this.fontService.AvailableFonts;
        FontFamilyComboBox.Loaded += FontFamilyComboBox_Loaded;
        FontSizeTextBox.Text = workingCopy.FontSize.ToString();
        FontStyleComboBox.SelectedValue = workingCopy.FontStyle;
        FontWeightComboBox.SelectedValue = workingCopy.FontWeight;

        Loaded += FontEditorWindow_Loaded;
        Closing += FontEditorWindow_Closing;
    }

    private void FontEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _windowChrome = new WindowChrome(this);
        _windowChrome.Enable();
    }

    private void FontEditorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _windowChrome?.Detach();
    }

    void FontFamilyComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        var matchingFontFamily = AvailableFonts.FirstOrDefault(f => f.Source == workingCopy.FontFamily);
        if(matchingFontFamily != null)
            FontFamilyComboBox.SelectedItem = matchingFontFamily;
    }

    void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        fontService.ApplyFont(CurrentFont);
        Close();
    }

    void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}