using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace XiaoZhi.Unity.MIoT
{
    public class MiAccount
    {
        public const string Miio = "xiaomiio";
        public const string Mina = "micoapi";
        private const string AccountDomain = "https://account.xiaomi.com";
        private const string MinaDomain = "https://api2.mina.mi.com";
        private const CloudServer DefaultCloudServer = CloudServer.cn;

        private static readonly Dictionary<int, string> VerifyUrlMap = new()
        {
            { 4, "/identity/auth/verifyPhone" },
            { 8, "/identity/auth/verifyEmail" }
        };

        public class Token
        {
            public string Region;
            public string DeviceId;
            public string Sid;
            public string UserId;
            public string PassToken;
            public string Ssecurity;
            public string ServiceToken;

            public bool IsValid => !string.IsNullOrEmpty(ServiceToken);
        }

        private readonly Token _token;

        public MiAccount(string sid)
        {
            _token = ReadToken(sid);
            if (string.IsNullOrEmpty(_token.DeviceId))
                _token.DeviceId = GetRandom(16);
        }

        public async UniTask<(bool, string)> MinaRequest(string uri, Dictionary<string, string> data = null)
        {
            if (!_token.IsValid) return (false, "Mina ServiceToken is null.");
            var method = data != null ? UnityWebRequest.kHttpVerbPOST : UnityWebRequest.kHttpVerbGET;
            var requestId = $"app_ios_{GetRandom(30)}";
            if (data != null) data["requestId"] = requestId;
            else uri += $"&requestId={requestId}";
            var url = $"{MinaDomain}{uri}";
            using var request = new UnityWebRequest(url, method);
            request.SetRequestHeader("User-Agent",
                "MiHome/6.0.103 (com.xiaomi.mihome; build:6.0.103.1; iOS 14.4.0) Alamofire/6.0.103 MICO/iOSApp/appStore/6.0.103");
            request.SetRequestHeader("Cookie", $"userId={_token.UserId};serviceToken={_token.ServiceToken};");
            if (data != null)
            {
                request.uploadHandler = new UploadHandlerRaw(UnityWebRequest.SerializeSimpleForm(data));
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                await request.SendWebRequest();
            }
            catch (Exception ex)
            {
                return (false, $"HTTP Request Error: {url}\n{ex}");
            }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        public async UniTask<(bool, string)> MiioRequest(string uri, string data = null)
        {
            if (!_token.IsValid) return (false, "Miio ServiceToken is null.");
            var regionPrefix = _token.Region == DefaultCloudServer.ToString() ? "" : _token.Region + ".";
            var domainUri = $"https://{regionPrefix}api.io.mi.com";
            var method = data != null ? UnityWebRequest.kHttpVerbPOST : UnityWebRequest.kHttpVerbGET;
            var url = $"{domainUri}/app{uri}";
            using var request = new UnityWebRequest(url, method);
            request.SetRequestHeader("User-Agent",
                "iOS-14.4-6.0.103-iPhone12,3--D7744744F7AF32F0544445285880DD63E47D9BE9-8816080-84A3F44E137B71AE-iPhone");
            request.SetRequestHeader("x-xiaomi-protocal-flag-cli", "PROTOCAL-HTTP2");
            request.SetRequestHeader("Cookie",
                $"PassportDeviceId={_token.DeviceId};userId={_token.UserId};serviceToken={_token.ServiceToken};");
            if (data != null)
            {
                request.uploadHandler =
                    new UploadHandlerRaw(UnityWebRequest.SerializeSimpleForm(SignData(uri, data, _token.Ssecurity)));
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                await request.SendWebRequest();
            }
            catch (Exception ex)
            {
                return (false, $"HTTP Request Error: {url}\n{ex}");
            }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        public async UniTask<(bool, string)> Login(string userId, string password)
        {
            var response1 = await RequestAccount($"{AccountDomain}/pass/serviceLogin?sid={_token.Sid}&_json=true");
            if (response1 == null) return (false, "Request pass token failed.");
            if (response1.Value<int>("code") == 0) return await RequestServiceToken(response1);
            var response2 = await RequestVerifyToken(response1, userId, password);
            var location = response2.Value<string>("location");
            if (!string.IsNullOrEmpty(location)) return await RequestServiceToken(response2);
            var verifyUrl = response2.Value<string>("notificationUrl");
            if (string.IsNullOrEmpty(verifyUrl)) return (false, response2.ToString());
            if (!verifyUrl.StartsWith("http")) verifyUrl = $"{AccountDomain}{verifyUrl}";
            return (false, verifyUrl);
        }

        public async UniTask<(bool, string)> Verify(string url, string ticket)
        {
            var (success, data, options) = await RequestIdentityList(url);
            if (!success) return (false, data);
            string location = null;
            string error = null;
            foreach (var flag in options)
            {
                var verifyUrl = VerifyUrlMap.GetValueOrDefault(flag);
                if (string.IsNullOrEmpty(url)) continue;
                var response = await RequestAccount(
                    $"{AccountDomain}{verifyUrl}?_dc={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    new Dictionary<string, string>
                    {
                        { "flag", flag.ToString() },
                        { "ticket", ticket },
                        { "trust", "true" },
                        { "_json", "true" }
                    }, new Dictionary<string, string>
                    {
                        { "identity_session", data }
                    });
                if (response.Value<int>("code") == 0)
                {
                    location = response.Value<string>("location");
                    break;
                }

                error = response.ToString();
            }

            if (string.IsNullOrEmpty(location)) return (false, error);
            await RequestAccount(location);
            return (true, null);
        }

        private async UniTask<(bool, string, int[])> RequestIdentityList(string url, string path = "identity/authStart")
        {
            url = url.Replace(path, "identity/list");
            var cookies = new Dictionary<string, string>();
            var response1 = await RequestAccount(url, null, cookies);
            if (!cookies.TryGetValue("identity_session", out var identitySession))
                return (false, $"Error when request {url}: missing identity_session in cookie.", null);
            var flag = response1.Value<int>("flag");
            if (flag == 0) flag = 4;
            var options = response1.Value<JArray>("options").Values<int>().ToArray();
            if (options.Length == 0) options = new[] { flag };
            return (true, identitySession, options);
        }

        private async UniTask<(bool, string)> RequestServiceToken(JObject json)
        {
            _token.UserId = json["userId"]!.Value<string>();
            _token.PassToken = json["passToken"]!.Value<string>();
            _token.Ssecurity = json["ssecurity"]!.Value<string>();
            var location = json["location"]!.Value<string>();
            var nonce = json["nonce"]!.Value<string>();
            var nsec = $"nonce={nonce}&{_token.Ssecurity}";
            var clientSign = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(nsec)));
            location += $"&clientSign={Uri.EscapeDataString(clientSign)}";
            using var secondRequest = new UnityWebRequest(location, UnityWebRequest.kHttpVerbGET);
            secondRequest.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                await secondRequest.SendWebRequest();
            }
            catch (Exception ex)
            {
                return (false, $"HTTP Request Error: {location}\n{ex}");
            }

            var jsonResponse = secondRequest.downloadHandler.text;
            Debug.Log($"response code: {secondRequest.responseCode}");
            Debug.Log("response: " + jsonResponse);
            if (secondRequest.result != UnityWebRequest.Result.Success) return (false, jsonResponse);
            var cookies = secondRequest.GetResponseHeader("Set-Cookie");
            if (string.IsNullOrEmpty(cookies)) return (false, $"Error when request {location}: missing set-cookie.");
            var serviceTokenMatch = Regex.Match(cookies, "serviceToken=([^;]+)");
            if (!serviceTokenMatch.Success)
                return (false, $"Error when request {location}: missing serviceToken in cookie.");
            _token.ServiceToken = serviceTokenMatch.Groups[1].Value;
            SaveToken(_token);
            return (true, _token.UserId);
        }

        private async UniTask<JObject> RequestVerifyToken(JObject json, string userId, string password)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            var passwordMd5 = BitConverter.ToString(hash).Replace("-", "").ToUpper();
            var data = new Dictionary<string, string>
            {
                { "_json", "true" },
                { "sid", json.Value<string>("sid") },
                { "user", userId },
                { "hash", passwordMd5 },
                { "qs", json.Value<string>("qs") },
                { "_sign", json.Value<string>("_sign") },
                { "callback", json.Value<string>("callback") }
            };
            return await RequestAccount($"{AccountDomain}/pass/serviceLoginAuth2", data);
        }

        private async UniTask<JObject> RequestAccount(string url, Dictionary<string, string> data = null,
            Dictionary<string, string> cookies = null)
        {
            using var request = new UnityWebRequest(url,
                data != null ? UnityWebRequest.kHttpVerbPOST : UnityWebRequest.kHttpVerbGET);
            request.SetRequestHeader("User-Agent",
                "APP/com.xiaomi.mihome APPV/6.0.103 iosPassportSDK/3.9.0 iOS/14.4 miHSTS");
            var cookie = "sdkVersion=3.9;";
            if (cookies != null)
                cookie = cookies.Aggregate(cookie, (current, one) => current + $"{one.Key}={one.Value};");
            request.SetRequestHeader("Cookie", cookie);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (data != null)
            {
                request.uploadHandler = new UploadHandlerRaw(UnityWebRequest.SerializeSimpleForm(data));
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            try
            {
                await request.SendWebRequest();
            }
            catch (Exception ex)
            {
                Debug.LogError($"HTTP Request Error: {url}\n{ex}");
                return null;
            }

            if (cookies != null)
            {
                var setCookie = request.GetResponseHeader("Set-Cookie");
                if (!string.IsNullOrEmpty(setCookie))
                {
                    var matches = Regex.Matches(setCookie, "([^;]+)=([^;]+)");
                    foreach (Match match in matches)
                    {
                        var key = match.Groups[1].Value;
                        var value = match.Groups[2].Value;
                        cookies[key] = value;
                    }
                }
            }

            var text = request.downloadHandler.text;
            return text.Length > 11 ? JObject.Parse(text[11..]) : null;
        }

        public void Logout()
        {
            _token.ServiceToken = null;
            SaveToken(_token);
        }

        public Token GetToken()
        {
            return _token;
        }

        private Token ReadToken(string sid)
        {
            var settings = new Settings("miot");
            var tokenStr = settings.GetString(sid);
            return string.IsNullOrEmpty(tokenStr)
                ? new Token { Sid = sid, Region = DefaultCloudServer.ToString() }
                : JsonConvert.DeserializeObject<Token>(tokenStr);
        }

        private void SaveToken(Token token)
        {
            var settings = new Settings("miot");
            settings.SetString(token.Sid, JsonConvert.SerializeObject(_token));
            settings.Save();
        }

        private static Dictionary<string, string> SignData(string uri, string data, string ssecurity)
        {
            var randomBytes = new byte[8];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(randomBytes);
            var time = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60);
            if (BitConverter.IsLittleEndian) time = BinaryPrimitives.ReverseEndianness(time);
            var timeBytes = BitConverter.GetBytes(time);
            var nonceBytes = randomBytes.Concat(timeBytes).ToArray();
            var nonce = Convert.ToBase64String(nonceBytes);
            var snonce = SignNonce(nonce, ssecurity);
            var msg = string.Join("&", uri, snonce, nonce, "data=" + data);
            var keyBytes = Convert.FromBase64String(snonce);
            var msgBytes = Encoding.UTF8.GetBytes(msg);
            using var hmac = new HMACSHA256(keyBytes);
            var signBytes = hmac.ComputeHash(msgBytes);
            var signature = Convert.ToBase64String(signBytes);
            return new Dictionary<string, string>
            {
                { "_nonce", nonce },
                { "data", data },
                { "signature", signature }
            };
        }

        private static string SignNonce(string nonce, string ssecurity)
        {
            using var sha256 = SHA256.Create();
            var ssecurityBytes = Convert.FromBase64String(ssecurity);
            var nonceBytes = Convert.FromBase64String(nonce);
            var combined = ssecurityBytes.Concat(nonceBytes).ToArray();
            var hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }

        private static string GetRandom(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new char[length];
            for (var i = 0; i < length; i++) result[i] = chars[random.Next(chars.Length)];
            return new string(result);
        }
    }
}