using SyncfusionIssue.ViewModel;

namespace SyncfusionIssue
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }

        protected override void OnAppearing()
        {
            if (BindingContext is MainViewModel viewModel)
            {
                viewModel.OnPageAppearing(this);
            }
        }
    }

}
