# NutriBite

**Author:** <FILL IN: full name + student ID>
**App:** NutriBite

NutriBite is a .NET MAUI 9 Food and Drink coursework app. It tracks foods and drinks, shows nutrition summaries, demonstrates mobile hardware features, and runs bundled on-device image recognition from camera photos.

## Development Plan

1. Build the core food and drink catalogue with search, category filtering, favourites, add-record validation, and a nutrition detail view.
2. Add mobile hardware features that support the nutrition theme: camera, on-device food recognition, location, speech, haptics, sensors, flashlight, and shake suggestions.
3. Improve accessibility and UX through semantic labels, dark/light themes, large text support, adaptive layouts, and WCAG contrast checks.
4. Keep the project verifiable with automated tests for validation, filtering, formatting, contrast, catalogue fallback behaviour, and vision preprocessing.

## Project Layout

- `FoodDrinkApp`: MAUI app for Android, iOS, Mac Catalyst, and Windows.
- `FoodDrinkApp.Core`: pure `net9.0` models and reusable services.
- `FoodDrinkApp.Tests`: xUnit tests for validation, filtering, formatting, contrast, catalogue behaviour, and ONNX classification.
- `docs`: submission notes and screenshot placeholders.

## Build And Test

Open `FoodDrinkApp.sln` in Visual Studio 2022 with the .NET MAUI workload installed, or run:

```powershell
dotnet build .\FoodDrinkApp\FoodDrinkApp.csproj -f net9.0-android
dotnet test
```

The Android target uses min SDK 24 because the bundled ONNX Runtime Android package requires API 24 or newer.

## Screencast

Screencast link: <add mmutube/Xuexitong link before submission>

Screenshots and submission notes live in `docs/`. Add final screenshots there after the human screencast review.

## Final Human Submission Steps

1. Replace the author placeholder with the real full name and student ID.
2. Add the final screencast link above.
3. Run the final manual review on phone/tablet where available.
4. Upload the university submission package.
