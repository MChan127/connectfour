using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectFour.Models
{
    public class Move
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public Room RoomID { get; set; }
        public string PlayerID { get; set; }
        [ForeignKey("PlayerID")]
        public virtual ApplicationUser Player { get; set; }
        public int XPos { get; set; }
        public int YPos { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Room Room { get; set; }
    }
}