using codecrafters_redis.src;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

Console.WriteLine("Server started...");

while (true)
{
    Socket client = server.AcceptSocket();
    Console.WriteLine("Client connected");
    Task.Run(() => HandleClient(client));
}

void HandleClient(Socket client)
{
    try
    {
        while (true)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = client.Receive(buffer);

            if (bytesRead == 0)
            {
                Console.WriteLine("Client disconnected");
                break;
            }

            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            string[] command = RespParser.ParseRESP(request);

            if(command.Length == 0)
            {
                return;
            }

            if (command[0].ToLower() == "echo")
            {
                string message = command[1];

                string response = $"${message.Length}\r\n{message}\r\n";
                client.Send(Encoding.UTF8.GetBytes(response));
            }
            else
            {
                byte[] response = Encoding.UTF8.GetBytes("+PONG\r\n");
                client.Send(response);
            }
            
        }
    }
    catch
    {
        Console.WriteLine("Connection error");
    }
    finally
    {
        client.Close();
    }
}

