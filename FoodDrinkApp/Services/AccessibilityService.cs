using System.Runtime.CompilerServices;

namespace FoodDrinkApp.Services;

/// <summary>
/// Applies the coursework large-text setting across MAUI visual trees.
/// </summary>
public static class AccessibilityService
{
    private const string TextScalePreferenceKey = "text_scale_level";
    private static readonly ConditionalWeakTable<BindableObject, FontSizeStore> OriginalFontSizes = new();
    private static int textScaleLevel;

    /// <summary>
    /// Gets or sets the selected text scale option from Settings.
    /// </summary>
    public static int TextScaleLevel
    {
        get => textScaleLevel;
        set
        {
            textScaleLevel = Math.Clamp(value, 0, 3);
            Preferences.Set(TextScalePreferenceKey, textScaleLevel);
        }
    }

    /// <summary>
    /// Gets or sets whether any large-text option is active.
    /// </summary>
    public static bool LargeTextEnabled
    {
        get => TextScaleLevel > 0;
        set => TextScaleLevel = value ? Math.Max(TextScaleLevel, 1) : 0;
    }

    /// <summary>
    /// Gets the numeric multiplier for the current text scale level.
    /// </summary>
    public static double CurrentTextScale => TextScaleLevel switch
    {
        1 => 1.35,
        2 => 1.75,
        3 => 2.0,
        _ => 1.0
    };

    /// <summary>
    /// Loads the saved text scale so large-text mode survives app restarts.
    /// </summary>
    public static void LoadSavedTextScale()
    {
        textScaleLevel = Math.Clamp(Preferences.Get(TextScalePreferenceKey, 0), 0, 3);
    }

    /// <summary>
    /// Applies the current text scale to labels, buttons, inputs, pickers, and search bars under the supplied root.
    /// </summary>
    public static void ApplyFontScale(Element root)
    {
        ApplyToElement(root);

        if (root is not IVisualTreeElement visualTreeElement)
        {
            return;
        }

        foreach (var child in visualTreeElement.GetVisualChildren().OfType<Element>())
        {
            ApplyFontScale(child);
        }
    }

    private static void ApplyToElement(Element element)
    {
        var scale = CurrentTextScale;

        switch (element)
        {
            case Label label:
                label.FontSize = GetOriginalFontSize(label, label.FontSize) * scale;
                break;
            case Button button:
                button.FontSize = GetOriginalFontSize(button, button.FontSize) * scale;
                break;
            case Entry entry:
                entry.FontSize = GetOriginalFontSize(entry, entry.FontSize) * scale;
                break;
            case Editor editor:
                editor.FontSize = GetOriginalFontSize(editor, editor.FontSize) * scale;
                break;
            case Picker picker:
                picker.FontSize = GetOriginalFontSize(picker, picker.FontSize) * scale;
                break;
            case SearchBar searchBar:
                searchBar.FontSize = GetOriginalFontSize(searchBar, searchBar.FontSize) * scale;
                break;
        }
    }

    private static double GetOriginalFontSize(BindableObject control, double currentSize)
    {
        var store = OriginalFontSizes.GetOrCreateValue(control);
        if (!store.HasValue)
        {
            store.Value = currentSize > 0 ? currentSize : 14;
            store.HasValue = true;
        }

        return store.Value;
    }

    private sealed class FontSizeStore
    {
        public bool HasValue { get; set; }
        public double Value { get; set; }
    }
}
