using System;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Net;

namespace Oxide.Plugins
{
    [Info("ChatManager", "Thias", "0.0.3")]
    [Description("Chat Manager; Moderates your chat and provides useful utilities")]

    public class ChatManager : CovalencePlugin 
    {
        #region permissions
        
        private void Init()
        {
            permission.RegisterPermission("ChatManager.stats", this);
            permission.RegisterPermission("ChatManager.ban", this);
            permission.RegisterPermission("ChatManager.karma", this);
        }
        
        #endregion
        
        #region log

        private readonly DynamicConfigFile _data = Interface.Oxide.DataFileSystem.GetDatafile("ChatManager");

        #endregion
        
        #region config

        private readonly List<string> _bannedWords = new List<string>();
        private readonly List<string> _whitelistedUrls = new List<string>();
        private const string Prefix = "[<color=#f4a261>ChatManager</color>]:";

        private void LoadBannedWords()
        {
            LogWarning("Trying to load banned words.");
            if (Config["BlockedWords"] == null || Config["BlockedWords"].ToString() == "") return;

            try
            {
                for (var i = 0; i < (Config["BlockedWords"] as List<object>).Count; i++)
                {
                    _bannedWords.Add((Config["BlockedWords"] as List<object>)[i].ToString());
                }
            }
            catch (NullReferenceException)
            {
                LogError("Could not load BlockedWords");
            }
            
        }

        private void LoadWhitelistedUrls()
        {
            LogWarning("Trying load to whitelisted URLs");
            if (Config["WhitelistedURLS"] == null || Config["WhitelistedURLS"].ToString() == "") return;

            try
            {
                for (var i = 0; i < (Config["WhitelistedURLS"] as List<object>).Count; i++)
                {
                    _whitelistedUrls.Add((Config["WhitelistedURLS"] as List<object>)[i].ToString());
                }
            }
            catch (NullReferenceException)
            {
                LogError("Could not load whitelisted URLs");
            }
        }

        // Load default config if no config is found
        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
            Config["BlockMessage"] = "Your message has been blocked due to a banned word.";
            Config["BlockMessageColor"] = "#e63946";
            Config["BlockedWords"] = new List<string>();
            Config["WhitelistedURLS"] = new List<string>();
            Config["Karma", "minimum"] = -50;
            Config["Karma", "WordWeight"] = new {};
        }
        
        private void Loaded()
        {
            LoadBannedWords();
            LoadWhitelistedUrls();
        }
        
        private void Unload()
        {
            Puts("Saving data...");
            _data.Save();
        }

        #endregion

        #region utils
        
        private static bool IsCommand(string text)
        {
            return text.StartsWith("/");
        }

        private int ContainsWeightedWord(string text)
        {
            var words = text.Split(' ');
            var foundWordValue = 0;
            Dictionary<string, object> dict = (Dictionary<string, object>) Config["Karma", "WordWeight"];
            
            foreach (var word in words)
            {
                if (dict.ContainsKey(word)) foundWordValue = (int) Config["Karma", "WordWeight", word];
            }

            Puts(foundWordValue.ToString());
            return foundWordValue;
        }
        
        private bool ContainsBlockedWord(string text)
        {
            var words = text.Split(' ');
            var foundBlockedWord = false;
            foreach (var word in words)
            {
                if (_bannedWords.Contains(word)) foundBlockedWord = true;
            }

            return foundBlockedWord;
        }

        private bool ContainsUrl(string text)
        {
            var words = text.Split(' ');
            var containedUrl = false;
            foreach (var word in words)
            {
                if (_whitelistedUrls.Contains(word)) continue;
                if (Uri.IsWellFormedUriString(word, UriKind.Absolute)) containedUrl = true;
            }
            
            return containedUrl;
        }

        private bool CheckPlayerKarma(string playerid)
        {
            return (int) _data[playerid, "karma"] <= (int) Config["Karma", "minimum"];
        }
        
        private void ReportBlockedChat(string playerid)
        {
            if (_data[playerid, "blocks"] != null)
            {
                if (_data[playerid, "blocks"].ToString() == "0") _data[playerid, "blocks"] = 1;
                
                _data[playerid, "blocks"] = (int) _data[playerid, "blocks"] + 1;
            }
            else
            {
                _data[playerid, "blocks"] = 1;
            }
            _data.Save();
        }

        private string IsAllowedText(string text, string playerid)
        {
            if (ContainsBlockedWord(text.ToLower()))
            {
                ReportBlockedChat(playerid);
                return "Blacklisted Word";
            }

            if (ContainsUrl(text))
            {
                ReportBlockedChat(playerid);
                return "Contains URL";
            }

            if (CheckPlayerKarma(playerid))
            {
                ReportBlockedChat(playerid);
                return "Too low karma";
            }
            return "";
        }

        private string IsMutedOrBanned(string playerid)
        {
            var bannedStatus = _data[playerid, "banned"];
            if (bannedStatus == null)
            {
                Puts("Player not banned");
                return "";
            }

            if (bannedStatus.ToString() == "True")
            {
                return "Banned";
            }
               
            return "";
        }

        private object GetValidPlayer(string[] args, IPlayer player)
        {
            if (args.Length == 0)
            {
                player.Reply($"{Prefix} You must specify a player.");
                return false;
            }
            
            IPlayer target = covalence.Players.FindPlayer(args[0]);

            if (target == null)
            {
                player.Reply($"{Prefix} Could not find the specified player.");
                return false;
            }

            return target;
        }

        private void ReplyBlockedWithReason(IPlayer player, string reason)
        {
            Puts($"Blocked chat from player: \"{player.Name}\" with reason: \"{reason}\"");
            player.Reply($"<color={Config["BlockMessageColor"]}>Your message has been blocked with reason: {reason}</color>");
        }

        private void SetDefaultKarma(IPlayer player)
        {
            if (_data[player.Id, "karma"] != null) return;
            
            _data[player.Id, "karma"] = 0;
            _data.Save();
        }
        
        private void ControlKarma(IPlayer player, string action, int amount)
        {
            switch (action)
            {
                case "increase":
                    _data[player.Id, "karma"] = (int) _data[player.Id, "karma"] + amount;
                    _data.Save();
                    break;
                case "decrease":
                    _data[player.Id, "karma"] = (int) _data[player.Id, "karma"] - amount;
                    _data.Save();
                    break;
                default:
                    Puts("Invalid karma action");
                    break;
            }
        }
        
        #endregion

        #region hooks
        
        object OnUserChat(IPlayer player, string message)
        {
            if (IsCommand($"{message}")) return null;

            var isMutedOrBanned = IsMutedOrBanned(player.Id);

            var isAllowed = IsAllowedText(message, player.Id);

            var weightedWord = ContainsWeightedWord(message);
            if (weightedWord != 0)
            {
                var positive = weightedWord > 0;
                Puts(positive.ToString());
                ControlKarma(player, positive ? "increase" : "decrease", positive ? weightedWord : weightedWord * -1);
            }
            
            if (isAllowed != "")
            {
                ReplyBlockedWithReason(player, isAllowed);
                
                // If karma was not already adjusted
                if (weightedWord == 0) ControlKarma(player, "decrease", 1);
                
                return false;
            }
            if (isMutedOrBanned != "")
            {
                ReplyBlockedWithReason(player, isMutedOrBanned);
                return false;
            }

            return null;

        }
        
        void OnUserConnected(IPlayer player)
        {
            SetDefaultKarma(player);
        }

        #endregion

        #region commands

        [Command("cm.karma.reset"), Permission("ChatManager.karma")]
        private void ResetPlayerKarma(IPlayer player, string command, string[] args)
        {
            var iPlayerObj = GetValidPlayer(args, player);
            if (iPlayerObj is bool) return;
            var target = (IPlayer) iPlayerObj;

            _data[player.Id, "karma"] = 0;
            _data.Save();
            
            player.Reply($"{Prefix} Reset karma for player: <color=#32CD32>{target.Name}</color>");
        }
        
        [Command("cm.ban"), Permission("ChatManager.ban")]
        private void BanPlayerCommand(IPlayer player, string command, string[] args)
        {
            var iPlayerObj = GetValidPlayer(args, player);
            if (iPlayerObj is bool) return;
            var target = (IPlayer) iPlayerObj;
            
            if ((bool) _data[player.Id, "banned"])
            {
                player.Reply($"{Prefix} Player: <color={Config["BlockMessageColor"]}>{target.Name}</color> - Is already banned from chat");
                return;
            }
            
            _data[player.Id, "banned"] = true;
            _data.Save();
            
            ControlKarma(player, "decrease", 5);
            
            player.Reply($"{Prefix} Player: <color={Config["BlockMessageColor"]}>{target.Name}</color> - Has been banned from the chat");
        }

        [Command("cm.unban"), Permission("ChatManager.ban")]
        private void UnbanPlayerCommand(IPlayer player, string command, string[] args)
        {
            var iPlayerObj = GetValidPlayer(args, player);
            if (iPlayerObj is bool) return;
            var target = (IPlayer) iPlayerObj;

            if (!(bool) _data[player.Id, "banned"])
            {
                player.Reply($"{Prefix} Player: <color=#32CD32>{target.Name}</color> - Is not banned from chat");
                return;
            }
            
            _data[player.Id, "banned"] = false;
            _data.Save();
            
            ControlKarma(player, "increase", 2);
            
            player.Reply($"{Prefix} Player: <color=#32CD32>{target.Name}</color> - Has been unbanned from the chat");
        }
        
        [Command("cm.stats"), Permission("ChatManager.stats")]
        private void ViewPlayerStatsCommand(IPlayer player, string command, string[] args)
        {
            var iPlayerObj = GetValidPlayer(args, player);
            if (iPlayerObj is bool) return;
            var target = (IPlayer) iPlayerObj;
            
            if (_data[target.Id, "blocks"] == null)
            {
                player.Reply($"{Prefix} <color=#32CD32>No Records</color> found for player.");
            }
            else
            {
                player.Reply($"{Prefix} Found <color=#e63946>{_data[target.Id, "blocks"]}</color> blocked messages for player.");
            }

            if (_data[target.Id, "banned"] == null || _data[target.Id, "banned"].ToString() == "False")
            {
                player.Reply($"{Prefix} Player is currently <color=#32CD32>not banned</color>.");
            }
            else
            {
                player.Reply($"{Prefix} Player is currently <color={Config["BlockMessageColor"]}>banned</color>.");
            }

            return;
        }
        
        #endregion
    }
}