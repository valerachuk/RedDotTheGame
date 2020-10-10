using System;

namespace RedDotServer
{
  public class Dot
  {
    public bool IsRed { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public long ID { get; set; }

    public Dot()
    {
      ID = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
    }
  }
}
