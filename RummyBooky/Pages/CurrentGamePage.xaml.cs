namespace RummyBooky.Pages;

public partial class CurrentGamePage : BasePage<CurrentGameViewModel>
{
	public CurrentGamePage(CurrentGameViewModel vm) : base(vm)
	{
		InitializeComponent();
	}

	protected override bool OnBackButtonPressed()
    {
        return true;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsEnabled = false,
            IsVisible = false
        });
    }
}
