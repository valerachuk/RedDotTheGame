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
    private List<GamePoint> _gameField = new List<GamePoint>();
    private List<ClientInfo> _clients = new List<ClientInfo>();
    private Random _rnd = new Random();
    private TcpListener _tcpListener = null;

    private int _matchCount = 3;
    private int MatchCount
    {
      get => _matchCount;
      set
      {
        if (value > 0)
        {
          _matchCount = value;
        }
      }
    }

    private int _matchTime = 40;
    private int MatchTime
    {
      get => _matchTime;
      set
      {
        if (value >= 30)
        {
          _matchTime = value;
        }
      }
    }

    //private void StartGenerationPoints()
    //{
    //  while (true)
    //  {
    //    Thread.Sleep(_rnd.Next(Constants.SPAWN_POINT_DELAY_MIN, Constants.SPAWN_POINT_DELAY_MAX));
    //    _gameField.Add(new GamePoint
    //    {
    //      X = (float)_rnd.NextDouble(),
    //      Y = (float)_rnd.NextDouble(),
    //      IsRed = _rnd.Next(0, 100) > 50
    //    });
    //  }
    //}

    //private void FlushDisconnected()
    //{
    //  lock (_clients)
    //  {
    //    var toDisconnect = _clients.Where(client => !client.TcpClient.Connected).ToList();
    //    toDisconnect.ForEach(client => client.TcpClient.Dispose());
    //    Console.WriteLine($"Disconnecting: {toDisconnect.Count}");
    //    _clients.RemoveAll(client => toDisconnect.Contains(client));
    //  }
    //}

    private void AcceptClient()
    {
      if (!_tcpListener.Pending()) return;
      lock (_clients)
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
      lock (_clients)
      {
        _clients
          .Where(client => client.TcpClient.Available > 0)
          .ToList()
          .ForEach(clientInfo => ProcessCommand(clientInfo, clientInfo.TcpClient.AcceptJsonBinaryObject<InputCommand>()));
      }
    }

    private void WriteAll<T>(T obj)
    {
      lock (_clients)
      {
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
        if (toDisconnect.Any())
        {
          RefreshRoomMembers();
        }
      }
    }

    private void ProcessCommand(ClientInfo clientInfo, InputCommand command)
    {
      switch (command.Action)
      {
        case "Register":
          Register(clientInfo, command);
          break;
        case "IncrementMatches":
          MatchCount += command.Payload.GetInt32();
          UpdateMatchesCount();
          break;
        case "IncrementMatchTime":
          MatchTime += command.Payload.GetInt32();
          UpdateMatchTime();
          break;
        case "Disconnect":
          Disconnect(clientInfo);
          break;
      }
    }

    private void Register(ClientInfo clientInfo, InputCommand command)
    {
      clientInfo.Name = command.Payload.GetString();
      Console.WriteLine($"Registering {clientInfo.TcpClient.Client.RemoteEndPoint} - {clientInfo.Name}");
      RefreshRoomMembers();
      UpdateMatchTime();
      UpdateMatchesCount();
    }

    private void RefreshRoomMembers()
    {
      lock (_clients)
      {
        WriteAll(new OutputCommand
        {
          Action = "RefreshRoomMembers",
          Payload = _clients.Any() ? _clients.Select(client => client.Name).Aggregate((acc, x) => $"{acc}\n{x}") : ""
        });
      }
    }

    private void UpdateMatchesCount()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateMatchesCount",
        Payload = MatchCount
      });
    }

    private void UpdateMatchTime()
    {
      WriteAll(new OutputCommand
      {
        Action = "UpdateMatchTime",
        Payload = MatchTime
      });
    }

    private void Disconnect(ClientInfo clientInfo)
    {
      lock (_clients)
      {
        _clients.Remove(clientInfo);
      }
      RefreshRoomMembers();
    }

    public void Start()
    {
      _tcpListener = new TcpListener(IPAddress.Any, Constants.SERVER_PORT);
      _tcpListener.Start();

      Console.WriteLine($"Server started at {_tcpListener.Server.LocalEndPoint}");

      var acceptClientsTimer = new Timer(s => AcceptClient(), null, 0, Constants.TCP_CLIENT_ACCEPT_DELAY);
      var listenClientsTimer = new Timer(s => ListenClients(), null, 0, Constants.TCP_CLIENT_LISTEN_DELAY);
      //var flushClientsTimer = new Timer(s => FlushDisconnected(), null, 0, Constants.TCP_CLIENT_FLUSH_DELAY);

      Thread.Sleep(Timeout.Infinite);
    }

    ~RedDotTcpServer()
    {
      _tcpListener.Stop();
    }

  }
}
