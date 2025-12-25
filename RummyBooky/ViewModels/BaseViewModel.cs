

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
    public virtual async Task<PopupResultsModel> ShowPopupAsync(string title, string message, bool isDismissable = true, List<PlayerModel>? players = null, GameStatus? gameStatus = GameStatus.Unknown)
    {
        var queryAttributes = new Dictionary<string, object>
        {
            [nameof(BasePopupViewModel.Title)] = title,
            [nameof(BasePopupViewModel.Message)] = message
        };
    if (players != null)
        queryAttributes["players"] = players;
    if (gameStatus != null)
        queryAttributes["gameStatus"] = gameStatus;
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
                 results is IPopupResult<PopupResultsModel> userResults)
        {
            return userResults.Result;
        }
        else
        {
            return new PopupResultsModel();
        }
    }
}
