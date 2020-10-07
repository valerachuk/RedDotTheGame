using System.Text.Json;

namespace RedDotServer.Models
{
  public class InputCommand
  {
    public string Action { get; set; }
    public JsonElement Payload { get; set; }
  }
}
