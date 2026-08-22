using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using RummyBooky.Extensions;
using RummyBooky.ViewModels;

namespace RummyBooky.Pages;

public partial class EditPlayerPage : BasePage<EditPlayerViewModel>, IQueryAttributable
{
	public EditPlayerPage(EditPlayerViewModel vm) : base(vm)
	{
		InitializeComponent();
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		ViewModel.ApplyQueryAttributes(query);
	}

	private async void Page_Loaded(object? sender, EventArgs e)
	{
		if (BindingContext is EditPlayerViewModel vm)
		{
			await vm.PageLoaded();
		}
	}

	private async void OnUpdatePlayerNameClicked(object? sender, EventArgs e)
	{
		if (sender is Microsoft.Maui.Controls.VisualElement element)
		{
			await element.AnimatePressAsync();
		}
	}

	private async void OnRemovePlayerClicked(object? sender, EventArgs e)
	{
		if (sender is Microsoft.Maui.Controls.VisualElement element)
		{
			await element.AnimatePressAsync();
		}
	}
}