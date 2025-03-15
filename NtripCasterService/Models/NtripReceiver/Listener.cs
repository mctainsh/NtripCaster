using System.Net;
using System.Net.Sockets;

namespace NtripCasterService.Models.NtripReceiver
{
	internal class Listener
	{
		readonly int _port;
		readonly TcpListener _tcpListener;
		readonly List<ClientTask> _clients = [];

		/// <summary>
		/// Construct the listener on the specified port
		/// </summary>
		internal Listener(int port)
		{
			_port = port;
			_tcpListener = new TcpListener(IPAddress.Any, _port);
			Log.Ln($"Preparing to listenon port {_port}");
		}

		/// <summary>
		/// Start listening. We block here for ever until the cancellation token is set
		/// </summary>
		internal async Task StartListeningAsync(CancellationToken cancellationToken)
		{
			_tcpListener.Start();
			Log.Ln($"Listening on port {_port}...");
			while (!cancellationToken.IsCancellationRequested)
			{
				var client = await _tcpListener.AcceptTcpClientAsync();
				var clientTask = new ClientTask(client);
				_clients.Add(clientTask);
				Log.Ln($"# {_clients.Count} > {client.Client.RemoteEndPoint} Connected ****** ");

				_ = Task.Run(async () =>
					{
						await clientTask.HandleClientAsync(cancellationToken);
						_clients.Remove(clientTask);
					}, cancellationToken);
			}
		}


		/// <summary>
		/// Shutdown and clean up all the sockets
		/// </summary>
		public void StopListening()
		{
			_tcpListener.Stop();
		}
	}
}
