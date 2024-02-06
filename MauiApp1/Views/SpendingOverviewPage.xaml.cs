using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class SpendingOverviewPage : ContentPage
{
	private readonly SpendingOverviewViewModel _viewModel;
	public SpendingOverviewPage(SpendingOverviewViewModel vm)
	{
		_viewModel = vm;
		BindingContext = vm;
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		_viewModel.OnAppearing();
    }

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_viewModel.OnDisappearing();
	}
}