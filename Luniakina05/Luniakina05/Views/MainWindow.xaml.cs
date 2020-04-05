using System.ComponentModel;
using Luniakina05.Managers;
using Luniakina05.ViewModels;

namespace Luniakina05
{

    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ProcessViewModel();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }
    }
}
