# NutriBite

Author: TODO student name

NutriBite is a .NET MAUI 9 Food and Drink coursework app. It tracks foods and drinks, shows nutrition summaries, demonstrates mobile hardware features, and runs bundled on-device image recognition from camera photos.

## Project Layout

- `FoodDrinkApp`: MAUI app for Android, iOS, Mac Catalyst, and Windows.
- `FoodDrinkApp.Core`: pure `net9.0` models and services.
- `FoodDrinkApp.Tests`: xUnit tests for validation, formatting, contrast, and ONNX classification.

## Build

Open `FoodDrinkApp.sln` in Visual Studio 2022 with the .NET MAUI workload installed, or run:

```powershell
dotnet build .\FoodDrinkApp\FoodDrinkApp.csproj -f net9.0-android
dotnet test
```

The Android target now uses min SDK 24 because the bundled ONNX Runtime Android package requires API 24 or newer.

## TODO Before Submission (Human)

Screencast, final manual review, push if needed, and university submission upload.
