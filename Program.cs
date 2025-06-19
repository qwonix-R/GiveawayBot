using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using SQLitePCL;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using static TgBot1.UserStateRepository;
using static System.Net.Mime.MediaTypeNames;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Security.AccessControl;
using Polly.Caching;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bots.Http;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System.Threading.Channels;
using System.Linq;
using Polly;
using Telegram.Bots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Eventing.Reader;
using System.Data;
using TgBot1.commands;





namespace TgBot1
{

    class Program
    {
        

        public static readonly UserStateRepository _stateRepository = new();
        private static Timer? _timerPost;
        private static Timer? _timerPoll;
        private static Timer? _timerBroad;

        public static string photosDirectory = "/giveawaybot/assets";

        #region ----------------------------------------------------------UPDATE HANDLER------------------------------------------------------
        private static async Task UpdateHandler(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            var admDB = new AdminContext();

            List<long> adminIds = admDB.Admins.Select(u => u.UserId).ToList();
            
                try
                {
                    

                    switch (update.Type)
                    {
                        case Telegram.Bot.Types.Enums.UpdateType.Message:
                        {
                            
                            var message = update.Message;
                            if (message.Chat.Type == ChatType.Private)
                            {

                                // From - это от кого пришло сообщение
                                var user = message.From;
                                // Состояние диалога (этап)
                                var userData = _stateRepository.GetUserData(user.Id);

                                // Выводим на экран то, что пишут нашему боту, а также небольшую информацию об отправителе
                                string currentTime = message.Date.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
                                Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение в {currentTime}: {message.Text}");

                                // Chat - содержит всю информацию о чате
                                var chat = message.Chat;
                                if (adminIds.Contains(update.Message.From.Id))
                                {
                                    switch (message.Type)
                                    {
                                        case MessageType.Text:
                                            {

                                                var text = message.Text;
                                                if (text.ToLower() == "/назад")
                                                {
                                                    _stateRepository.UpdateUserState(user.Id, UserState.Idle);
                                                  
                                                    var replyKeyboard = new ReplyKeyboardMarkup(
                                                        new List<KeyboardButton[]>()
                                                        {
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Пост"),new KeyboardButton("/Рассылка")
                                                                    //new KeyboardButton("/Редактировать Пост??"),
                                                                },
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Админ"),
                                                                    
                                                                },
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Назад")
                                                                }
                                                        })
                                                    {


                                                        ResizeKeyboard = true,  // автоматическое изменение размера клавиатуры
                                                    };

                                                    await botClient.SendMessage(
                                                        chat.Id,
                                                        "Вы вернулись в меню.",
                                                        replyMarkup: replyKeyboard);
                                                }
                                                else
                                                {
                                                    switch (userData.CurrentState)
                                                    {
                                                        case UserState.Idle:
                                                            {
                                                                if (text == "/start")
                                                                {
                                                                    // Тут все аналогично Inline клавиатуре, только меняются классы
                                                                    // НО! Тут потребуется дополнительно указать один параметр, чтобы
                                                                    // клавиатура выглядела нормально, а не как абы что
                                                                    _stateRepository.UpdateUserState(user.Id, UserState.Idle);
                                                                    _stateRepository.ClearTempData(user.Id);



                                                                    var replyKeyboard = new ReplyKeyboardMarkup(
                                                                        new List<KeyboardButton[]>()
                                                                        {
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Пост"),new KeyboardButton("/Рассылка")
                                                                    //new KeyboardButton("/Редактировать Пост??"),
                                                                },
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Админ"),
                                                                    
                                                                },
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Назад")
                                                                }
                                                                        })
                                                                    {


                                                                        ResizeKeyboard = true,  // автоматическое изменение размера клавиатуры
                                                                    };

                                                                    await botClient.SendMessage(
                                                                        chat.Id,
                                                                        "Для подписки на рассылку нажмите /подписаться!",
                                                                        replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup


                                                                }
                                                                else if (text.ToLower() == "/подписаться")
                                                                {
                                                                    await Subscribe(chat.Id, cancellationToken, user.Id, user.FirstName, botClient);
                                                                }
                                                                else if (text.ToLower() == "/пост")
                                                                {
                                                                    await PostMenuKeyboard(botClient, chat.Id);
                                                                    //await StartNewPostCreation(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                }
                                                                else if (text.ToLower() == "/админ")
                                                                {
                                                                    await AddAdmin(chat.Id, cancellationToken, user.Id, user.Username, botClient, text, admDB);

                                                                }
                                                                else if (text.ToLower() == "/назад")
                                                                {
                                                                    _stateRepository.UpdateUserState(user.Id, UserState.Idle);
                                                                    var replyKeyboard = new ReplyKeyboardMarkup(
                                                                        new List<KeyboardButton[]>()
                                                                        {
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Пост"),new KeyboardButton("/Рассылка")
                                                                    //new KeyboardButton("/Редактировать Пост??"),
                                                                },
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Админ"),
                                                                    
                                                                },
                                                                new KeyboardButton[]
                                                                {
                                                                    new KeyboardButton("/Назад")
                                                                }
                                                                        })
                                                                    {


                                                                        ResizeKeyboard = true,  // автоматическое изменение размера клавиатуры
                                                                    };

                                                                    await botClient.SendMessage(
                                                                        chat.Id,
                                                                        "Вы вернулись в меню.",
                                                                        replyMarkup: replyKeyboard);
                                                                    
                                                                }
                                                                else if (text.ToLower() == "/рассылка")
                                                                {
                                                                    await BroadMenuKeyboard(botClient, chat.Id);
                                                                }


                                                                
                                                            }
                                                            return;
                                                        case UserState.PostMenu:
                                                            {
                                                                //await PostMenuKeyboard()
                                                                switch (text.ToLower())
                                                                {
                                                                    case "/создать":
                                                                        {
                                                                            await PostCommands.StartNewPostCreation(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                            return;
                                                                        }
                                                                    case "/редактировать":
                                                                        {
                                                                            await PostCommands.StartEditingPost(user.Id, botClient);

                                                                            return;
                                                                        }
                                                                    case "/удалить":
                                                                        {
                                                                            await PostCommands.StartCancellingPost(user.Id, botClient);

                                                                            return;
                                                                        }
                                                                    case "/отправить":
                                                                        {
                                                                            await PostCommands.StartInstaPost(user.Id, botClient);

                                                                            return;
                                                                        }
                                                                    case "/завершить":
                                                                        {
                                                                            

                                                                            return;
                                                                        }

                                                                }



                                                                return;
                                                            }
                                                        case UserState.BroadMenu:
                                                            {
                                                                //await PostMenuKeyboard()
                                                                switch (text.ToLower())
                                                                {
                                                                    case "/создать":
                                                                        {
                                                                            await BroadCommands.StartBroadcastCreation(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                            return;
                                                                        }
                                                                    case "/редактировать":
                                                                        {
                                                                            await BroadCommands.StartBroadEdition(user.Id, botClient);

                                                                            return;
                                                                        }
                                                                    case "/удалить":
                                                                        {
                                                                            await BroadCommands.StartBroadCancellation(user.Id, botClient);

                                                                            return;
                                                                        }
                                                                    case "/отправить":
                                                                        {
                                                                            await BroadCommands.StartInstaBroad(user.Id, botClient);

                                                                            return;
                                                                        }
                                                                    

                                                                }



                                                                return;
                                                            }
                                                        case UserState.AwaitingText:
                                                            {
                                                                await PostCommands.SaveTextFromAnswer(chat.Id, cancellationToken, user.Id, text, botClient);




                                                                return;
                                                            }
                                                        case UserState.AwaitingTextBroad:
                                                            {
                                                                await BroadCommands.SaveBroadText(chat.Id, cancellationToken, user.Id, text, botClient);




                                                                return;
                                                            }
                                                        case UserState.AwaitingPhoto:
                                                            {
                                                                if (text.ToLower() == "далее")
                                                                {
                                                                    _stateRepository.UpdateUserState(user.Id, UserState.AwaitingWinners);
                                                                    await botClient.SendMessage(chat.Id, "Отправьте количество победителей");
                                                                }
                                                                else { await botClient.SendMessage(chat.Id, "Отправьте фото (если фото отстутствует напишите Далее)"); }
                                                                return;
                                                            }
                                                        case UserState.AwaitingPhotoBroad:
                                                            {
                                                                if (text.ToLower() == "далее")
                                                                {
                                                                    _stateRepository.UpdateUserState(user.Id, UserState.AwaitingBroadTime);
                                                                    await botClient.SendMessage(chat.Id, "Напишите время отправки поста в формате ЧЧ:ММ ДД.ММ");
                                                                }
                                                                else { await botClient.SendMessage(chat.Id, "Отправьте фото (если фото отстутствует напишите Далее)"); }
                                                                return;
                                                            }
                                                        case UserState.AwaitingWinners:
                                                            {
                                                                await PostCommands.SaveWinnersInt(chat.Id, cancellationToken, user.Id, text, botClient);
                                                                //await GetTextFromAnswer(chat.Id, cancellationToken, user.Id, text, botClient);




                                                                return;
                                                            }
                                                        case UserState.AwaitingPrizes:
                                                            {
                                                                await PostCommands.SavePrizes(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                return;
                                                            }
                                                        case UserState.AwaitingUploadTime:
                                                            {
                                                                await PostCommands.SaveUploadTime(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                return;
                                                            }
                                                        case UserState.AwaitingBroadTime:
                                                            {
                                                                await BroadCommands.SaveBroadTime(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                return;
                                                            }
                                                        case UserState.AwaitingPollTime:
                                                            {
                                                                await PostCommands.SavePollTime(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                return;
                                                            }
                                                        case UserState.AwaitingChannel:
                                                            {
                                                                await PostCommands.SaveChannel(chat.Id, cancellationToken, user.Id, text, botClient);

                                                                return;
                                                            }
                                                        case UserState.AddingAdmin:
                                                            {
                                                                await AddAdmin(chat.Id, cancellationToken, user.Id, user.Username, botClient, text, admDB);

                                                                return;
                                                            }
                                                        



                                                        case UserState.EditingBroadId:
                                                            {
                                                                await BroadCommands.ChooseEditBroadId(chat.Id, botClient, text);

                                                                return;
                                                            }
                                                        case UserState.EditingBroadText:
                                                            {

                                                                await BroadCommands.EditBroadText(chat.Id, botClient, text);


                                                                return;
                                                            }
                                                        case UserState.EditingBroadPhoto:
                                                            {
                                                                await BroadCommands.EditBroadPhoto(chat.Id, botClient, message);
                                                                return;
                                                            }
                                                        case UserState.EditingBroadTime:
                                                            {
                                                                await BroadCommands.EditBroadUploadTime(chat.Id, botClient, text);
                                                                return;
                                                            }
                                                        case UserState.ConfirmBroadEdition:
                                                            {
                                                                await BroadCommands.ConfirmBroadEdition(chat.Id, botClient, text);
                                                                return;
                                                            }

                                                        case UserState.CancelBroadId:
                                                            {
                                                                await BroadCommands.ChooseCancelBroadId(chat.Id, botClient, text);
                                                                return;
                                                            }
                                                        case UserState.ConfirmCancellationBroad:
                                                            {
                                                                await BroadCommands.ConfirmCancellationBroad(chat.Id, botClient, text);
                                                                return;
                                                            }




                                                        case UserState.InstaBroadId:
                                                            {
                                                                await BroadCommands.ChooseInstaBroadId(chat.Id, botClient, text);
                                                                return;
                                                            }
                                                        case UserState.ConfirmInstaBroad:
                                                            {
                                                                await BroadCommands.ConfirmInstaBroad(chat.Id, botClient, text);
                                                                return;
                                                            }



                                                        case UserState.EditingPostId:
                                                            {
                                                                await PostCommands.ChooseEditPostId(chat.Id, botClient, text);

                                                                return;
                                                            }
                                                        case UserState.EditingPostText:
                                                            {

                                                                await PostCommands.EditPostText(chat.Id, botClient, text);


                                                                return;
                                                            }
                                                        case UserState.EditingPostPhoto:
                                                            {
                                                                await PostCommands.EditPostPhoto(chat.Id, botClient, message);
                                                                return;
                                                            }
                                                        case UserState.EditingPostWinners:
                                                            {
                                                                await PostCommands.EditPostWinners(chat.Id, botClient, text);
                                                                return;
                                                            }
                                                        case UserState.EditingPostUploadTime:
                                                            {
                                                                await PostCommands.EditPostUploadTime(chat.Id, botClient, text);
                                                                return;
                                                            }
                                                        case UserState.EditingPostPollTime:
                                                            {
                                                                await PostCommands.EditPostPollTime(chat.Id, botClient, text);
                                                                return;
                                                            }

                                                        /* UNUSED
                                                    case UserState.EditingPostPrizes:
                                                        {
                                                            await EditPostPrizes(chat.Id, botClient, text);
                                                            return;
                                                        }
                                                        */


                                                        case UserState.ConfirmPostEdition:
                                                            {
                                                                await PostCommands.SaveEdition(chat.Id, botClient, text);
                                                                return;
                                                            }



                                                        case UserState.CancelPostId:
                                                            {
                                                                await PostCommands.ChooseCancelPostId(chat.Id, botClient, text);
                                                                return;
                                                            }
                                                        case UserState.ConfirmCancellationPost:
                                                            {
                                                                await PostCommands.ConfirmCancellationPost(chat.Id, botClient, text);
                                                                return;
                                                            }


                                                        

                                                        case UserState.InstaPostId:
                                                            {
                                                                await PostCommands.ChooseInstaPostId(chat.Id, botClient, text);
                                                                return;
                                                            }
                                                        case UserState.ConfirmInstaPost:
                                                            {
                                                                await PostCommands.ConfirmInstaPost(chat.Id, botClient, text);
                                                                return;
                                                            }


                                                    }


                                                }
                                                return;
                                            }


                                        case MessageType.Photo:
                                            {
                                                switch (userData.CurrentState)
                                                {
                                                    case UserState.AwaitingPhoto:
                                                        {

                                                            await PostCommands.SavePhotoAndPath(chat.Id, cancellationToken, user.Id, message, botClient);

                                                            return;
                                                        }
                                                    case UserState.AwaitingPhotoBroad:
                                                        {

                                                            await BroadCommands.SavePhotoBroad(chat.Id, cancellationToken, user.Id, message, botClient);

                                                            return;
                                                        }
                                                    
                                                    case UserState.EditingPostPhoto:
                                                        {

                                                            await PostCommands.EditPostPhoto(chat.Id, botClient, message);

                                                            return;
                                                        }
                                                    case UserState.EditingBroadPhoto:
                                                        {

                                                            await BroadCommands.EditBroadPhoto(chat.Id, botClient, message);

                                                            return;
                                                        }


                                                    default:

                                                        return;

                                                }


                                                return;
                                            }


                                        default:

                                            return;
                                    }
                                }
                                else if (message.Text == "/start")
                                {
                                    _stateRepository.UpdateUserState(user.Id, UserState.Idle);
                                    _stateRepository.ClearTempData(user.Id);



                                    var replyKeyboard = new ReplyKeyboardMarkup(
                                        new List<KeyboardButton[]>()
                                        {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("/Подписаться")
                                            //new KeyboardButton("/Редактировать Пост??"),
                                        }
                                        })
                                    {


                                        ResizeKeyboard = true,  // автоматическое изменение размера клавиатуры
                                    };

                                    await botClient.SendMessage(
                                        chat.Id,
                                        "Для подписки на рассылку нажмите /подписаться!",
                                        replyMarkup: replyKeyboard);
                                }
                                else if (message.Text.ToLower() == "/подписаться")
                                {
                                    await Subscribe(chat.Id, cancellationToken, user.Id, user.Username, botClient);
                                }
                                
                            }
                            return;
                        }
                            case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                            {
                                var callbackQuery = update.CallbackQuery;
                                var userId = callbackQuery.From.Id;
                                var username = "@" + callbackQuery.From.Username;
                                var channelId = callbackQuery.Message.Chat.Id;
                                Console.WriteLine($"Получил нажатие от {callbackQuery.From.FirstName} {username} ({userId}) chatId: {channelId}");
                                
                                var data = callbackQuery.Data;
                                
                                
                                

                                if (callbackQuery != null)
                                {

                                    if (data.StartsWith("giveaway_"))
                                    {

                                        var giveawayId = int.Parse(data.Replace("giveaway_", ""));
                                        User bot = await botClient.GetMe();
                                        var db = new GiveawayContext();

                                        var alreadyParticipated = await db.GiveawayParticipants.AnyAsync(p => p.GiveawayId == giveawayId && p.UserId == userId);
                                        var alreadySubscribed = await db.Subscribers.AnyAsync(p => p.UserId == userId);

                                        var chatMember = await botClient.GetChatMember(
                                            chatId: channelId,
                                            userId: userId,
                                            cancellationToken: cancellationToken);

                                                                // Допустимые статусы участника
                                        var allowedStatuses = new[]
                                        {
                                            ChatMemberStatus.Member,
                                            ChatMemberStatus.Administrator,
                                            ChatMemberStatus.Creator
                                        };

                                        if (alreadyParticipated)
                                        {
                                            await botClient.AnswerCallbackQuery(
                                            callbackQuery.Id,
                                            "Вы уже участвуете в этом розыгрыше!",
                                            cancellationToken: cancellationToken);


                                        }
                                        else if (!allowedStatuses.Contains(chatMember.Status))
                                        {
                                            await botClient.AnswerCallbackQuery(
                                                callbackQuery.Id,
                                                $"Для участия необходимо быть подписанным на нашу группу и @{bot.Username} (/подписаться!)!",
                                                showAlert: true,
                                                cancellationToken: cancellationToken);
                                            return;
                                        }
                                        else if (!alreadySubscribed)
                                        {
                                            await botClient.AnswerCallbackQuery(
                                                callbackQuery.Id,
                                                $"Для участия необходимо быть подписанным на @{bot.Username} (/подписаться!)",
                                                showAlert: true,
                                                cancellationToken: cancellationToken);
                                            return;
                                        }
                                        else
                                        {
                                            db.GiveawayParticipants.Add(new GiveawayParticipant
                                            {
                                                GiveawayId = giveawayId,
                                                UserId = userId,
                                                Username = username

                                            });

                                            await db.SaveChangesAsync();

                                            // Отправляем подтверждение
                                            await botClient.AnswerCallbackQuery(
                                                callbackQuery.Id,
                                                "Вы участвуете в розыгрыше! Удачи!",
                                                cancellationToken: cancellationToken);
                                        }

                                        await db.DisposeAsync();


                                    

                                    }

                                }

                                return;
                            }


                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

            
        }
        #endregion

        #region ----------------------------------------------------------USER RULES----------------------------------------------------------
        private static async Task Subscribe(long chatId, CancellationToken cancellationToken, long userId, string username, ITelegramBotClient botClient)
        {

            var db = new GiveawayContext();
            var alreadySubscribed = await db.Subscribers.AnyAsync(p => p.UserId == userId);
            if (!alreadySubscribed)
            {
                await db.Subscribers.AddAsync(new Subscriber
                {
                    Username = username,
                    UserId = userId,
                });
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Вы успешно подписались на рассылку!",
                    cancellationToken: cancellationToken);
                await db.SaveChangesAsync();
                
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Вы уже подписаны на рассылку.",
                    cancellationToken: cancellationToken);
            }
            await db.DisposeAsync();
        }
        
        private static async Task AddAdmin(long chatId, CancellationToken cancellationToken, long userId, string userName, ITelegramBotClient botClient, string messageText, AdminContext admDB)
        {
            if (_stateRepository.GetUserData(userId).CurrentState == UserState.Idle)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Отправьте userID человека, которого хотите сделать администратором. Чтобы узнать свой userID напишите боту @getmyid_bot Внимание! Это действие нельзя отменить",
                    cancellationToken: cancellationToken);
                _stateRepository.UpdateUserState(userId, UserState.AddingAdmin);
            }
            else if (_stateRepository.GetUserData(userId).CurrentState == UserState.AddingAdmin)
            {
                try
                {
                    
                    admDB.Admins.Add(new Admin
                    {
                        UserName = null,
                        UserId = long.Parse(messageText),
                    });
                    await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Пользователь {long.Parse(messageText)} успешно назначен администратором!",
                    cancellationToken: cancellationToken);
                    _stateRepository.UpdateUserState(userId, UserState.Idle);
                    await admDB.SaveChangesAsync();
                }
                catch (Exception ex) 
                {
                    await botClient.SendMessage(
                    chatId: chatId,
                    text: $"Введите верный userId",
                    cancellationToken: cancellationToken);
                }

            }
        }
        
        #endregion

        #region ----------------------------------------------------------MENU----------------------------------------------------------------

        private static async Task PostMenuKeyboard(ITelegramBotClient botClient, long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>()
                {
                        new KeyboardButton[]
                        {
                            new KeyboardButton("/Создать"),
                            new KeyboardButton("/Редактировать")
                            
                        },
                        new KeyboardButton[]
                        {
                            new KeyboardButton("/Удалить"),
                            new KeyboardButton("/Отправить")
                        },
                        new KeyboardButton[]
                        {
                            new KeyboardButton("/Завершить"),
                            new KeyboardButton("/Назад")
                        }
                })
            {


                ResizeKeyboard = true,  // автоматическое изменение размера клавиатуры
            };

            await botClient.SendMessage(
                chatId,
                "Вы открыли меню постов.",
                replyMarkup: replyKeyboard);
            _stateRepository.UpdateUserState(chatId, UserState.PostMenu);
        }
        private static async Task BroadMenuKeyboard(ITelegramBotClient botClient, long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>()
                {
                        new KeyboardButton[]
                        {
                            new KeyboardButton("/Создать"),
                            new KeyboardButton("/Редактировать")
                            
                        },
                        new KeyboardButton[]
                        {
                            new KeyboardButton("/Удалить"),
                            new KeyboardButton("/Отправить")
                        },
                        new KeyboardButton[]
                        {
                            
                            new KeyboardButton("/Назад")
                        }
                })
            {


                ResizeKeyboard = true,  // автоматическое изменение размера клавиатуры
            };

            await botClient.SendMessage(
                chatId,
                "Вы открыли меню рассылок.",
                replyMarkup: replyKeyboard);
            _stateRepository.UpdateUserState(chatId, UserState.BroadMenu);
        }
        #endregion
        
        
        
        private static async Task ResetTimers(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var givedb = new GiveawayContext();
            var postdb = new PostContext();
            var broadb = new BroadContext();

            List<Post> postList = postdb.PostDbSet.Where(p => (p.IsPublished == 0 || p.IsEnded == 0) && p.IsCancelled != 1).ToList();
            List<Broadcast> broadList = broadb.Broadcasts.Where(p => p.IsPublished == 0 && p.IsCancelled != 1).ToList();
            List<long> subscribersIds = await givedb.Subscribers.Select(p => p.UserId).Distinct().ToListAsync();

            foreach (var post in postList)
            {
                
                DateTime dateNow = DateTime.Now;
                string uploadTime = post.UploadTime;
                string pollTime = post.PollTime;

                string format = "HH:mm dd.MM";
                CultureInfo provider = CultureInfo.InvariantCulture;
                DateTime dateUpload = DateTime.ParseExact(uploadTime, format, provider);
                DateTime datePoll = DateTime.ParseExact(pollTime, format, provider);
                TimeSpan differencePost = dateUpload - dateNow;
                TimeSpan differencePoll = datePoll - dateUpload;
                double totalMinutesPost = differencePost.TotalMinutes;
                double totalMinutesPoll = differencePoll.TotalMinutes;
                Console.WriteLine($"количество минут до отправки: {totalMinutesPost}");
                int winnersCount = post.Winners;
                Console.WriteLine($"datePoll > dateNow {datePoll > dateNow}");
                Console.WriteLine($"totalMinutesPoll > totalMinutesPost {totalMinutesPoll > totalMinutesPost}");
                if (post.IsPublished == 0)
                {
                    

                    
                    if (dateUpload > dateNow)
                    {
                        Console.WriteLine($"Пост id{post.PostId} будет отправлен в {post.UploadTime}\n");
                        _timerPost = new Timer(async _ =>
                        {
                            try
                            {

                                using (var freshDb = new PostContext())
                                {
                                    var freshPost = await freshDb.PostDbSet.FirstOrDefaultAsync(p => p.PostId == post.PostId);
                                    if (freshPost == null || freshPost.IsCancelled == 1) return;
                                    if (freshPost.IsPublished != 1)
                                    {
                                        await PostCommands.SendInChannel(post.PostId, freshDb, botClient);
                                        freshPost.IsPublished = 1;
                                        await freshDb.SaveChangesAsync();
                                    }


                                    _timerPoll = new Timer(async __ =>
                                    {
                                        try
                                        {
                                            using (var pollDb = new PostContext())
                                            {
                                                var pollPost = await pollDb.PostDbSet.FirstOrDefaultAsync(p => p.PostId == post.PostId);
                                                if (pollPost == null || pollPost.IsCancelled == 1) return;
                                                if (pollPost.IsEnded != 1)
                                                {
                                                    await PostCommands.GiveawayWinners(post.PostId, pollPost.Winners, botClient);
                                                    pollPost.IsEnded = 1;
                                                    await pollDb.SaveChangesAsync();
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Ошибка в таймере опроса: {ex.Message}");
                                        }
                                        finally
                                        {
                                            _timerPoll?.Dispose();
                                        }
                                    },
                                    null,
                                    TimeSpan.FromMinutes(totalMinutesPoll),
                                    TimeSpan.Zero);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка в таймере публикации: {ex.Message}");
                            }
                        },
                        null,
                        TimeSpan.FromMinutes(totalMinutesPost),
                        TimeSpan.Zero);
                    }
                    
                    else
                    {
                        Console.WriteLine($"Пост id{post.PostId} со временем {post.UploadTime} был отмечен как завершённый\n");
                        post.IsPublished = 1;
                        post.IsEnded = 1;
                        await postdb.SaveChangesAsync();
                    }
                }
                else if (post.IsEnded == 0)
                {
                        post.IsPublished = 1;
                        await postdb.SaveChangesAsync();

                        // Если пост уже должен был быть отправлен, сразу запускаем таймер для опроса

                        TimeSpan remainingPollTime = datePoll - dateNow;

                        double remainingMinutesPoll = remainingPollTime.TotalMinutes;
                        
                        if (datePoll > dateNow)
                        {
                        
                            _timerPoll = new Timer(async __ =>
                            {
                                try
                                {
                                    using (var pollDb = new PostContext())
                                    {
                                        var pollPost = await pollDb.PostDbSet.FirstOrDefaultAsync(p => p.PostId == post.PostId);
                                        if (pollPost == null || pollPost.IsCancelled == 1) return;

                                        await PostCommands.GiveawayWinners(post.PostId, pollPost.Winners, botClient);
                                        pollPost.IsEnded = 1;
                                        await pollDb.SaveChangesAsync();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка в таймере опроса: {ex.Message}");
                                }
                                finally
                                {
                                    _timerPoll?.Dispose();
                                }
                            },
                            null,
                            TimeSpan.FromMinutes(totalMinutesPoll),
                            TimeSpan.Zero);
    
                        }
                        else
                        {
                            Console.WriteLine($"Пост id{post.PostId} со временем {post.UploadTime} был отмечен как завершённый\n");
                            post.IsPublished = 1;
                            post.IsEnded = 1;
                            await postdb.SaveChangesAsync();
                        }
                }

            }
            foreach (var broad in broadList)
            {

                string format = "HH:mm dd.MM";
                CultureInfo provider = CultureInfo.InvariantCulture;
                DateTime dateNow = DateTime.Now;

                string uploadTime = broad.BroadTime;

                DateTime dateUpload = DateTime.ParseExact(uploadTime, format, provider);
                TimeSpan differencePost = dateUpload - dateNow;
                double totalMinutesPost = differencePost.TotalMinutes;

                bool IsPhoto = false;
                if (broad.BroadPhotoPath != null)
                {
                    IsPhoto = true;

                }
                var gdb = new GiveawayContext();
                
                if (dateUpload > dateNow)
                {
                    Console.WriteLine($"Рассылка id{broad.Id} будет отправлена в {broad.BroadTime}\n");
                    _timerBroad = new Timer(async _ =>
                    {

                            foreach (long userid in subscribersIds)
                            {
                                try
                                {
                                    if (IsPhoto)
                                    {
                                        await using (var sendImageStream = System.IO.File.Open(broad.BroadPhotoPath, FileMode.Open))
                                        {
                                            await botClient.SendPhoto(userid, sendImageStream, broad.BroadText);
                                        }
                                    }
                                    else
                                    {
                                        await botClient.SendMessage(userid, broad.BroadText);
                                    }
                                }
                                catch (Exception ex) 
                                {
                                    Console.WriteLine($"Ошибка для пользователя {userid}: {ex.Message}");
                                }
                            }
                            broad.IsPublished = 1;
                            await broadb.SaveChangesAsync();
                    },
                    null,
                    TimeSpan.FromMinutes(totalMinutesPost),
                    TimeSpan.Zero);

                }
                else
                {
                    Console.WriteLine($"Рассылка id{broad.Id} со временем {broad.BroadTime} была отмечена как завершённая\n");
                    broad.IsPublished = 1;
                    await broadb.SaveChangesAsync();
                }
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));

        }
        

        async static Task Main(string[] args)
        {

            var jsonReader = new jsonReader();
            await jsonReader.ReadJson();
            string token = jsonReader.token;
            ITelegramBotClient bot = new TelegramBotClient(token);

            Console.WriteLine("Запущен бот " + bot.GetMe().Result.FirstName);
            //Console.Out.Flush();

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            Console.WriteLine("Рестарт отложенных постов и рассылок:\n");
            await ResetTimers(bot, cancellationToken);  // Рестарт таймеров постов и рассылок с IsPublished = 0

            bot.StartReceiving(
                UpdateHandler,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            while (true) { await Task.Delay(1000); }
            //Console.ReadLine();

        }
    }
}