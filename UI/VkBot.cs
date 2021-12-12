using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnkiBot.UI.Commands;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace UI
{
    public class VkBot : Bot
    {
        private readonly VkApi api;

        public VkBot(VkApi api, ICommand[] commands) : base(commands)
        {
            this.api = api;
        }

        public override async void Start()
        {
            while (true)
            {
                var s = await api.Groups.GetLongPollServerAsync(184492586);
                var poll = await api.Groups.GetBotsLongPollHistoryAsync(
                    new BotsLongPollHistoryParams
                        {Server = s.Server, Ts = s.Ts, Key = s.Key, Wait = 25});
                if (poll?.Updates == null)
                    continue;
                foreach (var update in poll.Updates.Where(u => u.Type == GroupUpdateType.MessageNew))
                {
                    var userMessage = update.Message.Text.ToLower();
                    var userId = update.Message.FromId;

                    await api.Messages.MarkAsReadAsync(userId.Value.ToString());
                    await HandleTextMessage(userId.Value, userMessage);
                }
            }
        }

        public override async Task SendMessage(long chatId, string text, bool clearKeyboard = true)
        {
            var keyboard = new KeyboardBuilder().Build();
            if (!clearKeyboard)
                keyboard = null;
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = chatId,
                Message = text,
                Keyboard = keyboard,
                RandomId = new Random().Next()
            });
        }

        public override async Task SendMessageWithKeyboard(long chatId, string text,
            IEnumerable<IEnumerable<string>> labels)
        {
            var keyboard = MakeKeyboard(labels);
            await api.Messages.SendAsync(new MessagesSendParams
            {
                PeerId = chatId,
                Message = text,
                Keyboard = keyboard,
                RandomId = new Random().Next()
            });
        }

        private static MessageKeyboard MakeKeyboard(IEnumerable<IEnumerable<string>> labels)
        {
            var buttons = labels.Select(
                b => b.Select(label =>
                    new MessageKeyboardButton
                    {
                        Action = new MessageKeyboardButtonAction
                        {
                            Type = KeyboardButtonActionType.Text,
                            Label = label
                        },
                        Color = KeyboardButtonColor.Primary
                    }));
            return new MessageKeyboard {Buttons = buttons};
        }
    }
}