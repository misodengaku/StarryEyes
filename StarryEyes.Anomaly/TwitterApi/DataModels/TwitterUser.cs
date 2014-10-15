﻿using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterUser
    {
        public TwitterUser()
        {
            ScreenName = String.Empty;
        }

        public TwitterUser(dynamic json)
        {
            this.Id = ((string)json.id_str).ParseLong();
            this.ScreenName = ParsingExtension.ResolveEntity(json.screen_name);
            this.Name = ParsingExtension.ResolveEntity(json.name ?? String.Empty);
            this.Description = ParsingExtension.ResolveEntity(json.description ?? String.Empty);
            this.Location = ParsingExtension.ResolveEntity(json.location ?? String.Empty);
            this.Url = json.url;
            this.IsDefaultProfileImage = json.default_profile_image;
            this.ProfileImageUri = ((string)json.profile_image_url).ParseUri();
            this.ProfileBackgroundImageUri = ((string)json.profile_background_image_url).ParseUri();
            if (json.profile_banner_url())
            {
                this.ProfileBannerUri = ((string)json.profile_banner_url).ParseUri();
            }
            this.IsProtected = json["protected"];
            this.IsVerified = json.verified;
            this.IsTranslator = json.is_translator;
            this.IsContributorsEnabled = json.contributors_enabled;
            this.IsGeoEnabled = json.geo_enabled;
            this.StatusesCount = (long)((double?)json.statuses_count ?? default(double));
            this.FollowingsCount = (long)((double?)json.friends_count ?? default(double));
            this.FollowersCount = (long)((double?)json.followers_count ?? default(double));
            this.FavoritesCount = (long)((double?)json.favourites_count ?? default(double));
            this.ListedCount = (long)((double?)json.listed_count ?? default(double));
            this.Language = json.lang;
            this.CreatedAt = ((string)json.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat);
            if (json.entities())
            {
                if (json.entities.url())
                {
                    UrlEntities = Enumerable.ToArray(TwitterEntity.GetEntities(json.entities.url));
                }
                if (json.entities.description())
                {
                    DescriptionEntities = Enumerable.ToArray(TwitterEntity.GetEntities(json.entities.description));
                }
            }
        }

        public const string TwitterUserUrl = "https://twitter.com/{0}";
        public const string FavstarUserUrl = "http://favstar.fm/users/{0}";
        public const string TwilogUserUrl = "http://twilog.org/{0}";

        /// <summary>
        /// Exactly Numeric ID of this user. (PRIMARY KEY)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// ScreenName ( sometimes also call @ID ) of this user.
        /// </summary>
        [NotNull]
        public string ScreenName { get; set; }

        /// <summary>
        /// Name for the display of this user.
        /// </summary>
        [CanBeNull]
        public string Name { get; set; }

        /// <summary>
        /// Description of this user, also calls &quot;Bio&quot;
        /// </summary>
        [CanBeNull]
        public string Description { get; set; }

        /// <summary>
        /// Location of this user.
        /// </summary>
        [CanBeNull]
        public string Location { get; set; }

        /// <summary>
        /// Url of this user. <para />
        /// Warning: This property, named URL but, may not be exactly URI.
        /// </summary>
        [CanBeNull]
        public string Url { get; set; }

        /// <summary>
        /// Profile image is default or not.
        /// </summary>
        public bool IsDefaultProfileImage { get; set; }

        /// <summary>
        /// Profile image of this user.
        /// </summary>
        [CanBeNull]
        public Uri ProfileImageUri { get; set; }

        /// <summary>
        /// Profile background image of this user.
        /// </summary>
        [CanBeNull]
        public Uri ProfileBackgroundImageUri { get; set; }

        /// <summary>
        /// Profile background image of this user.
        /// </summary>
        [CanBeNull]
        public Uri ProfileBannerUri { get; set; }

        /// <summary>
        /// Flag for check protected of this user.
        /// </summary>
        public bool IsProtected { get; set; }

        /// <summary>
        /// Flag of this user is verified by twitter official.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Flag of this user works as translator.
        /// </summary>
        public bool IsTranslator { get; set; }

        /// <summary>
        /// Flag of this user using &quot;Writers&quot;
        /// </summary>
        public bool IsContributorsEnabled { get; set; }

        /// <summary>
        /// Flag of this user using &quot;geo&quot; feature.
        /// </summary>
        public bool IsGeoEnabled { get; set; }

        /// <summary>
        /// Amount of tweets of this user.
        /// </summary>
        public long StatusesCount { get; set; }

        /// <summary>
        /// Amount of friends(a.k.a followings) of this user.
        /// </summary>
        public long FollowingsCount { get; set; }

        /// <summary>
        /// Amount of followers of this user.
        /// </summary>
        public long FollowersCount { get; set; }

        /// <summary>
        /// Amount of favorites of this user.
        /// </summary>
        public long FavoritesCount { get; set; }

        /// <summary>
        /// Amount of listed by someone of this user.
        /// </summary>
        public long ListedCount { get; set; }

        /// <summary>
        /// Language of this user
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Created time of this user
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Entities of this user url
        /// </summary>
        [CanBeNull]
        public TwitterEntity[] UrlEntities { get; set; }

        /// <summary>
        /// Entities of this user description
        /// </summary>
        [CanBeNull]
        public TwitterEntity[] DescriptionEntities { get; set; }

        [NotNull]
        public string UserPermalink
        {
            get { return String.Format(TwitterUserUrl, ScreenName); }
        }

        [NotNull]
        public string FavstarUserPermalink
        {
            get { return String.Format(FavstarUserUrl, ScreenName); }
        }

        [NotNull]
        public string TwilogUserPermalink
        {
            get { return String.Format(TwilogUserUrl, ScreenName); }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this.Id == ((TwitterUser)obj).Id;
        }

        [NotNull]
        public string GetEntityAidedUrl()
        {
            if (this.UrlEntities != null)
            {
                var entity = this.UrlEntities.FirstOrDefault(u => u.EntityType == EntityType.Urls);
                if (entity != null)
                {
                    return entity.OriginalUrl;
                }
            }
            return Url ?? String.Empty;
        }

        [NotNull]
        public string GetEntityAidedDescription(bool showFullUrl = false)
        {
            var builder = new StringBuilder();
            var escaped = this.Description ?? String.Empty;
            TwitterEntity prevEntity = null;
            foreach (var entity in this.DescriptionEntities.Guard().OrderBy(e => e.StartIndex))
            {
                var pidx = 0;
                if (prevEntity != null)
                    pidx = prevEntity.EndIndex;
                if (pidx < entity.StartIndex)
                {
                    // output raw
                    builder.Append(ParsingExtension.ResolveEntity(escaped.Substring(pidx, entity.StartIndex - pidx)));
                }
                switch (entity.EntityType)
                {
                    case EntityType.Hashtags:
                        builder.Append("#" + entity.DisplayText);
                        break;
                    case EntityType.Urls:
                        builder.Append(showFullUrl
                                           ? ParsingExtension.ResolveEntity(entity.OriginalUrl)
                                           : ParsingExtension.ResolveEntity(entity.DisplayText));
                        break;
                    case EntityType.Media:
                        builder.Append(showFullUrl
                                           ? ParsingExtension.ResolveEntity(entity.MediaUrl)
                                           : ParsingExtension.ResolveEntity(entity.DisplayText));
                        break;
                    case EntityType.UserMentions:
                        builder.Append("@" + entity.DisplayText);
                        break;
                }
                prevEntity = entity;
            }
            if (prevEntity == null)
            {
                builder.Append(ParsingExtension.ResolveEntity(escaped));
            }
            else if (prevEntity.EndIndex < escaped.Length)
            {
                builder.Append(ParsingExtension.ResolveEntity(
                    escaped.Substring(prevEntity.EndIndex, escaped.Length - prevEntity.EndIndex)));
            }
            return builder.ToString();
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}