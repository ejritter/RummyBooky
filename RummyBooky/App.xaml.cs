namespace RummyBooky;

public partial class App : Application
{
    public App(IAppAudioService appAudioService)
    {
        InitializeComponent();
        _appAudioService = appAudioService;
    }
    private readonly IAppAudioService _appAudioService;

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override async void OnStart()
    {
        // Start background soundtrack once. It keeps playing across navigation non-stop.
        await _appAudioService.StartAsync();
        base.OnStart();
    }

    protected override void OnSleep()
    {
        _appAudioService.Pause();
        base.OnSleep();
    }

    protected override void OnResume()
    {
        _appAudioService.Resume();
        base.OnResume();
    }
}