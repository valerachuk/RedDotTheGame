namespace RedDotServer
{
  public static class Constants
  {
    public const int SPAWN_POINT_DELAY_MIN = 200;
    public const int SPAWN_POINT_DELAY_MAX = 1000;

    public const int TCP_CLIENT_ACCEPT_DELAY = 100;
    public const int TCP_CLIENT_LISTEN_DELAY = 10;

    public const int SERVER_PORT = 56531;

    public const int MIN_GAME_DURATION = 20;
    public const int DEFAULT_GAME_DURATION = 40;

    public const int MIN_GAME_COUNT = 1;
    public const int DEFAULT_GAME_COUNT = 3;

    public const int DOT_LIFETIME = 1000;
    public const int MAX_DOTS_ON_BOARD = 12;
    public const int RED_DOT_CHANCE = 70;

    public const int DOT_FLUSH_DELAY = 300;
  }
}
