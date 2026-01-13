using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NotepadEx.MVVM.View.UserControls;
using NotepadEx.MVVM.ViewModels;
using NotepadEx.Util;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace NotepadEx.MVVM.View;

public partial class GradientPickerWindow : Window
{
    public Action OnSelectedColorChanged;
    public ObservableCollection<GradientStop> GradientStops { get; set; } = new();
    public LinearGradientBrush GradientBrush => GradientPreview;
    bool updatingFromAngle = false;
    bool updatingFromOffset = false;
    bool updatingFromScale = false;

    CustomTitleBarViewModel titleBarViewModel;
    private WindowChrome _windowChrome;

    public CustomTitleBarViewModel TitleBarViewModel => titleBarViewModel;

    public GradientPickerWindow()
    {
        InitializeComponent();
        DataContext = this;
        titleBarViewModel = CustomTitleBar.InitializeTitleBar(this, "Gradient Picker", showMinimize: false, showMaximize: false, isResizable: false);

        GradientStops = new ObservableCollection<GradientStop>();
        StopsListBox.ItemsSource = GradientStops;
        GradientStops.Add(new GradientStop(Colors.White, 0));
        GradientStops.Add(new GradientStop(Colors.Black, 1));
        UpdateGradientPreview();

        Loaded += GradientPickerWindow_Loaded;
        Closing += GradientPickerWindow_Closing;
    }

    private void GradientPickerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _windowChrome = new WindowChrome(this);
        _windowChrome.Enable();
    }

    private void GradientPickerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _windowChrome?.Detach();
    }

    public void SetGradient(LinearGradientBrush brush)
    {
        StartXSlider.Value = brush.StartPoint.X;
        StartYSlider.Value = brush.StartPoint.Y;

        EndXSlider.Value = brush.EndPoint.X;
        EndYSlider.Value = brush.EndPoint.Y;

        var stops = brush.GradientStops;
        GradientStops.Clear();
        foreach(var stop in stops)
            GradientStops.Add(new(stop.Color, stop.Offset));

        UpdateGradientPreview();
    }

    void UpdateGradientPreview()
    {
        if(GradientPreview == null) return;

        GradientPreview.GradientStops.Clear();
        foreach(var stop in GradientStops)
            GradientPreview.GradientStops.Add(stop);

        if(StartXSlider != null && StartYSlider != null && EndXSlider != null && EndYSlider != null)
        {
            double startXOriginal = StartXSlider.Value;
            double startYOriginal = StartYSlider.Value;
            double endXOriginal = EndXSlider.Value;
            double endYOriginal = EndYSlider.Value;

            double centerX = (startXOriginal + endXOriginal) / 2;
            double centerY = (startYOriginal + endYOriginal) / 2;

            double dx = endXOriginal - startXOriginal;
            double dy = endYOriginal - startYOriginal;
            double angle = Math.Atan2(dy, dx);
            double originalLength = Math.Sqrt(dx * dx + dy * dy);

            double scaledLengthX = originalLength;
            double scaledLengthY = originalLength;

            double newHalfLengthX = (scaledLengthX / 2) * Math.Cos(angle);
            double newHalfLengthY = (scaledLengthY / 2) * Math.Sin(angle);

            double startX = centerX - newHalfLengthX;
            double startY = centerY - newHalfLengthY;
            double endX = centerX + newHalfLengthX;
            double endY = centerY + newHalfLengthY;

            GradientPreview.StartPoint = new Point(startX, startY);
            GradientPreview.EndPoint = new Point(endX, endY);
        }

        OnSelectedColorChanged?.Invoke();
    }

    void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if(updatingFromAngle || updatingFromOffset) return;
        UpdateGradientPreview();
    }

    void SliderOffsetX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if(updatingFromOffset) return;
        updatingFromOffset = true;
        UpdateGradientPreview();
        updatingFromOffset = false;
    }

    void SliderOffsetY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if(updatingFromOffset) return;
        updatingFromOffset = true;
        UpdateGradientPreview();
        updatingFromOffset = false;
    }

    void SetStopColor(SolidColorBrush brush, int stopIndex, double stopOffset)
    {
        if(stopIndex != -1)
            GradientStops[stopIndex] = new GradientStop(brush.Color, stopOffset);
    }

    void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    void StopSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateGradientPreview();

    void AddStop_Click(object sender, RoutedEventArgs e)
    {
        GradientStops.Add(new GradientStop(Colors.Gray, 0.5));
        UpdateGradientPreview();
    }

    void EditStop_Click(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is GradientStop selectedStop)
        {
            var ogColor = selectedStop.Color;
            var stopIndex = GradientStops.IndexOf(selectedStop);
            var stopOffset = GradientStops[stopIndex].Offset;

            var colorPicker = new ColorPickerWindow
            {
                SelectedColor = selectedStop.Color
            };

            colorPicker.myColorPicker.OnSelectedColorChanged += () =>
            {
                SetStopColorEz(colorPicker.SelectedColor);
                UpdateGradientPreview();
            };

            if(colorPicker.ShowDialog() == true)
            {
                SetStopColorEz(colorPicker.SelectedColor);
                UpdateGradientPreview();
            }
            else
            {
                SetStopColorEz(ogColor);
                UpdateGradientPreview();
            }

            void SetStopColorEz(Color color) => SetStopColor(new SolidColorBrush(color), stopIndex, stopOffset);
        }
    }

    void CopyStop_Click(object sender, RoutedEventArgs e)
    {
        if(sender is not Button button || button.Tag is not System.Windows.Shapes.Rectangle rectangle ||
                   rectangle.Fill is not SolidColorBrush brush) return;

        Clipboard.SetText(ColorUtil.ColorToHexString(brush.Color));
    }

    void PasteStop_Click(object sender, RoutedEventArgs e)
    {
        if(sender is not Button button || button.Tag is not System.Windows.Shapes.Rectangle rectangle ||
            rectangle.Fill is not SolidColorBrush brush) return;

        var color = ColorUtil.HexStringToColor(Clipboard.GetText());
        if(color.HasValue)
        {
            brush.Color = color.Value;
            if((button.DataContext as GradientStop) is GradientStop selectedStop)
            {
                var stopIndex = GradientStops.IndexOf(selectedStop);
                SetStopColor(brush, stopIndex, selectedStop.Offset);
            }

            UpdateGradientPreview();
        }
    }

    void DeleteStop_Click(object sender, RoutedEventArgs e)
    {
        if(sender is not Button button || button.Tag is not System.Windows.Shapes.Rectangle rectangle ||
            rectangle.Fill is not SolidColorBrush brush) return;

        if((sender as FrameworkElement)?.DataContext is GradientStop selectedStop)
        {
            GradientStops.Remove(selectedStop);
            UpdateGradientPreview();
        }
    }

    void RandomizeStop_Click(object sender, RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is GradientStop selectedStop)
        {
            var stopIndex = GradientStops.IndexOf(selectedStop);
            SetStopColor(ColorUtil.GetRandomColorBrush(), stopIndex, selectedStop.Offset);
        }
        UpdateGradientPreview();
    }
}