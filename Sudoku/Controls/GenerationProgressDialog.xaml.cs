using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sudoku;

public partial class GenerationProgressDialog : Window
{
    private readonly MessageBoxViewModel vm = new();

    public GenerationProgressDialog(string message)
    {
        InitializeComponent();

        DataContext = vm;
        messageText.Text = message;

        SourceInitialized += (s, e) => Native.RemoveIcon(this);

        Loaded += (s, e) => Mouse.OverrideCursor = null;
        Closed += (s, e) => Mouse.OverrideCursor = Cursors.Wait;
    }

    public bool Cancelled { get; private set; }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Cancelled = true;
        Close();
    }
}
