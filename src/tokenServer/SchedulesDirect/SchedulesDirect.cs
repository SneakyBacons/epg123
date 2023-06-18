﻿using epg123;
using GaRyan2.SchedulesDirectAPI;
using GaRyan2.Utilities;
using System;
using System.Net.Http;
using System.Threading;
using tokenServer;
using static GaRyan2.BaseAPI;

namespace GaRyan2
{
    public static class SchedulesDirect
    {
        private static API api;
        private static Timer _timer;

        public static string Token { get; private set; }
        public static DateTime TokenTimestamp = DateTime.MinValue;
        public static bool GoodToken;
        public static string Username { get; private set; }
        public static string PasswordHash { get; private set; }
        public static string ApiBaseAddress { get; private set; }
        public static string ApiBaseArtwork { get; private set; }

        /// <summary>
        /// Initializes the http client to communicate with Schedules Direct
        /// </summary>
        public static void Initialize()
        {
            RefreshConfiguration();
        }

        public static void RefreshConfiguration()
        {
            // load epg123 config file
            epgConfig config = Helper.ReadXmlFile(Helper.Epg123CfgPath, typeof(epgConfig)) ?? new epgConfig();
            if (api == null || api.BaseAddress != config.BaseApiUrl || api.BaseArtworkAddress != config.BaseArtworkUrl)
            {
                api = new API()
                {
                    BaseAddress = ApiBaseAddress = config.BaseApiUrl,
                    BaseArtworkAddress = ApiBaseArtwork = config.BaseArtworkUrl,
                    UserAgent = $"EPG123/{Helper.Epg123Version}"
                };
                api.Initialize();
                _ = GetToken(config.UserAccount?.LoginName, config.UserAccount?.PasswordHash);
            }
            else if (Username != config.UserAccount?.LoginName || PasswordHash != config.UserAccount?.PasswordHash)
            {
                _ = GetToken(config.UserAccount?.LoginName, config.UserAccount?.PasswordHash);
            }
            JsonImageCache.cacheRetention = config.CacheRetention;
        }

        /// <summary>
        /// Retrieves a session token from Schedules Direct
        /// </summary>
        /// <returns>true if successful</returns>
        public static bool GetToken()
        {
            return GetToken(Username, PasswordHash);
        }
        public static bool GetToken(string username = null, string password = null, bool requestNew = false)
        {
            if (!requestNew && DateTime.UtcNow - TokenTimestamp < TimeSpan.FromMinutes(1)) return true;
            if (username == null || password == null) return false;

            api.ClearToken();
            var ret = api.GetApiResponse<TokenResponse>(Method.POST, "token", new TokenRequest { Username = username, PasswordHash = password });
            if (ret != null && ret.Code == 0)
            {
                WebStats.IncrementTokenRefresh();
                Username = username; PasswordHash = password;
                GoodToken = true;
                TokenTimestamp = ret.Datetime;
                api.SetToken(Token = ret.Token);
                _timer = new Timer(TimerEvent);
                _ = _timer.Change(60000, 60000); // timer event every 60 seconds
                Logger.WriteInformation("Refreshed Schedules Direct API token.");
            }
            else
            {
                GoodToken = false;
                _timer = new Timer(TimerEvent);
                _timer.Change(900000, 900000); // timer event every 15 minutes
                Logger.WriteError("Did not receive a response from Schedules Direct for a token request.");
            }
            return ret != null;
        }

        public static HttpResponseMessage GetImage(string uri, DateTimeOffset ifModifiedSince)
        {
            return api.GetSdImage(uri.Substring(1), ifModifiedSince).Result;
        }

        private static void TimerEvent(object state)
        {
            if (DateTime.UtcNow - TokenTimestamp > TimeSpan.FromHours(23))
            {
                JsonImageCache.Save();
                GetToken();
            }
        }
    }
}