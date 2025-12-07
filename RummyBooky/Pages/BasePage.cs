namespace RummyBooky.Pages;

public abstract class BasePage<TViewModel> : ContentPage where TViewModel : BaseViewModel
{
    protected BasePage(TViewModel viewModel)
    {
        BindingContext = viewModel;
    }

    
}
