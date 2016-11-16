using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectFour.Models
{
    public enum Status { waiting, playing, finished }
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string AuthorID { get; set; }
        [ForeignKey("AuthorID")]
        public virtual ApplicationUser Author { get; set; } // author of the room
        public string OpponentID { get; set; }
        [ForeignKey("OpponentID")]
        public virtual ApplicationUser Opponent { get; set; } // opponent of the author
        public string WinnerID { get; set; }
        [ForeignKey("WinnerID")]
        public virtual ApplicationUser Winner { get; set; }
        public string FirstMoveID { get; set; }
        [ForeignKey("FirstMoveID")]
        public virtual ApplicationUser FirstMove { get; set; } // the player who went first (randomly picked by server)
        public string Name { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Status Status { get; set; }
        
        public virtual ICollection<Move> Moves { get; set; }
    }
}