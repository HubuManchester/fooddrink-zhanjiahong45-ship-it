# mockapi.io Data Source Setup

NutriBite can read and import food data from mockapi.io. The app also keeps a local SQLite database so records remain available after restart.

## 1. Current Data Source Flow

The original project used local mock data in:

```text
FoodDrinkApp.Core/Services/FoodCatalogService.cs
```

The current data flow is:

- Use mockapi.io REST API when an endpoint is configured and reachable.
- Use local fallback data when the API is not configured or the network is unavailable.
- Import remote or fallback catalog records into the local SQLite database.
- Use the local SQLite database for browse, search, add, edit, and delete inside the app.

## 2. Create a Resource in mockapi.io

1. Open [mockapi.io](https://mockapi.io).
2. Create or open a project.
3. Add a resource.
4. Use this recommended resource name:

```text
foods
```

5. Add these fields:

| Field | Suggested Type | Description |
|---|---|---|
| name | String | Food or drink name |
| category | String | Category, such as Breakfast, Lunch, or Drinks |
| description | String | Description |
| calories | Number | Calories |
| protein | Number | Protein in grams |
| carbs | Number | Carbohydrates in grams |
| fat | Number | Fat in grams |
| allergyNote | String | Allergy note |
| tags | String | Search tags |

The `id` field is generated automatically by mockapi.io and does not need to be added manually.

## 3. Example Data

You can add records similar to these:

```json
{
  "name": "Berry Yogurt Bowl",
  "category": "Breakfast",
  "description": "Greek yogurt with mixed berries, oats, and a small amount of honey.",
  "calories": 340,
  "protein": 24,
  "carbs": 42,
  "fat": 8,
  "allergyNote": "Contains dairy and gluten.",
  "tags": "healthy breakfast yogurt berries"
}
```

```json
{
  "name": "Chicken Brown Rice Box",
  "category": "Lunch",
  "description": "Grilled chicken breast, brown rice, spinach, cucumber, and lemon dressing.",
  "calories": 520,
  "protein": 38,
  "carbs": 58,
  "fat": 14,
  "allergyNote": "No common allergens recorded.",
  "tags": "meal prep protein lunch"
}
```

## 4. Configure the API URL

After the resource is created, mockapi.io generates a URL like this:

```text
https://682xxxx.mockapi.io/api/v1/foods
```

Open:

```text
FoodDrinkApp.Core/Services/MockApiConfig.cs
```

Change `EndpointUrl` to your URL:

```csharp
public const string EndpointUrl = "https://682xxxx.mockapi.io/api/v1/foods";
```

After restarting the app:

- Pull-to-refresh on the home page imports records from mockapi.io into the local SQLite database.
- The app still works from fallback data if the API is not configured or unavailable.
- Add, edit, delete, browse, and search use the local database for persistence.

## 5. How to Explain This in the Screencast

Suggested explanation:

> The app can use mockapi.io as a realistic REST data source, but it also has a local SQLite database for persistence. On startup the app seeds the local database from the catalog source, and pull-to-refresh can import the latest mockapi.io or fallback records. The main user actions, including browse, search, add, edit, and delete, operate on local SQLite data so records remain available after restarting the app.
