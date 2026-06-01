using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FoodDrinkApp.Services;

/// <summary>
/// Represents the top image-classification label and confidence score.
/// </summary>
public sealed record Prediction(string Label, float Confidence);

/// <summary>
/// Runs bundled ONNX image classification over camera photos.
/// </summary>
public sealed class FoodVisionService : IDisposable
{
    private static readonly float[] ImageNetMean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] ImageNetStd = [0.229f, 0.224f, 0.225f];

    private readonly InferenceSession session;
    private readonly string inputName;
    private readonly IReadOnlyList<string> labels;
    private bool disposed;

    public FoodVisionService(byte[] modelBytes, IReadOnlyList<string> labels)
    {
        ArgumentNullException.ThrowIfNull(modelBytes);
        ArgumentNullException.ThrowIfNull(labels);

        if (modelBytes.Length == 0)
        {
            throw new ArgumentException("Model bytes cannot be empty.", nameof(modelBytes));
        }

        if (labels.Count == 0)
        {
            throw new ArgumentException("At least one label is required.", nameof(labels));
        }

        session = new InferenceSession(modelBytes);
        inputName = session.InputMetadata.Keys.First();
        this.labels = labels;
    }

    public Prediction Classify(byte[] imageBytes, int size = 224)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var tensor = CreateInputTensor(imageBytes, size);
        using var results = session.Run([NamedOnnxValue.CreateFromTensor(inputName, tensor)]);
        var scores = results.First().AsEnumerable<float>().ToArray();
        var probabilities = Softmax(scores);
        var index = 0;
        var confidence = probabilities[0];

        for (var i = 1; i < probabilities.Length; i++)
        {
            if (probabilities[i] > confidence)
            {
                index = i;
                confidence = probabilities[i];
            }
        }

        return new Prediction(LabelFor(index, probabilities.Length), confidence);
    }

    public static DenseTensor<float> CreateInputTensor(byte[] imageBytes, int size = 224)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        if (imageBytes.Length == 0)
        {
            throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));
        }

        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Image size must be positive.");
        }

        using var image = Image.Load<Rgb24>(imageBytes);
        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Crop
        }));

        var tensor = new DenseTensor<float>([1, 3, size, size]);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < size; y++)
            {
                var row = accessor.GetRowSpan(y);

                for (var x = 0; x < size; x++)
                {
                    var pixel = row[x];
                    tensor[0, 0, y, x] = Normalize(pixel.R, 0);
                    tensor[0, 1, y, x] = Normalize(pixel.G, 1);
                    tensor[0, 2, y, x] = Normalize(pixel.B, 2);
                }
            }
        });

        return tensor;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        session.Dispose();
        disposed = true;
    }

    private static float Normalize(byte value, int channel) =>
        (value / 255f - ImageNetMean[channel]) / ImageNetStd[channel];

    private static float[] Softmax(float[] scores)
    {
        var max = scores.Max();
        var sum = 0.0;
        var probabilities = new float[scores.Length];

        for (var i = 0; i < scores.Length; i++)
        {
            var value = Math.Exp(scores[i] - max);
            probabilities[i] = (float)value;
            sum += value;
        }

        if (sum <= 0)
        {
            return probabilities;
        }

        for (var i = 0; i < probabilities.Length; i++)
        {
            probabilities[i] = (float)(probabilities[i] / sum);
        }

        return probabilities;
    }

    private string LabelFor(int index, int outputCount)
    {
        if (labels.Count == outputCount)
        {
            return CleanLabel(labels[index]);
        }

        if (labels.Count == outputCount + 1 && index + 1 < labels.Count)
        {
            return CleanLabel(labels[index + 1]);
        }

        return $"ImageNet class {index}";
    }

    private static string CleanLabel(string label)
    {
        var trimmed = label.Trim();
        var firstSpace = trimmed.IndexOf(' ', StringComparison.Ordinal);

        if (firstSpace > 0 && trimmed[..firstSpace].StartsWith('n') && trimmed[..firstSpace].Skip(1).All(char.IsDigit))
        {
            return trimmed[(firstSpace + 1)..].Replace('_', ' ');
        }

        return trimmed.Replace('_', ' ');
    }
}
