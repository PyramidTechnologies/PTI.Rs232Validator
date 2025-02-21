using System.Windows;

namespace PTI.Rs232Validator.Desktop.Controls;

/// <summary>
/// A control containing a button to perform some action and a label to display the result.
/// </summary>
public partial class ActionResultDisplay
{
    public static readonly DependencyProperty ActionButtonContentProperty = DependencyProperty.Register(
        nameof(ActionButtonContent), typeof(object), typeof(ActionResultDisplay), new PropertyMetadata(default(object)));
    
    public static readonly DependencyProperty ResultDescriptionProperty = DependencyProperty.Register(
        nameof(ResultDescription), typeof(string), typeof(ActionResultDisplay), new PropertyMetadata(default(string)));
    
    public static readonly DependencyProperty ResultValueProperty = DependencyProperty.Register(
        nameof(ResultValue), typeof(object), typeof(ActionResultDisplay), new PropertyMetadata(default(object)));

    public ActionResultDisplay()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? OnButtonClick;

    public object ActionButtonContent
    {
        get => GetValue(ActionButtonContentProperty);
        set => SetValue(ActionButtonContentProperty, value);
    }

    public string ResultDescription
    {
        get => (string) GetValue(ResultDescriptionProperty);
        set => SetValue(ResultDescriptionProperty, value);
    }

    public object ResultValue
    {
        get => GetValue(ResultValueProperty);
        set => SetValue(ResultValueProperty, value);
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        OnButtonClick?.Invoke(sender, e);
    }
}