namespace RedDotServer
{
  internal class Program
  {
    private static void Main()
    {
      var server = new RedDotTcpServer();
      server.Start();
    }
  }
}
