using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TgBot1.UserStateRepository;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgBot1.commands
{
    class BroadCommands
    {

        public static readonly UserStateRepository _stateRepository = Program._stateRepository;
        private static Timer? _timerBroad;
        public static string photosDirectory = Program.photosDirectory;
        #region ----------------------------------------------------------BROADCAST-----------------------------------------------------------

        #region Создание Рассылки
        public static async Task StartBroadcastCreation(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            int subscribers;
            using (var db = new GiveawayContext())
            {
                subscribers = await db.Subscribers.CountAsync();
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: $"Текущее количество подписчиков на рассылку: {subscribers}",
                cancellationToken: cancellationToken);
            await botClient.SendMessage(
                chatId: chatId,
                text: "Отправьте мне текст рассылки",
                cancellationToken: cancellationToken);
            _stateRepository.UpdateUserState(userId, UserState.AwaitingTextBroad);
        }
        public static async Task SaveBroadText(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {
                _stateRepository.UpdateUserState(userId, UserState.AwaitingPhotoBroad);
                var userData = _stateRepository.GetUserData(userId);
                _stateRepository.FillBroadText(userId, messageText);
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
        public static async Task SavePhotoBroad(long chatId, CancellationToken cancellationToken, long userId, Telegram.Bot.Types.Message message, ITelegramBotClient botClient)
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
            _stateRepository.FillPhotoPathBroad(userId, filePath);
            _stateRepository.UpdateUserState(userId, UserState.AwaitingBroadTime);
            string sendPath = _stateRepository.GetPhotoPathBroad(userId);
            Console.Write("фото сохранено по пути: " + sendPath + "\n");
            await botClient.SendMessage(chatId, "Напишите время отправки поста в формате ЧЧ:ММ ДД.ММ");

        }
        public static async Task SaveBroadTime(long chatId, CancellationToken cancellationToken, long userId, string messageText, ITelegramBotClient botClient)
        {
            try
            {
                if (DateTime.TryParseExact(messageText, "HH:mm dd.MM", null, System.Globalization.DateTimeStyles.None, out DateTime userDateTime))
                {
                    DateTime currentDateTime = DateTime.Now;

                    if (userDateTime > currentDateTime)
                    {
                        _stateRepository.FillBroadTime(chatId, messageText);
                        _stateRepository.FillTimeStampBroad(userId, $"{DateTime.Now:yyyyMMddHHmmssfff}");
                        string tempMessage = _stateRepository.GetBroadTime(userId);

                        await botClient.SendMessage(chatId, $"Рассылка будет отправлена в {tempMessage}");
                        Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");

                        _stateRepository.UpdateUserState(userId, UserState.BroadMenu);
                        var bdb = new BroadContext();
                        await SaveBroadToDatabase(chatId, bdb, cancellationToken, userId, botClient);

                        string postText = _stateRepository.GetBroadText(userId);

                        Broadcast lastBroad = bdb.Broadcasts.Where(p => p.TimeStamp == _stateRepository.GetTimeStampBroad(userId)).FirstOrDefault();
                        string lastPath = lastBroad.BroadPhotoPath;
                        int broadId = lastBroad != null ? lastBroad.Id : 0;


                        await ScheduleUploadBroad(botClient, broadId, bdb);
                        await bdb.DisposeAsync();
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
        #endregion


        #region Отправка Рассылки и БД

        public static async Task SendBroad(int broadId, BroadContext bdb, ITelegramBotClient botClient)
        {
            var gdb = new GiveawayContext();
            List<long> subscribersIds = await gdb.Subscribers.Select(p => p.UserId).Distinct().ToListAsync();
            try
            {
                Broadcast broad = await bdb.Broadcasts.FirstOrDefaultAsync(p => p.Id == broadId && p.IsPublished != 1 && p.IsCancelled != 1);


                bool IsPhoto = false;
                if (broad.BroadPhotoPath != null)
                {
                    IsPhoto = true;

                }

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
                await bdb.SaveChangesAsync();
            }
            catch { }
            await gdb.DisposeAsync();

        }

        public static async Task ScheduleUploadBroad(ITelegramBotClient botClient, int broadId, BroadContext bdb)
        {
            string format = "HH:mm dd.MM";
            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime dateNow = DateTime.Now;

            Broadcast broad = await bdb.Broadcasts.FirstOrDefaultAsync(p => p.Id == broadId && p.IsPublished != 1);

            string uploadTime = broad.BroadTime;

            DateTime dateUpload = DateTime.ParseExact(uploadTime, format, provider);
            TimeSpan differencePost = dateUpload - dateNow;
            double totalMinutesPost = differencePost.TotalMinutes;



            if (dateUpload > dateNow)
            {
                _timerBroad = new Timer(async _ =>
                {
                    using (var freshdb = new BroadContext()) { await SendBroad(broadId, freshdb, botClient); }
                },
                null,
                TimeSpan.FromMinutes(totalMinutesPost),
                TimeSpan.Zero);

            }
            else
            {
                broad.IsPublished = 1;
                await bdb.SaveChangesAsync();
            }
        }



        public static async Task SaveBroadToDatabase(long chatId, BroadContext bdb, CancellationToken cancellationToken, long userId, ITelegramBotClient botClient)
        {

            string broadText = _stateRepository.GetBroadText(chatId);

            string broadPhotoPath = _stateRepository.GetPhotoPathBroad(chatId);
            bool IsPhotoPath;
            if (broadPhotoPath == null) { IsPhotoPath = false; } else { IsPhotoPath = true; }


            string broadTime = _stateRepository.GetBroadTime(userId);  // Время отправки  
            string broadTimeStamp = _stateRepository.GetTimeStampBroad(userId);



            var newBroad = new Broadcast();

            if (IsPhotoPath == true)
            {
                newBroad = new Broadcast
                {
                    BroadText = broadText,
                    BroadPhotoPath = broadPhotoPath,
                    BroadTime = broadTime,
                    TimeStamp = broadTimeStamp,
                    IsPublished = 0
                };
            }
            else
            {
                newBroad = new Broadcast
                {
                    BroadText = broadText,
                    BroadTime = broadTime,
                    TimeStamp = broadTimeStamp,
                    IsPublished = 0
                };
            }
            bdb.Broadcasts.Add(newBroad);
            await bdb.SaveChangesAsync();
            var lastPost = bdb.Broadcasts.FirstOrDefaultAsync(p => p.TimeStamp == broadTimeStamp);
        }

        #endregion


        #region Редактирование Рассылки


        public static async Task StartBroadEdition(long chatId, ITelegramBotClient botClient)
        {
            var db = new BroadContext();
            List<Broadcast> broadList = await db.Broadcasts.Where(p => p.IsPublished == 0 && p.IsCancelled != 1).ToListAsync();

            Console.WriteLine($"broadlist = {broadList}, broadlist.Count = {broadList.Count}");

            string broadListToString = "Доступные для редактирования рассылки:\n";
            if (broadList != null && broadList.Count != 0)
            {
                foreach (Broadcast broad in broadList)
                {
                    DateTime timeStamp = DateTime.ParseExact
                    (
                        broad.TimeStamp,
                        "yyyyMMddHHmmssfff",
                        CultureInfo.InvariantCulture
                    );

                    string creationTime = timeStamp.ToString("HH:mm dd.MM");

                    broadListToString += $"ID: {broad.Id}, Время создания: {creationTime}\n";

                }
                await botClient.SendMessage(chatId, broadListToString);
                await botClient.SendMessage(chatId, "Введите id рассылки, которую вы хотите отредактировать");

                _stateRepository.UpdateUserState(chatId, UserState.EditingBroadId);

            }
            else if (broadList.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нет доступных рассылок для редактирования");
                _stateRepository.UpdateUserState(chatId, UserState.BroadMenu);
            }
            await db.DisposeAsync();
        }

        public static async Task ChooseEditBroadId(long chatId, ITelegramBotClient botClient, string message) // UserState.EditingBroadId
        {
            var db = new BroadContext();
            try
            {
                int broadId = Int32.Parse(message);
                bool broadExist = await db.Broadcasts.AnyAsync(p => p.Id == broadId && p.IsPublished == 0);
                if (broadExist)
                {
                    _stateRepository.FillEditBroadId(chatId, broadId);
                    Console.WriteLine($"айди изменяемой рассылки: {broadId}");
                    _stateRepository.UpdateUserState(chatId, UserState.EditingBroadText);

#pragma warning disable CS8600
                    string oldText = await db.Broadcasts.Where(p => p.Id == broadId).Select(p => p.BroadText).FirstOrDefaultAsync();
#pragma warning restore CS8600

                    await botClient.SendMessage(chatId, "Введите новый текст, либо напишите Далее");
                    await botClient.SendMessage(chatId, $"Предыдущий текст:\n {oldText}");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Рассылки с заданным id не существует либо она уже отправлена! Введите верный id");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chatId, "Введите верный id");
            }

            await db.DisposeAsync();
        }
        public static async Task EditBroadText(long chatId, ITelegramBotClient botClient, string message) // UserState.EditingBroadText
        {
            var db = new BroadContext();
            int broadId = _stateRepository.GetEditBroadId(chatId);
            if (message.ToLower() != "далее")
            {
                _stateRepository.FillEditBroadText(chatId, message);
                Console.WriteLine($"Текст поста с айди {broadId} был заменен на: {message}");
            }
            else { _stateRepository.FillEditBroadText(chatId, null); }
            _stateRepository.UpdateUserState(chatId, UserState.EditingBroadPhoto);



#pragma warning disable CS8600
            string oldPhoto = await db.Broadcasts.Where(p => p.Id == broadId).Select(p => p.BroadPhotoPath).FirstOrDefaultAsync();
#pragma warning restore CS8600


            Console.WriteLine($"Путь до предыдущего фото: {oldPhoto}");
            await botClient.SendMessage(chatId, "Отправьте новое фото, либо напишите Далее");
            if (oldPhoto != null)
            {
                try
                {
                    await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, $"Предыдущее фото");
                    }

                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            }
            await db.DisposeAsync();
        }
        public static async Task EditBroadPhoto(long chatId, ITelegramBotClient botClient, Telegram.Bot.Types.Message message)
        {

            var db = new BroadContext();
            int broadId = _stateRepository.GetEditBroadId(chatId);

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
                _stateRepository.FillEditBroadPhoto(chatId, filePath);

                _stateRepository.UpdateUserState(chatId, UserState.EditingBroadTime);
                await botClient.SendMessage(chatId, "Запишите новое время отправки в формате ЧЧ:ММ ДД.ММ либо Далее");
                string oldUploadTime = await db.Broadcasts.Where(p => p.Id == broadId).Select(p => p.BroadTime).FirstOrDefaultAsync();
                await botClient.SendMessage(chatId, $"Текущее время:\n{oldUploadTime}");
            }
            else if (message.Type == MessageType.Text && message.Text.ToLower() == "далее")
            {
                _stateRepository.FillEditBroadPhoto(chatId, null);
                _stateRepository.UpdateUserState(chatId, UserState.EditingBroadTime);
                await botClient.SendMessage(chatId, "Запишите новое время отправки в формате ЧЧ:ММ ДД.ММ либо Далее");
                string oldUploadTime = await db.Broadcasts.Where(p => p.Id == broadId).Select(p => p.BroadTime).FirstOrDefaultAsync();
                await botClient.SendMessage(chatId, $"Текущее время:\n{oldUploadTime}");

            }
            else
            {
                await botClient.SendMessage(chatId, $"Отправьте фото");
            }
            await db.DisposeAsync();
        }

        public static async Task EditBroadUploadTime(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new BroadContext();
            int broadId = _stateRepository.GetEditBroadId(chatId);
            Broadcast broad = await db.Broadcasts.Where(p => p.Id == broadId).FirstOrDefaultAsync();

            if (message.ToLower() != "далее")
            {
                try
                {
                    if (DateTime.TryParseExact(message, "HH:mm dd.MM", null, System.Globalization.DateTimeStyles.None, out DateTime newUploadTime))
                    {
                        DateTime currentDateTime = DateTime.Now;
                        string format = "HH:mm dd.MM";






                        if (newUploadTime > currentDateTime)
                        {
                            _stateRepository.FillEditBroadUploadTime(chatId, message);

                            //Сюда
                            string oldText = broad.BroadText;
                            string oldPhoto = broad.BroadPhotoPath;
                            string oldUploadTime = broad.BroadTime;

                            string? newText;
                            string? newPhoto;
                            int newWinners;


                            if (_stateRepository.GetEditBroadText(chatId) != null) { newText = _stateRepository.GetEditBroadText(chatId); }
                            else { newText = oldText; }

                            if (_stateRepository.GetEditBroadPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditBroadPhoto(chatId); }
                            else { newPhoto = oldPhoto; }



                            string oldBroad = $"СТАРАЯ РАССЫЛКА\nТекст: {oldText}\nВремя отправки: {oldUploadTime}";
                            string newBroad = $"НОВАЯ РАССЫЛКА\nТекст: {newText}\nВремя отправки: {newUploadTime}";

                            if (oldPhoto != null)
                            {
                                await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                                {
                                    await botClient.SendPhoto(chatId, sendImageStream, oldBroad);
                                }
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, oldBroad);
                            }
                            if (newPhoto != null)
                            {
                                await using (var sendImageStream = System.IO.File.Open(newPhoto, FileMode.Open))
                                {
                                    await botClient.SendPhoto(chatId, sendImageStream, newBroad);
                                }
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, newBroad);
                            }

                            await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет");
                            _stateRepository.UpdateUserState(chatId, UserState.ConfirmBroadEdition);

                            string tempMessage = _stateRepository.GetEditBroadUploadTime(chatId);
                            Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");
                            _stateRepository.UpdateUserState(chatId, UserState.ConfirmBroadEdition);
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
                _stateRepository.FillEditBroadUploadTime(chatId, null);
                //Сюда
                string oldText = broad.BroadText;
                string oldPhoto = broad.BroadPhotoPath;
                string oldUploadTime = broad.BroadTime;

                string? newText;
                string? newPhoto;
                string newUploadTime = oldUploadTime;

                if (_stateRepository.GetEditBroadText(chatId) != null) { newText = _stateRepository.GetEditBroadText(chatId); }
                else { newText = oldText; }

                if (_stateRepository.GetEditBroadPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditBroadPhoto(chatId); }
                else { newPhoto = oldPhoto; }



                string oldBroad = $"СТАРАЯ РАССЫЛКА\nТекст: {oldText}\nВремя отправки: {oldUploadTime}";
                string newBroad = $"НОВАЯ РАССЫЛКА\nТекст: {newText}\nВремя отправки: {newUploadTime}";

                if (oldPhoto != null)
                {
                    await using (var sendImageStream = System.IO.File.Open(oldPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, oldBroad);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, oldBroad);
                }
                if (newPhoto != null)
                {
                    await using (var sendImageStream = System.IO.File.Open(newPhoto, FileMode.Open))
                    {
                        await botClient.SendPhoto(chatId, sendImageStream, newBroad);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, newBroad);
                }

                await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет");
                _stateRepository.UpdateUserState(chatId, UserState.ConfirmBroadEdition);

                string tempMessage = _stateRepository.GetEditBroadUploadTime(chatId);
                Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");
                _stateRepository.UpdateUserState(chatId, UserState.ConfirmBroadEdition);

            }
            await db.DisposeAsync();
        }

        public static async Task ConfirmBroadEdition(long chatId, ITelegramBotClient botClient, string message)
        {
            if (message.ToLower() == "нет" || message.ToLower() == "да")
            {
                var db = new BroadContext();
                int broadId = _stateRepository.GetEditBroadId(chatId);
                Broadcast broad = await db.Broadcasts.Where(p => p.Id == broadId).FirstOrDefaultAsync();

                if (message.ToLower() == "да")
                {
                    string oldText = broad.BroadText;
                    string oldPhoto = broad.BroadPhotoPath;
                    string oldUploadTime = broad.BroadTime;

                    string? newText;
                    string? newPhoto;
                    string? newUploadTime;

                    if (_stateRepository.GetEditBroadText(chatId) != null) { newText = _stateRepository.GetEditBroadText(chatId); }
                    else { newText = oldText; }

                    if (_stateRepository.GetEditBroadPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditBroadPhoto(chatId); }
                    else { newPhoto = oldPhoto; }

                    if (_stateRepository.GetEditBroadUploadTime(chatId) != null) { newUploadTime = _stateRepository.GetEditBroadUploadTime(chatId); }
                    else { newUploadTime = oldUploadTime; }


                    if (_stateRepository.GetEditBroadUploadTime(chatId) != null)
                    {
                        var newBroad = new Broadcast();
                        broad.IsCancelled = 1;
                        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        newBroad = new Broadcast
                        {
                            BroadText = newText,
                            BroadPhotoPath = newPhoto,
                            BroadTime = newUploadTime,
                            TimeStamp = timestamp,
                            IsPublished = 0,
                            IsCancelled = 0
                        };
                        db.Broadcasts.Add(newBroad);


                        await db.SaveChangesAsync();

                        Broadcast lastBroad = await db.Broadcasts.FirstOrDefaultAsync(p => p.TimeStamp == timestamp);

                        await ScheduleUploadBroad(botClient, lastBroad.Id, db);
                        await botClient.SendMessage(chatId, $"Рассылка успешно изменена!\nОтправка рассылки в {lastBroad.BroadTime}");

                    }
                    else
                    {
                        broad.BroadText = newText;
                        broad.BroadPhotoPath = newPhoto;

                        await botClient.SendMessage(chatId, $"Рассылка успешно изменена!\nОтправка рассылки в {broad.BroadTime}");
                    }
                    await db.SaveChangesAsync();


                }
                else
                {
                    _stateRepository.FillEditBroadText(chatId, null);
                    _stateRepository.FillEditBroadPhoto(chatId, null);
                    _stateRepository.FillEditBroadUploadTime(chatId, null);
                    await botClient.SendMessage(chatId, $"Изменения отменены!");
                }
                _stateRepository.UpdateUserState(chatId, UserState.BroadMenu);
                await db.DisposeAsync();
            }
            else { await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет"); }
        }


        /*
        public static async Task EditBroadUploadTime(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new BroadContext();
            int broadId = _stateRepository.GetEditBroadId(chatId);
            Broadcast broad = await db.Broadcasts.Where(p => p.Id == broadId).FirstOrDefaultAsync();

            if (message.ToLower() != "далее")
            {
                try
                {
                    if (DateTime.TryParseExact(message, "HH:mm dd.MM", null, System.Globalization.DateTimeStyles.None, out DateTime userDateTime))
                    {
                        DateTime currentDateTime = DateTime.Now;

                        if (userDateTime > currentDateTime)
                        {
                            _stateRepository.FillEditBroadUploadTime(chatId, message);


                            //сюда надо ту штуку

                            string tempMessage = _stateRepository.GetEditBroadUploadTime(chatId);
                            Console.Write("во временное хранилище записано время отправки: " + tempMessage + "\n");
                            _stateRepository.UpdateUserState(chatId, UserState.ConfirmBroadEdition);
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
                _stateRepository.FillEditBroadUploadTime(chatId, null);
                _stateRepository.UpdateUserState(chatId, UserState.ConfirmBroadEdition);
                string oldText = broad.BroadText;
                string oldPhoto = broad.BroadPhotoPath;
               
                //string oldUploadTime = post.UploadTime;
                //string oldPrizes = post.Prizes;

                string? newText;
                string? newPhoto;
                int newWinners;
                string? newUploadTime;
                string? newPollTime;
                //string? newPrizes;

                if (_stateRepository.GetEditBroadText(chatId) != null) { newText = _stateRepository.GetEditBroadText(chatId); }
                else { newText = oldText; }

                if (_stateRepository.GetEditBroadPhoto(chatId) != null) { newPhoto = _stateRepository.GetEditBroadPhoto(chatId); }
                else { newPhoto = oldPhoto; }

                

                //if (_stateRepository.GetEditUploadTime(chatId) != null) { newUploadTime = _stateRepository.GetEditUploadTime(chatId); }
                //else { newUploadTime = oldUploadTime; }

                

                //if (_stateRepository.GetEditPrizes(chatId) != null) { newPrizes = _stateRepository.GetEditPrizes(chatId); }
                //else { newPrizes = oldPrizes; }


                string oldPost = $"СТАРЫЙ ПОСТ\nТекст: {oldText}\nВремя отправки: {oldUploadTime}\nВремя розыгрыша:{oldPollTime}";
                string newPost = $"НОВЫЙ ПОСТ\nТекст: {newText}\nВремя отправки: {newUploadTime}\nВремя розыгрыша:{newPollTime}";

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

        */

        #endregion


        #region BroadCancellation
        public static async Task StartBroadCancellation(long chatId, ITelegramBotClient botClient)
        {

            string format = "yyyyMMddHHmmssfff";
            var db = new BroadContext();

            List<Broadcast> broadList = await db.Broadcasts.Where(p => p.IsPublished == 0 && p.IsCancelled != 1).ToListAsync();
            string postListToString = "Доступные для удаления рассылки:\n";
            if (broadList != null && broadList.Count != 0)
            {
                foreach (Broadcast broad in broadList)
                {
                    DateTime timeStamp = DateTime.ParseExact
                    (
                        broad.TimeStamp,
                        "yyyyMMddHHmmssfff",
                        CultureInfo.InvariantCulture
                    );

                    string creationTime = timeStamp.ToString("HH:mm dd.MM");

                    postListToString += $"ID: {broad.Id}, Время создания: {creationTime}\n";

                }
                await botClient.SendMessage(chatId, postListToString);
                await botClient.SendMessage(chatId, "Введите id рассылки, которую вы хотите удалить");
                _stateRepository.UpdateUserState(chatId, UserState.CancelBroadId);

            }
            else if (broadList.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нет доступных для удаления рассылок");
                _stateRepository.UpdateUserState(chatId, UserState.BroadMenu);
            }
            await db.DisposeAsync();
        }
        public static async Task ChooseCancelBroadId(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new BroadContext();
            try
            {
                int broadId = Int32.Parse(message);
                bool broadExist = await db.Broadcasts.AnyAsync(p => p.Id == broadId && p.IsPublished == 0);
                if (broadExist)
                {
                    _stateRepository.FillCancelBroadId(chatId, broadId);
                    Console.WriteLine($"айди удаляемой рассылки: {broadId}");
                    _stateRepository.UpdateUserState(chatId, UserState.ConfirmCancellationBroad);

#pragma warning disable CS8600
                    Broadcast broad = await db.Broadcasts.Where(p => p.Id == broadId).FirstOrDefaultAsync();
#pragma warning restore CS8600


                    await botClient.SendMessage(chatId, $"Содержание рассылки: \nID:{broadId} \nТекст:{broad.BroadText}\nВремя отправки:{broad.BroadTime}");
                    await botClient.SendMessage(chatId, "Подтвердить удаление? Да/Нет");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Рассылки с заданным id не существует либо она уже отправлена! Введите верный id");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chatId, "Введите верный id");
            }
            await db.DisposeAsync();
        }
        public static async Task ConfirmCancellationBroad(long chatId, ITelegramBotClient botClient, string message)
        {
            if (message.ToLower() == "нет" || message.ToLower() == "да")
            {
                var db = new BroadContext();
                int broadId = _stateRepository.GetCancelBroadId(chatId);
                Broadcast broad = await db.Broadcasts.Where(p => p.Id == broadId).FirstOrDefaultAsync();

                if (message.ToLower() == "да")
                {
                    broad.IsCancelled = 1;
                    await db.SaveChangesAsync();
                    await botClient.SendMessage(chatId, $"Рассылка id{broadId} успешно удалена!");
                }
                else
                {
                    _stateRepository.FillCancelBroadId(chatId, 0);
                    await botClient.SendMessage(chatId, $"Удаление рассылки отменено!");
                }

                _stateRepository.UpdateUserState(chatId, UserState.BroadMenu);

                await db.DisposeAsync();

            }
            else { await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет"); }
        }
        #endregion


        #region BroadInstaUpload

        public static async Task StartInstaBroad(long chatId, ITelegramBotClient botClient)
        {
            string format = "yyyyMMddHHmmssfff";
            var db = new BroadContext();

            List<Broadcast> broadList = await db.Broadcasts.Where(p => p.IsPublished == 0 && p.IsCancelled != 1).ToListAsync();
            string postListToString = "Доступные для отправки рассылки:\n";
            if (broadList != null && broadList.Count != 0)
            {
                foreach (Broadcast broad in broadList)
                {
                    DateTime timeStamp = DateTime.ParseExact
                    (
                        broad.TimeStamp,
                        "yyyyMMddHHmmssfff",
                        CultureInfo.InvariantCulture
                    );

                    string creationTime = timeStamp.ToString("HH:mm dd.MM");

                    postListToString += $"ID: {broad.Id}, Время создания: {creationTime}\n";

                }
                await botClient.SendMessage(chatId, postListToString);
                await botClient.SendMessage(chatId, "Введите id рассылки, которую вы хотите отправить прямо сейчас");
                _stateRepository.UpdateUserState(chatId, UserState.InstaBroadId);

            }
            else if (broadList.Count == 0)
            {
                await botClient.SendMessage(chatId, "Нет доступных для отправки рассылок");
                _stateRepository.UpdateUserState(chatId, UserState.BroadMenu);
            }
            await db.DisposeAsync();

        }
        public static async Task ChooseInstaBroadId(long chatId, ITelegramBotClient botClient, string message)
        {
            var db = new BroadContext();
            try
            {
                int broadId = Int32.Parse(message);
                bool broadExist = await db.Broadcasts.AnyAsync(p => p.Id == broadId && p.IsPublished == 0);
                if (broadExist)
                {
                    _stateRepository.FillInstaBroadId(chatId, broadId);
                    Console.WriteLine($"айди отправляемого поста: {broadId}");
                    _stateRepository.UpdateUserState(chatId, UserState.ConfirmInstaBroad);

#pragma warning disable CS8600
                    Broadcast broad = await db.Broadcasts.Where(p => p.Id == broadId).FirstOrDefaultAsync();
#pragma warning restore CS8600


                    await botClient.SendMessage(chatId, $"Содержание рассылки: \nID:{broadId} \nТекст:{broad.BroadText}\nВремя отправки:{broad.BroadTime}");
                    await botClient.SendMessage(chatId, "Подтвердить отправку? Да/Нет");
                }
                else
                {
                    await botClient.SendMessage(chatId, "Рассылки с заданным id не существует либо она уже отправлена! Введите верный id");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendMessage(chatId, "Введите верный id");
            }
            await db.DisposeAsync();
        }
        public static async Task ConfirmInstaBroad(long chatId, ITelegramBotClient botClient, string message)


        {
            if (message.ToLower() == "нет" || message.ToLower() == "да")
            {
                var db = new BroadContext();
                int broadId = _stateRepository.GetInstaBroadId(chatId);
                Broadcast broad = await db.Broadcasts.Where(p => p.Id == broadId).FirstOrDefaultAsync();

                if (message.ToLower() == "да")
                {
                    await SendBroad(broadId, db, botClient);
                    broad.IsPublished = 1;
                    await db.SaveChangesAsync();
                    await botClient.SendMessage(chatId, $"Пост id{broadId} успешно отправлен!");
                }
                else
                {
                    _stateRepository.FillInstaBroadId(chatId, 0);
                    await botClient.SendMessage(chatId, $"Действие отменено!");
                }

                _stateRepository.UpdateUserState(chatId, UserState.BroadMenu);

                await db.DisposeAsync();

            }
            else { await botClient.SendMessage(chatId, $"Подтвердить изменения? Да/Нет"); }
        }

        #endregion





        #endregion
    }
}
