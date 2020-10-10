using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RedDotServer.Models;


namespace RedDotServer
{
  class RedDotTcpServer
  {
    private readonly List<ClientInfo> _clients = new List<ClientInfo>();
    private TcpListener _tcpListener = null;

    private readonly object _timerLock = new object();

    private RedDotGameSession _gameSession = new RedDotGameSession();

    private Timer _dotFlushTimer;
    private Timer _countdownTimer;
    private Timer _spawnPointTimer;
    private Timer _acceptClientsTimer;

    private void AcceptClient()
    {
      if (!_tcpListener.Pending()) return;
      {
        var client = _tcpListener.AcceptTcpClient();
        Console.WriteLine($"Accepting client: {client.Client.RemoteEndPoint}");
        _clients.Add(new ClientInfo
        {
          TcpClient = client
        });
      }
    }

    private void ListenClients()
    {
      _clients
        .Where(client => client.TcpClient.Available > 0)
        .ToList()
        .ForEach(clientInfo => ProcessCommand(clientInfo, clientInfo.TcpClient.AcceptJsonBinaryObject<InputCommand>()));
    }

    private void WriteAll<T>(T obj)
    {
      if (!_clients.Any()) return;

      var toDisconnect = new List<ClientInfo>();
      _clients.ForEach(client =>
      {
        try
        {
          client.TcpClient.WriteJsonBinaryObject(obj);
        }
        catch
        {
          Console.WriteLine($"Disconnecting: {client.TcpClient.Client.RemoteEndPoint} - {client.Name}");
          toDisconnect.Add(client);
        }
      });

      _clients.RemoveAll(client => toDisconnect.Contains(client));
      toDisconnect.ForEach(client => client.TcpClient.Dispose());
      if (!_clients.Any())
      {
        GameOver();
        return;
      }

      if (toDisconnect.Any())
      {
        SendScoreboard();
        SendRoomMembers();
      }

    }

    private void ProcessCommand(ClientInfo clientInfo, InputCommand command)
    {
      switch (command.Action)
      {
        case "Register":
          Register(clientInfo, command.Payload.GetString());
          break;
        case "IncrementMatches":
          _gameSession.MatchCount += command.Payload.GetInt32();
          SendMatchesCount();
          break;
        case "IncrementMatchTime":
          _gameSession.MatchDuration += command.Payload.GetInt32();
          SendMatchTime();
          break;
        case "Disconnect":
          Disconnect(clientInfo);
          break;
        case "StartGame":
          StartGame();
          break;
        case "TouchDot":
          TouchDot(clientInfo, command.Payload.GetInt64());
          break;
        default:
          throw new Exception("Unknown command");
      }
    }

    private void Register(ClientInfo clientInfo, string stringArg)
    {
      clientInfo.Name = stringArg;
      Console.WriteLine($"Registering {clientInfo.TcpClient.Client.RemoteEndPoint} - {clientInfo.Name}");
      SendRoomMembers();
      SendMatchTime();
      SendMatchesCount();
    }

    private void StartGame()
    {
      _acceptClientsTimer.Change(Timeout.Infinite, Timeout.Infinite);

      WriteAll(new OutputCommand { Action = "OpenGame" });
      _gameSession.CommitSettings();
      _gameSession.StartMatch();

      SendGameTimeLeft();
      SendMatchesLeft();
      SendScoreboard();

      _countdownTimer.Change(0, 1000);
      _dotFlushTimer.Change(0, Constants.DOT_FLUSH_DELAY);
      _spawnPointTimer.Change(0, _gameSession.ComputePointSpawnDelay());
    }

    private void GameOver()
    {
      _countdownTimer.Change(Timeout.Infinite, Timeout.Infinite);
      _dotFlushTimer.Change(Timeout.Infinite, Timeout.Infinite);
      _spawnPointTimer.Change(Timeout.Infinite, Timeout.Infinite);

      WriteAll(new OutputCommand { Action = "GameOver" });

      _clients.ForEach(cl => cl.TcpClient.Dispose());
      _clients.Clear();
      _gameSession = new RedDotGameSession();
      _acceptClientsTimer.Change(0, Constants.TCP_CLIENT_ACCEPT_DELAY);

      Console.WriteLine("GameOver");
    }

    private void TouchDot(ClientInfo client, long id)
    {
      var reward = _gameSession.RewardPoint(id);
      if (reward != 0)
      {
        client.Score += reward;
        SendScoreboard();
      }
      SendBoard();
    }

    private void SendBoard()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateBoard",
        Payload = _gameSession.GameField
      });
    }

    private void SendScoreboard()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateScoreboard",
        Payload = _clients.Any() ?
          _clients
            .OrderByDescending(x => x.Score)
            .Select(x => $"{x.Name} - {x.Score}")
            .Aggregate((acc, x) => $"{acc}\n{x}") :
            ""
      });
    }

    private void SendGameTimeLeft()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateTimerCountdown",
        Payload = _gameSession.MatchTimeLeft
      });
    }

    private void SendMatchesLeft()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateGameCountdown",
        Payload = _gameSession.MatchesLeft
      });
    }

    private void SendRoomMembers()
    {
      WriteAll(new OutputCommand
      {
        Action = "RefreshRoomMembers",
        Payload = _clients.Any() ? _clients.Select(client => client.Name).Aggregate((acc, x) => $"{acc}\n{x}") : ""
      });
    }

    private void SendMatchesCount()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateMatchesCount",
        Payload = _gameSession.MatchCount
      });
    }

    private void SendMatchTime()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateMatchTime",
        Payload = _gameSession.MatchDuration
      });
    }

    private void Disconnect(ClientInfo clientInfo)
    {
      _clients.Remove(clientInfo);
      SendRoomMembers();
    }

    public void Start()
    {
      _tcpListener = new TcpListener(IPAddress.Any, Constants.SERVER_PORT);
      _tcpListener.Start();

      Console.WriteLine($"Server started at {_tcpListener.Server.LocalEndPoint}");

      var listenClientsTimer = new Timer(s =>
      {
        lock (_timerLock) { ListenClients(); }
      }, null, 0, Constants.TCP_CLIENT_LISTEN_DELAY);
      _acceptClientsTimer = new Timer(s =>
      {
        lock (_timerLock) { AcceptClient(); }
      }, null, 0, Constants.TCP_CLIENT_ACCEPT_DELAY);
      _dotFlushTimer = new Timer(s =>
      {
        lock (_timerLock) { FlushPoints(); }
      });
      _spawnPointTimer = new Timer(s =>
      {
        lock (_timerLock) { SpawnPoint(); }
      });
      _countdownTimer = new Timer(s =>
      {
        lock (_timerLock) { GameCountdown(); }
      });

      Thread.Sleep(Timeout.Infinite);
    }

    private void GameCountdown()
    {
      var isPositive = _gameSession.DecrementTime();
      if (!isPositive)
      {
        if (_gameSession.StartMatch())
        {
          _spawnPointTimer.Change(0, _gameSession.ComputePointSpawnDelay());
          SendMatchesLeft();
          SendBoard();
        }
        else
        {
          GameOver();
          return;
        }
      }

      SendGameTimeLeft();
    }

    private void FlushPoints()
    {
      if (_gameSession.DeleteOldPoints())
      {
        SendBoard();
      }
    }

    private void SpawnPoint()
    {
      if (_gameSession.SpawnPoint())
      {
        SendBoard();
      }
    }

    ~RedDotTcpServer()
    {
      _tcpListener.Stop();
    }

  }
}
