using System;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("ChatManager", "Thias", "0.0.1")]
    [Description("Chat Manager; Moderates your chat and provides useful utilities")]

    public class ChatManager : CovalencePlugin 
    {
        #region permissions
        
        private void Init()
        {
            permission.RegisterPermission("ChatManager.stats", this);
        }
        
        #endregion
        
        #region log

        private readonly DynamicConfigFile _blockedLog = Interface.Oxide.DataFileSystem.GetDatafile("BlockedData");

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
            LogWarning("Trying to whitelisted URLs");
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
        }
        
        private void Loaded()
        {
            LoadBannedWords();
            LoadWhitelistedUrls();
        }
        
        private void Unload()
        {
            Puts("Saving data...");
        }

        #endregion

        #region utils
        
        private static bool IsCommand(string text)
        {
            return text.StartsWith("/");
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

        private void ReportBlockedChat(string playerid)
        {
            if (_blockedLog[playerid] != null)
            {
                if (_blockedLog[playerid, "blocks"].ToString() == "0") _blockedLog[playerid, "blocks"] = 1;
                
                _blockedLog[playerid, "blocks"] = Int64.Parse(_blockedLog[playerid, "blocks"].ToString()) + 1;
            }
            else
            {
                _blockedLog[playerid, "blocks"] = 1;
            }
            _blockedLog.Save();
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
            return "";
        }
        
        #endregion

        object OnUserChat(IPlayer player, string message)
        {
            if (IsCommand($"{message}")) return null;

            var isAllowed = IsAllowedText(message, player.Id);

            if (isAllowed != "")
            {
                Puts($"Blocked chat from player: \"{player.Name}\" with reason: \"{isAllowed}\"");
                player.Reply($"<color={Config["BlockMessageColor"]}>Your message has been blocked with reason: {isAllowed}</color>");
                return false;
            }

            return null;

        }
        
        #region commands

        [Command("cm.stats"), Permission("ChatManager.stats")]
        private void ViewPlayerStatsCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.Reply($"{Prefix} You must specify a player.");
                return;
            }
            
            IPlayer target = covalence.Players.FindPlayer(args[0]);

            if (target == null)
            {
                player.Reply($"{Prefix} Could not find the specified player.");
                return;
            }
            
            if (_blockedLog[target.Id, "blocks"] == null)
            {
                player.Reply($"{Prefix} <color=#32CD32>No Records</color> found for player.");
                return;
            }

            player.Reply($"{Prefix} Found <color=#e63946>{_blockedLog[target.Id, "blocks"]}</color> blocked messages for player.");
        }
        
        #endregion
    }
}