using System.ComponentModel.DataAnnotations;

namespace DiscordBot_G.Logic.Objects
{
    public class User
    {
        /// <summary>
        /// User ID
        /// </summary>
        [Key]
        public ulong UserID { get; set; }
        /// <summary>
        /// Foreign key to Links
        /// </summary>
        public virtual ICollection<Link> Links { get; set;}
    }
}
