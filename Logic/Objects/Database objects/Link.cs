using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordBot_G.Logic.Objects
{
    [PrimaryKey("linkID")]
    public class Link
    {        
        /// <summary>
        /// Primary key
        /// </summary>
        public int linkID { get; set; }
        /// <summary>
        /// Foreign key to Guild ID
        /// </summary>
        [ForeignKey("server")]
        public ulong ServerID { get; set; }
        /// <summary>
        /// Foreign key to User ID
        /// </summary>
        [ForeignKey("user")]
        public ulong UserID { get; set; }
        /// <summary>
        /// Chance for this user at this guild
        /// </summary>
        public int Chance { get; set; }
        /// <summary>
        /// Foreign reference
        /// </summary>
        public virtual Server server { get; set; }
        /// <summary>
        /// Foreign reference
        /// </summary>
        public virtual User user { get; set; }
    }
}
