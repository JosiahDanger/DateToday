using DateToday.Models;
using System.Threading.Tasks;

namespace DateToday.Services;

internal interface IAlertService
{
	Task ShowAlertAsync(AlertFlavour flavour, string message);
}
