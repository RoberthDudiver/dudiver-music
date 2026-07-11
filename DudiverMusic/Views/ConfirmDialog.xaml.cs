using System.Windows;

namespace DudiverMusic.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void OnConfirm(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    private void OnCancel(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    /// <summary>Muestra el diálogo modal y devuelve true si el usuario confirma.</summary>
    public static bool Show(string title, string message)
    {
        var dlg = new ConfirmDialog(title, message) { Owner = Application.Current.MainWindow };
        return dlg.ShowDialog() == true;
    }
}
