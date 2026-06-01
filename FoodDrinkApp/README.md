# NutriBite

**Author:** TODO: add real full name and student ID before submission
**App:** NutriBite

NutriBite is a .NET MAUI 9 Food and Drink app for tracking nutrition, meal context, and device-supported interactions. It combines a searchable food catalogue, nutrition detail views, accessibility settings, and a hardware demonstration page with bundled on-device image classification.

## Development Plan And Feature List

- Foods tab: searchable food and drink list, category filter, favourites-only filter, swipe-to-favourite action, pull-to-refresh, details page, and add-record form.
- Details flow: macro summary, animated macro ring, read-aloud nutrition summary, and vibration reminder.
- Hardware tab: camera capture, on-device MobileNetV2 ONNX recognition, location/geocoding, text-to-speech, vibration/haptic feedback, accelerometer, compass, gyroscope, flashlight, and shake-to-suggest.
- Help tab: clear feature instructions, read-aloud help, and a friendly handled-error demo.
- Settings tab: system/light/dark themes and text scaling up to 200%.
- Quality plan: keep Core services reusable, keep the MAUI pages thin where practical, and cover validation, filtering, formatting, contrast, fallback data, and vision preprocessing with xUnit tests.

## Build And Run

Use Visual Studio 2022 with the .NET MAUI workload and Android SDK installed. Select an Android emulator or device and run `FoodDrinkApp`.

Command-line checks from the repository root:

```powershell
dotnet build .\FoodDrinkApp\FoodDrinkApp.csproj -f net9.0-android
dotnet test
```

The repository uses `Directory.Build.props` to redirect Windows build output to `C:\MauiBuild\...`, reducing long-path and Android packaging issues.

## Hardware Verification

- Camera: capture a food photo and confirm a label plus confidence appears.
- ML/CV: bundled `mobilenetv2-7.onnx` and `imagenet-slim-labels.txt` are MAUI raw assets, so recognition runs without network access.
- Location: use emulator location controls or a real device and confirm address plus coordinates.
- Sensors: use Android Emulator Extended Controls, Virtual sensors for accelerometer, compass, and gyroscope.
- Flashlight and haptics: verify on a real device where emulator hardware is unavailable.
- Shake: shake the emulator/device and confirm a meal suggestion appears and can be spoken.

## Accessibility And WCAG

- Semantic descriptions, hints, headings, and screen reader announcements are included on the main controls.
- Text scaling supports standard, 135%, 175%, and 200% sizes for WCAG 1.4.4 review.
- Core tests verify representative palette pairs meet WCAG AA contrast thresholds.
- Tablet and desktop layouts use adaptive spacing and two-column food cards via `OnIdiom`.

## Screencast

Screencast link: TODO: paste final mmutube/Xuexitong link before submission

Screenshots and submission notes live in `docs/` at the repository root. Add final screenshots there after the human screencast review.

## Manual Checklist

- Run light mode, dark mode, and system theme.
- Run screen reader read-through of Foods, Hardware, Help, and Settings.
- Test large text at 200% and confirm labels/buttons remain usable.
- Test swipe-to-favourite, category filter, favourites-only filter, and refresh.
- Test Android phone emulator, Android tablet emulator, and Windows if available.
- Replace the TODO author and screencast placeholders with the real submission details before submission.
