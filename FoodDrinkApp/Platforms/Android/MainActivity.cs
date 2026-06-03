using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.Unspecified,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        AndroidCameraCaptureService.OnActivityResult(requestCode, resultCode);
        base.OnActivityResult(requestCode, resultCode, data);
    }
}
