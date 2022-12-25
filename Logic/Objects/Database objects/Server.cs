using System.ComponentModel.DataAnnotations;

namespace DiscordBot_G.Logic.Objects
{
    public class Server
    {
        /// <summary>
        /// Guild ID
        /// </summary>
        [Key]
        public ulong ServerID { get; set; }
        /// <summary>
        /// Unique token
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Foreign key to links
        /// </summary>
        public virtual ICollection<Link> Links { get; set; }
    }
}
