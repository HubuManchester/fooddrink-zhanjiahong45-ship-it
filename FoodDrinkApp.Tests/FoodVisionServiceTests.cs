using FoodDrinkApp.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace FoodDrinkApp.Tests;

public sealed class FoodVisionServiceTests
{
    [Fact]
    public void CreateInputTensor_returns_nchw_imagenet_tensor()
    {
        var tensor = FoodVisionService.CreateInputTensor(CreateSampleFoodImage(), 224);

        Assert.Equal([1, 3, 224, 224], tensor.Dimensions.ToArray());
        Assert.All(tensor.ToArray(), value => Assert.True(float.IsFinite(value)));
    }

    [Fact]
    public void Classify_returns_label_and_confidence_for_sample_food_image()
    {
        using var service = new FoodVisionService(LoadAsset("mobilenetv2-7.onnx"), LoadLabels());

        var prediction = service.Classify(CreateSampleFoodImage());

        Assert.False(string.IsNullOrWhiteSpace(prediction.Label));
        Assert.InRange(prediction.Confidence, 0f, 1f);
        Assert.True(prediction.Confidence > 0f);
    }

    private static byte[] CreateSampleFoodImage()
    {
        using var image = new Image<Rgb24>(96, 96);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);

                for (var x = 0; x < row.Length; x++)
                {
                    var warm = (byte)Math.Min(255, 180 + x / 2);
                    var green = (byte)Math.Min(255, 110 + y / 3);
                    row[x] = new Rgb24(warm, green, 42);
                }
            }
        });

        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    private static byte[] LoadAsset(string fileName) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, fileName));

    private static IReadOnlyList<string> LoadLabels() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "imagenet-slim-labels.txt"))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
}
