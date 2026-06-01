using Microsoft.Maui.Devices;

namespace FoodDrinkApp.Services;

/// <summary>
/// Maintains flashlight state while delegating hardware access to MAUI Essentials.
/// </summary>
public sealed class FlashlightService
{
    /// <summary>
    /// Gets whether the service believes the device flashlight is currently on.
    /// </summary>
    public bool IsOn { get; private set; }

    /// <summary>
    /// Toggles the flashlight and returns the new on/off state.
    /// </summary>
    public async Task<bool> ToggleAsync()
    {
        if (IsOn)
        {
            await TurnOffAsync();
            return false;
        }

        await Flashlight.Default.TurnOnAsync();
        IsOn = true;
        return true;
    }

    /// <summary>
    /// Turns the flashlight off if it is currently on.
    /// </summary>
    public async Task TurnOffAsync()
    {
        if (!IsOn)
        {
            return;
        }

        try
        {
            await Flashlight.Default.TurnOffAsync();
        }
        finally
        {
            IsOn = false;
        }
    }

    /// <summary>
    /// Gets the compact button label for a flashlight state.
    /// </summary>
    public static string ButtonTextFor(bool isOn) => isOn ? "Off" : "Flash";
}
