using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using ТелеграммБот.KinopoiskApi.KinoSearch;
using ТелеграммБот.KinopoiskApi.KinoPremiers;

namespace ТелеграммБот
{
    class Program
    {
        private static string token { get; set; }  = "token";
        private static TelegramBotClient bot;
        private static kinoPremieres kinoPremieres;
        private static KinoSearchBot kinoSearch;
        private delegate InlineKeyboardButton FunvKino(int i);

        private static int MessageId;
        private static long ChatId;
        private static string datePast = "";

        private const string botName = "@testJH1_bot";
        private const int len = 8;

        public static async Task Main(string[] args)
        {
            Console.Title = "Telegram Bot";

            bot = new TelegramBotClient(token);

            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery },
                Limit = 50
            };
            try
            {
                bot.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    cancellationToken: cts.Token);
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }

            var me = await bot.GetMeAsync();

            Console.WriteLine($"> Старт для @{me.Username}");
            Console.ReadLine();
            Console.WriteLine("> Stop bot...");

            cts.Cancel();
        }
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
                await MessageAdoption(botClient, update, cancellationToken);
            else if (update.Type == UpdateType.CallbackQuery)
                await CallbackQueryAdoption(botClient, update, cancellationToken);
        }
        private static InlineKeyboardMarkup MessageListButton(int course = 0)
        {
            FunvKino funcKinoPremieres = delegate (int i)
            {
                return InlineKeyboardButton.WithCallbackData(
                    text: $"{kinoPremieres.items[i].nameRu} ({kinoPremieres.items[i].premiereRu})",
                    callbackData: "prf_" + kinoPremieres.items[i].kinopoiskId.ToString());
            };
            FunvKino funcKinoSearch = delegate (int i)
            {
                return InlineKeyboardButton.WithCallbackData(
                    text: $"{kinoSearch.films[i].nameRu} ({kinoSearch.films[i].year})",
                    callbackData: "shf_" + kinoSearch.films[i].filmId.ToString());
            };

            int numb = course,
                lenFilm = (int) Math.Ceiling( (double)kinoPremieres.items.Length / len ) - 1,
                end;

            if (numb < 0)
                numb = 0;
            else if (numb > lenFilm)
                numb = lenFilm;

            end = (numb + 1) * len;
            if (kinoPremieres.items.Length < (numb + 1) * len)
                end = kinoPremieres.items.Length;

            List<InlineKeyboardButton[]> list;

            list = new List<InlineKeyboardButton[]>();

            for (int i = numb * len; i < end; i++)
            {
                InlineKeyboardButton button = InlineKeyboardButton.WithCallbackData(
                    text: $"{kinoPremieres.items[i].nameRu} ({kinoPremieres.items[i].premiereRu})",
                    callbackData: "prf_" + kinoPremieres.items[i].kinopoiskId.ToString());

                list.Add(new[] { button });
            }
            list.Add(
                new[]{
                        InlineKeyboardButton.WithCallbackData( text: $"\U000021A9 назад",  callbackData: "prfButton_" + (numb - 1) ),
                        InlineKeyboardButton.WithCallbackData( text: $"\U000021AA вперед", callbackData: "prfButton_" + (numb + 1) )
                });

            return new InlineKeyboardMarkup(list);
        }
        private static async Task MessageAdoption(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            InlineKeyboardMarkup inlineKeyboard;
            List<InlineKeyboardButton[]> list;

            long chatId = update.Message.Chat.Id;
            int count;
            string[] messageText = update.Message.Text.Split(" ");

            Console.WriteLine($"Пишет '{update.Message.From.FirstName}': '{update.Message.Text}' собщение в чате {chatId}.");

            if (messageText.Length < 1)
                return;

            if (messageText[0].ToLower() == "премьеры")
            {
                if (kinoPremieres is null)
                    kinoPremieres = KinopoiskApi.KinopoiskApi.QueryPremieres(update.Message.Date.Year, update.Message.Date.Month);

                if (kinoPremieres is null)
                    return;

                inlineKeyboard = MessageListButton();

                Message message = await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: $"\U0001F3AD Список кинопремьер в текущем месяце ({1} из { Math.Ceiling((decimal)kinoPremieres.items.Length / len) }):",
                     replyMarkup: inlineKeyboard,
                     cancellationToken: cancellationToken);

                ChatId = chatId;
                MessageId = message.MessageId;
            }
            else if (messageText[0].ToLower() == "поиск" && messageText.Length > 1)
            {
                kinoSearch = KinopoiskApi.KinopoiskApi.SearchFilm(String.Join(" ", messageText, 1, messageText.Length - 1));

                if (kinoSearch is null)
                {
                    Message messageErr = await botClient.SendTextMessageAsync(
                         chatId: chatId,
                         text: $"\U00002757 Неудалось найти фильм «{String.Join(" ", messageText, 1, messageText.Length - 1)}»",
                         cancellationToken: cancellationToken);

                    return;
                }

                count = kinoSearch.films.Length;
                if (count > len)
                    count = len;

                list = new List<InlineKeyboardButton[]>();

                for (byte i = 0; i < count; i++)
                {
                    if (kinoSearch.films[i].filmLength is null)
                        continue;

                    InlineKeyboardButton button = InlineKeyboardButton.WithCallbackData(
                        text: $"{kinoSearch.films[i].nameRu} ({kinoSearch.films[i].year})",
                        callbackData: "shf_" + kinoSearch.films[i].filmId.ToString());

                    list.Add( new[] { button } );
                }

                inlineKeyboard = new InlineKeyboardMarkup(list);

                Message message = await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: $"По названию фильма «{ String.Join(" ", messageText, 1, messageText.Length - 1) }» удалось найти:",
                     replyMarkup: inlineKeyboard,
                     cancellationToken: cancellationToken);
            }
            else if (messageText[0][0] == '/')
            {
                string answer = "";

                switch (messageText[0])
                {
                    case "/start":
                        answer = "\U0001F3AC КиноБот поможет найти фильм или сериал\n\n" +
                            "Для получения информации по командам и возможностям нажмите */help*";
                        break;
                    case "/help":
                        answer = @"*КиноБот может:*" + "\n" +
                            "\U0001F4CC" + @" Найти фильм или сериал по ключевым словам: __поиск фильм__" + "\n" +
                            "\U0001F4CC" + @" Подсказать премьеры в текущем месяце: __премьеры__" + "\n\n" +
                            "\U0001F4DC *Вам доступны следующие команды:*\n" +
                            @" */start* \- знакомство с ботом" + "\n" +
                            @" */help* \- помощь по командам" + "\n" +
                            @" */settings* \- пока недоступна";
                        break;
                    case "/settings ":
                        answer = "\U0001F527 Данная команда пока не готова!";
                        break;
                    default:
                        answer = "Такой команды нет";
                        break;
                }

                Message message = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: answer,
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
            }
        }
        private static async Task CallbackQueryAdoption(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;
            string[] date = update.CallbackQuery.Data.Split("_");

            Console.WriteLine($"Кнопка <CallbackQuery> '{update.CallbackQuery.From.FirstName}' Date {date[1]}: '{update.CallbackQuery.Data}' собщение в чате {chatId}.");

            if (date[0] == "prf") // премьеры
            {
                int idFilm;
                try { idFilm = Convert.ToInt32(date[1]); } catch (FormatException) { return; }

                if (kinoPremieres is null)
                    kinoPremieres = KinopoiskApi.KinopoiskApi.QueryPremieres(update.CallbackQuery.Message.Date.Year, update.CallbackQuery.Message.Date.Month);

                foreach (items film in kinoPremieres.items)
                {
                    if (film.kinopoiskId != idFilm)
                        continue;

                    string caption = $"Фильм «{film.nameRu}»"
                                    + $"\nГод: {film.year}"
                                    + "\nСтрана: " + String.Join(", ", film.countries.Select(x => x.country))
                                    + "\nЖанр: " + String.Join(", ", film.genres.Select(x => x.genre));

                    Message sentMessage = await botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: film.posterUrlPreview,
                        caption: caption,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );

                    break;
                }
            }
            else if (date[0] == "shf") // поиск
            {
                int idFilm;
                try {
                    idFilm = Convert.ToInt32(date[1]);
                }
                catch (FormatException) {
                    return;
                }

                foreach (films film in kinoSearch.films)
                {
                    if (film.filmId != idFilm || film.filmLength is null)
                        continue;

                    string caption = $"Фильм «{film.nameRu}»"
                                    + $"\nГод: {film.year}"
                                    + "\nСтрана: " + String.Join(", ", film.countries.Select(x => x.country))
                                    + "\nЖанр: " + String.Join(", ", film.genres.Select(x => x.genre))
                                    + "\nРейтинг: " + film.rating;

                    Message sentMessage = await botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: film.posterUrlPreview,
                        caption: caption,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );

                    break;
                }
            }
            else if (date[0] == "prfButton")
            {
                if (kinoPremieres is null || date[1] == datePast)
                    return;

                int countItems = (int)Math.Ceiling((decimal)kinoPremieres.items.Length / len),
                        items = Convert.ToInt32(date[1]) + 1;

                InlineKeyboardMarkup inlineKeyboard = MessageListButton(Convert.ToInt32(date[1]));

                if (items > countItems || items < 1)
                    return;

                await botClient.EditMessageTextAsync(
                    chatId: ChatId,
                    messageId: MessageId,
                    text: $"Список кинопремьер в текущем месяце ({ items } из { countItems }):",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken);
                /*
                Message message = await botClient.SendTextMessageAsync(
                     chatId: chatId,
                     text: $"Список кинопремьер в текущем месяце ({len * ( Convert.ToInt32(date[1]) + 1) } из {kinoPremieres.items.Length}):",
                     replyMarkup: inlineKeyboard,
                     cancellationToken: cancellationToken);
                */
                ChatId = chatId;
                datePast = date[1];
                //MessageId = message.MessageId; // update.Message.MessageId;
            }
        }
        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _=> exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
