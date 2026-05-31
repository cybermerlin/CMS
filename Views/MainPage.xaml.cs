using CMS.Models;
using CMS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.Controls;
using System;

namespace CMS.Views;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage() => InitializeComponent();


    public void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}
