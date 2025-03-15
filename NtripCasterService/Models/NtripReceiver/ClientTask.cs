using System.Net.Sockets;
using System.Text;

namespace NtripCasterService.Models.NtripReceiver
{
	internal class ClientTask
	{
		/// <summary>
		/// TCP client for the connection
		/// </summary>
		readonly TcpClient _client;

		/// <summary>
		/// Remove IP and port of the client
		/// </summary>
		readonly string RemoteEP;

		/// <summary>
		/// Current build state of the NTRIP message
		/// </summary>
		enum NtripState
		{
			WaitingForRequest,
			WaitingForAuthorization,
			WaitingForData
		}
		NtripState _state = NtripState.WaitingForAuthorization;

		/// <summary>
		/// Build the auth packet here
		/// </summary>
		readonly StringBuilder _authBuilder = new();

		internal ClientTask(TcpClient client)
		{
			_client = client;
			RemoteEP = _client.Client.RemoteEndPoint!.ToString() ?? "null";
		}

		/// <summary>
		/// Task to handle client communications. Runs forever or until the client disconnects.
		/// </summary>
		internal async Task HandleClientAsync(CancellationToken cancellationToken)
		{
			using (_client)
			{
				try
				{
					var stream = _client.GetStream();

					var buffer = new byte[1024];

					// Timeout is only used until we have credentials
					var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
					Log.Ln($"{RemoteEP} - Waiting for client data...");

					// Loop until the client disconnects or auth fails / data is invalid
					while (!cancellationToken.IsCancellationRequested)
					{
						var readTask = stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
						Task completedTask;
						if (_state == NtripState.WaitingForData)
							completedTask = await Task.WhenAny(readTask);
						else
							completedTask = await Task.WhenAny(readTask, timeoutTask);

						if (completedTask == timeoutTask)
						{
							Log.Ln($"{RemoteEP} - Timeout waiting for client data.");
							break;
						}

						if (!await ProcessClientDataAsync(buffer, readTask))
							break;
					}
					_client.Close();
				}
				catch (Exception ex)
				{
					Log.Ln($"{RemoteEP} Error handling client: {ex}");
				}
			}
		}
		int _totalBytes = 0;

		/// <summary>
		/// Rewad the inbound data and process according to type
		/// </summary>
		/// <returns>True if all is well</returns>
		async Task<bool> ProcessClientDataAsync(byte[] buffer, Task<int> readTask)
		{
			var bytesRead = await readTask;

			if (_state == NtripState.WaitingForAuthorization)
			{
				var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
				Log.Ln($"Received {bytesRead} bytes from client: {receivedData}");
				if (!ProcessAuthorization(receivedData))
					return false;
			}
			else
			{
				// Handle data
				//var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
				_totalBytes+= bytesRead;
				Console.Write($"\rReceived {bytesRead,4:N0} {_totalBytes,10:N0}   ");
			}
			return true;
		}

		/// <summary>
		/// Check for the autherisation data and process it
		///		SOURCE {_sPassword} {_sCredential}\r\n
		///		Source-Agent: NTRIP UM98/ESP32_T_Display_SX\r\n
		///		STR: \r\n
		///		\r\n
		/// </summary>
		bool ProcessAuthorization(string receivedData)
		{
			_authBuilder.Append(receivedData);
			if (_authBuilder.ToString().Contains("\r\n\r\n"))
			{
				Log.Ln($"{RemoteEP} - Authorization complete.");
				_state = NtripState.WaitingForData;

				Log.Ln($"{RemoteEP} - Authorization data: {_authBuilder}");

				// TODO: Parse the authorization data and return false if invalid
				_state = NtripState.WaitingForData;
			}
			if (_authBuilder.Length > 1000)
			{
				Log.Ln($"{RemoteEP} - Authorization data too long.");
				_authBuilder.Clear();
				return false;
			}
			return true;
		}
	}
}
