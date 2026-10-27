using CommunityToolkit.Mvvm.ComponentModel;
using DateToday.DomainServices;
using DateToday.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class ContentConfigViewModel : ObservableObject
{
	private readonly IWidgetModelSnapshot _widgetModelSnapshot;
	private readonly WidgetModelMutationService _widgetModelMutationService;
	private readonly bool _isPropertyInitialisationComplete;

	public ContentConfigViewModel(
		IWidgetModelSnapshot widgetModelSnapshot,
		WidgetModelMutationService widgetModelMutationService)
	{
		_widgetModelSnapshot = widgetModelSnapshot;
		_widgetModelMutationService = widgetModelMutationService;

		DateTimeFormat = _widgetModelSnapshot.Content.DateTimeFormat;
		DateTimeCulture = _widgetModelSnapshot.Content.DateTimeCulture;
		IsOrdinalDaySuffixEnabled = _widgetModelSnapshot.Content.IsOrdinalDaySuffixEnabled;
		RefreshIntervalSeconds = _widgetModelSnapshot.Content.RefreshIntervalSeconds;

		_isPropertyInitialisationComplete = true;
	}

	[ObservableProperty]
	public partial string DateTimeFormat { get; set; }

	[ObservableProperty]
	public partial CultureInfo DateTimeCulture { get; set; }

	[ObservableProperty]
	public partial bool IsOrdinalDaySuffixEnabled { get; set; }

	[ObservableProperty]
	public partial uint RefreshIntervalSeconds { get; set; }

	public static ReadOnlyObservableCollection<CultureInfo> AvailableLocales =>
		GetAvailableLocales();

	private static ReadOnlyObservableCollection<CultureInfo> GetAvailableLocales()
	{
		IOrderedEnumerable<CultureInfo> allLocalesOrderedEnumerable =
			CultureInfo.GetCultures(CultureTypes.AllCultures)
					   .Where(culture => culture != CultureInfo.InvariantCulture)
					   .OrderBy(culture => culture.DisplayName);

		ObservableCollection<CultureInfo> allLocalesObservableCollection =
			new(allLocalesOrderedEnumerable);

		return new ReadOnlyObservableCollection<CultureInfo>(allLocalesObservableCollection);
	}

	partial void OnDateTimeFormatChanged(string value)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		_widgetModelMutationService.MutateContentConfig(
			content => content with { DateTimeFormat = value });
	}

	partial void OnDateTimeCultureChanged(CultureInfo value)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		_widgetModelMutationService.MutateContentConfig(
			content => content with { DateTimeCulture = value });
	}

	partial void OnIsOrdinalDaySuffixEnabledChanged(bool value)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		_widgetModelMutationService.MutateContentConfig(
			content => content with { IsOrdinalDaySuffixEnabled = value });
	}

	partial void OnRefreshIntervalSecondsChanged(uint value)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		_widgetModelMutationService.MutateContentConfig(
			content => content with { RefreshIntervalSeconds = value });
	}
}
