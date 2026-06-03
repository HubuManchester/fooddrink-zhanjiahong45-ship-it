using Microsoft.Maui.Devices.Sensors;

namespace FoodDrinkApp.Services;

/// <summary>
/// Coordinates MAUI motion sensor subscriptions so pages can keep event handlers thin.
/// </summary>
public sealed class SensorMonitorService
{
    /// <summary>
    /// Gets whether the accelerometer readout handler is currently subscribed.
    /// </summary>
    public bool AccelerometerReadoutEnabled { get; private set; }

    /// <summary>
    /// Gets whether shake-to-suggest is currently subscribed.
    /// </summary>
    public bool ShakeSuggestionEnabled { get; private set; }

    /// <summary>
    /// Gets whether the current device exposes an accelerometer.
    /// </summary>
    public bool IsAccelerometerSupported => Accelerometer.Default.IsSupported;

    /// <summary>
    /// Gets whether the current device exposes a compass.
    /// </summary>
    public bool IsCompassSupported => Compass.Default.IsSupported;

    /// <summary>
    /// Gets whether the compass is currently producing readings.
    /// </summary>
    public bool IsCompassMonitoring => Compass.Default.IsMonitoring;

    /// <summary>
    /// Gets whether the current device exposes a gyroscope.
    /// </summary>
    public bool IsGyroscopeSupported => Gyroscope.Default.IsSupported;

    /// <summary>
    /// Gets whether the gyroscope is currently producing readings.
    /// </summary>
    public bool IsGyroscopeMonitoring => Gyroscope.Default.IsMonitoring;

    /// <summary>
    /// Starts accelerometer readings for the supplied page handler.
    /// </summary>
    public void StartAccelerometerReadout(EventHandler<AccelerometerChangedEventArgs> handler)
    {
        Accelerometer.Default.ReadingChanged += handler;
        AccelerometerReadoutEnabled = true;
        EnsureAccelerometerRunning();
    }

    /// <summary>
    /// Stops accelerometer readings for the supplied page handler.
    /// </summary>
    public void StopAccelerometerReadout(EventHandler<AccelerometerChangedEventArgs> handler)
    {
        Accelerometer.Default.ReadingChanged -= handler;
        AccelerometerReadoutEnabled = false;
        StopAccelerometerIfUnused();
    }

    /// <summary>
    /// Starts compass readings for the supplied page handler.
    /// </summary>
    public static void StartCompass(EventHandler<CompassChangedEventArgs> handler)
    {
        Compass.Default.ReadingChanged += handler;
        Compass.Default.Start(SensorSpeed.UI);
    }

    /// <summary>
    /// Stops compass readings for the supplied page handler.
    /// </summary>
    public static void StopCompass(EventHandler<CompassChangedEventArgs> handler)
    {
        Compass.Default.ReadingChanged -= handler;
        Compass.Default.Stop();
    }

    /// <summary>
    /// Starts gyroscope readings for the supplied page handler.
    /// </summary>
    public static void StartGyroscope(EventHandler<GyroscopeChangedEventArgs> handler)
    {
        Gyroscope.Default.ReadingChanged += handler;
        Gyroscope.Default.Start(SensorSpeed.UI);
    }

    /// <summary>
    /// Stops gyroscope readings for the supplied page handler.
    /// </summary>
    public static void StopGyroscope(EventHandler<GyroscopeChangedEventArgs> handler)
    {
        Gyroscope.Default.ReadingChanged -= handler;
        Gyroscope.Default.Stop();
    }

    /// <summary>
    /// Starts shake detection from accelerometer readings and shares the accelerometer with the live readout.
    /// </summary>
    public void StartShakeSuggestion(EventHandler<AccelerometerChangedEventArgs> handler)
    {
        Accelerometer.Default.ReadingChanged += handler;
        ShakeSuggestionEnabled = true;
        EnsureAccelerometerRunning();
    }

    /// <summary>
    /// Stops shake detection and stops the accelerometer if no readout remains active.
    /// </summary>
    public void StopShakeSuggestion(EventHandler<AccelerometerChangedEventArgs> handler)
    {
        Accelerometer.Default.ReadingChanged -= handler;
        ShakeSuggestionEnabled = false;
        StopAccelerometerIfUnused();
    }

    /// <summary>
    /// Stops all motion sensors and unsubscribes page event handlers.
    /// </summary>
    public void StopAll(
        EventHandler<AccelerometerChangedEventArgs> accelerometerHandler,
        EventHandler<AccelerometerChangedEventArgs> shakeHandler,
        EventHandler<CompassChangedEventArgs> compassHandler,
        EventHandler<GyroscopeChangedEventArgs> gyroscopeHandler)
    {
        if (AccelerometerReadoutEnabled)
        {
            Accelerometer.Default.ReadingChanged -= accelerometerHandler;
            AccelerometerReadoutEnabled = false;
        }

        if (ShakeSuggestionEnabled)
        {
            Accelerometer.Default.ReadingChanged -= shakeHandler;
            ShakeSuggestionEnabled = false;
        }

        if (Accelerometer.Default.IsSupported && Accelerometer.Default.IsMonitoring)
        {
            Accelerometer.Default.Stop();
        }

        if (Compass.Default.IsSupported && Compass.Default.IsMonitoring)
        {
            Compass.Default.ReadingChanged -= compassHandler;
            Compass.Default.Stop();
        }

        if (Gyroscope.Default.IsSupported && Gyroscope.Default.IsMonitoring)
        {
            Gyroscope.Default.ReadingChanged -= gyroscopeHandler;
            Gyroscope.Default.Stop();
        }
    }

    /// <summary>
    /// Formats an accelerometer reading for on-screen display.
    /// </summary>
    public static string FormatAcceleration(AccelerometerChangedEventArgs e) =>
        SensorFormatter.Vector3("Acceleration", e.Reading.Acceleration);

    /// <summary>
    /// Formats a compass heading for on-screen display.
    /// </summary>
    public static string FormatHeading(CompassChangedEventArgs e) =>
        SensorFormatter.Heading(e.Reading.HeadingMagneticNorth);

    /// <summary>
    /// Formats a gyroscope reading for on-screen display.
    /// </summary>
    public static string FormatAngularVelocity(GyroscopeChangedEventArgs e) =>
        SensorFormatter.Vector3("Angular velocity", e.Reading.AngularVelocity);

    private static void EnsureAccelerometerRunning()
    {
        if (!Accelerometer.Default.IsMonitoring)
        {
            Accelerometer.Default.Start(SensorSpeed.UI);
        }
    }

    private void StopAccelerometerIfUnused()
    {
        if (!AccelerometerReadoutEnabled && !ShakeSuggestionEnabled && Accelerometer.Default.IsMonitoring)
        {
            Accelerometer.Default.Stop();
        }
    }
}
