using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;
using AUri = Android.Net.Uri;
using JFile = Java.IO.File;

namespace FoodDrinkApp.Services;

internal static class AndroidCameraCaptureService
{
    private const int CapturePhotoRequestCode = 47291;
    private static readonly object SyncRoot = new();
    private static CaptureRequest? pendingRequest;

    internal static async Task<byte[]?> CapturePhotoAsync()
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("The Android activity is not ready for camera capture.");
        var packageManager = activity.PackageManager
            ?? throw new InvalidOperationException("The Android package manager is not available.");

        var captureIntent = new Intent(MediaStore.ActionImageCapture);
        if (captureIntent.ResolveActivity(packageManager) is null)
        {
            throw new FeatureNotSupportedException("No Android camera app can handle image capture.");
        }

        var cacheDir = activity.CacheDir
            ?? throw new InvalidOperationException("The Android cache directory is not available.");
        var outputFile = new JFile(cacheDir, $"{Guid.NewGuid():N}.jpg");
        var outputUri = CreateOutputUri(activity, outputFile);

        captureIntent.PutExtra(MediaStore.ExtraOutput, outputUri);
        captureIntent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

        var grantedPackages = GrantCameraUriPermissions(activity, packageManager, captureIntent, outputUri);
        var request = new CaptureRequest(
            new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously),
            outputFile,
            outputUri,
            grantedPackages);

        lock (SyncRoot)
        {
            if (pendingRequest is not null)
            {
                throw new InvalidOperationException("A camera capture is already in progress.");
            }

            pendingRequest = request;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(
                () => activity.StartActivityForResult(captureIntent, CapturePhotoRequestCode));

            var result = await request.Completion.Task;
            if (result != Result.Ok)
            {
                return null;
            }

            if (!outputFile.Exists() || outputFile.Length() == 0)
            {
                throw new IOException("The camera returned without saving an image.");
            }

            return await File.ReadAllBytesAsync(outputFile.AbsolutePath);
        }
        finally
        {
            ClearPendingRequest(request);
            RevokeCameraUriPermissions(activity, request);
            TryDelete(outputFile);
        }
    }

    internal static bool OnActivityResult(int requestCode, Result resultCode)
    {
        if (requestCode != CapturePhotoRequestCode)
        {
            return false;
        }

        CaptureRequest? request;
        lock (SyncRoot)
        {
            request = pendingRequest;
        }

        request?.Completion.TrySetResult(resultCode);
        return true;
    }

    private static AUri CreateOutputUri(Activity activity, JFile outputFile)
    {
        var authority = $"{activity.PackageName}.fileProvider";
        return AndroidX.Core.Content.FileProvider.GetUriForFile(activity, authority, outputFile)
            ?? throw new InvalidOperationException("The camera output URI could not be created.");
    }

    private static IReadOnlyList<string> GrantCameraUriPermissions(
        Activity activity,
        PackageManager packageManager,
        Intent captureIntent,
        AUri outputUri)
    {
        var grantedPackages = new List<string>();
        var handlers = packageManager.QueryIntentActivities(captureIntent, PackageInfoFlags.MatchDefaultOnly);

        foreach (var handler in handlers)
        {
            var packageName = handler.ActivityInfo?.PackageName;
            if (string.IsNullOrWhiteSpace(packageName))
            {
                continue;
            }

            activity.GrantUriPermission(
                packageName,
                outputUri,
                ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            grantedPackages.Add(packageName);
        }

        return grantedPackages;
    }

    private static void RevokeCameraUriPermissions(Activity activity, CaptureRequest request)
    {
        activity.RevokeUriPermission(
            request.OutputUri,
            ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
    }

    private static void ClearPendingRequest(CaptureRequest request)
    {
        lock (SyncRoot)
        {
            if (ReferenceEquals(pendingRequest, request))
            {
                pendingRequest = null;
            }
        }
    }

    private static void TryDelete(JFile outputFile)
    {
        try
        {
            if (outputFile.Exists())
            {
                outputFile.Delete();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Delete temporary camera photo", ex);
        }
    }

    private sealed record CaptureRequest(
        TaskCompletionSource<Result> Completion,
        JFile OutputFile,
        AUri OutputUri,
        IReadOnlyList<string> GrantedPackages);
}
