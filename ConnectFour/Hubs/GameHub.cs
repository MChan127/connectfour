using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using ConnectFour.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Security.Claims;
using System.Data.Entity;
using System.Diagnostics;

namespace ConnectFour.Hubs
{
    // only logged in users should be able to interact with the game through the hub
    [Authorize]
    public class GameHub : Hub
    {
        public enum updateGameStatus {
            GAME_START = 0,
            WON_GAME = 1,
            OPPONENT_FORFEIT = 2
        };

        private readonly UserManager<ApplicationUser> _userManager;

        private ApplicationDbContext db = new ApplicationDbContext();

        public GameHub()
        {
            // get the current http context
            //System.Web.HttpContextBase httpContext = Context.Request.GetHttpContext();

            // get the user manager from the current http context
            //_userManager = httpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
        }

        public async Task joinGame(int roomID)
        {
            Room room = db.Room.Find(roomID);
            string roomName = roomID.ToString();
            // check that room exists
            if (room == null)
            {
                throw new Exception("The room with that ID does not exist.");
            }
            // check that the current game has not started or ended yet
            if (room.Status.ToString() == "playing" || room.Status.ToString() == "finished")
            {
                throw new Exception("The game is currently in progress.");
            }
            Debug.WriteLine("connection id: " + Context.ConnectionId);
            await Groups.Add(Context.ConnectionId, roomName);
        }

        public async Task startGame(int roomID)
        {
            Room room = db.Room.Find(roomID);
            // check that room exists
            if (room == null)
            {
                throw new Exception("The room with that ID does not exist.");
            }
            // check that the current game has not started or ended yet
            if (room.Status.ToString() == "playing" || room.Status.ToString() == "finished")
            {
                throw new Exception("The game is currently in progress.");
            }

            // update room information
            room.Status = (int)RoomStatus.playing;
            room.OpponentID = ((ClaimsIdentity)Context.User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
            // randomly decide which player goes first
            Random coinFlip = new Random();
            int firstTurn = coinFlip.Next(0, 2); // if 0, then the author goes first; if 1, the opponent goes first
            room.FirstMoveID = firstTurn == 0 ? room.AuthorID : room.OpponentID;
            db.Entry(room).State = EntityState.Modified;
            await db.SaveChangesAsync();
            
            // notify both clients that the game will begin
            Clients.Group(roomID.ToString()).updateGameStatus(updateGameStatus.GAME_START, new
            {
                first_turn = firstTurn
            });
        }

        public async Task endTurn(int roomID, int x, int y)
        {
            string playerID = ((ClaimsIdentity)Context.User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;

            Room room = db.Room.Find(roomID);
            // check that room exists
            if (room == null)
            {
                throw new Exception("The room with that ID does not exist.");
            }
            // check that the current game is in progress
            if (room.Status.ToString() != "playing")
            {
                throw new Exception("The game is currently not in progress.");
            }

            // check that it's this player's turn to make a move
            Move lastMove = db.Move.OrderByDescending(m => m.ID).First(m => m.RoomID == roomID);
            if (lastMove != null) {
                if (lastMove.PlayerID == playerID)
                {
                    throw new Exception("A player cannot move two turns in a row.");
                }
            } else
            {
                if (room.FirstMoveID == playerID)
                {
                    throw new Exception("A player cannot move two turns in a row.");
                }
            }

            // check that the move does not already exist on this board
            if (db.Move.Any(m => m.RoomID == roomID && m.XPos == x && m.YPos == y))
            {
                throw new Exception("That move already exists on this board.");
            }
            // check that the move is allowed
            // a move is allowed if there is a floor or another piece in the space beneath
            if (
                y != 5 &&
                !db.Move.Any(m => m.RoomID == roomID && m.XPos == x && m.YPos == y+1)
               )
            {
                throw new Exception("That move is not allowed.");
            }

            Move move = new Move();
            move.PlayerID = playerID;
            move.RoomID = roomID;
            move.XPos = x;
            move.YPos = y;
            db.Entry(move).State = EntityState.Modified;
            await db.SaveChangesAsync();

            // server checks if this is a winning move
            // if so, a signal is sent to both players that the game has ended
            // otherwise, send the signal to the other player that their turn can begin
            bool victory = this.checkIfWinningMove(roomID, playerID, x, y);
            if (victory)
            {
                room.UpdatedAt = DateTime.Today;
                room.WinnerID = playerID;
                room.Status = (int)RoomStatus.finished;
                db.Entry(room).State = EntityState.Modified;
                await db.SaveChangesAsync();

                await Clients.Group(roomID.ToString()).updateGameStatus(updateGameStatus.WON_GAME, new
                {
                    winnerID = playerID
                });
            }
            else
            {
                room.UpdatedAt = DateTime.Today;
                db.Entry(room).State = EntityState.Modified;
                await db.SaveChangesAsync();

                await Clients.Group(roomID.ToString()).endOpponentTurn(new
                {
                    x = x,
                    y = y
                });
            }
        }

        private bool checkIfWinningMove(int roomID, string playerID, int x, int y)
        {
            // get all moves for this room from this player
            var queryString = "SELECT XPos, YPos FROM Moves WHERE RoomID = @0 AND PlayerID = @1 AND " +
                "ABS(XPos - @2) <= 3 ORDER BY XPos ASC;";
            List<Move> allMoves = db.Move.SqlQuery(queryString, roomID, playerID, x).ToList();

            // create a dictionary of all the moves to represent a grid
            // with the x axis coordinates as keys and y coordinates as values
            SortedDictionary<int, List<int>> moveGrid = new SortedDictionary<int, List<int>>();
            foreach (Move move in allMoves)
            {
                List<int> gridColumn = new List<int>();
                if (!moveGrid.TryGetValue(move.XPos, out gridColumn))
                {
                    gridColumn.Add(move.YPos);
                    moveGrid.Add(move.XPos, gridColumn);
                } else {
                    gridColumn.Add(move.YPos);
                    moveGrid[move.XPos] = gridColumn;
                }
            }

            // remove columns that are a distance or more removed from the column containing the player's move
            // for example, if we have columns [0, 2, 3, 4, 6] and our move is in column 3, columns 0 and 6 should be removed
            bool foundLeftMostColumn = false, foundRightMostColumn = false;
            string direction = "left";
            int startingColumn = y;
            while (!foundLeftMostColumn || !foundRightMostColumn)
            {
                if (direction == "left")
                {
                    if (moveGrid.ContainsKey(startingColumn - 1))
                    {
                        startingColumn--;
                        if (startingColumn < 0)
                        {
                            direction = "right";
                            foundLeftMostColumn = true;
                            startingColumn = y;
                        }
                    } else
                    {
                        for (int i = 0; i < startingColumn - 1; i++)
                        {
                            moveGrid.Remove(i);
                        }

                        direction = "right";
                        foundLeftMostColumn = true;
                        startingColumn = y;
                    }
                } else if (direction == "right")
                {
                    if (moveGrid.ContainsKey(startingColumn + 1))
                    {
                        startingColumn++;
                        if (startingColumn > 6)
                        {
                            foundRightMostColumn = true;
                        }
                    }
                    else
                    {
                        for (int i = moveGrid.Keys.Last(); i < startingColumn + 1; i--)
                        {
                            moveGrid.Remove(i);
                        }
                        
                        foundRightMostColumn = true;
                    }
                }
            }

            // search for a four-in-a-row
            if (this.searchForFourInRow(moveGrid, x, y, "vertical", new List<int>()))
                return true;
            if (this.searchForFourInRow(moveGrid, x, y, "horizontal", new List<int>()))
                return true;
            if (this.searchForFourInRow(moveGrid, x, y, "ascending", new List<int>()))
                return true;
            if (this.searchForFourInRow(moveGrid, x, y, "descending", new List<int>()))
                return true;

            return false;
        }

        private bool searchForFourInRow(SortedDictionary<int, List<int>> moveGrid, int x, int y, string angle, List<int> omitted)
        {
            // all values are converted to positive/negative when travelling in the opposite direction
            int add_to_x, add_to_y;
            switch (angle)
            {
                case "vertical":
                    add_to_x = 0;
                    add_to_y = 1;
                    break;
                case "horizontal":
                    add_to_x = 1;
                    add_to_y = 0;
                    break;
                case "ascending":
                    add_to_x = 1;
                    add_to_y = 1;
                    break;
                case "descending":
                    add_to_x = -1;
                    add_to_y = 1;
                    break;
                // should never go here, but this is required to compile
                default:
                    add_to_x = 0;
                    add_to_y = 0;
                    break;
            }

            int totalInRow = 1;
            bool forwardTravelled = false, backwardTravelled = false;
            string direction = "forward";
            int current_x = x, current_y = y;
            while ((!forwardTravelled || !backwardTravelled) && totalInRow < 4)
            {
                if (direction == "forward")
                {
                    List<int> column = new List<int>();
                    if (moveGrid.TryGetValue(current_x + add_to_x, out column))
                    {
                        if (column.Contains(current_y + add_to_y))
                        {
                            current_x += add_to_x;
                            current_y += add_to_y;
                            totalInRow++;
                        }
                        else
                        {
                            current_x = x;
                            current_y = y;
                            forwardTravelled = true;
                            direction = "backward";
                        }
                    }
                    else
                    {
                        current_x = x;
                        current_y = y;
                        forwardTravelled = true;
                        direction = "backward";
                    }
                }
                else if (direction == "backward")
                {
                    List<int> column = new List<int>();
                    if (moveGrid.TryGetValue(current_x - add_to_x, out column))
                    {
                        if (column.Contains(current_y - add_to_y))
                        {
                            current_x -= add_to_x;
                            current_y -= add_to_y;
                            totalInRow++;
                        }
                        else
                        {
                            backwardTravelled = true;
                        }
                    }
                    else
                    {
                        backwardTravelled = true;
                    }
                }
            }

            return totalInRow == 4;
        }
    }
}