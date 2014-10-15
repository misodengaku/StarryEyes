﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Relations
    {
        #region Friends

        public static Task<ICursorResult<IEnumerable<long>>> GetFriendsIdsAsync(
            this IOAuthCredential credential, long userId,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetFriendsIdsCoreAsync(credential, userId, null, cursor, count);
        }

        public static Task<ICursorResult<IEnumerable<long>>> GetFriendsIdsAsync(
            this IOAuthCredential credential, string screenName,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetFriendsIdsCoreAsync(credential, null, screenName, cursor, count);
        }

        private static async Task<ICursorResult<IEnumerable<long>>> GetFriendsIdsCoreAsync(
            IOAuthCredential credential, long? userId, string screenName,
            long cursor, int? count)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"cursor", cursor},
                {"count", count}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("friends/ids.json", param));
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region Followers

        public static Task<ICursorResult<IEnumerable<long>>> GetFollowersIdsAsync(
            this IOAuthCredential credential, long userId,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetFollowersIdsCoreAsync(credential, userId, null, cursor, count);
        }

        public static Task<ICursorResult<IEnumerable<long>>> GetFollowersIdsAsync(
            this IOAuthCredential credential, string screenName,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetFollowersIdsCoreAsync(credential, null, screenName, cursor, count);
        }

        private static async Task<ICursorResult<IEnumerable<long>>> GetFollowersIdsCoreAsync(
            IOAuthCredential credential, long? userId, string screenName,
            long cursor, int? count)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"cursor", cursor},
                {"count", count}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("followers/ids.json", param));
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region Others

        public static async Task<IEnumerable<long>> GetNoRetweetsIdsAsync(
            this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var client = credential.CreateOAuthClient();
            var respStr = await client.GetStringAsync(new ApiAccess("friendships/no_retweets/ids.json"));
            return await Task.Run(() => ((dynamic[])DynamicJson.Parse(respStr)).Select(d => (long)d));
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetMuteIdsAsync(
            this IOAuthCredential credential, long cursor = -1)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            return await Task.Run(async () =>
            {
                var respStr = await client.GetStringAsync(new ApiAccess("mutes/users/ids.json", param));
                var parsed = DynamicJson.Parse(respStr);
                var ids = ((dynamic[])parsed.ids).Select(d => (long)d);
                return new CursorResult<IEnumerable<long>>(ids,
                    parsed.previous_cursor_str, parsed.next_cursor_str);
            });
        }

        #endregion

        #region Follow

        public static Task<TwitterUser> CreateFriendshipAsync(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return CreateFriendshipCoreAsync(credential, userId, null);
        }

        public static Task<TwitterUser> CreateFriendshipAsync(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return CreateFriendshipCoreAsync(credential, null, screenName);
        }

        private static async Task<TwitterUser> CreateFriendshipCoreAsync(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("friendships/create.json"), param);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region Remove

        public static Task<TwitterUser> DestroyFriendshipAsync(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return DestroyFriendshipCoreAsync(credential, userId, null);
        }

        public static Task<TwitterUser> DestroyFriendshipAsync(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return DestroyFriendshipCoreAsync(credential, null, screenName);
        }

        private static async Task<TwitterUser> DestroyFriendshipCoreAsync(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("friendships/destroy.json"), param);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region Show friendships

        public static Task<TwitterFriendship> ShowFriendshipAsync(
            this IOAuthCredential credential, long sourceId, long targetId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return ShowFriendshipCoreAsync(credential, sourceId, null, targetId, null);
        }

        public static Task<TwitterFriendship> ShowFriendshipAsync(
            this IOAuthCredential credential, string sourceScreenName, string targetScreenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (sourceScreenName == null) throw new ArgumentNullException("sourceScreenName");
            if (targetScreenName == null) throw new ArgumentNullException("targetScreenName");
            return ShowFriendshipCoreAsync(credential, null, sourceScreenName, null, targetScreenName);
        }

        public static async Task<TwitterFriendship> ShowFriendshipCoreAsync(
            IOAuthCredential credential, long? sourceId, string sourceScreenName,
                long? targetId, string targetScreenName)
        {
            var param = new Dictionary<string, object>
            {
                {"source_id", sourceId},
                {"source_screen_name", sourceScreenName},
                {"target_id", targetId},
                {"target_screen_name", targetScreenName},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("friendships/show.json", param));
            return await response.ReadAsFriendshipAsync();
        }

        #endregion

        #region Update friendships

        public static Task<TwitterFriendship> UpdateFriendshipAsync(
            this IOAuthCredential credential, long userId,
            bool? enableDeviceNotifications, bool? showRetweet)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return UpdateFriendshipCoreAsync(credential, userId, null, enableDeviceNotifications, showRetweet);
        }

        public static Task<TwitterFriendship> UpdateFriendshipAsync(
            this IOAuthCredential credential, string screenName,
            bool? enableDeviceNotifications, bool? showRetweet)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return UpdateFriendshipCoreAsync(credential, null, screenName, enableDeviceNotifications, showRetweet);
        }

        private static async Task<TwitterFriendship> UpdateFriendshipCoreAsync(
            IOAuthCredential credential, long? userId, string screenName,
            bool? enableDeviceNotifications, bool? showRetweet)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"device", enableDeviceNotifications},
                {"retweets", showRetweet},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("friendships/update.json"), param);
            return await response.ReadAsFriendshipAsync();
        }

        #endregion

        #region Mutes

        public static Task<TwitterUser> UpdateMuteAsync(this IOAuthCredential credential,
            long userId, bool mute)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return UpdateMuteCoreAsync(credential, userId, null, mute);
        }

        public static Task<TwitterUser> UpdateMuteAsync(
            this IOAuthCredential credential, string screenName, bool mute)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return UpdateMuteCoreAsync(credential, null, screenName, mute);
        }

        private static async Task<TwitterUser> UpdateMuteCoreAsync(
            IOAuthCredential credential, long? userId, string screenName, bool mute)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var endpoint = mute ? "mutes/users/create" : "mutes/users/destroy";
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess(endpoint), param);
            return await response.ReadAsUserAsync();
        }

        #endregion
    }
}
