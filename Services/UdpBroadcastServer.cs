using System.Net;
using System.Net.Sockets;
using System.Text;
using BookHeaven.Domain.Constants;

namespace BookHeaven.Server.Services;

public class UdpBroadcastServer(ILogger<UdpBroadcastServer> logger) : BackgroundService
{
    private readonly int _maxRetries = 5;
    private readonly string _message = $"{Broadcast.SERVER_URL_MESSAGE_PREFIX}{Environment.GetEnvironmentVariable("SERVER_URL")}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (Environment.GetEnvironmentVariable("SERVER_URL") == null)
        {
            logger.LogWarning("SERVER_URL environment variable is not set, can't start broadcast.");
            return;
        }
        
        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Broadcast.BROADCAST_PORT));
        
        while (true)
        {
            logger.LogInformation("Waiting for clients...");
            var result = await udpClient.ReceiveAsync(stoppingToken);
            var receivedMessage = Encoding.UTF8.GetString(result.Buffer);

            if (!receivedMessage.StartsWith(Broadcast.DISCOVER_MESSAGE_PREFIX))
            {
                await Task.Delay(2000);
                continue;
            }
            
            var clientIp = receivedMessage[Broadcast.DISCOVER_MESSAGE_PREFIX.Length..];
            
            logger.LogInformation($"Received discover broadcast from {clientIp}");
            var broadcastAddress = new IPEndPoint(IPAddress.Parse(clientIp), Broadcast.BROADCAST_PORT);
            var messageBytes = Encoding.UTF8.GetBytes(_message);
            
            logger.LogInformation($"Broadcasting message {_message} to {broadcastAddress.Address}");
            var acknowledged = false;
            var retries = 0;
            while (!acknowledged)
            {
                await udpClient.SendAsync(messageBytes, messageBytes.Length, broadcastAddress);
                
                var task = udpClient.ReceiveAsync();
                
                var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
                if (completedTask != task)
                {
                    logger.LogInformation($"Client {broadcastAddress.Address} hasn't responded, resending broadcast...");
                    if (++retries >= _maxRetries)
                    {
                        logger.LogInformation($"Client {broadcastAddress.Address} hasn't responded after {_maxRetries} retries, stopping broadcast.");
                        break;
                    }
                    continue;
                }
                var ackResult = task.Result;
                var ackMessage = Encoding.UTF8.GetString(ackResult.Buffer);
                if (ackMessage == Broadcast.ACK_MESSAGE)
                {
                    logger.LogInformation($"Received ACK from {broadcastAddress.Address}");
                    acknowledged = true;
                }
            }
        }
    }
}