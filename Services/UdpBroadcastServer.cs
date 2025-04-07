using System.Net;
using System.Net.Sockets;
using System.Text;
using BookHeaven.Domain.Constants;

namespace BookHeaven.Server.Services;

public class UdpBroadcastServer(ILogger<UdpBroadcastServer> logger)
{
    private readonly string _message = $"{Broadcast.SERVER_URL_MESSAGE_PREFIX}{Environment.GetEnvironmentVariable("SERVER_URL")}";

    public async Task StartAsync()
    {
        if (Environment.GetEnvironmentVariable("SERVER_URL") == null)
        {
            logger.LogError("SERVER_URL environment variable is not set, can't start broadcast.");
            return;
        }
        
        using var udpClient = new UdpClient
        {
            EnableBroadcast = true
        };
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Broadcast.BROADCAST_PORT));
        
        while (true)
        {
            logger.LogInformation("Waiting for clients...");
            var result = await udpClient.ReceiveAsync();
            var receivedMessage = Encoding.UTF8.GetString(result.Buffer);

            if (receivedMessage != Broadcast.DISCOVER_MESSAGE)
            {
                await Task.Delay(2000);
                continue;
            }
            
            logger.LogInformation($"Received discover broadcast from {result.RemoteEndPoint.Address}");
            var broadcastAddress = new IPEndPoint(IPAddress.Broadcast, Broadcast.BROADCAST_PORT);
            var messageBytes = Encoding.UTF8.GetBytes(_message);

            
            await udpClient.SendAsync(messageBytes, messageBytes.Length, broadcastAddress);
            logger.LogInformation($"Broadcasted message {_message}");
            
            var acknowledged = false;
            while (!acknowledged)
            {
                logger.LogInformation("Waiting for ACK...");
                var ackResult = await udpClient.ReceiveAsync();
                var ackMessage = Encoding.UTF8.GetString(ackResult.Buffer);
                if (ackMessage == Broadcast.ACK_MESSAGE)
                {
                    logger.LogInformation($"Received ACK from {ackResult.RemoteEndPoint.Address}");
                    acknowledged = true;
                }
                else
                {
                    await Task.Delay(2000);
                }
            }
        }
    }
}