using FoodDrinkApp.Services;
using Microsoft.ML.OnnxRuntime;
using Microsoft.Maui.Devices.Sensors;
using SixLabors.ImageSharp;

namespace FoodDrinkApp;

public partial class HardwarePage : ContentPage
{
    private static readonly TimeSpan ShakeSuggestionCooldown = TimeSpan.FromSeconds(2);
    private int feedbackTestCount;
    private readonly CameraVisionService cameraVisionService = new();
    private readonly FlashlightService flashlightService = new();
    private readonly LocationLookupService locationLookupService = new();
    private readonly SensorMonitorService sensorMonitor = new();
    private readonly SemaphoreSlim shakeSuggestionGate = new(1, 1);
    private Prediction? latestPrediction;
    private readonly Random suggestionRandom = new();
    private DateTimeOffset lastShakeSuggestionAt = DateTimeOffset.MinValue;
    private bool? wideHardwareLayoutApplied;

    public HardwarePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
        ApplyHardwareLayout(Width, Height);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        ApplyHardwareLayout(width, height);
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
        const string takePhoto = "Take a photo";
        const string chooseFromGallery = "Choose from gallery";

        try
        {
            var choice = await DisplayActionSheet("Add a food photo", "Cancel", null, takePhoto, chooseFromGallery);
            switch (choice)
            {
                case takePhoto:
                    await CaptureFoodPhotoAsync();
                    break;
                case chooseFromGallery:
                    await PickFoodPhotoAsync();
                    break;
                default:
                    SetPhotoStatus("No photo selected.");
                    break;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Open food photo action sheet", ex);
            SetPhotoStatus("Photo options could not be opened right now.");
        }
    }

    private async Task CaptureFoodPhotoAsync()
    {
        try
        {
            var permission = await Permissions.RequestAsync<Permissions.Camera>();
            if (permission != PermissionStatus.Granted)
            {
                SetPhotoStatus("Camera permission is needed to take a photo. You can choose from the gallery instead.");
                return;
            }

            SetPhotoStatus("Opening camera...");
            await LoadAndClassifyPhotoAsync(
                cameraVisionService.CapturePhotoAsync,
                "Food photo captured. Running on-device recognition...",
                "No photo taken.");
        }
        catch (PermissionException ex)
        {
            AppLog.Error("Camera capture", ex);
            SetPhotoStatus("Camera permission was denied. You can choose from the gallery instead.");
        }
        catch (FeatureNotSupportedException ex)
        {
            AppLog.Error("Camera capture", ex);
            SetPhotoStatus("Camera capture is not available here. Please choose a photo from the gallery instead.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Camera capture", ex);
            SetPhotoStatus("Could not open the camera. Please choose a photo from the gallery instead.");
        }
    }

    private async Task PickFoodPhotoAsync()
    {
        try
        {
            SetPhotoStatus("Opening gallery...");
            await LoadAndClassifyPhotoAsync(
                cameraVisionService.PickPhotoAsync,
                "Food photo selected. Running on-device recognition...",
                "No photo selected.");
        }
        catch (FeatureNotSupportedException ex)
        {
            AppLog.Error("Pick food photo from gallery", ex);
            SetPhotoStatus("Photo gallery selection is not available on this device.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Pick food photo from gallery", ex);
            SetPhotoStatus("Photo gallery selection could not be completed right now.");
        }
    }

    private async Task LoadAndClassifyPhotoAsync(
        Func<Task<byte[]?>> loadPhotoAsync,
        string recognitionStatus,
        string cancelledStatus)
    {
        var imageBytes = await loadPhotoAsync();
        if (imageBytes is null)
        {
            SetPhotoStatus(cancelledStatus);
            return;
        }

        FoodPhoto.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);

        latestPrediction = null;
        PredictionLabel.Text = "Classifying food photo...";
        ReadPredictionButton.IsEnabled = false;
        SetStatus(recognitionStatus);

        try
        {
            latestPrediction = await cameraVisionService.ClassifyAsync(imageBytes);
            PredictionLabel.Text = $"Food recognition: {latestPrediction.Label} ({latestPrediction.Confidence:P0})";
            ReadPredictionButton.IsEnabled = true;
            SetStatus("Food recognition completed.");
        }
        catch (FileNotFoundException ex)
        {
            AppLog.Error("Load food recognition assets", ex);
            SetPhotoStatus("Food recognition assets are missing from this app build.");
        }
        catch (UnknownImageFormatException ex)
        {
            AppLog.Error("Decode selected food photo", ex);
            SetPhotoStatus("The selected file is not a supported image.");
        }
        catch (InvalidImageContentException ex)
        {
            AppLog.Error("Decode selected food photo", ex);
            SetPhotoStatus("The selected image could not be decoded. Try a different JPEG or PNG.");
        }
        catch (OnnxRuntimeException ex)
        {
            AppLog.Error("Run food recognition inference", ex);
            SetPhotoStatus("Food recognition inference could not run on this device right now.");
        }
        catch (InvalidOperationException ex)
        {
            AppLog.Error("Run food recognition", ex);
            SetPhotoStatus("Food recognition could not start on this device right now.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Classify food photo", ex);
            SetPhotoStatus("Food recognition could not be completed right now.");
        }
    }

    private async void OnGetLocationClicked(object? sender, EventArgs e)
    {
        try
        {
            SetStatus("Getting location...");
            var result = await locationLookupService.GetCurrentMealLocationAsync();

            if (result is null)
            {
                SetStatus("Current location could not be found.");
                return;
            }

            CoordinateLabel.Text = result.CoordinatesText;
            LocationLabel.Text = result.AddressText;
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
            var isOn = await flashlightService.ToggleAsync();
            FlashlightButton.Text = FlashlightService.ButtonTextFor(isOn);
            SetStatus(isOn ? "Flashlight turned on." : "Flashlight turned off.");
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
        if (!shakeSuggestionGate.Wait(0))
        {
            return;
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now - lastShakeSuggestionAt < ShakeSuggestionCooldown)
            {
                return;
            }

            lastShakeSuggestionAt = now;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SetStatus("Choosing a meal suggestion...");
            });

            var repository = await AppDataService.GetRepositoryAsync();
            var items = await repository.GetAllAsync();
            if (items.Count == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetStatus("No saved foods are available for shake suggestions yet.");
                });
                return;
            }

            var suggestion = MealSuggestionService.PickRandom(items, suggestionRandom);
            var text = $"Shake suggestion: {suggestion.Name} ({suggestion.CaloriesLabel})";

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ShakeSuggestionLabel.Text = text;
                SetStatus(text);
            });

            await MainThread.InvokeOnMainThreadAsync(() => SpeechService.SpeakAsync($"Try {suggestion.Name}."));
        }
        catch (Exception ex)
        {
            AppLog.Error("Select shake meal suggestion", ex);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SetStatus("A meal suggestion could not be selected right now.");
            });
        }
        finally
        {
            shakeSuggestionGate.Release();
        }
    }

    private async Task TurnFlashlightOffAsync()
    {
        if (!flashlightService.IsOn)
        {
            return;
        }

        try
        {
            await flashlightService.TurnOffAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("Turn flashlight off", ex);
        }

        FlashlightButton.Text = FlashlightService.ButtonTextFor(false);
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

    private void SetPhotoStatus(string message)
    {
        latestPrediction = null;
        PredictionLabel.Text = message;
        ReadPredictionButton.IsEnabled = false;
        SetStatus(message);
    }

    private void ApplyHardwareLayout(double width, double height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var useWideLayout = width >= 700 && width > height;
        if (wideHardwareLayoutApplied == useWideLayout)
        {
            return;
        }

        wideHardwareLayoutApplied = useWideLayout;
        HardwareSectionsGrid.ColumnDefinitions.Clear();
        HardwareSectionsGrid.RowDefinitions.Clear();

        if (useWideLayout)
        {
            HardwareSectionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            HardwareSectionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            HardwareSectionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            HardwareSectionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            HardwareSectionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            SetHardwareSectionPosition(PhotoCard, 0, 0);
            SetHardwareSectionPosition(LocationCard, 1, 0);
            SetHardwareSectionPosition(SensorsCard, 0, 1, rowSpan: 2);
            SetHardwareSectionPosition(FlashShakeCard, 2, 0, columnSpan: 2);
            FoodPhotoFrame.HeightRequest = 160;
            return;
        }

        HardwareSectionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (var row = 0; row < 4; row++)
        {
            HardwareSectionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        SetHardwareSectionPosition(PhotoCard, 0, 0);
        SetHardwareSectionPosition(LocationCard, 1, 0);
        SetHardwareSectionPosition(SensorsCard, 2, 0);
        SetHardwareSectionPosition(FlashShakeCard, 3, 0);
        FoodPhotoFrame.HeightRequest = 220;
    }

    private static void SetHardwareSectionPosition(View section, int row, int column, int rowSpan = 1, int columnSpan = 1)
    {
        Grid.SetRow(section, row);
        Grid.SetColumn(section, column);
        Grid.SetRowSpan(section, rowSpan);
        Grid.SetColumnSpan(section, columnSpan);
    }
}
