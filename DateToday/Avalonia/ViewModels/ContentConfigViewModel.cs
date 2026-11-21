using CommunityToolkit.Mvvm.ComponentModel;
using Darp.MvvmValidator;
using DateToday.Avalonia.Resources;
using DateToday.DomainServices;
using DateToday.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class ContentConfigViewModel : ValidatingObservableObject
{
	private static readonly ReadOnlyObservableCollection<CultureInfo> s_availableLocales =
		GetAvailableLocales();

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
	[Required]
	public partial string DateTimeFormat { get; set; }

	[ObservableProperty]
	[Required(
		ErrorMessageResourceType =
			typeof(Strings),
		ErrorMessageResourceName =
			nameof(Strings.ContentConfigViewModel_Validation_DateTimeCulture_Invalid))]
	public partial CultureInfo DateTimeCulture { get; set; }

	[ObservableProperty]
	public partial bool IsOrdinalDaySuffixEnabled { get; set; }

	[ObservableProperty]
	public partial uint RefreshIntervalSeconds { get; set; }

	public static ReadOnlyObservableCollection<CultureInfo> AvailableLocales => s_availableLocales;

	private static ReadOnlyObservableCollection<CultureInfo> GetAvailableLocales()
	{
		IOrderedEnumerable<CultureInfo> allLocalesOrderedEnumerable =
			CultureInfo.GetCultures(CultureTypes.AllCultures)
					   .Where(culture => culture != CultureInfo.InvariantCulture)
					   .OrderBy(culture => culture.DisplayName);

		ObservableCollection<CultureInfo> allLocalesObservableCollection =
			[with(allLocalesOrderedEnumerable)];

		return new ReadOnlyObservableCollection<CultureInfo>(allLocalesObservableCollection);
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		switch (e.PropertyName)
		{
			case nameof(DateTimeFormat) when !(GetErrors(nameof(DateTimeFormat)).Any()):

				_widgetModelMutationService.MutateContentConfig(
					content =>
						content with
						{
							DateTimeFormat = this.DateTimeFormat
						});

				break;

			case nameof(DateTimeCulture) when !(GetErrors(nameof(DateTimeCulture)).Any()):

				_widgetModelMutationService.MutateContentConfig(
					content =>
						content with
						{
							DateTimeCulture = this.DateTimeCulture
						});

				break;

			case nameof(IsOrdinalDaySuffixEnabled):

				_widgetModelMutationService.MutateContentConfig(
					content =>
						content with
						{
							IsOrdinalDaySuffixEnabled = this.IsOrdinalDaySuffixEnabled
						});

				break;

			case nameof(RefreshIntervalSeconds):

				_widgetModelMutationService.MutateContentConfig(
					content =>
						content with
						{
							RefreshIntervalSeconds = this.RefreshIntervalSeconds
						});

				break;
		}
	}
}
