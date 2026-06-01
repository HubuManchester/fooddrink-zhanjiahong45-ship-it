using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace FoodDrinkApp.Services;

/// <summary>
/// Represents a camera capture that has been classified by the bundled food-vision model.
/// </summary>
/// <param name="ImageBytes">The captured image bytes that can be displayed by the UI.</param>
/// <param name="Prediction">The top food-recognition prediction for the captured image.</param>
public sealed record CameraVisionResult(byte[] ImageBytes, Prediction Prediction);

/// <summary>
/// Coordinates camera capture and bundled ONNX food recognition for the hardware page.
/// </summary>
public sealed class CameraVisionService
{
    private FoodVisionService? foodVisionService;

    /// <summary>
    /// Gets whether the current device supports still-photo capture.
    /// </summary>
    public bool IsCaptureSupported => MediaPicker.Default.IsCaptureSupported;

    /// <summary>
    /// Captures a photo and returns its bytes, or null when the user cancels capture.
    /// </summary>
    public async Task<byte[]?> CapturePhotoAsync()
    {
        var photo = await MediaPicker.Default.CapturePhotoAsync();
        return photo is null ? null : await ReadImageBytesAsync(photo);
    }

    /// <summary>
    /// Classifies image bytes with the cached bundled ONNX model.
    /// </summary>
    public async Task<Prediction> ClassifyAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var visionService = await LoadFoodVisionServiceAsync();
        return await Task.Run(() => visionService.Classify(imageBytes), cancellationToken);
    }

    /// <summary>
    /// Captures a photo and returns a complete classified result in one operation.
    /// </summary>
    public async Task<CameraVisionResult?> CaptureAndClassifyAsync(CancellationToken cancellationToken = default)
    {
        var imageBytes = await CapturePhotoAsync();
        if (imageBytes is null)
        {
            return null;
        }

        var prediction = await ClassifyAsync(imageBytes, cancellationToken);
        return new CameraVisionResult(imageBytes, prediction);
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

    private static async Task<byte[]> ReadImageBytesAsync(FileResult photo)
    {
        await using var stream = await photo.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}
