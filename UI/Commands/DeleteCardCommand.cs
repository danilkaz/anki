using System.Linq;
using System.Threading.Tasks;
using AnkiBot.App;
using AnkiBot.Domain;
using App;
using UI;
using UI.Dialogs;

namespace AnkiBot.UI.Commands
{
    public class DeleteCardCommand : Command
    {
        private readonly IRepository repository;
        private readonly Converter converter;

        public DeleteCardCommand(IRepository repository, Converter converter)
        {
            this.repository = repository;
            this.converter = converter;
        }

        public override string Name => "Удалить карточку";

        public override async Task<IDialog> Execute(User user, string message, IBot bot)
        {
            var decksNames = repository.GetDecksByUser(user).Select(converter.ToUiDeck);
            if (!decksNames.Any())
            {
                await bot.SendMessage(user, "У вас нет ни одной колоды. Сначала создайте ее", false);
                return null;
            }

            var decksKeyboard = decksNames
                .Select(d => new[] {d.Name})
                .ToArray();
            await bot.SendMessageWithKeyboard(user, "Выберите колоду:", new KeyboardProvider(decksKeyboard));
            return new DeleteCardDialog(repository, converter);
        }
    }
}