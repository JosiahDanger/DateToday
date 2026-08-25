using Avalonia.Controls;
using DateToday.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DateToday.Services;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDomainServices(
		this IServiceCollection collection, WidgetModel activeWidgetModel)
	{
		collection.AddSingleton(activeWidgetModel);

		return collection;
	}

	public static IServiceCollection AddPresentationServices(
		this IServiceCollection collection, Window parentWindow)
	{
		collection.AddSingleton(new Lazy<Window>(() => parentWindow));
		collection.AddTransient<AlertService>();

		return collection;
	}
}
