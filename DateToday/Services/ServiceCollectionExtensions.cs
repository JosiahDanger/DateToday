using DateToday.Avalonia.ViewModels;
using DateToday.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DateToday.Services;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCommonServices(
		this IServiceCollection collection, WidgetModel initialWidgetModel)
	{
		collection.AddSingleton(initialWidgetModel);
		collection.AddTransient<WidgetViewModel>();

		return collection;
	}
}
