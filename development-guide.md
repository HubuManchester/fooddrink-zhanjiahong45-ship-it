# NutriBite Development Guide

## 1. Project Overview

NutriBite is a cross-platform .NET MAUI mobile app for the Food and Drink theme. The core experience helps users browse meals and drinks, review nutrition details, add their own records, and use mobile hardware features to make the app feel like a real device-based product.

The app supports:

- Browsing food and drink records.
- Searching by food name, drink name, category, description, or tags.
- Adding and editing local food or drink records.
- Deleting records from the local database.
- Viewing nutrition details.
- Capturing food photos.
- Reading meal location data, including country, city, area, latitude, and longitude.
- Reading nutrition summaries or help content with text-to-speech.
- Stopping speech when leaving a page.
- Vibration and haptic feedback.
- System, light, and dark themes.
- Persistent text-size preferences and screen-reader-friendly labels.

## 2. Assessment Coverage

| Assessment Area | Weight | Current Implementation |
|---|---:|---|
| UI/UX Design and Accessibility | 30% | Multi-page XAML UI, warm food-oriented visual style, bottom tab navigation, theme support, persistent large text, semantic descriptions, and screen-reader announcements. |
| Use of Mobile Hardware | 20% | Camera, Location/Geolocation, Geocoding, Text-to-speech, Vibration, and Haptic feedback are implemented. |
| Functionality | 20% | Food list, search, detail view, add/edit/delete, local persistence, hardware demonstrations, settings, speech stop behavior, and address display. |
| Validation and Error Handling | 10% | The add/edit page validates required fields, numeric ranges, and realistic nutrition upper bounds. Camera, location, speech, vibration, and data access paths include error handling and user-facing messages. |
| Code Quality | 10% | Models, services, pages, validation, formatting, data access, and logging are separated into focused classes. |
| Deployment | 5% | The project targets Android and Windows. Android builds have been verified from the command line. |
| GitHub Usage | 5% | The repository contains README files, development docs, tests, and multiple meaningful commits. |

The implementation covers the main coursework requirements. The final mark will still depend heavily on a clear screencast that demonstrates each feature on a working device or emulator.

## 3. Project Structure

```text
FoodDrinkApp/
  App.xaml
  App.xaml.cs
  AppShell.xaml
  AppShell.xaml.cs
  MainPage.xaml
  MainPage.xaml.cs
  AddItemPage.xaml
  AddItemPage.xaml.cs
  FoodDetailPage.xaml
  FoodDetailPage.xaml.cs
  HardwarePage.xaml
  HardwarePage.xaml.cs
  SettingsPage.xaml
  SettingsPage.xaml.cs
  HelpPage.xaml
  HelpPage.xaml.cs
  Services/
    AccessibilityService.cs
    AppDataService.cs
    SensorMonitorService.cs
    SpeechService.cs
    ThemePreferenceService.cs
  Platforms/
    Android/
      AndroidManifest.xml
  Resources/
    Styles/
      Colors.xaml
      Styles.xaml

FoodDrinkApp.Core/
  Models/
    FoodItem.cs
  Services/
    AppLog.cs
    ContrastRatio.cs
    FoodCatalogService.cs
    FoodFilterService.cs
    FoodItemValidator.cs
    FoodRepository.cs
    FoodVisionService.cs
    MealSuggestionService.cs
    MockApiConfig.cs
    SensorFormatter.cs

FoodDrinkApp.Tests/
  FoodCatalogServiceTests.cs
  FoodFilterServiceTests.cs
  FoodItemTests.cs
  FoodItemValidatorTests.cs
  FoodRepositoryTests.cs
  FoodVisionServiceTests.cs
  MealSuggestionServiceTests.cs
  SensorFormatterTests.cs
```

## 4. Key Files

### App.xaml / App.xaml.cs

`App.xaml` loads the global resource dictionaries for colors and control styles.

`App.xaml.cs` applies saved theme and text-scale preferences before creating the main shell:

```csharp
Services.ThemePreferenceService.ApplySavedTheme();
Services.AccessibilityService.LoadSavedTextScale();
return new Window(new AppShell());
```

### AppShell.xaml / AppShell.xaml.cs

`AppShell.xaml` defines the main tab navigation:

- Food
- Hardware
- Settings

`AppShell.xaml.cs` registers the routed pages:

```csharp
Routing.RegisterRoute(nameof(AddItemPage), typeof(AddItemPage));
Routing.RegisterRoute(nameof(FoodDetailPage), typeof(FoodDetailPage));
```

### MainPage.xaml / MainPage.xaml.cs

The home page shows the food and drink list.

Main controls:

- `SearchBar` for searching by food, drink, category, description, or tags.
- `CollectionView` for food cards.
- `RefreshView` for importing the latest mockapi.io or fallback catalog records into the local database.
- Swipe actions for delete and favorite feedback.

The page reads from the local SQLite repository through `AppDataService`:

```csharp
var repository = await AppDataService.GetRepositoryAsync();
var foods = await repository.SearchAsync(query);
FoodCollection.ItemsSource = foods;
```

### AddItemPage.xaml / AddItemPage.xaml.cs

The add/edit page lets users create or update food and drink records.

Form fields:

- Name
- Category
- Description
- Calories
- Protein
- Carbs
- Fat
- Allergy note

Validation is handled through `FoodItemValidator`:

- Name is required.
- Category is required.
- Description is required.
- Nutrition values must be valid non-negative numbers.
- Nutrition values must stay within realistic upper bounds.

When validation fails, the page shows an error panel and triggers vibration feedback. When validation succeeds, it saves the record to the local SQLite database.

### FoodDetailPage.xaml / FoodDetailPage.xaml.cs

The detail page shows one food or drink record.

Displayed data:

- Name
- Category
- Calories
- Protein, carbs, and fat
- Description
- Allergy note

The detail page supports:

- Reading the nutrition summary aloud.
- Stopping speech.
- Vibration feedback.
- Editing the current record.

Speech is centralized through `SpeechService`:

```csharp
await SpeechService.SpeakAsync(currentItem.AccessibleSummary);
```

Speech stops automatically when the user leaves the page:

```csharp
protected override void OnDisappearing()
{
    SpeechService.Stop();
    base.OnDisappearing();
}
```

### HardwarePage.xaml / HardwarePage.xaml.cs

The hardware page demonstrates mobile device capabilities.

| Feature | API |
|---|---|
| Camera | `MediaPicker.Default.CapturePhotoAsync()` |
| Location | `Geolocation.Default.GetLocationAsync()` |
| Reverse geocoding | `Geocoding.Default.GetPlacemarksAsync()` |
| Text-to-speech | `TextToSpeech.Default.SpeakAsync()` through `SpeechService` |
| Vibration | `Vibration.Default.Vibrate()` |
| Haptic feedback | `HapticFeedback.Default.Perform()` |
| Image classification | `FoodVisionService` with the bundled ONNX model |

Location display uses `SensorFormatter` so the text is consistent and easy to test:

```csharp
CoordinateLabel.Text = SensorFormatter.FormatCoordinates(location.Latitude, location.Longitude);
```

Haptic feedback increments a visible count so the behavior can be verified in a screencast:

```csharp
feedbackTestCount++;
FeedbackCountLabel.Text = SensorFormatter.FormatFeedbackCount(feedbackTestCount);
```

### SettingsPage.xaml / SettingsPage.xaml.cs

The settings page handles accessibility and appearance preferences.

Current options:

- Follow system theme.
- Light theme.
- Dark theme.
- Text scale from normal to 200%.

Theme settings are saved through `ThemePreferenceService`, and text-scale settings are saved through `AccessibilityService`. Both are restored on app startup.

### Models/FoodItem.cs

`FoodItem` is the food and drink data model.

Main properties:

- `Id`
- `LocalId`
- `Name`
- `Category`
- `Description`
- `Calories`
- `Protein`
- `Carbs`
- `Fat`
- `AllergyNote`
- `Tags`

It also exposes display and speech helper properties:

```csharp
public string CaloriesLabel => $"{Calories} kcal";
public string MacroSummary => $"Protein {Protein}g, carbs {Carbs}g, fat {Fat}g";
public string AccessibleSummary => $"{Name}. {Category}. {Calories} calories. {MacroSummary}. {AllergyNote}";
```

### Services/FoodRepository.cs

`FoodRepository` is the local SQLite data layer.

It supports:

- Initial database creation.
- Seeding when the database is empty.
- Reading all records.
- Searching records.
- Reading by public ID or local ID.
- Adding records.
- Updating records.
- Deleting records.
- Importing mockapi.io or fallback catalog records without duplicating existing public IDs.

### Services/FoodCatalogService.cs

`FoodCatalogService` reads food data from mockapi.io or the local fallback catalog.

It supports:

- mockapi.io REST API first.
- Local fallback data when the API URL is not configured or the network is unavailable.
- Search.
- Reading details by ID.
- Adding a new remote record when the API is configured.

The mockapi.io endpoint is configured in:

```text
FoodDrinkApp.Core/Services/MockApiConfig.cs
```

See `mockapi-setup.md` for the setup steps.

### Services/SpeechService.cs

`SpeechService` centralizes text-to-speech behavior.

It supports:

- Stopping previous speech before starting new speech.
- Stopping speech on demand.
- Selecting the best available speech locale from the device.
- Setting volume and pitch.

Voice quality depends on the speech packages installed on the device. A physical phone usually sounds better than an emulator.

### Services/AccessibilityService.cs

`AccessibilityService` applies the saved text scale to visual controls such as `Label`, `Button`, `Entry`, `Editor`, `Picker`, and `SearchBar`.

### Platforms/Android/AndroidManifest.xml

The Android manifest contains the required permissions:

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.VIBRATE" />
```

These permissions support camera, location, and vibration features.

### Resources/Styles/Styles.xaml

The global styles define consistent UI behavior for:

- Button
- Entry
- Editor
- Picker
- SearchBar
- Label
- Shell TabBar

The visual style uses a warm food-focused palette with cream backgrounds, tomato red, roasted orange, and basil green.

## 5. Build and Run

Android build:

```powershell
dotnet build .\FoodDrinkApp\FoodDrinkApp.csproj -f net9.0-android --no-incremental
```

Windows build:

```powershell
dotnet build .\FoodDrinkApp\FoodDrinkApp.csproj -f net9.0-windows10.0.19041.0 --no-incremental
```

Android run:

```powershell
dotnet build .\FoodDrinkApp\FoodDrinkApp.csproj -f net9.0-android -t:Run
```

If Visual Studio cannot launch the Android project, start an Android emulator or connect a physical device first.

## 6. Screencast Demonstration Plan

Recommended order:

1. Introduce the Food and Drink theme and the NutriBite app name.
2. Show the home UI, search box, food cards, and bottom navigation.
3. Search for a category such as Breakfast or Drinks.
4. Open a detail page and show nutrition information.
5. Tap Read Summary, then Stop Reading, and mention that speech stops when leaving the page.
6. Tap the vibration action and mention Vibration plus Haptic feedback.
7. Open the add/edit page, leave fields blank or enter invalid values, and show validation.
8. Add a valid record and confirm it appears in the list.
9. Edit that record and confirm the changes persist.
10. Delete a record and confirm it is removed from the local database.
11. Open the hardware page and demonstrate camera capture.
12. Demonstrate location and show country, city, area, and coordinates.
13. Demonstrate help speech and stopping speech.
14. Demonstrate haptic feedback and the visible count.
15. Open settings and demonstrate system, light, and dark themes.
16. Change text scale and show that the larger text persists.
17. Show the code structure: models, services, XAML pages, Android manifest permissions, and tests.
18. Show build or test results.
19. Show GitHub commits and README.

## 7. Remaining Submission Notes

- GitHub usage is worth 5%, so keep meaningful commits instead of one final bulk commit.
- The screencast is a major part of the assessment. Demonstrate every scoring item clearly.
- If the emulator cannot show a realistic city, use the emulator Location controls or test on a physical device.
- Text-to-speech quality depends on the operating system speech package.
