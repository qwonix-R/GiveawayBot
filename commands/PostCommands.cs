using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TgBot1.UserStateRepository;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgBot1.commands
{
    class PostCommands
    {
        public static readonly UserStateRepository _stateRepository = Program._stateRepository;

        private static Timer? _timerPost;
        private static Timer? _timerPoll;
        public static string photosDirectory = Program.photosDirectory;
        #region ----------------------------------------------------------POST----------------------------------------------------------------


        #region PostCreation
        public static async Task StartNewPostCreation(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Отправьте мне текст поста",
                cancellationToken: cancellationToken);
            _stateRepository.UpdateUserState(userId, UserState.AwaitingText);

        }
        public static async Task SaveTextFromAnswer(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {
                _stateRepository.UpdateUserState(userId, UserState.AwaitingPhoto);
                var userData = _stateRepository.GetUserData(userId);
                _stateRepository.FillPostText(userId, messageText);
                string tempMessage = _stateRepository.GetPostText(userId);
                Console.Write("во временное хранилище записано: " + tempMessage + "\n");
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Отлично! А теперь отправьте фото (если фото отсутствует напишите Далее)",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: "Ошибка! Введите текст",
                    cancellationToken: cancellationToken);
            }

        }
        public static async Task SavePhotoAndPath(long chatId, CancellationToken cancellationToken, long userId, Telegram.Bot.Types.Message message, ITelegramBotClient botClient)
        {
            var photos = message.Photo;
            var fileId = photos[^1].FileId;
            
            if (!Directory.Exists(photosDirectory))
            {
                Directory.CreateDirectory(photosDirectory);
            }

            string fileName = $"photo_{DateTime.Now:yyyyMMddHHmmssfff}.jpg";
            string filePath = Path.Combine(photosDirectory, fileName);
            await using (var saveImageStream = System.IO.File.Open(filePath, FileMode.Create))
            {
                await botClient.GetInfoAndDownloadFile(fileId, saveImageStream);
            }
            _stateRepository.FillPhotoPath(userId, filePath);
            _stateRepository.UpdateUserState(userId, UserState.AwaitingWinners);
            string sendPath = _stateRepository.GetPhotoPath(userId);
            Console.Write("фото сохранено по пути: " + sendPath + "\n");
            await botClient.SendMessage(chatId, "Отправьте количество победителей");

        }
        public static async Task SaveWinnersInt(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {

                int winners = Int32.Parse(messageText);
                _stateRepository.FillWinners(userId, winners);

                //await botClient.SendMessage(chatId, "Опишите призы");
                await botClient.SendMessage(chatId, "Напишите время отправки поста в формате ЧЧ:ММ ДД.ММ");

                if (winners > 0)
                {
                    int tempMessage = _stateRepository.GetWinners(userId);
                    Console.Write("во временное хранилище записано: " + tempMessage + " победителей\n");
                    _stateRepository.UpdateUserState(userId, UserState.AwaitingUploadTime);
                }
                else
                {
                    await botClient.SendMessage(chatId, "Введите верное число");
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
                await botClient.SendMessage(chatId, "Введите верное число");

            }
        }

        public static async Task SavePrizes(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {

                _stateRepository.FillPrizes(chatId, messageText);
                await botClient.SendMessage(chatId, "Напишите время отправки поста в формате ЧЧ:ММ ДД.ММ");
                string tempMessage = _stateRepository.GetPrizes(userId);
                Console.Write("во временное хранилище записаны призы: " + tempMessage + "\n");
                _stateRepository.UpdateUserState(userId, UserState.AwaitingUploadTime);

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex?.ToString());

            }

        }
        public static async Task SaveUploadTime(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {
                if (DateTime.TryParseExact(messageText, "HH:mm dd.MM", null, System.Globalization.DateTimeStyles.None, out DateTime userDateTime))
                {
                    DateTime currentDateTime = DateTime.Now;

                    if (userDateTime > currentDateTime)
                    {
                        _stateRepository.FillUploadTime(chatId, messageText);
                        await botClient.SendMessage(chatId, "Напишите время проведения розыгрыша в формате ЧЧ:ММ ДД.ММ");
                        string tempMessage = _stateRepository.GetUploadTime(userId);
                        Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");
                        _stateRepository.UpdateUserState(userId, UserState.AwaitingPollTime);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Напишите верное время");
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, "Некорректный формат даты и времени!");
                }


            }
            catch (Exception ex)
            {

                Console.WriteLine(ex?.ToString());

            }
        }
        public static async Task SavePollTime(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {
                if (DateTime.TryParseExact(messageText, "HH:mm dd.MM", null, System.Globalization.DateTimeStyles.None, out DateTime userDateTime))
                {
                    DateTime currentDateTime = DateTime.Now;
                    var uploadString = _stateRepository.GetUploadTime(chatId);

                    string format = "HH:mm dd.MM";
                    DateTime userUploadTime = DateTime.ParseExact(
                        uploadString,
                        format,
                        CultureInfo.InvariantCulture, // или CultureInfo.CurrentCulture
                        DateTimeStyles.None
                    );


                    if (userDateTime > currentDateTime && userDateTime > userUploadTime)
                    {
                        _stateRepository.FillPollTime(chatId, messageText);
                        await botClient.SendMessage(chatId, "Напишите юзернейм группы, в которой хотите проводить розыгрыш. Убедитесь, что бот состоит в группе в качестве администратора");
                        string tempMessage = _stateRepository.GetPollTime(userId);
                        Console.Write("во временное хранилище записано время розыгрыша: " + tempMessage + "\n");
                        _stateRepository.UpdateUserState(userId, UserState.AwaitingChannel);
                    }
                    else if (userDateTime > currentDateTime)
                    {
                        await botClient.SendMessage(chatId, "Время розыгрыша должно быть позже времени отправки поста");
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Введите верное время");
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, "Некорректный формат даты и времени!");
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex?.ToString());

            }
        }

        public static async Task SaveChannel(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {
                if (messageText.StartsWith("@"))
                {
                    _stateRepository.FillChannel(userId, messageText);
                    _stateRepository.FillTimeStamp(userId, $"{DateTime.Now:yyyyMMddHHmmssfff}");
                    string tempMessage = _stateRepository.GetChannel(userId);
                    Console.Write("во временное хранилище записана группа: " + tempMessage + "\n");

                    await botClient.SendMessage(chatId, $"Все готово! Пост будет отправлен в {_stateRepository.GetUploadTime(userId)}");


                    string sendPath = _stateRepository.GetPhotoPath(userId);

                    if (sendPath != null)
                    {
                        await using (var sendImageStream = System.IO.File.Open(sendPath, FileMode.Open))
                        {
                            await botClient.SendPhoto(chatId, sendImageStream);
                        }
                    }

                    _stateRepository.UpdateUserState(userId, UserState.PostMenu);

                    var db = new PostContext();
                    int postId = await SavePostToDatabase(chatId, db, cancellationToken, userId, botClient);

                    await botClient.SendMessage(chatId, $"Содержание поста: \nID:{postId} \nТекст:{_stateRepository.GetPostText(userId)}\nКоличество победителей:{_stateRepository.GetWinners(userId)}\nВремя отправки:{_stateRepository.GetUploadTime(userId)}\nВремя розыгрыша: {_stateRepository.GetPollTime(userId)}\nГруппа: {_stateRepository.GetChannel(userId)}");

                    string postText = _stateRepository.GetPostText(userId);
                    string channel = _stateRepository.GetChannel(userId);

                    var postList = db.PostDbSet.Where(p => p.ChatId == chatId && p.TimeStamp == _stateRepository.GetTimeStamp(userId));
                    Post lastPost = postList.FirstOrDefault();
                    string lastPath = lastPost.PhotoPath;

                    Console.WriteLine($"путь фото в памяти: {_stateRepository.GetPhotoPath(userId)} \n путь фото в бд: {lastPost.PhotoPath} \n айди поста: {lastPost.PostId}");
                    
                    _stateRepository.FillPostText(chatId, null);
                    _stateRepository.FillPhotoPath(chatId, null);
                    _stateRepository.FillWinners(chatId, 0);
                    _stateRepository.FillUploadTime(chatId, null);
                    _stateRepository.FillPollTime(chatId, null);
                    _stateRepository.FillChannel(chatId, null);

                    await ScheduleUploadPost(botClient, postId, db);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex?.ToString());

            }
        }
        #endregion


        #region Пост и БД
        public static async Task<int> SavePostToDatabase(long chatId, PostContext db, CancellationToken cancellationToken, long userId, ITelegramBotClient botClient)
        {

            string postText = _stateRepository.GetPostText(chatId);

            string photoPath = _stateRepository.GetPhotoPath(chatId);
            bool IsPhotoPath;
            if (photoPath == null) { IsPhotoPath = false; } else { IsPhotoPath = true; }

            int winners = _stateRepository.GetWinners(chatId);
            string prizes = _stateRepository.GetPrizes(chatId);

            string uploadTime = _stateRepository.GetUploadTime(userId);  // Время отправки
            string pollTime = _stateRepository.GetPollTime(userId);      // Время розыгрыша
            string channel = _stateRepository.GetChannel(userId);
            string timeStamp = _stateRepository.GetTimeStamp(userId);



            var newPost = new Post();

            if (IsPhotoPath == true)
            {
                newPost = new Post
                {
                    ChatId = chatId,
                    Text = postText,
                    PhotoPath = photoPath,
                    Winners = winners,
                    //Prizes = prizes, unused
                    UploadTime = uploadTime,
                    PollTime = pollTime,
                    Channel = channel,
                    TimeStamp = timeStamp,
                    IsPublished = 0
                };
            }
            else
            {
                newPost = new Post
                {
                    ChatId = chatId,
                    Text = postText,
                    PhotoPath = null,
                    Winners = winners,
                    //Prizes = prizes, unused
                    UploadTime = uploadTime,
                    PollTime = pollTime,
                    Channel = channel,
                    TimeStamp = timeStamp,
                    IsPublished = 0
                };
            }

            db.PostDbSet.Add(newPost);
            await db.SaveChangesAsync();
            Post lastPost = await db.PostDbSet.FirstOrDefaultAsync(p => p.ChatId == chatId && p.TimeStamp == timeStamp);
            int postId = lastPost.PostId;
            return postId;
        }

        
        
        public static async Task ScheduleUploadPost(ITelegramBotClient botClient, int postId, PostContext db)
        {
            DateTime dateNow = DateTime.Now;

            // Получаем пост только для проверки времени
            Post initialPost = await db.PostDbSet.FirstOrDefaultAsync(p => p.PostId == postId);
            if (initialPost == null) return;

            string uploadTime = initialPost.UploadTime;
            string pollTime = initialPost.PollTime;

            string format = "HH:mm dd.MM";
            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime dateUpload = DateTime.ParseExact(uploadTime, format, provider);
            DateTime datePoll = DateTime.ParseExact(pollTime, format, provider);
            TimeSpan differencePost = dateUpload - dateNow;
            TimeSpan differencePoll = datePoll - dateUpload;
            double totalMinutesPost = differencePost.TotalMinutes;
            double totalMinutesPoll = differencePoll.TotalMinutes;

            if (dateUpload > dateNow)
            {
                _timerPost = new Timer(async _ =>
                {
                    try
                    {

                        using (var freshDb = new PostContext())
                        {
                            var freshPost = await freshDb.PostDbSet.FirstOrDefaultAsync(p => p.PostId == postId);
                            if (freshPost == null || freshPost.IsCancelled == 1) return;
                            if (freshPost.IsPublished != 1)
                            {
                                await SendInChannel(postId, freshDb, botClient);
                                freshPost.IsPublished = 1;
                                await freshDb.SaveChangesAsync();
                            }

                            _timerPoll = new Timer(async __ =>
                            {
                                try
                                {
                                    using (var pollDb = new PostContext())
                                    {
                                        var pollPost = await pollDb.PostDbSet.FirstOrDefaultAsync(p => p.PostId == postId);
                                        if (pollPost == null || pollPost.IsCancelled == 1) return;
                                        if (pollPost.IsEnded != 1)
                                        {
                                            await GiveawayWinners(postId, pollPost.Winners, botClient);
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
                initialPost.IsPublished = 1;
                await db.SaveChangesAsync();
            }
        }
        public static async Task SendInChannel(int postId, PostContext db, ITelegramBotClient botClient)
        {
            Post post = await db.PostDbSet.FirstOrDefaultAsync(p => p.PostId == postId);

            User bot = await botClient.GetMe();
            string botUsername = bot.Username;
            string channel = post.Channel;
            //string finalText = post.Text + "\n" + "Призы:\n" + post.Prizes + "\n\n" + "Количество победителей: " 
            //    + post.Winners + "\n\n" + "Условия:\n" + "1. Подписаться на группу\n" + $"2. Подписаться на <a href=\"https://t.me/{botUsername}\">@{botUsername}</a>";
            string finalText = post.Text + "\n\n" + "Условия:\n" + "1. Подписаться на группу\n" + $"2. Подписаться на <a href=\"https://t.me/{botUsername}\">@{botUsername}</a>";
            var button = new InlineKeyboardButton("Участвовать!", $"giveaway_{postId}");
            string photoPath = post.PhotoPath;

            if (photoPath != null)
            {
                await using (var sendImageStream = System.IO.File.Open(photoPath, FileMode.Open))
                {
                    await botClient.SendPhoto(channel, sendImageStream, finalText, Telegram.Bot.Types.Enums.ParseMode.Html, null, button);
                }
            }
            else
            {
                await botClient.SendMessage(channel, finalText, Telegram.Bot.Types.Enums.ParseMode.Html, null, button);
            }

            Console.Write("Пост опубликован в группу " + post.Channel + " В " + post.UploadTime);
        }
        #endregion


        #region PostEdition

        public static async Task StartEditingPost(long chatId, ITelegramBotClient botClient) // UserState.Idle
        {
            string format = "yyyyMMddHHmmssfff";
            var db = new PostContext();

            List<Post> postList = await db.PostDbSet.Where(p => p.IsPublished == 0 && p.IsCancelled != 1).ToListAsync();
            Console.WriteLine($"postlist = {postList}, postlist.Count = {postList.Count}");
            string postListToString = "Доступные для редактирования посты:\n";
            if (postList != null && postList.Count != 0)
            {
                foreach (Post post in postList)
                {
                    DateTime timeStamp = DateTime.ParseExact
                    (
                        post.TimeStamp,
                        "yyyyMMddHHmmssfff",
                        CultureInfo.InvariantCulture
                    );

                    string creationTime = timeStamp.ToString("HH:mm dd.MM");

                    postListToString += $"ID: {post.PostId}, Время создания: {creationTime}\n";

                }
                await botClient.SendMessage(chatId, postListToString);
                await botClient.SendMessage(chatId, "Введите id поста, который вы хотите отредактировать");

                _stateRepository.UpdateUserState(chatId, UserState.EditingPostId);

            }
            else if (postList.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нет доступных постов для редактирования");
                _stateRepository.UpdateUserState(chatId, UserState.PostMenu);
            }
            await db.DisposeAsync();

        }

        public static async Task ChooseEditPostId(long chatId, ITelegramBotClient botClient, string message) // UserState.EditingPostId
        {
            var db = new PostContext();
            try
            {
                int postId = Int32.Parse(message);
                bool postExist = await db.PostDbSet.AnyAsync(p => p.PostId == postId && p.IsPublished == 0);
                if (postExist)
                {
                    _stateRepository.FillEditPostId(chatId, postId);
                    Console.WriteLine($"айди изменяемого поста: {postId}");
                    _stateRepository.UpdateUserState(chatId, UserState.EditingPostText);

#pragma warning disable CS8600
                    string oldText = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.Text).FirstOrDefaultAsync();
#pragma warning restore CS8600

                    await botClient.SendMessage(chatId, "Введите новый текст, либо напишите Далее");
                    await botClient.SendMessage(chatId, $"Текущий текст:\n {oldText}");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Поста с заданным id не существует либо уже отправлен! Введите верный id");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chatId, "Введите верный id");
            }

            await db.DisposeAsync();
        }

        public static async Task EditPostText(long chatId, ITelegramBotClient botClient, string message) // UserState.EditingPostText
        {
            var db = new PostContext();
            int postId = _stateRepository.GetEditPostId(chatId);
            if (message.ToLower() != "далее")
            {
                _stateRepository.FillEditPostText(chatId, message);
                Console.WriteLine($"Текст поста с айди {postId} был заменен на: {message}");
            }
            else { _stateRepository.FillEditPostText(chatId, null); }
            _stateRepository.UpdateUserState(chatId, UserState.EditingPostPhoto);



#pragma warning disable CS8600
            string oldPhoto = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.PhotoPath).FirstOrDefaultAsync();
#pragma warning restore CS8600


            Console.WriteLine($"Путь до предыдущего фото: {oldPhoto}");
            await botClient.SendMessage(chatId, "Отправьте новое фото, либо напишите Далее");
            if (oldPhoto != null)
            {
                try
                {
                    await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, $"Текущее фото");
                    }

                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            }
            await db.DisposeAsync();
        }
        public static async Task EditPostPhoto(long chatId, ITelegramBotClient botClient, Telegram.Bot.Types.Message message)
        {

            var db = new PostContext();
            int postId = _stateRepository.GetEditPostId(chatId);

            if (message.Type == MessageType.Photo)
            {
                var photos = message.Photo;
                var fileId = photos[^1].FileId;
                
                if (!Directory.Exists(photosDirectory))
                {
                    Directory.CreateDirectory(photosDirectory);
                }

                string fileName = $"photo_{DateTime.Now:yyyyMMddHHmmssfff}.jpg";
                string filePath = Path.Combine(photosDirectory, fileName);
                await using (var saveImageStream = System.IO.File.Open(filePath, FileMode.Create))
                {
                    await botClient.GetInfoAndDownloadFile(fileId, saveImageStream);
                }
                _stateRepository.FillEditPostPhoto(chatId, filePath);

                _stateRepository.UpdateUserState(chatId, UserState.EditingPostWinners);
                int oldWinners = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.Winners).FirstOrDefaultAsync();

                await botClient.SendMessage(chatId, $"Отправьте новое количество победителей либо напишите Далее");
                await botClient.SendMessage(chatId, $"Текущее значение: {oldWinners}");
            }
            else if (message.Type == MessageType.Text && message.Text.ToLower() == "далее")
            {
                _stateRepository.FillEditPostPhoto(chatId, null);
                _stateRepository.UpdateUserState(chatId, UserState.EditingPostWinners);
                int oldWinners = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.Winners).FirstOrDefaultAsync();

                await botClient.SendMessage(chatId, $"Отправьте новое количество победителей либо напишите Далее");
                await botClient.SendMessage(chatId, $"Текущее значение: {oldWinners}");

            }
            else
            {
                await botClient.SendMessage(chatId, $"Отправьте фото");
            }
            await db.DisposeAsync();
        }
        public static async Task EditPostWinners(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new PostContext();
            int postId = _stateRepository.GetEditPostId(chatId);

            if (message.ToLower() != "далее")
            {
                try
                {

                    int winners = Int32.Parse(message);
                    _stateRepository.FillEditPostWinners(chatId, winners);

                    if (winners > 0)
                    {
                        int tempMessage = _stateRepository.GetEditPostWinners(chatId);
                        Console.Write("во временное хранилище записано изменение: " + tempMessage + " победителей\n");
                        _stateRepository.UpdateUserState(chatId, UserState.EditingPostUploadTime);
                        await botClient.SendMessage(chatId, "Запишите новое время отправки в формате чч:мм ДД.ММ либо Далее");
                        string oldUploadTime = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.UploadTime).FirstOrDefaultAsync();
                        await botClient.SendMessage(chatId, $"Текущее время:\n{oldUploadTime}");

                        #region Сравнение резов
                        /*
                        string oldText = post.Text;
                        string oldPhoto = post.PhotoPath;
                        int oldWinners = post.Winners;
                        //string oldPrizes = post.Prizes;

                        string? newText;
                        string? newPhoto;
                        int newWinners;
                        //string? newPrizes;

                        if (_stateRepository.GetEditText(chatId) != null) { newText = _stateRepository.GetEditText(chatId); }
                        else { newText = oldText; }

                        if (_stateRepository.GetEditPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditPhoto(chatId); }
                        else { newPhoto = oldPhoto; }

                        if (_stateRepository.GetEditWinners(chatId) != 0) { newWinners = _stateRepository.GetEditWinners(chatId); }
                        else { newWinners = oldWinners; }

                        //if (_stateRepository.GetEditPrizes(chatId) != null) { newPrizes = _stateRepository.GetEditPrizes(chatId); }
                        //else { newPrizes = oldPrizes; }


                        string oldPost = $"СТАРЫЙ ПОСТ\nТекст: {oldText}\n Количество победителей: {oldWinners}";
                        string newPost = $"НОВЫЙ ПОСТ\nТекст: {newText}\n Количество победителей: {newWinners}";
                        
                        if (oldPhoto != null)
                        {
                            await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                            {
                                await botClient.SendPhoto(chatId, sendImageStream, oldPost);
                            }
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, oldPost);
                        }
                        if (newPhoto != null)
                        {
                            await using (var sendImageStream = System.IO.File.Open(newPhoto, FileMode.Open))
                            {
                                await botClient.SendPhoto(chatId, sendImageStream, newPost);
                            }
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, newPost);
                        }

                        await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет");
                        _stateRepository.UpdateUserState(chatId, UserState.ConfirmEdition);
                        */
                        #endregion
                        await db.DisposeAsync();

                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Введите верное число");
                    }



                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());
                    await botClient.SendMessage(chatId, "Введите верное число");

                }
            }
            else
            {
                _stateRepository.FillEditPostWinners(chatId, 0);
                _stateRepository.UpdateUserState(chatId, UserState.EditingPostUploadTime);

                await botClient.SendMessage(chatId, "Запишите новое время отправки в формате ЧЧ:ММ ДД.ММ либо Далее");
                string oldUploadTime = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.UploadTime).FirstOrDefaultAsync();
                await botClient.SendMessage(chatId, $"Текущее время:\n{oldUploadTime}");
                #region Тоже Сравнение резов
                /*
                string oldText = post.Text;
                string oldPhoto = post.PhotoPath;
                int oldWinners = post.Winners;
                //string oldPrizes = post.Prizes;

                string? newText;
                string? newPhoto;
                int newWinners;
                //string? newPrizes;

                if (_stateRepository.GetEditText(chatId) != null) { newText = _stateRepository.GetEditText(chatId); }
                else { newText = oldText; }

                if (_stateRepository.GetEditPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditPhoto(chatId); }
                else { newPhoto = oldPhoto; }

                if (_stateRepository.GetEditWinners(chatId) != 0) { newWinners = _stateRepository.GetEditWinners(chatId); }
                else { newWinners = oldWinners; }

                //if (_stateRepository.GetEditPrizes(chatId) != null) { newPrizes = _stateRepository.GetEditPrizes(chatId); }
                //else { newPrizes = oldPrizes; }


                string oldPost = $"СТАРЫЙ ПОСТ\nТекст: {oldText}\n Количество победителей: {oldWinners}";
                string newPost = $"НОВЫЙ ПОСТ\nТекст: {newText}\n Количество победителей: {newWinners}";

                if (oldPhoto != null)
                {
                    await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, oldPost);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, oldPost);
                }
                if (newPhoto != null)
                {
                    await using (var sendImageStream = System.IO.File.Open(newPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, newPost);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, newPost);
                }

                await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет");
                _stateRepository.UpdateUserState(chatId, UserState.ConfirmEdition);
                */
                #endregion
                await db.DisposeAsync();


            }


        }
        public static async Task EditPostUploadTime(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new PostContext();
            int postId = _stateRepository.GetEditPostId(chatId);
            if (message.ToLower() != "далее")
            {
                try
                {
                    if (DateTime.TryParseExact(message, "HH:mm dd.MM", null, System.Globalization.DateTimeStyles.None, out DateTime userDateTime))
                    {
                        DateTime currentDateTime = DateTime.Now;

                        if (userDateTime > currentDateTime)
                        {
                            _stateRepository.FillEditPostUploadTime(chatId, message);


                            await botClient.SendMessage(chatId, "Напишите время проведения розыгрыша в формате ЧЧ:ММ ДД.ММ либо Далее");
                            string oldPollTime = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.PollTime).FirstOrDefaultAsync();
                            await botClient.SendMessage(chatId, $"Текущее время:\n{oldPollTime}");

                            string tempMessage = _stateRepository.GetEditPostUploadTime(chatId);
                            Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");
                            _stateRepository.UpdateUserState(chatId, UserState.EditingPostPollTime);
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, "Напишите верное время");
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Некорректный формат даты и времени!");
                    }


                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex?.ToString());

                }
            }
            else
            {
                _stateRepository.FillEditPostUploadTime(chatId, null);
                _stateRepository.UpdateUserState(chatId, UserState.EditingPostPollTime);

                await botClient.SendMessage(chatId, "Напишите время проведения розыгрыша в формате ЧЧ:ММ ДД.ММ");
                string oldPollTime = await db.PostDbSet.Where(p => p.PostId == postId).Select(p => p.PollTime).FirstOrDefaultAsync();
                await botClient.SendMessage(chatId, $"Текущее время:\n{oldPollTime}");

            }
            await db.DisposeAsync();
        }
        public static async Task EditPostPollTime(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new PostContext();
            int postId = _stateRepository.GetEditPostId(chatId);
            Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();

            if (message.ToLower() != "далее")
            {
                try
                {
                    if (DateTime.TryParseExact(message, "HH:mm dd.MM", null, System.Globalization.DateTimeStyles.None, out DateTime userPollTime))
                    {
                        DateTime currentDateTime = DateTime.Now;
                        string format = "HH:mm dd.MM";

                        string oldUploadTime = post.UploadTime;
                        string? newUploadTime;


                        if (_stateRepository.GetEditPostUploadTime(chatId) != null) { newUploadTime = _stateRepository.GetEditPostUploadTime(chatId); }
                        else { newUploadTime = oldUploadTime; }


                        DateTime userUploadTime = DateTime.ParseExact(
                        newUploadTime,
                        format,
                        CultureInfo.InvariantCulture, // или CultureInfo.CurrentCulture
                        DateTimeStyles.None
                        );



                        if (userPollTime > currentDateTime && userPollTime > userUploadTime)
                        {
                            _stateRepository.FillEditPostPollTime(chatId, message);

                            //Сюда
                            string oldText = post.Text;
                            string oldPhoto = post.PhotoPath;
                            int oldWinners = post.Winners;
                            //string oldUploadTime = post.UploadTime;
                            string oldPollTime = post.PollTime;
                            //string oldPrizes = post.Prizes;

                            string? newText;
                            string? newPhoto;
                            int newWinners;
                            //string? newUploadTime;
                            string? newPollTime;
                            //string? newPrizes;

                            if (_stateRepository.GetEditPostText(chatId) != null) { newText = _stateRepository.GetEditPostText(chatId); }
                            else { newText = oldText; }

                            if (_stateRepository.GetEditPostPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditPostPhoto(chatId); }
                            else { newPhoto = oldPhoto; }

                            if (_stateRepository.GetEditPostWinners(chatId) != 0) { newWinners = _stateRepository.GetEditPostWinners(chatId); }
                            else { newWinners = oldWinners; }

                            //if (_stateRepository.GetEditUploadTime(chatId) != null) { newUploadTime = _stateRepository.GetEditUploadTime(chatId); }
                            //else { newUploadTime = oldUploadTime; }

                            if (_stateRepository.GetEditPostPollTime(chatId) != null) { newPollTime = _stateRepository.GetEditPostPollTime(chatId); }
                            else { newPollTime = oldPollTime; }

                            //if (_stateRepository.GetEditPrizes(chatId) != null) { newPrizes = _stateRepository.GetEditPrizes(chatId); }
                            //else { newPrizes = oldPrizes; }


                            string oldPost = $"СТАРЫЙ ПОСТ\nТекст: {oldText}\n Количество победителей: {oldWinners}\nВремя отправки: {oldUploadTime}\nВремя розыгрыша:{oldPollTime}";
                            string newPost = $"НОВЫЙ ПОСТ\nТекст: {newText}\n Количество победителей: {newWinners}\nВремя отправки: {newUploadTime}\nВремя розыгрыша:{newPollTime}";

                            if (oldPhoto != null)
                            {
                                await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                                {
                                    await botClient.SendPhoto(chatId, sendImageStream, oldPost);
                                }
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, oldPost);
                            }
                            if (newPhoto != null)
                            {
                                await using (var sendImageStream = System.IO.File.Open(newPhoto, FileMode.Open))
                                {
                                    await botClient.SendPhoto(chatId, sendImageStream, newPost);
                                }
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, newPost);
                            }

                            await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет");
                            _stateRepository.UpdateUserState(chatId, UserState.ConfirmPostEdition);

                            string tempMessage = _stateRepository.GetUploadTime(chatId);
                            Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");
                            _stateRepository.UpdateUserState(chatId, UserState.ConfirmPostEdition);
                        }
                        else if (userPollTime > currentDateTime)
                        {
                            await botClient.SendMessage(chatId, "Время розыгрыша должно быть позже времени отправки поста");
                        }
                        else
                        {
                            await botClient.SendMessage(chatId, "Напишите верное время");
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Некорректный формат даты и времени!");
                    }


                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex?.ToString());

                }

            }
            else
            {
                _stateRepository.FillEditPostPollTime(chatId, null);

                // и сюда
                string oldText = post.Text;
                string oldPhoto = post.PhotoPath;
                int oldWinners = post.Winners;
                string oldUploadTime = post.UploadTime;
                string oldPollTime = post.PollTime;
                //string oldPrizes = post.Prizes;

                string? newText;
                string? newPhoto;
                int newWinners;
                string? newUploadTime;
                string? newPollTime;
                //string? newPrizes;

                if (_stateRepository.GetEditPostText(chatId) != null) { newText = _stateRepository.GetEditPostText(chatId); }
                else { newText = oldText; }

                if (_stateRepository.GetEditPostPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditPostPhoto(chatId); }
                else { newPhoto = oldPhoto; }

                if (_stateRepository.GetEditPostWinners(chatId) != 0) { newWinners = _stateRepository.GetEditPostWinners(chatId); }
                else { newWinners = oldWinners; }

                if (_stateRepository.GetEditPostUploadTime(chatId) != null) { newUploadTime = _stateRepository.GetEditPostUploadTime(chatId); }
                else { newUploadTime = oldUploadTime; }
                if (_stateRepository.GetEditPostPollTime(chatId) != null) { newPollTime = _stateRepository.GetEditPostPollTime(chatId); }
                else { newPollTime = oldPollTime; }
                //if (_stateRepository.GetEditPrizes(chatId) != null) { newPrizes = _stateRepository.GetEditPrizes(chatId); }
                //else { newPrizes = oldPrizes; }


                string oldPost = $"СТАРЫЙ ПОСТ\nТекст: {oldText}\nКоличество победителей: {oldWinners}\nВремя отправки: {oldUploadTime}\nВремя розыгрыша:{oldPollTime}";
                string newPost = $"НОВЫЙ ПОСТ\nТекст: {newText}\nКоличество победителей: {newWinners}\nВремя отправки: {newUploadTime}\nВремя розыгрыша:{newPollTime}";

                if (oldPhoto != null)
                {
                    await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, oldPost);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, oldPost);
                }
                if (newPhoto != null)
                {
                    await using (var sendImageStream = System.IO.File.Open(newPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, newPost);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, newPost);
                }

                await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет");
                _stateRepository.UpdateUserState(chatId, UserState.ConfirmPostEdition);

                string tempMessage = _stateRepository.GetUploadTime(chatId);
                Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");
                _stateRepository.UpdateUserState(chatId, UserState.ConfirmPostEdition);

            }
            await db.DisposeAsync();
        }

        #region Unused PostPrizes

        public static async Task EditPostPrizes(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new PostContext();
            int postId = _stateRepository.GetEditPostId(chatId);
            Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();
            if (message.ToLower() != "далее")
            {
                //_stateRepository.FillEditPrizes(chatId, message);

            }
            else { /*_stateRepository.FillEditPrizes(chatId, null);*/ }




            string oldText = post.Text;
            string oldPhoto = post.PhotoPath;
            int oldWinners = post.Winners;
            //string oldPrizes = post.Prizes;

            string? newText;
            string? newPhoto;
            int newWinners;
            //string? newPrizes;

            if (_stateRepository.GetEditPostText(chatId) != null) { newText = _stateRepository.GetEditPostText(chatId); }
            else { newText = oldText; }

            if (_stateRepository.GetEditPostPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditPostPhoto(chatId); }
            else { newPhoto = oldPhoto; }

            if (_stateRepository.GetEditPostWinners(chatId) != 0) { newWinners = _stateRepository.GetEditPostWinners(chatId); }
            else { newWinners = oldWinners; }

            //if (_stateRepository.GetEditPrizes(chatId) != null) {newPrizes = _stateRepository.GetEditPrizes(chatId); }
            //else { newPrizes = oldPrizes; }


            string oldPost = $"СТАРЫЙ ПОСТ\nТекст: {oldText}\n Количество победителей: {oldWinners}";
            string newPost = $"НОВЫЙ ПОСТ\nТекст: {newText}\n Количество победителей: {newWinners}";

            if (oldPhoto != null)
            {
                await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                {
                    await botClient.SendPhoto(chatId, sendImageStream, oldPost);
                }
            }
            else
            {
                await botClient.SendMessage(chatId, oldPost);
            }
            if (newPhoto != null)
            {
                await using (var sendImageStream = System.IO.File.Open(newPhoto, FileMode.Open))
                {
                    await botClient.SendPhoto(chatId, sendImageStream, newPost);
                }
            }
            else
            {
                await botClient.SendMessage(chatId, newPost);
            }

            await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет");
            _stateRepository.UpdateUserState(chatId, UserState.ConfirmPostEdition);

            await db.DisposeAsync();
        }

        #endregion

        public static async Task SaveEdition(long chatId, ITelegramBotClient botClient, string message)
        {
            if (message.ToLower() == "нет" || message.ToLower() == "да")
            {
                var db = new PostContext();
                int postId = _stateRepository.GetEditPostId(chatId);
                Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();

                if (message.ToLower() == "да")
                {
                    string oldText = post.Text;
                    string oldPhoto = post.PhotoPath;
                    int oldWinners = post.Winners;
                    string oldUploadTime = post.UploadTime;
                    string oldPollTime = post.PollTime;
                    //string oldPrizes = post.Prizes;

                    string? newText;
                    string? newPhoto;
                    int newWinners;
                    string? newUploadTime;
                    string? newPollTime;
                    //string? newPrizes;

                    if (_stateRepository.GetEditPostText(chatId) != null) { newText = _stateRepository.GetEditPostText(chatId); }
                    else { newText = oldText; }

                    if (_stateRepository.GetEditPostPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditPostPhoto(chatId); }
                    else { newPhoto = oldPhoto; }

                    if (_stateRepository.GetEditPostWinners(chatId) != 0) { newWinners = _stateRepository.GetEditPostWinners(chatId); }
                    else { newWinners = oldWinners; }

                    if (_stateRepository.GetEditPostUploadTime(chatId) != null) { newUploadTime = _stateRepository.GetEditPostUploadTime(chatId); }
                    else { newUploadTime = oldUploadTime; }

                    if (_stateRepository.GetEditPostPollTime(chatId) != null) { newPollTime = _stateRepository.GetEditPostPollTime(chatId); }
                    else { newPollTime = oldPollTime; }

                    //if (_stateRepository.GetEditPrizes(chatId) != null) { newPrizes = _stateRepository.GetEditPrizes(chatId); }
                    //else { newPrizes = oldPrizes; }
                    if (_stateRepository.GetEditPostPollTime(chatId) != null || _stateRepository.GetEditPostUploadTime(chatId) != null)
                    {
                        var newPost = new Post();
                        post.IsCancelled = 1;
                        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        newPost = new Post
                        {
                            ChatId = chatId,
                            Text = newText,
                            PhotoPath = newPhoto,
                            Winners = newWinners,
                            //Prizes = prizes, unused
                            UploadTime = newUploadTime,
                            PollTime = newPollTime,
                            Channel = post.Channel,
                            TimeStamp = timestamp,
                            IsPublished = 0
                        };
                        db.PostDbSet.Add(newPost);


                        await db.SaveChangesAsync();

                        Post lastPost = await db.PostDbSet.FirstOrDefaultAsync(p => p.ChatId == chatId && p.TimeStamp == timestamp);

                        await ScheduleUploadPost(botClient, lastPost.PostId, db);
                        await botClient.SendMessage(chatId, $"Пост успешно изменен!\nОтправка поста в {lastPost.UploadTime}");

                    }
                    else
                    {
                        post.Text = newText;
                        post.PhotoPath = newPhoto;
                        post.Winners = newWinners;

                        //post.Prizes = newPrizes;

                        await botClient.SendMessage(chatId, $"Пост успешно изменен!\nОтправка поста в {post.UploadTime}");
                    }
                    await db.SaveChangesAsync();


                }
                else
                {
                    
                    //_stateRepository.FillEditPrizes(chatId, null);
                    await botClient.SendMessage(chatId, $"Изменения отменены!");
                }
                _stateRepository.FillEditPostText(chatId, null);
                _stateRepository.FillEditPostPhoto(chatId, null);
                _stateRepository.FillEditPostWinners(chatId, 0);
                _stateRepository.FillEditPostUploadTime(chatId, null);
                _stateRepository.FillEditPostPollTime(chatId, null);
                _stateRepository.UpdateUserState(chatId, UserState.PostMenu);
                await db.DisposeAsync();
            }
            else { await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет"); }

        }
        #endregion


        #region PostCancellation
        public static async Task StartCancellingPost(long chatId, ITelegramBotClient botClient)
        {

            string format = "yyyyMMddHHmmssfff";
            var db = new PostContext();

            List<Post> postList = await db.PostDbSet.Where(p => p.IsPublished == 0 && p.IsCancelled != 1).ToListAsync();
            string postListToString = "Доступные для удаления посты:\n";
            if (postList != null && postList.Count != 0)
            {
                foreach (Post post in postList)
                {
                    DateTime timeStamp = DateTime.ParseExact
                    (
                        post.TimeStamp,
                        "yyyyMMddHHmmssfff",
                        CultureInfo.InvariantCulture
                    );

                    string creationTime = timeStamp.ToString("HH:mm dd.MM");

                    postListToString += $"ID: {post.PostId}, Время создания: {creationTime}\n";

                }
                await botClient.SendMessage(chatId, postListToString);
                await botClient.SendMessage(chatId, "Введите id поста, который вы хотите удалить");
                _stateRepository.UpdateUserState(chatId, UserState.CancelPostId);

            }
            else if (postList.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нет доступных для удаления постов");
                _stateRepository.UpdateUserState(chatId, UserState.PostMenu);
            }
        }
        public static async Task ChooseCancelPostId(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new PostContext();
            try
            {
                int postId = Int32.Parse(message);
                bool postExist = await db.PostDbSet.AnyAsync(p => p.PostId == postId && p.IsPublished == 0);
                if (postExist)
                {
                    _stateRepository.FillCancelPostId(chatId, postId);
                    Console.WriteLine($"айди удаляемого поста: {postId}");
                    _stateRepository.UpdateUserState(chatId, UserState.ConfirmCancellationPost);

#pragma warning disable CS8600
                    Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();
#pragma warning restore CS8600


                    await botClient.SendMessage(chatId, $"Содержание поста: \nID:{postId} \nТекст:{post.Text}\nКоличество победителей:{post.Winners}\nВремя отправки:{post.UploadTime}\nВремя розыгрыша: {post.PollTime}\nГруппа: {post.Channel}");
                    await botClient.SendMessage(chatId, "Подтвердить удаление? Да/Нет");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Поста с заданным id не существует либо уже отправлен! Введите верный id");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chatId, "Введите верный id");
            }
            await db.DisposeAsync();
        }
        public static async Task ConfirmCancellationPost(long chatId, ITelegramBotClient botClient, string message)
        {
            if (message.ToLower() == "нет" || message.ToLower() == "да")
            {
                var db = new PostContext();
                int postId = _stateRepository.GetCancelPostId(chatId);
                Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();

                if (message.ToLower() == "да")
                {
                    post.IsCancelled = 1;
                    await db.SaveChangesAsync();
                    await botClient.SendMessage(chatId, $"Пост id{postId} успешно удален!");
                }
                else
                {
                    _stateRepository.FillCancelPostId(chatId, 0);
                    await botClient.SendMessage(chatId, $"Удаление поста отменено!");
                }

                _stateRepository.UpdateUserState(chatId, UserState.PostMenu);

                await db.DisposeAsync();

            }
            else { await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет"); }
        }
        #endregion


        #region PostInstaUpload

        public static async Task StartInstaPost(long chatId, ITelegramBotClient botClient)
        {
            string format = "yyyyMMddHHmmssfff";
            var db = new PostContext();

            List<Post> postList = await db.PostDbSet.Where(p => p.IsPublished == 0 && p.IsCancelled != 1).ToListAsync();
            string postListToString = "Доступные для отправки посты:\n";
            if (postList != null && postList.Count != 0)
            {
                foreach (Post post in postList)
                {
                    DateTime timeStamp = DateTime.ParseExact
                    (
                        post.TimeStamp,
                        "yyyyMMddHHmmssfff",
                        CultureInfo.InvariantCulture
                    );

                    string creationTime = timeStamp.ToString("HH:mm dd.MM");

                    postListToString += $"ID: {post.PostId}, Время создания: {creationTime}\n";

                }
                await botClient.SendMessage(chatId, postListToString);
                await botClient.SendMessage(chatId, "Введите id поста, который вы хотите отправить прямо сейчас");
                _stateRepository.UpdateUserState(chatId, UserState.InstaPostId);

            }
            else if (postList.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нет доступных для отправки постов");
                _stateRepository.UpdateUserState(chatId, UserState.PostMenu);
            }
            await db.DisposeAsync();

        }
        public static async Task ChooseInstaPostId(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new PostContext();
            try
            {
                int postId = Int32.Parse(message);
                bool postExist = await db.PostDbSet.AnyAsync(p => p.PostId == postId && p.IsPublished == 0);
                if (postExist)
                {
                    _stateRepository.FillInstaPostId(chatId, postId);
                    Console.WriteLine($"айди отправляемого поста: {postId}");
                    _stateRepository.UpdateUserState(chatId, UserState.ConfirmInstaPost);

#pragma warning disable CS8600
                    Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();
#pragma warning restore CS8600


                    await botClient.SendMessage(chatId, $"Содержание поста: \nID:{postId} \nТекст:{post.Text}\nКоличество победителей:{post.Winners}\nВремя отправки:{post.UploadTime}\nВремя розыгрыша: {post.PollTime}\nГруппа: {post.Channel}");
                    await botClient.SendMessage(chatId, "Подтвердить отправку? Да/Нет");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Поста с заданным id не существует либо уже отправлен! Введите верный id");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chatId, "Введите верный id");
            }
            await db.DisposeAsync();
        }
        public static async Task ConfirmInstaPost(long chatId, ITelegramBotClient botClient, string message)

        /*
        await SendInChannel(postId, freshDb, botClient);
        freshPost.IsPublished = 1;
        await freshDb.SaveChangesAsync();
        */
        {
            if (message.ToLower() == "нет" || message.ToLower() == "да")
            {
                var db = new PostContext();
                int postId = _stateRepository.GetInstaPostId(chatId);
                Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();

                if (message.ToLower() == "да")
                {
                    await SendInChannel(postId, db, botClient);
                    post.IsPublished = 1;
                    await db.SaveChangesAsync();
                    await botClient.SendMessage(chatId, $"Пост id{postId} успешно отправлен!");
                }
                else
                {
                    _stateRepository.FillInstaPostId(chatId, 0);
                    await botClient.SendMessage(chatId, $"Действие отменено!");
                }

                _stateRepository.UpdateUserState(chatId, UserState.PostMenu);

                await db.DisposeAsync();

            }
            else { await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет"); }
        }

        #endregion


        #endregion

        #region ----------------------------------------------------------GIVEAWAY------------------------------------------------------------
        public static async Task GiveawayWinners(int postId, int winnersCount, ITelegramBotClient botClient)
        {
            var db = new GiveawayContext();
            int count = db.GiveawayParticipants.Count(p => p.GiveawayId == postId);
            var rand = new Random();
            List<GiveawayParticipant> randomRecords = new List<GiveawayParticipant>();
            bool AreWinners = true;
            if (count <= winnersCount && count > 0)
            {
                randomRecords = db.GiveawayParticipants
                    .Where(x => x.GiveawayId == postId)
                    .AsEnumerable() // Переводим в память
                    .OrderBy(x => rand.Next()) // Используем локальный Random
                    .Take(count)
                    .ToList();
            }
            else if (count > 0)
            {
                randomRecords = db.GiveawayParticipants
                    .Where(x => x.GiveawayId == postId)
                    .AsEnumerable() // Переводим в память
                    .OrderBy(x => rand.Next()) // Используем локальный Random
                    .Take(winnersCount)
                    .ToList();
            }
            else
            {
                AreWinners = false;
            }
	    
            List<string> winnerUsernames = new List<string>();      
            int i = 0;
            List<long> winnerUserIds = new List<long>();
            foreach (GiveawayParticipant winner in randomRecords)
            {
                i++;
                winnerUsernames.Add($"{i}. {winner.Username}");
                winnerUserIds.Add(winner.UserId);
                db.Winners.Add(new Winner
                {
                    GiveawayId = winner.GiveawayId,
                    UserId = winner.UserId,
                    Username = winner.Username
                });

            }

            await PostResults(db, botClient, postId, winnerUsernames, count, AreWinners);

            await db.SaveChangesAsync();
            //await db.DisposeAsync();
	         
        }
        public static async Task PostResults(GiveawayContext db, ITelegramBotClient botClient, int postId, List<string> winnerUsernames, int participants, bool AreWinners)
        {
            var postdb = new PostContext();
            var postList = postdb.PostDbSet.Where(p => p.PostId == postId);
            var post = postList.FirstOrDefault();
            var channel = post.Channel;
            Console.WriteLine($"Розыгрыш завершен в группе {channel}");
            if (AreWinners)
            {
                string winnersMarkedList = string.Join("\n", winnerUsernames);
                await botClient.SendMessage(
                    channel,
                    $"Победители:\n{winnersMarkedList}\n\nВсего участников: {participants}"
                    );
                Console.WriteLine($"Победители:\n{winnersMarkedList}\n\nВсего участников: {participants}");
            }
            else
            {
                await botClient.SendMessage(
                    channel,
                    $"Победителей нет"
                    );
                Console.WriteLine("Победителей нет");
            }

            await postdb.DisposeAsync();
        }


        #region GiveawayInstaFinish
        public static async Task StartInstaGiveaway(long chatId, ITelegramBotClient botClient)
        {
            string format = "yyyyMMddHHmmssfff";
            var db = new PostContext();

            List<Post> postList = await db.PostDbSet.Where(p => p.IsEnded == 0 && p.IsCancelled != 1).ToListAsync();
            string postListToString = "Доступные для завершения розыгрыши:\n";
            if (postList != null && postList.Count != 0)
            {
                foreach (Post post in postList)
                {
                    DateTime timeStamp = DateTime.ParseExact
                    (
                        post.TimeStamp,
                        "yyyyMMddHHmmssfff",
                        CultureInfo.InvariantCulture
                    );

                    string creationTime = timeStamp.ToString("HH:mm dd.MM");

                    postListToString += $"ID: {post.PostId}, Время создания: {creationTime}\n";

                }
                await botClient.SendMessage(chatId, postListToString);
                await botClient.SendMessage(chatId, "Введите id розыгрыша, который вы хотите завершить прямо сейчас");
                _stateRepository.UpdateUserState(chatId, UserState.InstaGiveawayId);

            }
            else if (postList.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нет активных розыгрышей");
                _stateRepository.UpdateUserState(chatId, UserState.PostMenu);
            }

        }
        public static async Task ChooseInstaGiveawayId(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new PostContext();
            try
            {
                int postId = Int32.Parse(message);
                bool postExist = await db.PostDbSet.AnyAsync(p => p.PostId == postId && p.IsEnded == 0);
                if (postExist)
                {
                    _stateRepository.FillInstaGiveawayId(chatId, postId);
                    Console.WriteLine($"айди завершаемого розыгрыша: {postId}");
                    _stateRepository.UpdateUserState(chatId, UserState.ConfirmInstaGiveaway);

#pragma warning disable CS8600
                    Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();
#pragma warning restore CS8600


                    await botClient.SendMessage(chatId, $"Содержание поста: \nID:{postId} \nТекст:{post.Text}\nКоличество победителей:{post.Winners}\nВремя отправки:{post.UploadTime}\nВремя розыгрыша: {post.PollTime}\nГруппа: {post.Channel}");
                    await botClient.SendMessage(chatId, "Подтвердить отправку? Да/Нет");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Поста с заданным id не существует либо уже отправлен! Введите верный id");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chatId, "Введите верный id");
            }
            await db.DisposeAsync();
        }
        public static async Task ConfirmInstaGiveaway(long chatId, ITelegramBotClient botClient, string message)

        
        {
            if (message.ToLower() == "нет" || message.ToLower() == "да")
            {
                try
                {
                    var db = new PostContext();
                    int postId = _stateRepository.GetInstaGiveawayId(chatId);
                    Post post = await db.PostDbSet.Where(p => p.PostId == postId).FirstOrDefaultAsync();

                    if (message.ToLower() == "да")
                    {
                        await GiveawayWinners(postId, post.Winners, botClient);
                        post.IsEnded = 1;
                        await db.SaveChangesAsync();
                        await botClient.SendMessage(chatId, $"Пост id{postId} успешно отправлен!");
                    }
                    else
                    {
                        _stateRepository.FillInstaGiveawayId(chatId, 0);
                        await botClient.SendMessage(chatId, $"Отправка поста отменена!");
                    }

                    _stateRepository.UpdateUserState(chatId, UserState.PostMenu);

                    await db.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());   
                }

            }
            else { await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет"); }
        }



        #endregion

        #endregion
    }
}
