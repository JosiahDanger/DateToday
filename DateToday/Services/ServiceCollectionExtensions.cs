using DateToday.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DateToday.Services;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCommonServices(
		this IServiceCollection collection, WidgetModel activeWidgetModel)
	{
		collection.AddSingleton(activeWidgetModel);

		return collection;
	}
}
