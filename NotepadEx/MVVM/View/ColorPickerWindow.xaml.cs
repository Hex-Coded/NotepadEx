using System.Windows;
using NotepadEx.MVVM.View.UserControls;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Util;
using Color = System.Windows.Media.Color;

namespace NotepadEx.MVVM.View;

public partial class ColorPickerWindow : Window
{
    CustomTitleBarViewModel titleBarViewModel;
    private WindowChrome _windowChrome;

    public CustomTitleBarViewModel TitleBarViewModel => titleBarViewModel;

    public Color SelectedColor
    {
        get => myColorPicker.SelectedColor;
        set => myColorPicker.SelectedColor = value;
    }

    public ColorPickerWindow()
    {
        InitializeComponent();
        DataContext = this;
        titleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "Color Picker", showMinimize: false, showMaximize: false, isResizable: false);
        myColorPicker.OnWindowCancel += OnCancel;
        myColorPicker.OnWindowConfirm += OnConfirm;

        Loaded += ColorPickerWindow_Loaded;
        Closing += ColorPickerWindow_Closing;
    }

    private void ColorPickerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _windowChrome = new WindowChrome(this);
        _windowChrome.Enable();
    }

    private void ColorPickerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _windowChrome?.Detach();
    }

    void OnCancel()
    {
        DialogResult = false;
        Close();
    }

    void OnConfirm()
    {
        DialogResult = true;
        Close();
    }
}