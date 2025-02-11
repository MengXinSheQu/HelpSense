﻿using CommandSystem;
using HelpSense.Helper.Chat;
using HelpSense.ConfigSystem;
using LabApi.Features.Wrappers;
using System;

using Log = LabApi.Features.Console.Logger;
using HelpSense.API.Events;

namespace HelpSense.Commands.ChatCommand
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class AcCommand : ICommand
    {
        public string Command => "AC";

        public string[] Aliases => [];

        public string Description => "私聊管理-Talk to Admin";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            CommandTranslateConfig CommandTranslateConfig = CustomEventHandler.CommandTranslateConfig;
            Player player;

            if (sender is null || (player = Player.Get(sender)) is null)
            {
                response = CommandTranslateConfig.ChatCommandError;
                return false;
            }

            if (arguments.Count == 0 || player.IsMuted || !CustomEventHandler.Config.EnableAcSystem)
            {
                response = CommandTranslateConfig.ChatCommandFailed;
                return false;
            }

            ChatHelper.SendMessage(player, ChatMessage.MessageType.AdminPrivateChat, $"<noparse>{string.Join(" ", arguments)}</noparse>");

            Log.Info(player.Nickname + " 发送了 " + arguments.At(0));
            response = CommandTranslateConfig.ChatCommandOk;
            return true;
        }
    }
}
