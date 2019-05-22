﻿using D2NG;
using Serilog;
using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using D2NG.BNCS.Packet;
using System.Linq;
using D2NG.MCP;

namespace ConsoleBot
{
    class Program
    {
        static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Config File", LongName = "config", ShortName = "c")]
        [Required]
        public String ConfigFile { get; }

        private readonly Client Client = new Client();

        private Config Config;

        private void OnExecute()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("log.txt")
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            Log.Debug("Starting bot");
            
            Config = Config.FromFile(this.ConfigFile);

            Client.Bncs.OnReceivedPacketEvent(Sid.CHATEVENT, HandleChatEvent);

            try {
                var realm = Prompt.GetString($"Realm:", Config.Realm, ConsoleColor.Red);

                Client.Connect(realm, Config.ClassicKey, Config.ExpansionKey);

                var username = Prompt.GetString("Username: ", Config.Username, ConsoleColor.Red);
                var password = Prompt.GetPassword("Password: ", ConsoleColor.Red);
                
                Client.Login(username, password);

                var mcpRealm = SelectMcpRealm();
                Client.McpLogon(mcpRealm);

                var selectedChar = SelectCharacter();

                Client.SelectCharacter(selectedChar);

            }
            catch (Exception e)
            {
                Log.Error(e, "Unhandled Exception");
            }
        }

        private string SelectMcpRealm()
        {
            return Client.ListMcpRealms().First().Name;
        }

        private Character SelectCharacter()
        {
            var characters = Client.ListCharacters();

            var charsPrompt = characters.Select((c, index) => $"{index + 1}. {c.Name} - Level {c.Level} {(CharacterClass)c.CharacterClass}")
                .Aggregate((i, j) => i + "\n" + j);

            var charSelection = Prompt.GetInt($"Select Character:\n{charsPrompt}\n", 1, ConsoleColor.Red) - 1;

            return characters[charSelection];
        }
             
        private static void HandleChatEvent(BncsPacket obj)
        {
            _ = new ChatEventPacket(obj.Raw);
        }
    }
}
