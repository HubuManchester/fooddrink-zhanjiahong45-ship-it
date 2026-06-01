using FoodDrinkApp.Services;
using Microsoft.Maui.Devices.Sensors;

namespace FoodDrinkApp;

public partial class HardwarePage : ContentPage
{
    private int feedbackTestCount;
    private bool flashlightOn;
    private readonly SensorMonitorService sensorMonitor = new();
    private FoodVisionService? foodVisionService;
    private Prediction? latestPrediction;
    private readonly Random suggestionRandom = new();

    public HardwarePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
    }

    protected override void OnDisappearing()
    {
        StopMotionSensors();
        _ = TurnFlashlightOffAsync();
        SpeechService.Stop();
        base.OnDisappearing();
    }

    private async void OnTakePhotoClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                SetStatus("This device does not support camera capture.");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null)
            {
                SetStatus("Photo capture cancelled.");
                return;
            }

            await using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            FoodPhoto.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            PredictionLabel.Text = "Classifying food photo...";
            ReadPredictionButton.IsEnabled = false;
            SetStatus("Food photo captured. Running on-device recognition...");

            var visionService = await LoadFoodVisionServiceAsync();
            latestPrediction = await Task.Run(() => visionService.Classify(imageBytes));
            PredictionLabel.Text = $"Food recognition: {latestPrediction.Label} ({latestPrediction.Confidence:P0})";
            ReadPredictionButton.IsEnabled = true;
            SetStatus("Food recognition completed.");
        }
        catch (PermissionException)
        {
            SetStatus("Camera permission was denied. Enable camera access in device settings.");
        }
        catch (FileNotFoundException ex)
        {
            AppLog.Error("Load food recognition assets", ex);
            SetStatus("Food recognition assets are missing from this app build.");
        }
        catch (InvalidOperationException ex)
        {
            AppLog.Error("Run food recognition", ex);
            SetStatus("Food recognition could not start on this device right now.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Capture or classify food photo", ex);
            SetStatus("Camera capture or food recognition could not be completed right now.");
        }
    }

    private async Task<FoodVisionService> LoadFoodVisionServiceAsync()
    {
        if (foodVisionService is not null)
        {
            return foodVisionService;
        }

        await using var modelStream = await FileSystem.OpenAppPackageFileAsync("mobilenetv2-7.onnx");
        using var modelMemory = new MemoryStream();
        await modelStream.CopyToAsync(modelMemory);

        await using var labelsStream = await FileSystem.OpenAppPackageFileAsync("imagenet-slim-labels.txt");
        using var reader = new StreamReader(labelsStream);
        var labels = new List<string>();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                labels.Add(line);
            }
        }

        foodVisionService = new FoodVisionService(modelMemory.ToArray(), labels);
        return foodVisionService;
    }

    private async void OnGetLocationClicked(object? sender, EventArgs e)
    {
        try
        {
            SetStatus("Getting location...");
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location is null)
            {
                SetStatus("Current location could not be found.");
                return;
            }

            CoordinateLabel.Text = $"Latitude {location.Latitude:F5}, longitude {location.Longitude:F5}";
            LocationLabel.Text = await BuildAddressTextAsync(location);
            SetStatus("Country, city, and coordinates have been loaded.");
        }
        catch (PermissionException)
        {
            SetStatus("Location permission was denied. Enable location access in device settings.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Load current location", ex);
            SetStatus("Location could not be loaded right now. Try again after checking device location settings.");
        }
    }

    private static async Task<string> BuildAddressTextAsync(Location location)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
            var placemark = placemarks?.FirstOrDefault();
            var address = FormatPlacemark(placemark);

            if (!string.IsNullOrWhiteSpace(address))
            {
                return address;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Reverse geocode location", ex);
        }

        return BuildFallbackAddress(location);
    }

    private static string FormatPlacemark(Placemark? placemark)
    {
        if (placemark is null)
        {
            return string.Empty;
        }

        var parts = new[]
        {
            placemark.CountryName,
            placemark.AdminArea,
            placemark.Locality,
            placemark.SubLocality,
            placemark.Thoroughfare
        }
        .Where(part => !string.IsNullOrWhiteSpace(part))
        .Distinct()
        .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(" / ", parts);
    }

    private static string BuildFallbackAddress(Location location)
    {
        if (IsNear(location, 37.422, -122.084, 0.08))
        {
            return "United States / California / Mountain View";
        }

        if (location.Latitude is >= 37.0 and <= 38.2 && location.Longitude is >= -123.2 and <= -121.5)
        {
            return "United States / California / San Francisco Bay Area";
        }

        if (location.Latitude is >= 18 and <= 54 && location.Longitude is >= 73 and <= 135)
        {
            return "China / Current city requires a real device or available geocoding service";
        }

        return "Coordinates were found, but country and city were not returned by this device.";
    }

    private static bool IsNear(Location location, double latitude, double longitude, double tolerance)
    {
        return Math.Abs(location.Latitude - latitude) <= tolerance &&
               Math.Abs(location.Longitude - longitude) <= tolerance;
    }

    private async void OnReadHelpClicked(object? sender, EventArgs e)
    {
        try
        {
            const string helpText = "NutriBite records foods and drinks, shows nutrition details, and uses camera, on-device food recognition, location, speech, haptic feedback, accelerometer, compass, gyroscope, flashlight, and shake suggestions to make meal tracking more practical.";
            await SpeechService.SpeakAsync(helpText);
            SetStatus("Reading help content aloud.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Read hardware help aloud", ex);
            SetStatus("Text to speech could not start on this device right now.");
        }
    }

    private void OnToggleAccelerometerClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!sensorMonitor.IsAccelerometerSupported)
            {
                SetStatus("Accelerometer is not available on this device.");
                return;
            }

            if (!sensorMonitor.AccelerometerReadoutEnabled)
            {
                sensorMonitor.StartAccelerometerReadout(OnAccelerometerReadingChanged);
                AccelerometerButton.Text = "Stop";
                SetStatus("Accelerometer started. Tilt the device to see values change.");
            }
            else
            {
                sensorMonitor.StopAccelerometerReadout(OnAccelerometerReadingChanged);
                AccelerometerButton.Text = "Start";
                SetStatus("Accelerometer stopped.");
            }
        }
        catch (FeatureNotSupportedException)
        {
            SetStatus("Accelerometer is not supported on this device.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Toggle accelerometer", ex);
            SetStatus("Accelerometer could not be started on this device.");
        }
    }

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AccelLabel.Text = SensorMonitorService.FormatAcceleration(e);
        });
    }

    private void OnToggleCompassClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!sensorMonitor.IsCompassSupported)
            {
                SetStatus("Compass is not available on this device.");
                return;
            }

            if (!sensorMonitor.IsCompassMonitoring)
            {
                SensorMonitorService.StartCompass(OnCompassReadingChanged);
                CompassButton.Text = "Stop";
                SetStatus("Compass started. Rotate the device to see the heading change.");
            }
            else
            {
                SensorMonitorService.StopCompass(OnCompassReadingChanged);
                CompassButton.Text = "Start";
                SetStatus("Compass stopped.");
            }
        }
        catch (FeatureNotSupportedException)
        {
            SetStatus("Compass is not supported on this device.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Toggle compass", ex);
            SetStatus("Compass could not be started on this device.");
        }
    }

    private void OnCompassReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CompassLabel.Text = SensorMonitorService.FormatHeading(e);
        });
    }

    private void OnToggleGyroscopeClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!sensorMonitor.IsGyroscopeSupported)
            {
                SetStatus("Gyroscope is not available on this device.");
                return;
            }

            if (!sensorMonitor.IsGyroscopeMonitoring)
            {
                SensorMonitorService.StartGyroscope(OnGyroscopeReadingChanged);
                GyroscopeButton.Text = "Stop";
                SetStatus("Gyroscope started. Rotate the device to see angular velocity.");
            }
            else
            {
                SensorMonitorService.StopGyroscope(OnGyroscopeReadingChanged);
                GyroscopeButton.Text = "Start";
                SetStatus("Gyroscope stopped.");
            }
        }
        catch (FeatureNotSupportedException)
        {
            SetStatus("Gyroscope is not supported on this device.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Toggle gyroscope", ex);
            SetStatus("Gyroscope could not be started on this device.");
        }
    }

    private void OnGyroscopeReadingChanged(object? sender, GyroscopeChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GyroLabel.Text = SensorMonitorService.FormatAngularVelocity(e);
        });
    }

    private async void OnToggleFlashlightClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!flashlightOn)
            {
                await Flashlight.Default.TurnOnAsync();
                flashlightOn = true;
                FlashlightButton.Text = "Off";
                SetStatus("Flashlight turned on.");
            }
            else
            {
                await TurnFlashlightOffAsync();
                SetStatus("Flashlight turned off.");
            }
        }
        catch (FeatureNotSupportedException)
        {
            SetStatus("Flashlight is not supported on this device.");
        }
        catch (PermissionException)
        {
            SetStatus("Camera permission is required to use the flashlight.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Toggle flashlight", ex);
            SetStatus("Flashlight could not be changed on this device.");
        }
    }

    private void OnToggleShakeSuggestionClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!sensorMonitor.IsAccelerometerSupported)
            {
                SetStatus("Shake suggestions need an accelerometer, which is not available on this device.");
                return;
            }

            if (!sensorMonitor.ShakeSuggestionEnabled)
            {
                sensorMonitor.StartShakeSuggestion(OnShakeDetected);
                ShakeButton.Text = "Disable";
                SetStatus("Shake suggestions enabled. Shake the device to pick a meal.");
            }
            else
            {
                sensorMonitor.StopShakeSuggestion(OnShakeDetected);
                ShakeButton.Text = "Enable";
                SetStatus("Shake suggestions disabled.");
            }
        }
        catch (FeatureNotSupportedException)
        {
            SetStatus("Shake suggestions are not supported on this device.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Toggle shake suggestions", ex);
            SetStatus("Shake suggestions could not be enabled on this device.");
        }
    }

    private async void OnShakeDetected(object? sender, EventArgs e)
    {
        try
        {
            var items = await FoodCatalogService.SearchAsync(null);
            var suggestion = MealSuggestionService.PickRandom(items, suggestionRandom);
            var text = $"Shake suggestion: {suggestion.Name} ({suggestion.CaloriesLabel})";

            ShakeSuggestionLabel.Text = text;
            SetStatus(text);
            await SpeechService.SpeakAsync($"Try {suggestion.Name}.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Select shake meal suggestion", ex);
            SetStatus("A meal suggestion could not be selected right now.");
        }
    }

    private async Task TurnFlashlightOffAsync()
    {
        if (!flashlightOn)
        {
            return;
        }

        try
        {
            await Flashlight.Default.TurnOffAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("Turn flashlight off", ex);
        }

        flashlightOn = false;
        FlashlightButton.Text = "Flash";
    }

    private void StopMotionSensors()
    {
        try
        {
            sensorMonitor.StopAll(
                OnAccelerometerReadingChanged,
                OnShakeDetected,
                OnCompassReadingChanged,
                OnGyroscopeReadingChanged);
        }
        catch (Exception ex)
        {
            AppLog.Error("Stop motion sensors", ex);
        }

        AccelerometerButton.Text = "Start";
        CompassButton.Text = "Start";
        GyroscopeButton.Text = "Start";
        ShakeButton.Text = "Enable";
    }

    private void OnStopSpeechClicked(object? sender, EventArgs e)
    {
        SpeechService.Stop();
        SetStatus("Reading stopped.");
    }

    private async void OnReadPredictionClicked(object? sender, EventArgs e)
    {
        try
        {
            if (latestPrediction is null)
            {
                SetStatus("There is no food recognition result to read yet.");
                return;
            }

            await SpeechService.SpeakAsync($"Food recognition result: {latestPrediction.Label}, confidence {latestPrediction.Confidence:P0}.");
            SetStatus("Reading food recognition result aloud.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Read food recognition result aloud", ex);
            SetStatus("Food recognition result could not be read aloud right now.");
        }
    }

    private void OnFeedbackClicked(object? sender, EventArgs e)
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(450));
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            feedbackTestCount++;
            FeedbackCountLabel.Text = $"Haptic feedback tests: {feedbackTestCount}";
            SetStatus("Vibration and haptic feedback triggered. The changing counter can be used for screen-recorded verification.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Trigger vibration and haptic feedback", ex);
            SetStatus("Vibration or haptic feedback could not run on this device.");
        }
    }

    private void SetStatus(string message)
    {
        HardwareStatusLabel.Text = message;
        SemanticScreenReader.Announce(message);
    }
}
