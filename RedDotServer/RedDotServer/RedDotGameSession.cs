using System;
using System.Collections.Generic;

namespace RedDotServer
{
  class RedDotGameSession
  {
    private int _matchCount = Constants.DEFAULT_GAME_COUNT;
    public int MatchCount
    {
      get => _matchCount;
      set
      {
        if (value >= Constants.MIN_GAME_COUNT)
        {
          _matchCount = value;
        }
      }
    }
    public int MatchesLeft { get; private set; }

    private int _matchDuration = Constants.DEFAULT_GAME_DURATION;
    public int MatchDuration
    {
      get => _matchDuration;
      set
      {
        if (value >= Constants.MIN_GAME_DURATION)
        {
          _matchDuration = value;
        }
      }
    }
    public int MatchTimeLeft { get; private set; }

    public List<Dot> GameField { get; set; } = new List<Dot>();
    private readonly Random _rnd = new Random();

    public int ComputePointSpawnDelay()
    {
      return Constants.SPAWN_POINT_DELAY_MIN + (Constants.SPAWN_POINT_DELAY_MAX - Constants.SPAWN_POINT_DELAY_MIN) * (MatchesLeft + 1) / MatchCount;
    }

    public bool SpawnPoint()
    {
      if (GameField.Count >= Constants.MAX_DOTS_ON_BOARD) return false;

      var point = new Dot
      {
        IsRed = _rnd.Next(1, 101) <= Constants.RED_DOT_CHANCE,
        X = (float)_rnd.NextDouble(),
        Y = (float)_rnd.NextDouble(),
      };

      GameField.Add(point);
      return true;
    }

    public void CommitSettings()
    {
      MatchesLeft = MatchCount;
    }

    public bool StartMatch()
    {
      GameField.Clear();
      MatchesLeft--;
      MatchTimeLeft = MatchDuration;
      return MatchesLeft >= 0;
    }

    public bool DecrementTime()
    {
      MatchTimeLeft--;
      return MatchTimeLeft >= 0;
    }

    public int RewardPoint(long id)
    {
      var point = GameField.Find(pd => pd.ID == id);
      if (point == null) return 0;
      GameField.Remove(point);
      if (point.IsRed) return 1;
      return -1;
    }

    public bool DeleteOldPoints()
    {
      var nowMs = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
      return GameField.RemoveAll(point => nowMs - point.ID > Constants.DOT_LIFETIME) > 0;
    }
  }
}
