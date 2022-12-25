using Microsoft.EntityFrameworkCore;

namespace DiscordBot_G.Logic.Objects
{
    [PrimaryKey("Key")]
    public class Order
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Key { get; set; }
        /// <summary>
        /// Guild ID
        /// </summary>
        public ulong ServerID { get; set; }
        /// <summary>
        /// Channel ID
        /// </summary>
        public ulong ChanneID { get; set; }
        /// <summary>
        /// JSON string
        /// </summary>
        public string JSONData { get; set; }
        /// <summary>
        /// Date when this order was registered
        /// </summary>
        public DateTime Register { get; set; }
    }
}
