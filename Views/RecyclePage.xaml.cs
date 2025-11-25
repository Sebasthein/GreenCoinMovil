using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class RecyclePage : ContentPage
{
	public RecyclePage(RecyclingViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}