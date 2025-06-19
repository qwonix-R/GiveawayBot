using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TgBot1
{
    public class UserStateRepository
    {
        private static readonly Dictionary<long, UserData> _userStates = new();

        public UserData GetUserData(long userId)
        {
            if (!_userStates.ContainsKey(userId))
            {
                _userStates[userId] = new UserData
                {
                    UserId = userId,
                    CurrentState = UserState.Idle
                };
            }

            return _userStates[userId];
        }
        #region Fill/Get методы
        public void UpdateUserState(long userId, UserState newState)
        {
            var userData = GetUserData(userId);
            userData.CurrentState = newState;
            userData.LastActivity = DateTime.Now;
        }

        public void ClearTempData(long userId)
        {
            var userData = GetUserData(userId);
            userData.TempData.Clear();
        }

        public void FillPostText(long userId, string postText)
        {
            var userData = GetUserData(userId);
            userData.PostText = postText;
        }

        public string GetPostText(long userId)
        {
            var userData = GetUserData(userId);
            return userData.PostText;
        }
        public void FillPhotoPath(long userId, string photoPath)
        {
            var userData = GetUserData(userId);
            userData.PhotoPath = photoPath;
        }
        public string GetPhotoPath(long userId)
        {
            var userData = GetUserData(userId);
            return userData.PhotoPath;
        }
        public void FillWinners(long userId, int winners)
        {
            var userData = GetUserData(userId);
            userData.Winners = winners;
        }
        public int GetWinners(long userId)
        {
            var userData = GetUserData(userId);
            return userData.Winners;
        }
        public void FillPrizes(long userId, string prizes)
        {
            var userData = GetUserData(userId);
            userData.Prizes = prizes;
        }
        public string GetPrizes(long userId)
        {
            var userData = GetUserData(userId);
            return userData.Prizes;
        }
            
        public void FillUploadTime(long userId, string uploadTime)
        {
            var userData = GetUserData(userId);
            userData.UploadTime = uploadTime;
        }
        public string GetUploadTime(long userId)
        {
            var userData = GetUserData(userId);
            return userData.UploadTime;
        }
        public void FillPollTime(long userId, string pollTime)
        {
            var userData = GetUserData(userId);
            userData.PollTime = pollTime;
        }
        public string GetPollTime(long userId)
        {
            var userData = GetUserData(userId);
            return userData.PollTime;
        }
        public void FillChannel(long userId, string channel)
        {
            var userData = GetUserData(userId);
            userData.Channel = channel;
        }
        public string GetChannel(long userId)
        {
            var userData = GetUserData(userId);
            return userData.Channel;
        }
        public void FillTimeStamp(long userId, string timeStamp)
        {
            var userData = GetUserData(userId);
            userData.TimeStamp = timeStamp;
        }
        public string GetTimeStamp(long userId)
        {
            var userData = GetUserData(userId);
            return userData.TimeStamp;
        }

        public void FillBroadText(long userId, string broadText)
        {
            var userData = GetUserData(userId);
            userData.BroadText = broadText;
        }

        public string GetBroadText(long userId)
        {
            var userData = GetUserData(userId);
            return userData.BroadText;
        }
        public void FillPhotoPathBroad(long userId, string broadPhotoPath)
        {
            var userData = GetUserData(userId);
            userData.BroadPhotoPath = broadPhotoPath;
        }
        public string GetPhotoPathBroad(long userId)
        {
            var userData = GetUserData(userId);
            return userData.BroadPhotoPath;
        }
        public void FillBroadTime(long userId, string broadTime)
        {
            var userData = GetUserData(userId);
            userData.BroadTime = broadTime;
        }
        public string GetBroadTime(long userId)
        {
            var userData = GetUserData(userId);
            return userData.BroadTime;
        }
        public void FillTimeStampBroad(long userId, string broadTimeStamp)
        {
            var userData = GetUserData(userId);
            userData.BroadTimeStamp = broadTimeStamp;
        }
        public string GetTimeStampBroad(long userId)
        {
            var userData = GetUserData(userId);
            return userData.BroadTimeStamp;
        }
        public void FillBroadId(long userId, int broadId)
        {
            var userData = GetUserData(userId);
            userData.EditBroadId = broadId;
        }
        public int GetBroadId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditBroadId;
        }
        public void ClearBroadcastData(long userId)
        {
            // Получаем данные пользователя
            var userData = GetUserData(userId);

            // Очищаем все поля, связанные с редактированием рассылки
            userData.EditBroadId = 0;
            userData.BroadText = null;
            userData.BroadPhotoPath = null;
            userData.BroadTime = null;
            userData.BroadTimeStamp = null;

        }
        public void FillEditPostText(long userId, string editPostText)
        {
            var userData = GetUserData(userId);
            userData.EditPostText = editPostText;
        }

        public string GetEditPostText(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditPostText;
        }
        public void FillEditPostPhoto(long userId, string editPostPhoto)
        {
            var userData = GetUserData(userId);
            userData.EditPostPhotoPath = editPostPhoto;
        }
        public string GetEditPostPhoto(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditPostPhotoPath;
        }
        public void FillEditPostId(long userId, int editPostId)
        {
            var userData = GetUserData(userId);
            userData.EditPostId = editPostId;
        }
        public int GetEditPostId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditPostId;
        }
        public void FillEditPostWinners(long userId, int editWinners)
        {
            var userData = GetUserData(userId);
            userData.EditPostWinners = editWinners;
        }
        public int GetEditPostWinners(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditPostWinners;
        }
        /*
        public void FillEditPrizes(long userId, string editPrizes)
        {
            var userData = GetUserData(userId);
            userData.EditPrizes = editPrizes;
        }
        public string GetEditPrizes(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditPrizes;
        }*/

        public void FillEditPostUploadTime(long userId, string uploadTime)
        {
            var userData = GetUserData(userId);
            userData.EditPostUploadTime = uploadTime;
        }
        public string GetEditPostUploadTime(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditPostUploadTime;
        }
        public void FillEditPostPollTime(long userId, string pollTime)
        {
            var userData = GetUserData(userId);
            userData.EditPostPollTime = pollTime;
        }
        public string GetEditPostPollTime(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditPostPollTime;
        }
        public void FillCancelPostId(long userId, int cancelPostId)
        {
            var userData = GetUserData(userId);
            userData.CancelPostId = cancelPostId;
        }
        public int GetCancelPostId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.CancelPostId;
        }
        public void FillInstaPostId(long userId, int instaPostId)
        {
            var userData = GetUserData(userId);
            userData.InstaPostId = instaPostId;
        }
        public int GetInstaPostId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.InstaPostId;
        }
        public void FillInstaGiveawayId(long userId, int instaGiveawayId)
        {
            var userData = GetUserData(userId);
            userData.InstaGiveawayId = instaGiveawayId;
        }
        public int GetInstaGiveawayId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.InstaGiveawayId;
        }


        public void FillEditBroadId(long userId, int editBroadId)
        {
            var userData = GetUserData(userId);
            userData.EditBroadId = editBroadId;
        }
        public int GetEditBroadId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditBroadId;
        }
        public void FillEditBroadText(long userId, string editPostText)
        {
            var userData = GetUserData(userId);
            userData.EditBroadText = editPostText;
        }

        public string GetEditBroadText(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditBroadText;
        }
        public void FillEditBroadPhoto(long userId, string editBroadPhoto)
        {
            var userData = GetUserData(userId);
            userData.EditBroadPhotoPath = editBroadPhoto;
        }
        public string GetEditBroadPhoto(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditBroadPhotoPath;
        }
        public void FillEditBroadUploadTime(long userId, string uploadTime)
        {
            var userData = GetUserData(userId);
            userData.EditBroadUploadTime = uploadTime;
        }
        public string GetEditBroadUploadTime(long userId)
        {
            var userData = GetUserData(userId);
            return userData.EditBroadUploadTime;
        }




        public void FillCancelBroadId(long userId, int cancelBroadId)
        {
            var userData = GetUserData(userId);
            userData.CancelBroadId = cancelBroadId;
        }
        public int GetCancelBroadId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.CancelBroadId;
        }
        public void FillInstaBroadId(long userId, int instaBroadId)
        {
            var userData = GetUserData(userId);
            userData.InstaBroadId = instaBroadId;
        }
        public int GetInstaBroadId(long userId)
        {
            var userData = GetUserData(userId);
            return userData.InstaBroadId;
        }


        #endregion


        public class UserData
        {

            public long UserId { get; set; }
            public UserState CurrentState { get; set; }
            public Dictionary<string, object> TempData { get; set; } = new();

            // Можно добавить другие свойства пользователя
            public DateTime LastActivity { get; set; }
            public string PostText { get; set; }
            public string PhotoPath { get; set; }
            public int Winners { get; set; }
            public string Prizes { get; set; }
            public string UploadTime { get; set; }
            public string PollTime { get; set; }
            public string Channel {  get; set; }
            public string TimeStamp { get; set; }

            

            public int EditPostId { get; set; }
            public string? EditPostText {  get; set; }
            public string? EditPostPhotoPath {  get; set; }
            public int EditPostWinners { get; set; }
            public string? EditPostPrizes { get; set; }
            public string EditPostUploadTime { get; set; }
            public string EditPostPollTime { get; set; }
            public int CancelPostId { get; set; }
            public int InstaPostId  { get; set; }
            public int InstaGiveawayId { get; set; }

            public string BroadText { get; set; }   
            public string BroadPhotoPath { get; set; }
            public string BroadTime { get; set; }
            public string BroadTimeStamp { get; set; }
            public int EditBroadId { get; set; }
            public string? EditBroadText { get; set; }
            public string? EditBroadPhotoPath { get; set; }
            public string EditBroadUploadTime { get; set; }
            public int CancelBroadId { get; set; }
            public int InstaBroadId { get; set; }
        }
        public enum UserState
        {
            Idle,               // Ожидание команд

            PostMenu,
            BroadMenu,

            AwaitingText,       // Ожидание текста в Пост
            AwaitingPhoto,      // Ожидание фото в Пост
            AwaitingWinners,    // Ожидание количества победителей в Пост
            AwaitingPrizes,     // Ожидание описания призов в Пост
            AwaitingUploadTime, // Ожидание времени отправки поста в Пост
            AwaitingPollTime,   // Ожидание времени розыгрыша в Пост
            AwaitingChannel,
            AddingAdmin,

            CancelPostId,
            ConfirmCancellationPost,

            InstaPostId,
            ConfirmInstaPost,

            InstaGiveawayId,
            ConfirmInstaGiveaway,

            AwaitingTextBroad,      
            AwaitingPhotoBroad,
            AwaitingBroadTime,

            EditingBroadId,
            EditingBroadText,
            EditingBroadPhoto,
            EditingBroadTime,
            ConfirmBroadEdition,

            CancelBroadId,
            ConfirmCancellationBroad,

            InstaBroadId,
            ConfirmInstaBroad,

            EditingPostId,
            EditingPostText,
            EditingPostPhoto,
            EditingPostWinners,
            EditingPostPrizes, // unused
            EditingPostUploadTime,
            EditingPostPollTime,
            ConfirmPostEdition,

            StartingEditedBroadcast,
            InMultiStepDialog   // многошаговый диалог
        }
    }
}