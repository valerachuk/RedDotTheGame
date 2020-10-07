using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UnityEngine;

public static class Utils
{

  public static T AcceptJsonBinaryObject<T>(this TcpClient client)
  {
    var networkStream = client.GetStream();
    int packageSize = 0;

    using (var memoryStream = new MemoryStream())
    {
      var buffer = new byte[4];
      var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
      memoryStream.Write(buffer, 0, bytesRead);
      packageSize = BitConverter.ToInt32(memoryStream.ToArray(), 0);
    }

    using (var memoryStream = new MemoryStream())
    {
      var buffer = new byte[packageSize];
      var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
      memoryStream.Write(buffer, 0, bytesRead);

      var bytes = memoryStream.ToArray();
      var text = Encoding.ASCII.GetString(bytes);
      Debug.Log($"Accepting {text}");
      return JsonSerializer.Deserialize<T>(text);
    }

  }

  public static void WriteJsonBinaryObject<T>(this TcpClient client, T obj)
  {
    var text = JsonSerializer.Serialize(obj);
    Debug.Log($"Sending {text} to {client.Client.RemoteEndPoint}");
    var bytes = Encoding.ASCII.GetBytes(text);
    var bytesWithMeta = BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray();

    var stream = client.GetStream();
    stream.WriteAsync(bytesWithMeta, 0, bytesWithMeta.Length);
  }

}
