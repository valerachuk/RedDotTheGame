using System;

namespace RedDotServer.Models
{
  public class Dot
  {
    public bool IsRed { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public long Id { get; }

    public Dot()
    {
      Id = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
    }
  }
}
