using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace XiaoZhi.Unity.MIOT
{
    public class MiAccount
    {
        private class Token
        {
            public string Region;
            public string DeviceId;
            public string Sid;
            public string UserId;
            public string PassToken;
            public string Ssecurity;
            public string ServiceToken;
        }

        private const string Miio = "xiaomiio";
        private const string Mina = "micoapi";

        private Token _token;
        private Func<UniTask<(string, string, string)>> _inputProxy;

        public void SetInputProxy(Func<UniTask<(string, string, string)>> inputProxy)
        {
            _inputProxy = inputProxy;
        }

        public async UniTask<(bool, string)> MinaRequest(string uri, Dictionary<string, string> data = null)
        {
            _token ??= ReadToken(Mina);
            var isValidToken = !string.IsNullOrEmpty(_token.ServiceToken);
            if (!isValidToken)
            {
                var (success, error) = await Login(Mina);
                if (!success) return (false, error);
            }

            const string domainUri = "https://api2.mina.mi.com";
            var method = data != null ? UnityWebRequest.kHttpVerbPOST : UnityWebRequest.kHttpVerbGET;
            var requestId = $"app_ios_{GetRandom(30)}";
            if (data != null) data["requestId"] = requestId;
            else uri += $"&requestId={requestId}";
            var url = $"{domainUri}{uri}";
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
                return (false, $"HTTP Request Error: {ex}");
            }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        public async UniTask<(bool, string)> MiioRequest(string uri, string data = null)
        {
            Debug.Log(data);
            _token ??= ReadToken(Miio);
            var isValidToken = !string.IsNullOrEmpty(_token.ServiceToken);
            if (!isValidToken)
            {
                var (success, error) = await Login(Miio);
                if (!success) return (false, error);
            }

            var regionPrefix = _token.Region == "cn" ? "" : _token.Region + ".";
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
                return (false, $"HTTP Request Error: {ex}");
            }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        private async UniTask<(bool, string)> Login(string sid)
        {
            if (string.IsNullOrEmpty(_token.DeviceId))
                _token.DeviceId = GetRandom(16);
            // (_token.Region, _token.UserId, _token.PassToken) = await _inputProxy();
            _token.UserId = "2556468231";
            _token.PassToken =
                "V1:DXmurwq2/R1BHTELu6obCV2W16Ch9IjHqkrWktEk0sg5IViAJnt7Bwl8Pc3raeFQt2prC+QOxdpVUGVRo0GpXCnMuSqa7pyqov1hz2TLcNpglaYFRI+aJjbRuuMkQHCt2Ccn9LhRwIofNREc/9DsjwqXHmLYxw51zExWpIFk6MfXoUZe9f68D1P/wJ2/xr/Q430ny5GHE/SougB0EJmfdM8OGmfAn0dno5UjhwTvnOgkWg1WsNj+mr7iALa0Ipy2W1BhKAhyYmjPx58xhDvu04fVM9g/oOfr2aGP7o0FogfFp6jSvvsnA51nAc4na2QUlgiHiM6xJKyKQnfgWFOf+A==";
            const string domainUri = "https://account.xiaomi.com";
            var url = $"{domainUri}/pass/serviceLogin?sid={sid}&_json=true";
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.SetRequestHeader("User-Agent",
                "APP/com.xiaomi.mihome APPV/6.0.103 iosPassportSDK/3.9.0 iOS/14.4 miHSTS");
            request.SetRequestHeader("Cookie", $"sdkVersion=3.9;userId={_token.UserId};passToken={_token.PassToken};");
            request.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                await request.SendWebRequest();
            }
            catch (Exception ex)
            {
                return (false, $"HTTP Request Error: {ex}");
            }

            var jsonResponse = request.downloadHandler.text;
            if (request.result != UnityWebRequest.Result.Success) return (false, jsonResponse);
            var root = JObject.Parse(jsonResponse[11..]);
            if (!root.TryGetValue("code", out var jCode))
                return (false, $"Error when request {url}: missing 'code' in response.");
            var code = jCode.Value<int>();
            if (code != 0) return (false, $"Error when request {url}: code: {code}");
            _token.UserId = root["userId"]!.Value<string>();
            _token.PassToken = root["passToken"]!.Value<string>();
            _token.Ssecurity = root["ssecurity"]!.Value<string>();
            var location = root["location"]!.Value<string>();
            var nonce = root["nonce"]!.Value<string>();
            var nsec = $"nonce={nonce}&{_token.Ssecurity}";
            var clientSign = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(nsec)));
            url = $"{location}&clientSign={Uri.EscapeDataString(clientSign)}";
            using var secondRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            secondRequest.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                await secondRequest.SendWebRequest();
            }
            catch (Exception ex)
            {
                return (false, $"HTTP Request Error: {ex}");
            }

            jsonResponse = secondRequest.downloadHandler.text;
            Debug.Log($"response code: {secondRequest.responseCode}");
            Debug.Log("response: " + jsonResponse);
            if (secondRequest.result != UnityWebRequest.Result.Success) return (false, jsonResponse);
            var cookies = secondRequest.GetResponseHeader("Set-Cookie");
            if (string.IsNullOrEmpty(cookies)) return (false, $"Error when request {url}: missing set-cookie.");
            var serviceTokenMatch = System.Text.RegularExpressions.Regex.Match(cookies, "serviceToken=([^;]+)");
            if (!serviceTokenMatch.Success)
                return (false, $"Error when request {url}: missing serviceToken in cookie.");
            _token.ServiceToken = serviceTokenMatch.Groups[1].Value;
            SaveToken(_token);
            return (true, null);
        }

        private Token ReadToken(string sid)
        {
            var settings = new Settings("miot");
            var tokenStr = settings.GetString(sid);
            return string.IsNullOrEmpty(tokenStr)
                ? new Token { Sid = sid, Region = "cn" }
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
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new char[length];
            for (var i = 0; i < length; i++) result[i] = chars[random.Next(chars.Length)];
            return new string(result);
        }
    }
}