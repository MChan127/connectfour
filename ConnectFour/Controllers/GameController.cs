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
            // the player can only enter the room if the game is currently not in progress,
            // and if the game has not ended
            if (room.Status.ToString() == "playing" || room.Status.ToString() == "finished")
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            //string playerID = User.Identity.GetUserId();
            //ViewBag.thisUserId = playerID;

            return View(room);
        }
    }
}