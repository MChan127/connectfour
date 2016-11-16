using ConnectFour.Helpers;
using ConnectFour.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace ConnectFour.Controllers
{
    public class GameController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Game
        // game lobby
        public ActionResult Index()
        {
            return View();
        }

        /*[HttpGet]
        public ActionResult GetGameInfo(int ID)
        {
            // check if the current player is logged in
            // ...

            // check if the current player belongs to or is the author of the room
            // ...

            Room room = db.Room.Find(ID);
            return Json(GameLogic.getGameInfo(room), JsonRequestBehavior.AllowGet);
        }*/

        // starts the game when both players have entered the room
        // the server randomly then chooses which player goes first
        [HttpPost]
        public ActionResult StartGame(int roomID)
        {
            // ...
            return Json(new { });
        }
        // update game by adding a move
        // server determines if the move is allowed or not
        // if not, move is rejected and message is displayed to the player
        // if so, game logic checks if the player has won the game using that move
        [HttpPost]
        public ActionResult AddMove(int roomID, int playerID, int XPos, int YPos)
        {
            // ...
            return Json(new { });
        }
        // player leaves the game, causing them to lose
        [HttpPost]
        public ActionResult Concede(int roomID, int playerID)
        {
            // ...
            return Json(new { });
        }

        // for testing and developing the game itself
        [Authorize]
        public ActionResult TestGame()
        {
            // check that the current player is logged in
            // ...

            Room room = db.Room.Where(Room => Room.Name == "Test Room").FirstOrDefault();
            // check if room exists
            if (room == null)
            {
                return HttpNotFound();
            }
            // the player can only enter the room if the game is currently not in progress,
            // and if the game has not ended
            if (room.Status.ToString() == "playing" || room.Status.ToString() == "finished")
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            // get room info including its current status, turn count, etc.
            GameLogic.loadGameInfo(room, ViewBag);

            return View(room);
        }
    }
}