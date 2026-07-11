using System.Windows;
using DudiverMusic.Localization;
using DudiverMusic.Services;

namespace DudiverMusic;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Aplica el idioma guardado (o el del sistema) antes de mostrar la ventana.
        LocalizationManager.Instance.SetLanguage(SettingsStore.Load().Language);
    }
}
