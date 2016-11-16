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
            LOST_GAME = 1,
            WON_GAME = 2,
            OPPONENT_FORFEIT = 3
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
            try
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
            } catch (Exception e)
            {
                throw e;
            }
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
    }
}