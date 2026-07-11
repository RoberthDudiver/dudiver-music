using System.Windows.Data;
using System.Windows.Markup;

namespace DudiverMusic.Localization;

/// <summary>
/// Uso en XAML: <c>Text="{loc:Tr NothingPlaying}"</c>. Crea un binding al indexador
/// del <see cref="LocalizationManager"/>, así el texto se actualiza al cambiar de idioma.
/// </summary>
public sealed class TrExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public TrExtension() { }
    public TrExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
