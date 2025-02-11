﻿using HelpSense.API.Events;
using HelpSense.API.Features.Pool;
using HintServiceMeow.Core.Utilities;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HelpSense.Helper.Chat
{
    public class ChatMessage(Player sender, ChatMessage.MessageType type, string message)
    {
        public enum MessageType
        {
            /// <summary>
            /// Chat privately with admins
            /// </summary>
            AdminPrivateChat,
            /// <summary>
            /// Chat with all players
            /// </summary>
            BroadcastChat,
            /// <summary>
            /// Chat with all teammates
            /// </summary>
            TeamChat,
        }

        public DateTime TimeSent { get; } = DateTime.Now;

        public MessageType Type { get; } = type;
        public string Message { get; } = message;

        public string SenderName { get; } = sender.DisplayName;
        public Team SenderTeam { get; } = sender.Team;
        public RoleTypeId SenderRole { get; } = sender.Role;
    }

    public static class ChatHelper
    {
        private static CoroutineHandle _coroutine;

        private static readonly LinkedList<ChatMessage> MessageList = new();

        private static readonly Dictionary<Player, HintServiceMeow.Core.Models.Hints.Hint> MessageSlot = [];

        private static bool HaveAccess(Player player, ChatMessage message)
        {
            if ((DateTime.Now - message.TimeSent).TotalSeconds > CustomEventHandler.Config.MessageTime)
                return false;

            return message.Type switch
            {
                ChatMessage.MessageType.AdminPrivateChat => player.RemoteAdminAccess,
                ChatMessage.MessageType.BroadcastChat => true,
                ChatMessage.MessageType.TeamChat => player.Team == message.SenderTeam,
                _ => false,
            };
        }

        private static IEnumerator<float> MessageCoroutineMethod()
        {
            while (true)
            {
                var sb = StringBuilderPool.Pool.Get();

                foreach (var messageSlot in MessageSlot)
                {
                    if (!MessageList.Any(x => HaveAccess(messageSlot.Key, x)))
                    {
                        messageSlot.Value.Text = string.Empty;
                        continue;
                    }

                    sb.AppendLine(CustomEventHandler.TranslateConfig.ChatMessageTitle);

                    foreach (var message in MessageList)
                    {
                        if (HaveAccess(messageSlot.Key, message))
                        {
                            string messageStr = CustomEventHandler.Config.MessageTemplate
                                .Replace("{Message}", message.Message)
                                .Replace("{MessageType}", CustomEventHandler.TranslateConfig.MessageTypeName[message.Type])
                                .Replace("{MessageTypeColor}", message.Type switch
                                {
                                    ChatMessage.MessageType.AdminPrivateChat => "red",
                                    _ => "{SenderTeamColor}",//Replace by sender's team color later
                                })
                                .Replace("{SenderNickname}", message.SenderName)
                                .Replace("{SenderTeam}", CustomEventHandler.TranslateConfig.ChatSystemTeamTranslation[message.SenderTeam])
                                .Replace("{SenderRole}", CustomEventHandler.TranslateConfig.ChatSystemRoleTranslation[message.SenderRole])
                                .Replace("{SenderTeamColor}", message.SenderTeam switch
                                {
                                    Team.SCPs => "red",
                                    Team.ChaosInsurgency => "green",
                                    Team.Scientists => "yellow",
                                    Team.ClassD => "orange",
                                    Team.Dead => "white",
                                    Team.FoundationForces => "#4EFAFF",
                                    _ => "white"
                                })
                                .Replace("{CountDown}", (CustomEventHandler.Config.MessageTime - (int)(DateTime.Now - message.TimeSent).TotalSeconds).ToString());


                            sb.AppendLine(messageStr);
                        }
                    }

                    messageSlot.Value.Text = sb.ToString();
                    sb.Clear();
                }

                yield return Timing.WaitForSeconds(0.5f);
            }
        }

        public static void InitForPlayer(Player player)
        {
            if (!_coroutine.IsRunning)
                _coroutine = Timing.RunCoroutine(MessageCoroutineMethod());

            if (MessageSlot.ContainsKey(player))
            {
                return;
            }

            MessageSlot[player] = new HintServiceMeow.Core.Models.Hints.Hint
            {
                Alignment = HintServiceMeow.Core.Enum.HintAlignment.Left,
                YCoordinate = 250,
                FontSize = CustomEventHandler.Config.ChatSystemSize,
                LineHeight = 5
            };

            PlayerDisplay.Get(player.ReferenceHub).AddHint(MessageSlot[player]);
        }

        public static void SendMessage(Player sender, ChatMessage.MessageType type, string message) => SendMessage(new ChatMessage(sender, type, message));

        public static void SendMessage(ChatMessage message)
        {
            MessageList.AddFirst(message);
        }
    }
}
