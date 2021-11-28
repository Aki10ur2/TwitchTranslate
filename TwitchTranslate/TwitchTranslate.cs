using System;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using System.Configuration;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;


namespace TwitchTranslate
{
    [DataContract]
    class ApiResult
    {
        [DataMember]
        public string code;
        [DataMember]
        public string text;
    }


    class Program
    {
        static void Main(string[] args)
        {
            Bot bot = new Bot();
            Console.ReadLine();
        }
    }

    class Bot
    {
        TwitchClient twitch_client;
        string GoogleAppsScriptUrl = ConfigurationManager.AppSettings.Get("GoogleAppsScriptUrl");
        string HomeLaunguage = ConfigurationManager.AppSettings.Get("HomeLaunguage");
        string TranslateLaunguage = ConfigurationManager.AppSettings.Get("TranslateLaunguage");

        public Bot()
        {
            string twitch_username = ConfigurationManager.AppSettings.Get("twitch_username");
            string access_token = ConfigurationManager.AppSettings.Get("access_token");

            ConnectionCredentials credentials = new ConnectionCredentials(twitch_username, access_token);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            twitch_client = new TwitchClient(customClient);
            string channel = ConfigurationManager.AppSettings.Get("channel");
            twitch_client.Initialize(credentials, channel);

            //twitch_client.OnLog += Client_OnLog;
            //twitch_client.OnJoinedChannel += Client_OnJoinedChannel;
            twitch_client.OnMessageReceived += Client_OnMessageReceived;
            //twitch_client.OnWhisperReceived += Client_OnWhisperReceived;
            //twitch_client.OnNewSubscriber += Client_OnNewSubscriber;
            //twitch_client.OnConnected += Client_OnConnected;

            twitch_client.Connect();
            Console.WriteLine("Running.");
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Local:Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Local:Client_OnJoinedChannel");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            //if (e.ChatMessage.Message.Contains("badword"))
            //    client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(30), "Bad word! 30 minute timeout!");

            Console.WriteLine("Client_OnMessageReceived");

            //twitch_client.SendMessage(e.ChatMessage.Channel, $"今打たれたのは、{e.ChatMessage.Message}");

            try
            {
                // TODO: ボックス化じゃなくす方法があるか調べる
                var request = (HttpWebRequest)WebRequest.Create($"{GoogleAppsScriptUrl}?text={e.ChatMessage.Message}&target={HomeLaunguage}");
                var response = (HttpWebResponse)request.GetResponse();
                ApiResult info;
                using (response)
                {
                    using (var resStream = response.GetResponseStream())
                    {
                        var serializer = new DataContractJsonSerializer(typeof(ApiResult));
                        info = (ApiResult)serializer.ReadObject(resStream);
                    }
                }

                // TODO: 仮
                if (info.code == "200" && !string.IsNullOrEmpty(info.text))
                {
                    twitch_client.SendMessage(e.ChatMessage.Channel, $"翻訳結果、{info.text}");
                }
                
            }
            catch (Exception error)
            {
                Console.WriteLine("Translate Error.");
            }
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            Console.WriteLine("Local:Client_OnWhisperReceived");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            Console.WriteLine("Local:Client_OnNewSubscriber");
        }
    }
}
