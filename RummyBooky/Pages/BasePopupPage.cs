using CommunityToolkit.Maui.Views;
using RummyBooky.ViewModels;

namespace RummyBooky.Pages
{
    public abstract class BasePopupPage<TViewModel> : Popup where TViewModel : BasePopupViewModel
    {
        protected BasePopupPage(TViewModel viewModel)
        {
            BindingContext = viewModel;
            CanBeDismissedByTappingOutsideOfPopup = true;
        }
    }
}
