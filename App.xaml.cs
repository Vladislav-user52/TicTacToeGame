using Microsoft.Maui.Controls;

namespace TicTacToeGame
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
        
            MainPage = new NavigationPage(new UnifiedMainPage());
        }
    }
}