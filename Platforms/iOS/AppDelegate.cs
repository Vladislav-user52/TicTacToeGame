using Foundation;

namespace TicTacToeGameMauiApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => TicTacToeGame.MauiProgram.CreateMauiApp();
}