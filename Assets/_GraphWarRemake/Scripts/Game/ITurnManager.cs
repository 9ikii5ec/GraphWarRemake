namespace GraphWarRemake.Game
{
    public interface ITurnManager
    {
        GameState CurrentState { get; }
        uint CurrentPlayerId { get; }
        float TimeRemaining { get; }
        void StartGame();
        void NextTurn();
        void OnProjectileResolved();
        void OnPlayerDied(uint playerId);
    }
}
