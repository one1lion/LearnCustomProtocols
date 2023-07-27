using LearnCustomProtocols.Shared;
using System;
using System.Windows;

namespace LearnCustomProtocols.Wpf;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string _protocolName = "myCustWpfProt";
    private readonly CustomProtocol _custProtocol = new(_protocolName);

    public MainWindow()
    {
        InitializeComponent();
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        ResultTextBlock.Text = "Registering Protocol";
        string? msg;
        try
        {
            _custProtocol.RegisterUrlProtocol();
            msg = $"Successfully registered the custom protocol.  Use `start {_protocolName}://` to try it out.  You can also type `{_protocolName}://` into your browser.";
        }
        catch (Exception ex)
        {
            msg =
@$"Registration failed.

Exception Info:
{ex.Message}
{ex.StackTrace}";
        }

        ResultTextBlock.Text = msg;
    }

    private void UnregisterButton_Click(object sender, RoutedEventArgs e)
    {
        ResultTextBlock.Text = "Unregistering Protocol";
        string? msg;
        try
        {
            _custProtocol.UnregisterUrlProtocol();
            msg = "Successfully unregistered the custom protocol.";
        }
        catch (Exception ex)
        {
            msg =
@$"Failed to unregister the custom protocol. Although...it's still possible that the protocol was successfully unregistered. There may be an issue somewhere else. To verify, try using the protocol. Windows should inform you that you do not have an application to handle the link.

Exception Info:
{ex.Message}
{ex.StackTrace}";
        }
        ResultTextBlock.Text = msg;
    }
}
