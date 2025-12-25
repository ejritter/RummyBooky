namespace RummyBooky;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(NewGamePage), typeof(NewGamePage));
        Routing.RegisterRoute(nameof(CurrentGamePage), typeof(CurrentGamePage));
        Routing.RegisterRoute(nameof(LeaderboardPage), typeof(LeaderboardPage));

    }
}
