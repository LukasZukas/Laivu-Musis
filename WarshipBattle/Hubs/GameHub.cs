using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace WarshipBattle.Hubs
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Game> Games = new ConcurrentDictionary<string, Game>();
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GameHub(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task JoinGameRoom(int roomId)
        {
            var connectionId = Context.ConnectionId;
            var gameId = roomId.ToString();

            await Clients.Caller.SendAsync("Debug", $"ConnectionId: {connectionId} attempting to join room: {gameId}");

            await Groups.AddToGroupAsync(connectionId, gameId);

            if (!Games.TryGetValue(gameId, out var game))
            {
                await Clients.Caller.SendAsync("Debug", $"Room {gameId} does not exist. Creating new game for Player1: {connectionId}");
                game = new Game
                {
                    GameId = gameId,
                    Player1 = new Player { ConnectionId = connectionId, UserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, Board = new int[10, 10], ShotBoard = new int[10, 10] },
                    IsPlayer1Turn = true
                };
                Games.TryAdd(gameId, game);

                await Clients.Client(connectionId).SendAsync("PlayerJoined", "Player 1 has joined the room.");
                await Clients.Client(game.Player1.ConnectionId).SendAsync("GameStarted", roomId, true);
                await Clients.Caller.SendAsync("Debug", $"Player1 (ConnectionId: {connectionId}) joined room {gameId}. Waiting for Player2...");
            }
            else
            {
                if (game.Player1.ConnectionId == connectionId)
                {
                    await Clients.Caller.SendAsync("Debug", $"ConnectionId: {connectionId} is already Player1 in room {gameId}. Continuing...");
                    await Clients.Client(connectionId).SendAsync("GameStarted", roomId, true);
                    return;
                }
                if (game.Player2 != null && game.Player2.ConnectionId == connectionId)
                {
                    await Clients.Caller.SendAsync("Debug", $"ConnectionId: {connectionId} is already Player2 in room {gameId}. Continuing...");
                    await Clients.Client(connectionId).SendAsync("GameStarted", roomId, false);
                    return;
                }

                if (game.Player2 != null)
                {
                    await Clients.Caller.SendAsync("Debug", $"Room {gameId} is already full! ConnectionId: {connectionId} cannot join.");
                    await Clients.Client(connectionId).SendAsync("Error", "Room is already full!");
                    return;
                }

                await Clients.Caller.SendAsync("Debug", $"Room {gameId} exists. Assigning Player2: {connectionId}");
                game.Player2 = new Player { ConnectionId = connectionId, UserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, Board = new int[10, 10], ShotBoard = new int[10, 10] };
                Games[gameId] = game; // Atnaujiname žaidimą žodyne

                await Clients.Client(game.Player2.ConnectionId).SendAsync("GameStarted", roomId, false);

                await Clients.Client(game.Player1.ConnectionId).SendAsync("GameStartedMessage", "Žaidimas prasidėjo! Išdėstyk savo laivus");
                await Clients.Client(game.Player2.ConnectionId).SendAsync("GameStartedMessage", "Žaidimas prasidėjo! Išdėstyk savo laivus");

                await Clients.Group(gameId).SendAsync("PlayerJoined", "Player 2 has joined the room. Game starting!");
                await Clients.Caller.SendAsync("Debug", $"Player2 (ConnectionId: {connectionId}) joined room {gameId}. Game can start!");
            }
        }

        public async Task FinishPlacingShips(int roomId, int[][] playerBoard)
        {
            var gameId = roomId.ToString();
            await Clients.Caller.SendAsync("Debug", $"ConnectionId: {Context.ConnectionId} finished placing ships in room: {gameId}");

            if (Games.TryGetValue(gameId, out var game))
            {
                // Konvertuojame int[][] į int[,]
                int[,] board = new int[10, 10];
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        board[i, j] = playerBoard[i][j];
                    }
                }

                // Atnaujiname žaidėjo lentą
                if (Context.ConnectionId == game.Player1.ConnectionId)
                {
                    game.Player1.Board = board;
                    await Clients.Caller.SendAsync("Debug", "Player 1 board updated with ship positions.");
                }
                else if (Context.ConnectionId == game.Player2.ConnectionId)
                {
                    game.Player2.Board = board;
                    await Clients.Caller.SendAsync("Debug", "Player 2 board updated with ship positions.");
                }

                game.PlayersReady = (game.PlayersReady ?? 0) + 1;
                await Clients.Caller.SendAsync("Debug", $"Room {gameId} - Players ready: {game.PlayersReady}");

                if (game.PlayersReady == 2)
                {
                    if (string.IsNullOrEmpty(game.Player1?.ConnectionId) || string.IsNullOrEmpty(game.Player2?.ConnectionId))
                    {
                        await Clients.Caller.SendAsync("Debug", $"Error in room {gameId}: One of the players has lost connection!");
                        await Clients.Group(gameId).SendAsync("Error", "One of the players has lost connection!");
                        return;
                    }

                    await Clients.Caller.SendAsync("BothPlayersReady");
                    await Clients.OthersInGroup(gameId).SendAsync("BothPlayersReady");

                    await Clients.Group(gameId).SendAsync("Debug", $"Room {gameId} - Players ready: 2");
                    if (game.IsPlayer1Turn)
                    {
                        await Clients.Client(game.Player1.ConnectionId).SendAsync("Debug", $"Room {gameId} - Player1 (ConnectionId: {game.Player1.ConnectionId}) - It's your turn!");
                        await Clients.Client(game.Player2.ConnectionId).SendAsync("Debug", $"Room {gameId} - Player2 (ConnectionId: {game.Player2.ConnectionId}) - Waiting for opponent's turn.");
                        await Clients.Client(game.Player1.ConnectionId).SendAsync("GameStartedMessage", "Game started! Your turn...");
                        await Clients.Client(game.Player2.ConnectionId).SendAsync("GameStartedMessage", "Game started! Wait for opponent's turn...");
                    }
                    else
                    {
                        await Clients.Client(game.Player2.ConnectionId).SendAsync("Debug", $"Room {gameId} - Player2 (ConnectionId: {game.Player2.ConnectionId}) - It's your turn!");
                        await Clients.Client(game.Player1.ConnectionId).SendAsync("Debug", $"Room {gameId} - Player1 (ConnectionId: {game.Player1.ConnectionId}) - Waiting for opponent's turn.");
                        await Clients.Client(game.Player2.ConnectionId).SendAsync("GameStartedMessage", "Game started! Your turn...");
                        await Clients.Client(game.Player1.ConnectionId).SendAsync("GameStartedMessage", "Game started! Wait for opponent's turn...");
                    }
                    await Clients.Group(gameId).SendAsync("UpdateTurn", game.IsPlayer1Turn);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("Debug", $"Error: Game with ID {gameId} not found!");
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

                bool shipSunk = CheckIfShipSunk(opponent.Board, row, col);
                if (shipSunk)
                {
                    await Clients.Caller.SendAsync("ShipSunk", "You have sunk an opponent's ship!");
                    await Clients.OthersInGroup(gameId).SendAsync("ShipSunk", "Your opponent has sunk one of your ships!");

                    // Tikriname, ar visi priešininko laivai sunaikinti
                    bool allShipsSunk = CheckIfAllShipsSunk(opponent.Board);
                    if (allShipsSunk)
                    {
                        string winnerId = attacker.UserId; // Naudojame UserId kaip nugalėtojo ID
                        string winnerMessage = "Congratulations! You won! 🎉";
                        string loserMessage = "Opponent destroyed all your ships! You lost the game!";

                        await Clients.Caller.SendAsync("GameEnded", winnerMessage, winnerId);
                        await Clients.OthersInGroup(gameId).SendAsync("GameEnded", loserMessage, null);

                        // Pašaliname žaidimą iš sąrašo
                        Games.TryRemove(gameId, out _);
                        return;
                    }
                }
            }

            await Clients.Caller.SendAsync("AttackResult", row, col, hit);
            await Clients.OthersInGroup(gameId).SendAsync("ReceiveAttack", row, col, hit);

            await Clients.Caller.SendAsync("Debug", $"Before turn change - IsPlayer1Turn: {game.IsPlayer1Turn}");
            if (!hit)
            {
                game.IsPlayer1Turn = !game.IsPlayer1Turn;
            }
            await Clients.Caller.SendAsync("Debug", $"After turn change - IsPlayer1Turn: {game.IsPlayer1Turn}");

            await Clients.Group(gameId).SendAsync("UpdateTurn", game.IsPlayer1Turn);
        }

        private bool CheckIfAllShipsSunk(int[,] board)
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    if (board[row, col] == 1)
                        return false;
                }
            }
            return true;
        }

        private bool CheckIfShipSunk(int[,] board, int hitRow, int hitCol)
        {
            // Laivo ribos
            int startRow = hitRow, endRow = hitRow;
            int startCol = hitCol, endCol = hitCol;

            // Horizontaliai
            for (int col = hitCol - 1; col >= 0 && (board[hitRow, col] == 1 || board[hitRow, col] == 2); col--)
                startCol = col;
            for (int col = hitCol + 1; col < 10 && (board[hitRow, col] == 1 || board[hitRow, col] == 2); col++)
                endCol = col;

            // Vertikaliai
            for (int row = hitRow - 1; row >= 0 && (board[row, hitCol] == 1 || board[row, hitCol] == 2); row--)
                startRow = row;
            for (int row = hitRow + 1; row < 10 && (board[row, hitCol] == 1 || board[row, hitCol] == 2); row++)
                endRow = row;

            // Ar visi laivo langeliai pataikyti
            bool isHorizontal = startCol != endCol;
            if (isHorizontal)
            {
                for (int col = startCol; col <= endCol; col++)
                {
                    if (board[hitRow, col] != 2)
                        return false;
                }
            }
            else
            {
                for (int row = startRow; row <= endRow; row++)
                {
                    if (board[row, hitCol] != 2)
                        return false;
                }
            }
            return true;
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

        // Atsijungus žaidėjui, kambarys išvalomas
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            foreach (var game in Games)
            {
                if (game.Value.Player1?.ConnectionId == Context.ConnectionId || game.Value.Player2?.ConnectionId == Context.ConnectionId)
                {
                    var gameId = game.Key;
                    var opponent = game.Value.Player1.ConnectionId == Context.ConnectionId ? game.Value.Player2 : game.Value.Player1;
                    if (opponent != null)
                    {
                        await Clients.Client(opponent.ConnectionId).SendAsync("OpponentLeft", "Your opponent has disconnected.");
                    }
                    Games.TryRemove(gameId, out _);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
                    await Clients.Group(gameId).SendAsync("UserLeft", "A user has disconnected from the room.");
                    break;
                }
            }
            await base.OnDisconnectedAsync(exception);
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
        public string UserId { get; set; }
        public int[,] Board { get; set; }
        public int[,] ShotBoard { get; set; }
    }
}
