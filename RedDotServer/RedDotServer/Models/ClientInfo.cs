using System.Net.Sockets;

namespace RedDotServer.Models
{
  public class ClientInfo
  {
    public TcpClient TcpClient { get; set; }
    public string Name { get; set; }
    public int Score { get; set; }
  }
}
