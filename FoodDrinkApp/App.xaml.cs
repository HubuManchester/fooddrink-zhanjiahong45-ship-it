namespace FoodDrinkApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		Services.ThemePreferenceService.ApplySavedTheme();
		Services.AccessibilityService.LoadSavedTextScale();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
