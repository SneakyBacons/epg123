﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace tokenServer
{
    public static class TokenService
    {
        public static bool GoodToken;
        public static string Token = "";
        public static bool RefreshToken;
        private static DateTime lastRefresh = DateTime.MinValue;

        public static bool RefreshTokenFromSD()
        {
            if (DateTime.Now - lastRefresh < TimeSpan.FromMinutes(1)) return true;

            // get username and passwordhash
            epgConfig config;
            using (var stream = new StreamReader(Helper.Epg123CfgPath, Encoding.Default))
            {
                var serializer = new XmlSerializer(typeof(epgConfig));
                TextReader reader = new StringReader(stream.ReadToEnd());
                config = (epgConfig)serializer.Deserialize(reader);
                reader.Close();
            }
            if (config == null) goto End;

            // create the request with headers
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TokenRequest { Username = config.UserAccount.LoginName, PasswordHash = config.UserAccount.PasswordHash }));
            var req = (HttpWebRequest)WebRequest.Create($"{Helper.SdBaseName}/token");
            req.UserAgent = "EPG123";
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Accept = "application/json";
            req.ContentLength = body.Length;
            req.AutomaticDecompression = DecompressionMethods.Deflate;
            req.Timeout = 3000;

            // write the json username and password hash to the request stream
            var reqStream = req.GetRequestStream();
            reqStream.Write(body, 0, body.Length);
            reqStream.Close();

            // send request and get response
            try
            {
                using (var response = req.GetResponse() as HttpWebResponse)
                using (var sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var ret = JsonConvert.DeserializeObject<TokenResponse>(sr.ReadToEnd());
                    if (ret == null || ret.Code != 0) goto End;
                    WebStats.IncrementTokenRefresh();

                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\GaRyan2\epg123", RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        if (key != null)
                        {
                            key.SetValue("token", ret.Token);
                            key.SetValue("tokenExpires", ret.Datetime.ToLocalTime().AddDays(1));
                            Helper.WriteLogEntry("Refreshed token upon receiving an UNKNOWN_USER (5004) error code.");
                            lastRefresh = DateTime.Now;
                            return GoodToken = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing
            }

            End:
            Helper.WriteLogEntry("Failed to refresh token upon receiving an UNKNOWN_USER (5004) error code.");
            return GoodToken = false;
        }
    }

    public class BaseResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("serverID")]
        public string ServerId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("datetime")]
        public DateTime Datetime { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }
    }

    public class TokenRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string PasswordHash { get; set; }
    }

    public class TokenResponse : BaseResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }

    [XmlRoot("EPG123")]
    public class epgConfig
    {
        [XmlElement("UserAccount")]
        public SdUserAccount UserAccount { get; set; }
    }

    public class SdUserAccount
    {
        [XmlIgnore]
        private string _passwordHash;

        [XmlElement("LoginName")]
        public string LoginName { get; set; }

        [XmlElement("PasswordHash")]
        public string PasswordHash
        {
            get => _passwordHash;
            set => _passwordHash = value;
        }
    }
}