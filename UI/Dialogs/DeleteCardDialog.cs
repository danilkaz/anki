using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnkiBot.App;
using AnkiBot.Domain;
using App;

namespace UI.Dialogs
{
    public class DeleteCardDialog : IDialog
    {
        private readonly IRepository repository;
        private readonly Converter converter;
        private IEnumerable<Card> cards;

        private string deckId;
        private State state = State.ChooseDeck;

        public DeleteCardDialog(IRepository repository, Converter converter)
        {
            this.repository = repository;
            this.converter = converter;
        }

        public async Task<IDialog> Execute(User user, string message, IBot bot)
        {
            switch (state)
            {
                case State.ChooseDeck:
                {
                    var decks = repository.GetDecksByUser(user).Select(converter.ToDeck);
                    var findDeck = decks.FirstOrDefault(deck => deck.Name == message);
                    if (findDeck is null)
                    {
                        await bot.SendMessage(user, "Выберите колоду:", false);
                        return this;
                    }

                    deckId = findDeck.Id.ToString();
                    cards = findDeck.Cards;
                    if (!cards.Any())
                    {
                        await bot.SendMessage(user, "Колода пуста", false);
                        return null;
                    }

                    var cardsKeyboard = cards.Select(c => new[] {c.Front + "\n" + c.Id}).ToArray();
                    state = State.ChooseCard;
                    await bot.SendMessageWithKeyboard(user, "Выберите карту:", new KeyboardProvider(cardsKeyboard));
                    return this;
                }
                case State.ChooseCard:
                {
                    var splitMessage = message.Split('\n');
                    var card = cards.FirstOrDefault(c => c.Id.ToString() == splitMessage.Last());
                    if (card is null)
                    {
                        await bot.SendMessage(user, "Выберите карту:", false);
                        return this;
                    }

                    repository.DeleteCard(card.Id.ToString());
                    await bot.SendMessage(user, "Карта успешно удалена", false);
                    return null;
                }
                default:
                    return null;
            }
        }

        private enum State
        {
            ChooseDeck,
            ChooseCard
        }
    }
}