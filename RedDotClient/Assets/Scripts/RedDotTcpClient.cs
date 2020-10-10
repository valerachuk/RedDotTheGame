using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RedDotTcpClient : MonoBehaviour
{
  [SerializeField] private Text _statusText = null;
  [SerializeField] private Text _matchesCount = null;
  [SerializeField] private Text _matchTime = null;
  [SerializeField] private Text _scoreboard = null;
  [SerializeField] private Text _totalScoreboard = null;
  [SerializeField] private Text _timerCountdown = null;
  [SerializeField] private Text _matchesCountdown = null;
  [SerializeField] private InputField _ipInput = null;
  [SerializeField] private GameObject _connectButton = null;
  [SerializeField] private GameObject _connectedButtonGroup = null;
  [SerializeField] private GameObject _dotPrefab = null;
  [SerializeField] private RectTransform _gameField = null;
  [SerializeField] private UnityEvent _openGame = null;
  [SerializeField] private UnityEvent _onGameOver = null;

  public static RedDotTcpClient Instance { get; set; }
  
  private TcpClient _tcpClient = null;

  private void Start()
  {
    Instance = this;
  }

  private void WriteServer(object command)
  {
    try
    {
      _tcpClient.WriteJsonBinaryObject(command);
    }
    catch (Exception e)
    {
      Debug.LogError(e);
      SceneManager.LoadScene(0);
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

  public void StartGame()
  {
    WriteServer(new OutputCommand
    {
      Action = "StartGame"
    });
  }

  public void TouchDot(long id)
  {
    WriteServer(new OutputCommand
    {
      Action = "TouchDot",
      Payload = id
    });
  }

  private void Update()
  {
    if (_tcpClient != null && !_tcpClient.Connected)
    {
      _tcpClient.Dispose();
      _tcpClient = null;
      SceneManager.LoadScene(0);
    }

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
      case "OpenGame":
        _openGame?.Invoke();
        break;
      case "UpdateScoreboard":
        _scoreboard.text = command.Payload.GetString();
        break;
      case "UpdateTimerCountdown":
        _timerCountdown.text = command.Payload.GetInt32().ToString();
        break;
      case "UpdateGameCountdown":
        _matchesCountdown.text = command.Payload.GetInt32().ToString();
        break;
      case "GameOver":
        _totalScoreboard.text = _scoreboard.text;
        _tcpClient.Dispose();
        _tcpClient = null;
        _onGameOver?.Invoke();
        break;
      case "UpdateBoard":
        FillBoard(command.Payload.EnumerateArray().Select(je => new Dot
        {
          ID = je.GetProperty("ID").GetInt64(),
          IsRed = je.GetProperty("IsRed").GetBoolean(),
          X = je.GetProperty("X").GetSingle(),
          Y = je.GetProperty("Y").GetSingle(),
        }));    
        break;
      default:
        Debug.LogError($"Unknown command - {command.Action}");
        break;
    }
  }

  private void FillBoard(IEnumerable<Dot> dots)
  {
    foreach (Transform dot in _gameField.transform)
    {
      Destroy(dot.gameObject);
    }

    foreach (var dot in dots)
    {
      var spawned = Instantiate(_dotPrefab, _gameField);

      spawned.GetComponent<RectTransform>().localPosition = new Vector2(dot.X, dot.Y) * _gameField.rect.size;

      Debug.Log($"POs: {spawned.GetComponent<RectTransform>().position}");

      var dotController = spawned.GetComponent<DotController>();
      dotController.IsRed = dot.IsRed;
      dotController.ID = dot.ID;
    }
  }

}
