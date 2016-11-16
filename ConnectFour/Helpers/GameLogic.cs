using ConnectFour.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectFour.Helpers
{
    public class GameLogic
    {
        /*protected enum wonGame { won, lost, undecided }
        protected enum movedFirst { self, opponent, undecided }

        public static object getGameInfo(Room room)
        {
            int playerID = 2; // for testing, remove later and check for logged-in player id instead

            // compare the winner id with the current player id, value is true if the current player won the game
            // otherwise if the game is still in progress, value is "undecided"
            wonGame wonGame = wonGame.undecided;
            if (room.WinnerID != null)
            {
                wonGame = room.WinnerID == playerID ? wonGame.won : wonGame.lost;
            }

            // value is true if the current player went first
            // otherwise if the game has not started yet, value is null
            movedFirst movedFirst = movedFirst.undecided;
            if (room.FirstMoveID != null)
            {
                movedFirst = room.FirstMoveID == playerID ? movedFirst.self : movedFirst.opponent;
            }

            List<object> moveList = new List<object>();
            // get all the moves for the current game so far
            foreach (var move in room.Moves)
            {
                moveList.Add(
                    new
                    {
                        // value is true if this is the current player's own move
                        id = move.ID,
                        self = move.PlayerID == playerID ? true : false,
                        xpos = move.XPos,
                        ypos = move.YPos
                    }
                );
            }

            return new
            {
                name = room.Name,
                createdAt = room.CreatedAt,
                updatedAt = room.UpdatedAt,
                status = room.Status,
                winner = wonGame.ToString(),
                firstMove = movedFirst.ToString(),
                moves = moveList.ToArray()
            };
        }*/

        // fills the ViewBag with the necessary information about the room
        // when loading the page containing the game
        public static void loadGameInfo(Room room, dynamic ViewBag)
        {
            string playerID = "136533ca-4b8b-480e-b8b3-1cfd0fb02692"; // for testing, remove later and check for logged-in player id instead
            // get current player id
            // ...

            // create the appropriate message to display to the player
            // based on the game's current status
            string gameStatusMsg;
            switch (room.Status.ToString())
            {
                case "waiting":
                    gameStatusMsg = "Waiting for Opponent...";
                    break;
                case "playing":
                    gameStatusMsg = "Game in progress";
                    break;
                case "finished":
                    gameStatusMsg = "Ended";
                    break;
                default:
                    gameStatusMsg = "";
                    break;
            }

            string currentTurnMsg = "";
            int? numberOfTurns = null;

            if (room.Status.ToString() == "playing")
            {
                // message that tells the player whether it's currently their turn, their opponent's turn,
                // or a blank message if the game has not started yet or has ended
                if (room.FirstMoveID != null)
                {
                    currentTurnMsg = room.FirstMoveID == playerID ? "It is currently your turn." : "It is currently the opponent's turn.";
                }

                // calculate the current turn number based on the number of moves made so far by both opponents
                double totalMovesDivided = (double)room.Moves.Count / 2;
                if (totalMovesDivided % 1 != 0)
                {
                    numberOfTurns = (int)Math.Ceiling(totalMovesDivided);
                }
                else
                {
                    numberOfTurns = (int)totalMovesDivided + 1;
                }
            }

            ViewBag.gameStatusMsg = gameStatusMsg;
            ViewBag.currentTurnMsg = currentTurnMsg;
            ViewBag.numberOfTurns = numberOfTurns;
        }
    }
}