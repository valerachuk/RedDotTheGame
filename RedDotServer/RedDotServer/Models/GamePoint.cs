using System;

namespace RedDotServer
{
  [Serializable]
  public class GamePoint
  {
    public bool IsRed { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public long ID { get; private set; }

    public GamePoint()
    {
      ID = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
    }
  }
}
