using DiscordBot_G.Logic;
using DiscordBot_G.Logic.Objects;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace DiscordBot_G.Core
{
    internal class HttpServer
    {
        /// <summary>
        /// Listening port
        /// </summary>
        public int Port = 8080;
        /// <summary>
        /// Http listener
        /// </summary>
        private HttpListener? _listener;
        /// <summary>
        /// Initialize Http listener object
        /// </summary>
        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + Port.ToString() + "/");
            _listener.Start();

            Receive();
        }
        /// <summary>
        /// Stop listener
        /// </summary>
        public void Stop() => _listener.Stop();
        /// <summary>
        /// Start async listening
        /// </summary>
        private void Receive() => _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        /// <summary>
        /// Get POST request and POST data validation
        /// </summary>
        /// <param name="result"></param>
        private void ListenerCallback(IAsyncResult result)
        {
            if (_listener.IsListening)
            {
                var context = _listener.EndGetContext(result);
                var request = context.Request;

                #region Body
                Console.WriteLine($"{request.Url}");

                using StreamReader stream = new(request.InputStream);
                string postData = stream.ReadToEnd();

                var response = context.Response;

                if (Regex.IsMatch(postData,
                    @"^token=([a-zA-Z0-9]{8}\-[a-zA-Z0-9]{4}\-[a-zA-Z0-9]{4}\-[a-zA-Z0-9]{4}\-[a-zA-Z0-9]{12})&channelid=\d+&embed=([a-zA-Z0-9\D]+)",
                    RegexOptions.IgnoreCase))
                {
                    try
                    {
                        Dictionary<string, string> POSTData = postData.Split(new char[] { '&' })
                           .Select(part => part.Split(new char[] { '=' }))
                           .ToDictionary(split => split[0].ToLower(), split => HttpUtility.UrlDecode(split[1]));

                        string token = POSTData["token"];
                        ulong channelID = Convert.ToUInt64(POSTData["channelid"]);
                        string json = POSTData["embed"];

                        DiscordEmbed? discordEmbed = new(); string? message = string.Empty;

                        if (!Bot.isExist(token))
                            throw new Exception("Invalid token");
                        else if (!Bot.isExist(token, channelID))
                            throw new Exception("Invalid channel ID");
                        else if (!Bot.isExist(json, out discordEmbed, out message))
                            throw new Exception(message);

                        bool haveItems = false;
                        if (POSTData.Count > 3)
                        {
                            haveItems = true;

                            POSTData.Remove("token");
                            POSTData.Remove("channelid");
                            POSTData.Remove("embed");
                        }

                        if (haveItems)
                            Bot.RegisterOrder(token, channelID, discordEmbed, POSTData);
                        else
                            Bot.RegisterOrder(token, channelID, discordEmbed, null);

                        // In this point all data is valid
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/plain";
                        response.OutputStream.Write(new byte[] { }, 0, 0);
                        response.OutputStream.Close();
                    }
                    catch (Exception ex)
                    {
                        ResponseBadRequest(context.Response, ex.Message);
                    }
                }
                else
                {
                    ResponseBadRequest(context.Response, "Invalid format");
                }
                #endregion

                Receive();

                // Local functions for response on bad requset
                void ResponseBadRequest(HttpListenerResponse response, string message)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.ContentType = "text/plan";
                    byte[] invalidFormat = Encoding.UTF8.GetBytes(message);
                    response.OutputStream.Write(invalidFormat, 0, invalidFormat.Length);
                    response.OutputStream.Close();
                }
            }
        }
    }
}
