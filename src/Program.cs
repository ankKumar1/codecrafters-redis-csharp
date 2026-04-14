using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

var client = server.AcceptSocket();

var stream = new NetworkStream(client);

string response = "+PONG\r\n";
byte[] responseBytes = Encoding.UTF8.GetBytes(response);

stream.Write(responseBytes, 0, responseBytes.Length);

client.Close();



