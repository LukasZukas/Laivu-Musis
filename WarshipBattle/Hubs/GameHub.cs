using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace WarshipBattle.Hubs
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Game> Games = new ConcurrentDictionary<string, Game>();
        //private static int count = 1;

        public async Task JoinGameRoom(int roomId)
        {
            var connectionId = Context.ConnectionId;
            var gameId = roomId.ToString();
            //count++;

            await Groups.AddToGroupAsync(connectionId, gameId);

            if (!Games.TryGetValue(gameId, out var game))
            {
                game = new Game
                {
                    GameId = gameId,
                    Player1 = new Player { ConnectionId = connectionId, Board = new int[10, 10], ShotBoard = new int[10, 10] },
                    IsPlayer1Turn = true
                };
                Games.TryAdd(gameId, game);

                //await Clients.Caller.SendAsync("Print", "Tikrinu ar iki cia ateina - " + count);

                await Clients.Client(connectionId).SendAsync("PlayerJoined", "Player 1 has joined the room.");
                await Clients.Client(game.Player1.ConnectionId).SendAsync("GameStarted", roomId, true);
            }
            else
            {
                game.Player2 = new Player { ConnectionId = connectionId, Board = new int[10, 10], ShotBoard = new int[10, 10] };
                Games.TryAdd(gameId, game);

                await Clients.Client(game.Player2.ConnectionId).SendAsync("GameStarted", roomId, false);

                await Clients.Client(game.Player1.ConnectionId).SendAsync("GameStartedMessage", "Žaidimas prasidėjo! Išdėstyk savo laivus");
                await Clients.Client(game.Player2.ConnectionId).SendAsync("GameStartedMessage", "Žaidimas prasidėjo! Išdėstyk savo laivus");

                await Clients.Group(gameId).SendAsync("PlayerJoined", "Player 2 has joined the room. Game starting!");
            }
        }

        public async Task FinishPlacingShips(int roomId)
        {
            var gameId = roomId.ToString();
            if (Games.TryGetValue(gameId, out var game))
            {
                game.PlayersReady = (game.PlayersReady ?? 0) + 1;

                if (game.PlayersReady == 2)
                {
                    if (string.IsNullOrEmpty(game.Player1?.ConnectionId) || string.IsNullOrEmpty(game.Player2?.ConnectionId))
                    {
                        await Clients.Group(gameId).SendAsync("Error", "One of the players has lost connection!");
                        return;
                    }

                    await Clients.Caller.SendAsync("BothPlayersReady");
                    await Clients.OthersInGroup(gameId).SendAsync("BothPlayersReady");

                    // Siunčiame pranešimus ir eiles
                    if (game.IsPlayer1Turn)
                    {
                        // Player 1 turėtų gauti "Your turn", Player 2 – "Opponent's turn"
                        if (Context.ConnectionId == game.Player1.ConnectionId)
                        {
                            await Clients.Caller.SendAsync("GameStartedMessage", "Game started! Your turn...");
                            await Clients.OthersInGroup(gameId).SendAsync("GameStartedMessage", "Game started! Wait for opponent's turn...");
                            await Clients.Caller.SendAsync("UpdateTurn", true); // Player 1
                            await Clients.OthersInGroup(gameId).SendAsync("UpdateTurn", false); // Player 2
                        }
                        else
                        {
                            await Clients.Caller.SendAsync("GameStartedMessage", "Game started! Wait for opponent's turn...");
                            await Clients.OthersInGroup(gameId).SendAsync("GameStartedMessage", "Game started! Your turn...");
                            await Clients.Caller.SendAsync("UpdateTurn", false); // Player 2
                            await Clients.OthersInGroup(gameId).SendAsync("UpdateTurn", true); // Player 1
                        }
                    }
                    else
                    {
                        // Player 2 turėtų gauti "Your turn", Player 1 – "Opponent's turn"
                        if (Context.ConnectionId == game.Player1.ConnectionId)
                        {
                            await Clients.Caller.SendAsync("GameStartedMessage", "Game started! Wait for opponent's turn...");
                            await Clients.OthersInGroup(gameId).SendAsync("GameStartedMessage", "Game started! Your turn...");
                            await Clients.Caller.SendAsync("UpdateTurn", false); // Player 1
                            await Clients.OthersInGroup(gameId).SendAsync("UpdateTurn", true); // Player 2
                        }
                        else
                        {
                            await Clients.Caller.SendAsync("GameStartedMessage", "Game started! Your turn...");
                            await Clients.OthersInGroup(gameId).SendAsync("GameStartedMessage", "Game started! Wait for opponent's turn...");
                            await Clients.Caller.SendAsync("UpdateTurn", true); // Player 2
                            await Clients.OthersInGroup(gameId).SendAsync("UpdateTurn", false); // Player 1
                        }
                    }
                }
            }
        }

        public async Task Attack(int roomId, int row, int col)
        {
            var gameId = roomId.ToString();
            if (!Games.TryGetValue(gameId, out var game))
            {
                await Clients.Caller.SendAsync("Error", "Game not found!");
                return;
            }

            var attacker = game.Player1.ConnectionId == Context.ConnectionId ? game.Player1 : game.Player2;
            var opponent = game.Player1.ConnectionId == Context.ConnectionId ? game.Player2 : game.Player1;

            if (attacker.ShotBoard[row, col] != 0)
            {
                await Clients.Caller.SendAsync("Error", "You already guessed that spot!");
                return;
            }

            bool hit = opponent.Board[row, col] == 1;
            attacker.ShotBoard[row, col] = hit ? 2 : 3;
            if (hit)
            {
                opponent.Board[row, col] = 2;
            }

            await Clients.Caller.SendAsync("AttackResult", row, col, hit);
            await Clients.OthersInGroup(gameId).SendAsync("ReceiveAttack", row, col, hit);

            game.IsPlayer1Turn = !game.IsPlayer1Turn;
            if (Context.ConnectionId == game.Player1.ConnectionId)
            {
                await Clients.Caller.SendAsync("UpdateTurn", false); // Player 1
                await Clients.OthersInGroup(gameId).SendAsync("UpdateTurn", true); // Player 2
            }
            else
            {
                await Clients.Caller.SendAsync("UpdateTurn", true); // Player 2
                await Clients.OthersInGroup(gameId).SendAsync("UpdateTurn", false); // Player 1
            }
        }

        public async Task LeaveRoom(int roomId)
        {
            var gameId = roomId.ToString();
            if (Games.TryGetValue(gameId, out var game))
            {
                var opponent = game.Player1.ConnectionId == Context.ConnectionId ? game.Player2 : game.Player1;
                if (opponent != null)
                {
                    await Clients.Client(opponent.ConnectionId).SendAsync("OpponentLeft", "Your opponent has left the game.");
                }
                Games.TryRemove(gameId, out _);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("UserLeft", "A user has left the room.");
        }
    }

    public class Game
    {
        public string GameId { get; set; }
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public bool IsPlayer1Turn { get; set; }
        public int? PlayersReady { get; set; }
    }

    public class Player
    {
        public string ConnectionId { get; set; }
        public int[,] Board { get; set; }
        public int[,] ShotBoard { get; set; }
    }
}