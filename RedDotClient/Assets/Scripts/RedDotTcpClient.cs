using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RedDotTcpClient : MonoBehaviour
{
  [SerializeField] private Text _statusText = null;
  [SerializeField] private Text _matchesCount = null;
  [SerializeField] private Text _matchTime = null;
  [SerializeField] private InputField _ipInput = null;
  [SerializeField] private GameObject _connectButton = null;
  [SerializeField] private GameObject _connectedButtonGroup = null;
  private TcpClient _tcpClient = null;

  private void WriteServer(OutputCommand command)
  {
    try
    {
      _tcpClient.WriteJsonBinaryObject(command);
    }
    catch (Exception e)
    {
      Debug.LogError(e);
    }
  }

  public void Connect()
  {
    _tcpClient = new TcpClient();
    try
    {
      _tcpClient.Connect(IPAddress.Parse(_ipInput.text), Constants.SERVER_PORT);
      _connectedButtonGroup.SetActive(true);
      _connectButton.SetActive(false);
      WriteServer(new OutputCommand
      {
        Action = "Register",
        Payload = NameEditor.Name
      });
    }
    catch (Exception e)
    {
      _statusText.text = "Error, try again";
      Debug.Log(e);
    }
  }

  public void Disconnect()
  {
    WriteServer(new OutputCommand
    {
      Action = "Disconnect"
    });
    _tcpClient.Dispose();
    _tcpClient = null;
    _connectedButtonGroup.SetActive(false);
    _connectButton.SetActive(true);
    _statusText.text = "Disconnected";
  }

  public void IncrementMatchTime(int amount)
  {
    WriteServer(new OutputCommand
    {
      Action = "IncrementMatchTime",
      Payload = amount
    });
  }

  public void IncrementMatches(int amount)
  {
    WriteServer(new OutputCommand
    {
      Action = "IncrementMatches",
      Payload = amount
    });
  }

  private void Update()
  {
    while (_tcpClient != null && _tcpClient.Available > 0)
    {
      var command = _tcpClient.AcceptJsonBinaryObject<InputCommand>();
      ProcessCommand(command);
    }
  }

  private void ProcessCommand(InputCommand command)
  {
    switch (command.Action)
    {
      case "RefreshRoomMembers":
        _statusText.text = command.Payload.GetString();
        break;
      case "UpdateMatchesCount":
        _matchesCount.text = command.Payload.GetInt32().ToString();
        break;
      case "UpdateMatchTime":
        _matchTime.text = command.Payload.GetInt32().ToString();
        break;
      default:
        Debug.LogError("Invalid command");
        break;
    }
  }

}
