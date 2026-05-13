namespace RummyBooky.Views;

public abstract class BaseView : ContentView
{	
	private Page? _parentPage;

    private void UpdateHeight()
	{
		UpdateHeight(DeviceDisplay.MainDisplayInfo);
	}

	private void OnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
	{
		UpdateHeight(e.DisplayInfo);
	}

	private void UpdateHeight(DisplayInfo info)
	{
		// Convert to device-independent units
		var screenHeight = info.Height / info.Density;
		var screenWidth = info.Width / info.Density;

		// Choose sensible defaults per device idiom + orientation
		var idiom = DeviceInfo.Idiom;
		var isLandscape = info.Orientation == DisplayOrientation.Landscape;

		double widthMultiplier;
		double heightMultiplier;
		double minWidth;
		double maxWidth;
		double minHeight;
		double maxHeight;

		// tuned values: reduce height on phones and in landscape to avoid large empty spaces,
		// make width take most of the screen on phones so footer has room.
		if (DeviceInfo.Idiom.Equals(DeviceIdiom.Phone))
		{
			widthMultiplier = 0.9;        // use most of screen width on phones
			heightMultiplier = isLandscape ? 0.55 : 0.62;
			minWidth = 260;
			maxWidth = 360;
			minHeight = 300;
			maxHeight = isLandscape ? 420 : 470;
		}
		else if (DeviceInfo.Idiom.Equals(DeviceIdiom.Tablet))
		{
			widthMultiplier = 0.6;
			heightMultiplier = isLandscape ? 0.6 : 0.58;
			minWidth = 360;
			maxWidth = 380;
			minHeight = 360;
			maxHeight = 460;
		}
		else if (DeviceInfo.Idiom.Equals(DeviceIdiom.Desktop) || DeviceInfo.Idiom.Equals(DeviceIdiom.TV))
		{
			widthMultiplier = 0.35;
			heightMultiplier = 0.55;
			minWidth = 300;
			maxWidth = 400;
			minHeight = 320;
			maxHeight = 495;
		}
		else
		{
			widthMultiplier = 0.35;
			heightMultiplier = 0.55;
			minWidth = 300;
			maxWidth = 1200;
			minHeight = 320;
			maxHeight = 560;
		}

		// If screen is very narrow (small phones), reduce inner paddings by shrinking the card a bit more.
		if (screenWidth < 360)
		{
			widthMultiplier = Math.Min(widthMultiplier, 0.95);
			maxHeight = Math.Min(maxHeight, 460);
		}

		var desiredWidth = Math.Clamp(screenWidth * widthMultiplier, minWidth, maxWidth);
		var desiredHeight = Math.Clamp(screenHeight * heightMultiplier, minHeight, maxHeight);

	}


	public (double desiredWidth, double desiredHeight) GetWidthAndHeight(DisplayInfo info)
	{
		// Convert to device-independent units
		var screenHeight = info.Height / info.Density;
		var screenWidth = info.Width / info.Density;

		// Choose sensible defaults per device idiom + orientation
		var idiom = DeviceInfo.Idiom;
		var isLandscape = info.Orientation == DisplayOrientation.Landscape;

		double widthMultiplier;
		double heightMultiplier;
		double minWidth;
		double maxWidth;
		double minHeight;
		double maxHeight;

		// tuned values: reduce height on phones and in landscape to avoid large empty spaces,
		// make width take most of the screen on phones so footer has room.
		if (DeviceInfo.Idiom.Equals(DeviceIdiom.Phone))
		{
			widthMultiplier = 0.9;        // use most of screen width on phones
			heightMultiplier = isLandscape ? 0.55 : 0.62;
			minWidth = 260;
			maxWidth = 360;
			minHeight = 300;
			maxHeight = isLandscape ? 420 : 470;
		}
		else if (DeviceInfo.Idiom.Equals(DeviceIdiom.Tablet))
		{
			widthMultiplier = 0.6;
			heightMultiplier = isLandscape ? 0.6 : 0.58;
			minWidth = 360;
			maxWidth = 380;
			minHeight = 360;
			maxHeight = 460;
		}
		else if (DeviceInfo.Idiom.Equals(DeviceIdiom.Desktop) || DeviceInfo.Idiom.Equals(DeviceIdiom.TV))
		{
			widthMultiplier = 0.35;
			heightMultiplier = 0.55;
			minWidth = 300;
			maxWidth = 400;
			minHeight = 320;
			maxHeight = 495;
		}
		else
		{
			widthMultiplier = 0.35;
			heightMultiplier = 0.55;
			minWidth = 300;
			maxWidth = 1200;
			minHeight = 320;
			maxHeight = 560;
		}

		// If screen is very narrow (small phones), reduce inner paddings by shrinking the card a bit more.
		if (screenWidth < 360)
		{
			widthMultiplier = Math.Min(widthMultiplier, 0.95);
			maxHeight = Math.Min(maxHeight, 460);
		}

		var desiredWidth = Math.Clamp(screenWidth * widthMultiplier, minWidth, maxWidth);
		var desiredHeight = Math.Clamp(screenHeight * heightMultiplier, minHeight, maxHeight);
		return (desiredWidth, desiredHeight);

    }
	protected override void OnParentSet()
	{
		base.OnParentSet();

		Element? parent = this.Parent;
		Page? page = null;
		while (parent != null)
		{
			if (parent is Page p)
			{
				page = p;
				break;
			}
			parent = parent.Parent;
		}

		if (_parentPage == page)
			return;

		if (_parentPage != null)
		{
			_parentPage.Appearing -= OnParentPageAppearing;
			_parentPage.Disappearing -= OnParentPageDisappearing;
			_parentPage = null;
		}

		if (page != null)
		{
			_parentPage = page;
			_parentPage.Appearing += OnParentPageAppearing;
			_parentPage.Disappearing += OnParentPageDisappearing;
		}
	}

	private void OnParentPageAppearing(object? sender, EventArgs e)
	{
		// guard against double-subscribe
		DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
		DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;
		UpdateHeight();
	}

	private void OnParentPageDisappearing(object? sender, EventArgs e)
	{
		DeviceDisplay.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;
	}

}
