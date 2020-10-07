using System.Text.Json;

public class InputCommand
{
  public string Action { get; set; }
  public JsonElement Payload { get; set; }
}
