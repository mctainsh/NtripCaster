using NtripCasterService.Models;
using NtripCasterService.Models.NtripReceiver;

namespace NtripCasterService;

class Program
{
	static async Task Main()
	{
		// See https://aka.ms/new-console-template for more information
		Log.Ln("Hello, World!");

		try
		{
			var ct = new CancellationToken();
			var listener = new Listener(2101);

			// Wait here until cancelled
			await listener.StartListeningAsync(ct);

			Log.Ln("Goodbye, World!");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			Log.Ln(ex.ToString());
		}
	}
}
