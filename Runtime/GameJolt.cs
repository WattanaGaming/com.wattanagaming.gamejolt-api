using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using DateTime = System.DateTime;
using DateTimeOffset = System.DateTimeOffset;
using SimpleJSON;

namespace WattanaGaming.GameJoltAPI
{
    public static class GameJolt
    {
        public static string GameID;
        public static string GameKey;

        // User credentials are cached for later use after authentication.
        public static string Username { get; private set; }
        public static string UserToken { get; private set; }

        public static bool IsAuthenticated { get; private set; }
        public static bool IsAuthenticating { get; private set; }

        public static APIVersion Version = APIVersion.v1_2;

        // Public events for obvious stuff.
        public static event System.Action OnAuthenticated;
        public static event System.Action<TrophyEventType, int> OnTrophy;

        public static async Task Authenticate(string name, string token, bool forced = false)
        {
            if ((IsAuthenticated && !forced) || IsAuthenticating)
            {
                throw new APIError("Already authenticated or is currently authenticating.");
            }
            Debug.Log($"Attempting to authenticate as {name}...");
            IsAuthenticating = true;
            try
            {
                await APIRequest("users/auth/", new string[] { $"username={name}", $"user_token={token}" });
                Username = name;
                UserToken = token;
                IsAuthenticated = true;
                IsAuthenticating = false;
                Debug.Log("Successfully authenticated.");
                OnAuthenticated?.Invoke();
            }
            catch (APIError)
            {
                Username = UserToken = "";
                IsAuthenticated = false;
                IsAuthenticating = false;
                throw;
            }
        }

        public static async Task GrantTrophy(int id)
        {
            RequireAuthenticated("Attempt to grant trophy without an authenticated user.");
            Debug.Log($"Granting trophy {id}.");
            await APIRequest("trophies/add-achieved/", new string[] { $"username={Username}", $"user_token={UserToken}", $"trophy_id={id}" });
            Debug.Log("Trophy granted.");
            OnTrophy?.Invoke(TrophyEventType.Grant, id);
        }

        public static async Task RevokeTrophy(int id)
        {
            RequireAuthenticated("Attempt to revoke trophy without an authenticated user.");
            Debug.Log($"Revoking trophy {id}.");
            await APIRequest("trophies/removed-achieved/", new string[] { $"username={Username}", $"user_token={UserToken}", $"trophy_id={id}" });
            Debug.Log("Trophy revoked.");
            OnTrophy?.Invoke(TrophyEventType.Revoke, id);
        }

        public static async Task<TrophyData> FetchTrophy(int id)
        {
            RequireAuthenticated("Attempt to fetch trophy data without an authenticated user.");
            Debug.Log($"Fetching trophy data for {id}...");
            JSONNode response = await APIRequest("trophies/", new string[] { $"username={Username}", $"user_token={UserToken}", $"trophy_id={id}" });
            Debug.Log("Fetched trophy data.");
            TrophyData trophyData = new TrophyData(response["trophies"].AsArray[0]);
            return trophyData;
        }

        public static async Task<List<TrophyData>> ListTrophies(bool all = true, bool achieved = true)
        {
            RequireAuthenticated("Attempt to list trophies without an authenticated user.");
            List<string> queries = new List<string>() { $"username={Username}", $"user_token={UserToken}" };
            if (!all)
            {
                queries.Add($"achieved={achieved.ToString().ToLower()}");
            }

            Debug.Log("Fetching trophy list...");
            JSONArray trophies = (await APIRequest("trophies/", queries.ToArray()))["trophies"].AsArray;
            List<TrophyData> trophyDatas = new List<TrophyData>();
            if (trophies != null)
            {
                foreach (JSONObject trophy in trophies)
                {
                    trophyDatas.Add(new TrophyData(trophy));
                }
            }
            Debug.Log("Fetched trophy list.");
            return trophyDatas;

        }

        public static async Task<DateTime> GetServerTime(bool localTime = true)
        {
            JSONNode response = await APIRequest("time/", new string[] { });
            if (localTime)
            {
                return DateTimeOffset.FromUnixTimeSeconds(response["timestamp"].AsLong).ToLocalTime().DateTime;
            }
            else
            {
                return DateTimeOffset.FromUnixTimeSeconds(response["timestamp"].AsLong).DateTime;
            }
        }

        private static void RequireAuthenticated(string message = "Not authenticated")
        {
            if (!IsAuthenticated)
            {
                throw new APIError(message);
            }
        }

        private static async Task<JSONNode> APIRequest(string endpoint, params string[] queries)
        {
            string request = "";

            // Form a complete request
            switch (Version)
            {
                case APIVersion.v1_2:
                    request += "https://api.gamejolt.com/api/game/v1_2/";
                    break;
                default:
                    throw new APIError("Unsupported API version.");
            }
            request += $"{endpoint}?game_id={GameID}";
            foreach (string query in queries)
            {
                request += $"&{query}";
            }

            string result = await Helper.GET(AddSignature(request));
            JSONNode response = JSON.Parse(result)["response"];
            if (response["success"] == "false")
            {
                throw new APIError(response["message"]);
            }
            
            return response;
        }

        private static string AddSignature(string strToSign)
        {
            return $"{strToSign}&signature={Md5Sum(strToSign + GameKey)}";
        }

        private static string Md5Sum(string strToEncrypt)
        {
            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);

            // encrypt bytes
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);

            // Convert the encrypted bytes back to a string (base 16)
            string hashString = "";

            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
            }

            return hashString.PadLeft(32, '0');
        }

        public static class Helper
        {
            public static async Task<string> GET(string url)
            {
                UnityWebRequest webRequest = UnityWebRequest.Get(url);
                UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();
                while (!asyncOperation.isDone) { await Task.Yield(); }

                string result = "";

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        throw new System.Exception("A connection error has occured.");
                    case UnityWebRequest.Result.DataProcessingError:
                        throw new System.Exception("A data processing error has occured");
                    case UnityWebRequest.Result.ProtocolError:
                        throw new System.Exception("A protocol error has occured");
                    case UnityWebRequest.Result.Success:
                        result = webRequest.downloadHandler.text;
                        break;
                }

                return result;
            }
        }
    }
}
