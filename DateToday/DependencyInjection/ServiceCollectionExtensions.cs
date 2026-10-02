using Avalonia.Controls;
using DateToday.Avalonia.PresentationServices;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.DomainServices;
using DateToday.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DateToday.DependencyInjection;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDomainServices(
		this IServiceCollection collection, WidgetModel widgetModel)
	{
		collection.AddSingleton(widgetModel);

		collection.AddSingleton<IWidgetModelSnapshot>(sp => sp.GetRequiredService<WidgetModel>());

		collection.AddSingleton<WidgetModelMutationService>();

		return collection;
	}

	public static IServiceCollection AddPresentationServices(
		this IServiceCollection collection, IResourceHost application)
	{
		collection.AddSingleton<WidgetViewModel>();

		collection.AddSingleton<IPositionController>(
			serviceProvider => serviceProvider.GetRequiredService<WidgetViewModel>());

		collection.AddSingleton<WidgetView>();

		collection.AddSingleton<IParentScreenWorkingAreaProvider>(
			serviceProvider => serviceProvider.GetRequiredService<WidgetView>());

		collection.AddSingleton<Lazy<IParentScreenWorkingAreaProvider>>(
			sp => new Lazy<IParentScreenWorkingAreaProvider>(
				() => sp.GetRequiredService<WidgetView>()));

		collection.AddSingleton<WidgetDialogService>();

		collection.AddSingleton(new GeometryProvider(application));

		return collection;
	}
}
