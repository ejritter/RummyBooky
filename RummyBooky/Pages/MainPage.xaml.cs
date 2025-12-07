namespace RummyBooky.Pages;

public partial class MainPage : BasePage<MainPageViewModel>
{
    public MainPage(MainPageViewModel vm) : base(vm)
    {
        InitializeComponent();
    }
    

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainPageViewModel vm && vm.AppearingCommand.CanExecute(null))
        {
            await vm.AppearingCommand.ExecuteAsync(null);
        }
    }
}