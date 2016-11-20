using ConnectFour.Models;
using Microsoft.AspNet.Identity;
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
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        // for testing and developing the game itself
        [Authorize]
        public ActionResult TestGame()
        {
            Room room = db.Room.Where(Room => Room.Name == "Test Room").FirstOrDefault();
            // check if room exists
            if (room == null)
            {
                return HttpNotFound();
            }
            // player can only enter the room if it's in "waiting" status (no opponent yet)
            // or if the player belongs to the room and the room is a game in progress
            if (room.Status == (int)RoomStatus.playing)
            {
                if (room.AuthorID != User.Identity.GetUserId() &&
                    room.OpponentID != User.Identity.GetUserId())
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            } else if (room.Status == (int)RoomStatus.finished)
            {
                ViewBag.message = "The game you are trying to access has already finished.";
                return View("Error");
            }

            //string playerID = User.Identity.GetUserId();
            //ViewBag.thisUserId = playerID;

            return View(room);
        }
    }
}