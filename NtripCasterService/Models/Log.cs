﻿namespace NtripCasterService.Models
{
	static class Log
	{
		static internal string? LogFileName { private set; get; }
		static DateTime? _day;
		static Lock _lock = new ();

		/// <summary>
		/// Log the results to the console and to the log file
		/// </summary>
		internal static void Write(string data, bool console)
		{
			if (console)
				Console.Write(data);
			lock (_lock)
			{
				try
				{
					File.AppendAllText(LogFileName!, data);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error writing to log file: " + ex);
				}
			}
		}

		/// <summary>
		/// Write a line with a timestamp to the log file
		/// </summary>
		static void WriteLine(string data, bool console)
		{
			// Make the working log folder
			const string LOG_FOLDER = "ServiceLogs";
			if (LogFileName is null)
				Directory.CreateDirectory(LOG_FOLDER);

			var now = DateTime.Now;
			if (now.Day != _day?.Day)
			{
				_day = now;
				LogFileName = $"{LOG_FOLDER}\\Log_{now:yyyyMMdd_HHmmss}.txt";
			}

			Write(now.ToString("HH:mm:ss.fff") + " > "+ data + Environment.NewLine, console);
		}
		internal static void Ln(string data) => WriteLine(data, true);
		internal static void Note(string data) => WriteLine(data, false);
	}
}
