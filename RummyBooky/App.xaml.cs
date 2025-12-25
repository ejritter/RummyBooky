namespace RummyBooky;

public partial class App : Application
{
    public App(IAppAudioService appAudioService)
    {
        InitializeComponent();
        _appAudioService = appAudioService;
        
    }
    private readonly bool _debugging = true;
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }


       protected override async void OnStart()
    {
        // Start background soundtrack once. It keeps playing across navigation.
        if(_debugging == false)
            await _appAudioService.StartAsync();
        base.OnStart();
    }

    protected override void OnSleep()
    {
        // Optional: pause on sleep to be respectful.
        if(_debugging == false)
            _appAudioService.Pause();
        base.OnSleep();
    }

    protected override void OnResume()
    {
        // Optional: resume when app returns.
        if(_debugging == false)
            _appAudioService.Resume();
        base.OnResume();
    }
    private readonly IAppAudioService _appAudioService;
}