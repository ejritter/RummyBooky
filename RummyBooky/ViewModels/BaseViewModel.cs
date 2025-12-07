

namespace RummyBooky.ViewModels;

public abstract class BaseViewModel(IPopupService popupService, GameService gameService) : ObservableObject
{


    protected readonly IPopupService _popupService = popupService;
    protected readonly GameService _gameService = gameService;
    protected static AppTheme CurrentTheme => Application.Current?.RequestedTheme switch
    {
        AppTheme.Light => AppTheme.Light,
        AppTheme.Dark => AppTheme.Dark,
        _ => AppTheme.Dark
    };
    public virtual async Task<bool> ShowPopupAsync(string title, string message, bool isDismissable = true, List<PlayerModel>? players = null)
    {
        var queryAttributes = new Dictionary<string, object>
        {
            [nameof(BasePopupViewModel.Title)] = title,
            [nameof(BasePopupViewModel.Message)] = message
        };

        var results = await _popupService
                                .ShowPopupAsync<GeneralPopupViewModel>
                                   (shell: Shell.Current,
                                    options: new PopupOptions
                                    {
                                        CanBeDismissedByTappingOutsideOfPopup = isDismissable,
                                        PageOverlayColor = CurrentTheme == AppTheme.Light ? Colors.White : Colors.Black
                                    },
                                    shellParameters: queryAttributes);
        if (results is not null &&
                 results is IPopupResult<bool> userResults)
        {
            return userResults.Result;
        }
        else
        {
            return false;
        }
    }
}
