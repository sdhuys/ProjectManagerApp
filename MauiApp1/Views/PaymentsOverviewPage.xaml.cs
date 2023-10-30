using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class PaymentsOverviewPage : ContentPage
{
	PaymentsOverviewViewModel viewModel;
	public PaymentsOverviewPage(PaymentsOverviewViewModel vm)
	{
		BindingContext = vm;
		viewModel = vm;
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		viewModel.OnPageAppearing();
    }
}