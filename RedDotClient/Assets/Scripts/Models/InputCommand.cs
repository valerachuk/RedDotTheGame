using System.Collections.Generic;
using System.Text.Json;
using Assets.Scripts.Models;

public class InputCommand
{
  public string Action { get; set; }
  public JsonElement Payload { get; set; }
}
