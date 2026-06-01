# NutriBite

NutriBite is a .NET MAUI Food and Drink app for tracking nutrition, meal context, and device-supported interactions.

## Features

- Food and drink list with search, details, and refresh.
- Add-record form with friendly validation.
- Camera, location, text-to-speech, vibration, and haptic demonstrations.
- Theme switching, large text, semantic descriptions, and screen reader announcements.
- Local fallback data when mockapi.io is not configured.

## Build

Use Visual Studio 2022 with the .NET MAUI workload, or run:

```powershell
dotnet build .\FoodDrinkApp.csproj -f net9.0-android
```

The repository keeps Windows build outputs under `C:\MauiBuild\...` to avoid path issues during Android packaging on Windows.
