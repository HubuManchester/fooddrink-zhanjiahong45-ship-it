namespace FoodDrinkApp.Services;

/// <summary>
/// Provides a single cancellable text-to-speech channel for app narration.
/// </summary>
public static class SpeechService
{
    private static CancellationTokenSource? currentSpeech;

    /// <summary>
    /// Speaks text using an English locale when one is available.
    /// </summary>
    public static async Task SpeakAsync(string text)
    {
        Stop();

        currentSpeech = new CancellationTokenSource();
        var options = new SpeechOptions
        {
            Volume = 0.9f,
            Pitch = 1.05f,
            Locale = await FindEnglishLocaleAsync()
        };

        try
        {
            await TextToSpeech.Default.SpeakAsync(text, options, currentSpeech.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Compatibility wrapper for older call sites that requested Chinese speech.
    /// </summary>
    public static Task SpeakChineseAsync(string text) => SpeakAsync(text);

    /// <summary>
    /// Cancels and disposes any active text-to-speech request.
    /// </summary>
    public static void Stop()
    {
        if (currentSpeech is null)
        {
            return;
        }

        currentSpeech.Cancel();
        currentSpeech.Dispose();
        currentSpeech = null;
    }

    private static async Task<Locale?> FindEnglishLocaleAsync()
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        return locales.FirstOrDefault(locale => locale.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
    }
}
