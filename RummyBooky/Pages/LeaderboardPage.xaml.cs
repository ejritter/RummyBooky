namespace RummyBooky.Pages;

public partial class LeaderboardPage : BasePage<LeaderboardViewModel>
{
	public LeaderboardPage(LeaderboardViewModel vm) : base(vm)
	{
		InitializeComponent();
	}


    protected override void OnAppearing()
    {
        base.OnAppearing();
        if(BindingContext is LeaderboardViewModel vm)
        {
            vm.AppearingCommand.Execute(null);
        }
    }

}