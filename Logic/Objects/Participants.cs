namespace DiscordBot_G.Logic.Objects
{
    /// <summary>
    /// Participants items
    /// </summary>
    /// <typeparam name="Tvalue"></typeparam>
    internal class Participants<Tvalue> : List<Tvalue>, IDisposable
    {
        /// <summary>
        /// List with participants
        /// </summary>
        internal List<Link> Links = new List<Link>();
        /// <summary>
        /// Main constructor
        /// </summary>
        internal Participants() : base() { }
        Participants(int id) : base(id) { }
        /// <summary>
        /// Custom event
        /// </summary>
        internal event EventHandler ValueChanged;
        internal void OnValueChanged(object sender, EventArgs e)
        {
            EventHandler handler = ValueChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
        /// <summary>
        /// Add item to links and trigger custom event
        /// </summary>
        /// <param name="link"></param>
        internal void AddItem(Link link) { try { Links.Add(link); OnValueChanged(this, EventArgs.Empty); } catch { throw; } }
        /// <summary>
        /// Remove item from links and trigger custom event
        /// </summary>
        /// <param name="link"></param>
        internal void RemoveItem(Link link) { try { Links.Remove(link); OnValueChanged(this, EventArgs.Empty);} catch { throw; } }
        /// <summary>
        /// Return <see langword="true"/> if this participant already exist in links; otherwise <see langword="false"/>
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        internal bool isExist(ulong userID) => Links.Any(x => x.UserID == userID);
        /// <summary>
        /// Return first element of links
        /// </summary>
        /// <returns></returns>
        internal Link GetFirst() => Links[0];
        /// <summary>
        /// Returns an integer that represents the number of elements in links.
        /// </summary>
        /// <returns></returns>
        internal int GetCount() => Links.Count;
        /// <summary>
        /// Return the total chance for all elements by links
        /// </summary>
        /// <returns></returns>
        internal int GetPool() => Links.Sum(x => x.Chance);
        /// <summary>
        /// Return links
        /// </summary>
        /// <returns></returns>
        internal List<Link> GetLinks() => Links;
        /// <summary>
        /// Change chance for all participants by winner
        /// </summary>
        /// <param name="winnerID"></param>
        internal void Winner(ulong winnerID)
        {
            foreach (var i in Links)
                if (i.UserID == winnerID)
                    i.Chance = 0;
                else
                    i.Chance++;
        }
        /// <summary>
        /// Return specified link by participant ID
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        internal Link GetLinkByUserID(ulong userID) => Links.First(x => x.UserID == userID);
        /// <summary>
        /// Release all resorces
        /// </summary>
        public void Dispose() => Links.Clear();
    }
}
