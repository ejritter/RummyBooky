namespace RummyBooky.Pages;

public partial class NewGamePage : BasePage<NewGameViewModel>
{
	public NewGamePage(NewGameViewModel vm) : base(vm)
    {
		InitializeComponent();
	}

	protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is NewGameViewModel vm && vm.AppearingCommand.CanExecute(null))
        {
            await vm.AppearingCommand.ExecuteAsync(null);
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is NewGameViewModel vm && vm.DisappearingCommand.CanExecute(null))
        {
            await vm.DisappearingCommand.ExecuteAsync(null);
        }
    }
}