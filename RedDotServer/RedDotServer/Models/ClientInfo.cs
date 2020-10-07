using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RedDotServer
{
  public class ClientInfo
  {
    public TcpClient TcpClient { get; set; }
    public string Name { get; set; }
    public int Score { get; set; }
    public int TotalScore { get; set; }
  }
}
