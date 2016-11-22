using ConnectFour.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        public ActionResult Index(string postMessage = null)
        {
            if (Session["postMessage"] != null)
            {
                ViewBag.postMessage = Session["postMessage"];
                Session["postMessage"] = null;
            }

            // get all rooms that are either waiting or running
            List<Room> allRooms = db.Room.Where(r => r.Status != 2).ToList();

            // check if the player is currently in a game
            foreach (Room room in allRooms)
            {
                if (room.AuthorID == User.Identity.GetUserId() ||
                    room.OpponentID == User.Identity.GetUserId())
                {
                    ViewBag.currentGameId = room.ID;
                    ViewBag.currentGameName = room.Name;
                }
            }

            return View(allRooms);
        }

        [Authorize]
        [HttpPost]
        // create a new game
        public async Task<ActionResult> Create(string room_name)
        {
            Room room = new Room();
            room.AuthorID = User.Identity.GetUserId();
            room.CreatedAt = DateTime.Today;
            room.UpdatedAt = DateTime.Today;
            room.Name = room_name;
            room.Status = (int)RoomStatus.waiting;
            db.Room.Add(room);
            await db.SaveChangesAsync();

            //Session["postMessage"] = "Game was successfully created.";
            return RedirectToAction("Room", "Game", new { id = room.ID });
        }

        // for testing and developing the game itself
        [Authorize]
        public ActionResult Room(int id)
        {
            Room room = db.Room.Where(Room => Room.ID == id).FirstOrDefault();
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
                    Session["postMessage"] = "The game you are trying to access is already in progress.";
                    return RedirectToAction("Index", "Game");
                    //return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            }
            else if (room.Status == (int)RoomStatus.finished)
            {
                Session["postMessage"] = "The game you are trying to access has already finished.";
                return RedirectToAction("Index", "Game");
                //ViewBag.message = "The game you are trying to access has already finished.";
                //return View("Error");
            }

            //string playerID = User.Identity.GetUserId();
            //ViewBag.thisUserId = playerID;

            return View(room);
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