using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Darp.MvvmValidator;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.DomainServices;
using DateToday.Models;
using System;
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

		DateTimeCulture = _widgetModelSnapshot.Content.DateTimeCulture;
		IsOrdinalDaySuffixEnabled = _widgetModelSnapshot.Content.IsOrdinalDaySuffixEnabled;
		RefreshIntervalSeconds = _widgetModelSnapshot.Content.RefreshIntervalSeconds;

		/* Initialisation of DateTimeFormat will call ValidateDateTimeFormat(…), which in turn
		 * requires DateTimeCulture to be initialised. Therefore, DateTimeFormat must be initialised
		 * after DateTimeCulture. */

		DateTimeFormat = _widgetModelSnapshot.Content.DateTimeFormat;

		_isPropertyInitialisationComplete = true;
	}

	[ObservableProperty]
	[Required(
		ErrorMessageResourceType =
			typeof(Strings),
		ErrorMessageResourceName =
			nameof(Strings.ContentConfigViewModel_Validation_DateTimeFormat_Empty))]
	[CustomValidation(typeof(ContentConfigViewModel), nameof(ValidateDateTimeFormat))]
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

	public static ValidationResult? ValidateDateTimeFormat(
		string dateTimeFormat, ValidationContext context)
	{
		if (context.ObjectInstance is not ContentConfigViewModel contentConfigViewModel)
		{
			throw new InvalidOperationException(
				Strings.ContentConfigViewModel_Exception_UnsupportedValidationContext);
		}

		CultureInfo dateTimeCulture =
			contentConfigViewModel.DateTimeCulture
			?? throw new InvalidOperationException(
				Strings.ContentConfigViewModel_Exception_DateTimeCulture_Null);

		try
		{
			string dateTimeText =
				DateTimeTextFormatter.FormatOrThrow(dateTimeFormat, dateTimeCulture);

			/* Yes, this method is used for data validation. But that's no excuse to throw away a
			 * perfectly good DateTimeText value. Therefore, the new value is sent off via the
			 * WeakReferenceMessenger, for use by the WidgetViewModel. */

			WeakReferenceMessenger.Default.Send(new DateTimeTextChangedMessage(dateTimeText));
		}
		catch (ValidationException ex)
		{
			return new ValidationResult(ex.Message, [nameof(DateTimeFormat)]);
		}

		return ValidationResult.Success;
	}
}
