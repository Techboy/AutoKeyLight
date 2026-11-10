using System.Net;
using System.Net.Sockets;
using CamOnKeyLight.Core;

namespace CamOnKeyLight
{
    internal readonly record struct DiscoveryResult(IReadOnlyList<DiscoveredLight> Lights, string? Error);

    internal static class ElgatoBrowser
    {
        public static async Task<DiscoveryResult> SearchAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            List<byte[]> packets = new List<byte[]>();
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(timeout);
            List<UdpClient> clients = new List<UdpClient>();
            string? error = null;
            try
            {
                clients.Add(NewSender());
                try
                {
                    clients.Add(NewMulticastListener());
                }
                catch (SocketException)
                {
                }

                byte[] query = MdnsCodec.CreateQuery(0, unicastResponse: false);
                byte[] direct = MdnsCodec.CreateQuery(0, unicastResponse: true);
                IPEndPoint target = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
                bool sent = false;
                foreach (UdpClient client in clients)
                {
                    try
                    {
                        await client.SendAsync(query, target, linked.Token);
                        await client.SendAsync(direct, target, linked.Token);
                        sent = true;
                    }
                    catch (SocketException)
                    {
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                if (!sent)
                    error = "Couldn't search the network.";

                await Task.WhenAll(clients.Select(client => ReadUntil(client, packets, linked.Token)));
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
                error ??= "Couldn't search the network.";
            }
            finally
            {
                foreach (UdpClient client in clients)
                    client.Dispose();
            }

            IReadOnlyList<DiscoveredLight> lights = ElgatoDiscovery.FromPackets(packets);
            return new DiscoveryResult(lights, lights.Count == 0 ? error : null);
        }

        private static UdpClient NewSender()
        {
            UdpClient client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            return client;
        }

        private static UdpClient NewMulticastListener()
        {
            UdpClient client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 5353));
            client.JoinMulticastGroup(IPAddress.Parse("224.0.0.251"));
            return client;
        }

        private static async Task ReadUntil(UdpClient client, List<byte[]> packets, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    UdpReceiveResult result = await client.ReceiveAsync(cancellationToken);
                    if (result.Buffer.Length == 0)
                        continue;

                    lock (packets)
                        packets.Add(result.Buffer);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
        }
    }
}
