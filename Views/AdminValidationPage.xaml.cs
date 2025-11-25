using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class AdminValidationPage : ContentPage
{
	public AdminValidationPage(AdminValidationViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}