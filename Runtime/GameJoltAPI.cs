using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using DateTime = System.DateTime;
using SimpleJSON;

namespace WattanaGaming.GameJoltAPI
{
    public class GameJoltAPI : MonoBehaviour
    {
        static GameJoltAPI _instance;
        public static GameJoltAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameJolt API Instance");
                    go.AddComponent<GameJoltAPI>();
                }

                return _instance;
            }
        }

        public static string gameID;
        public static string gameKey;

        // User credentials are cached for later use after authentication.
        public static string username { get; private set; }
        public static string userToken { get; private set; }

        public static bool isAuthenticated { get; private set; }
        public static bool isAuthenticating { get; private set; }

        private static string baseURL = "https://api.gamejolt.com/api/game/v1_2/";

        /// <summary>
        /// Gets invoked upon an authentication attempt. Boolean indicates whether the attempt is successful or not.
        /// </summary>
        public event System.Action<bool> OnAuthenticate;
        /// <summary>
        /// Gets invoked upon granting or revoking a trophy. Int indicates trophy ID and TrophyEventType indicates the event type(Grant or Revoke).
        /// </summary>
        public event System.Action<int, TrophyEventType> OnTrophy;

        void Awake()
        {
            _instance = this;
        }

        /// <summary>
        /// Authenticate a GameJolt user with the specified username and token.
        /// </summary>
        /// <param name="name">The user's GJ username.</param>
        /// <param name="token">The user's GJ token.</param>
        /// <param name="callback">Optional callback.</param>
        /// <param name="forced">Force a re-authentication.</param>
        public void Authenticate(string name, string token, System.Action<bool> callback = null, bool forced = false)
        {
            if ((isAuthenticated && !forced) || isAuthenticating)
            {
                Debug.LogWarning("Already authenticated or is currently authenticating.");
                return;
            }
            isAuthenticating = true;
            Debug.Log($"Attempting to authenticate as {name}...");
            APIRequest("users/auth/", new List<string>() { $"username={name}", $"user_token={token}" }, (JSONNode response) =>
             {
                 username = name;
                 userToken = token;
                 isAuthenticated = true;
                 isAuthenticating = false;
                 callback?.Invoke(true);
                 OnAuthenticate?.Invoke(true);
             },
            () =>
            {
                // Debug.LogError($"Authentication failed. {response["message"]}");
                username = userToken = "";
                isAuthenticated = false;
                isAuthenticating = false;
                callback?.Invoke(false);
                OnAuthenticate?.Invoke(false);
                return;
            });
        }

        /// <summary>
        /// Grant the user a trophy with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public void GrantTrophy(int id)
        {
            if (!isAuthenticated)
            {
                Debug.LogError("Attempt to grant trophy without an authenticated user.");
                return;
            }
            Debug.Log($"Granting trophy {id}...");
            APIRequest("trophies/add-achieved/", new List<string>() { $"username={username}", $"user_token={userToken}", $"trophy_id={id}" }, (JSONNode _) =>
            {
                Debug.Log("Trophy granted.");
                OnTrophy?.Invoke(id, TrophyEventType.Grant);
            });
        }

        /// <summary>
        /// Revoke a trophy with the specified ID from the user.
        /// </summary>
        /// <param name="id"></param>
        public void RevokeTrophy(int id)
        {
            if (!isAuthenticated)
            {
                Debug.LogError("Attempt to revoke trophy without an authenticated user.");
                return;
            }
            Debug.Log($"Revoking trophy {id}...");
            APIRequest("trophies/remove-achieved/", new List<string>() { $"username={username}", $"user_token={userToken}", $"trophy_id={id}" }, (JSONNode _) =>
            {
                Debug.Log("Trophy revoked.");
                OnTrophy?.Invoke(id, TrophyEventType.Revoke);
            });
        }

        /// <summary>
        /// Get informations about a trophy.
        /// </summary>
        /// <param name="id">The ID of the trophy to fetch.</param>
        /// <param name="callback">Optional callback</param>
        public void FetchTrophy(int id, System.Action<TrophyData> callback=null)
        {
            if (!isAuthenticated)
            {
                Debug.LogError("Attempt to fetch trophy data without an authenticated user.");
                return;
            }
            Debug.Log($"Fetching trophy data for {id}...");
            APIRequest("trophies/", new List<string>() { $"username={username}", $"user_token={userToken}", $"trophy_id={id}" }, (JSONNode response) =>
            {
                Debug.Log("Fetched trophy data.");
                TrophyData trophyData = new TrophyData(response["trophies"].AsArray[0]);
                callback?.Invoke(trophyData);
            });
        }

        /// <summary>
        /// Get a list of trophies
        /// </summary>
        /// <param name="all">Get all of the trophies if set to true.</param>
        /// <param name="achieved">Only list achieved trophies if true and vice versa. Will be ignored if `all` is true.</param>
        /// <param name="callback">Optional callback.</param>
        public void ListTrophies(bool all, bool achieved, System.Action<List<TrophyData>> callback = null)
        {
            if (!isAuthenticated)
            {
                Debug.LogError("Attempt to list trophies without an authenticated user.");
                return;
            }
            List<string> queries = new List<string>() { $"username={username}", $"user_token={userToken}" };
            if (!all)
            {
                queries.Add($"achieved={achieved.ToString().ToLower()}");
            }
            Debug.Log("Fetching trophy list...");
            APIRequest("trophies/", queries, (JSONNode response) =>
            {
                JSONArray trophies = response["trophies"].AsArray;
                List<TrophyData> trophyDatas = new List<TrophyData>();
                if (trophies != null)
                {
                    foreach (JSONObject trophy in trophies)
                    {
                        trophyDatas.Add(new TrophyData(trophy));
                    }
                }
                Debug.Log("Fetched trophy list.");
                callback?.Invoke(trophyDatas);
            });
        }

        public void GetServerTime(System.Action<DateTime> callback = null)
        {
            APIRequest("time/", new List<string>(), (JSONNode response) =>
            {
                DateTime serverTime = new DateTime(response["year"].AsInt, response["month"].AsInt, response["day"].AsInt, response["hour"].AsInt, response["minute"].AsInt, response["second"].AsInt);
                callback?.Invoke(serverTime);
            });
        }

        void APIRequest(string endpoint, List<string> queries, System.Action<JSONNode> OnSuccess, System.Action OnError = null)
        {
            string request = $"{baseURL}{endpoint}?";
            request += $"game_id={gameID}";
            foreach (string query in queries)
            {
                request += $"&{query}";
            }
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError($"Error: {response["message"]}");
                    OnError?.Invoke();
                    return;
                }
                OnSuccess?.Invoke(response);
            }));
        }

        private IEnumerator GetRequest(string uri, System.Action<UnityWebRequest> callback = null)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    Debug.LogError("Connection error whilst making a web request.");
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Data processing error whilst making a web request.");
                    break;
                case UnityWebRequest.Result.Success:
                    callback?.Invoke(webRequest);
                    break;
            }
        }

        private string AddSignature(string strToSign)
        {
            return $"{strToSign}&signature={Md5Sum(strToSign + gameKey)}";
        }
        
        private string Md5Sum(string strToEncrypt)
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
    }
}
