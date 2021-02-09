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
        /// Authenticate a GameJolt user with the specified token.
        /// </summary>
        /// <param name="name">The user's GJ username.</param>
        /// <param name="token">The user's GJ token.</param>
        /// <param name="callback">Optional callback.</param>
        public void Authenticate(string name, string token, System.Action callback = null)
        {
            string request = $"{baseURL}users/auth/?game_id={gameID}&username={name}&user_token={token}";
            Debug.Log("Attempting to authenticate as " + name + "...");
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError("Authentication failed. " + response["message"]);
                    username = userToken = "";
                    isAuthenticated = false;
                    OnAuthenticate?.Invoke(false);
                    return;
                }
                Debug.Log("Authentication successful.");
                username = name;
                userToken = token;
                isAuthenticated = true;
                callback?.Invoke();
                OnAuthenticate?.Invoke(true);
            }));
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
            string request = $"{baseURL}trophies/add-achieved/?game_id={gameID}&username={username}&user_token={userToken}&trophy_id={id}";
            Debug.Log("Granting trophy " + id + "...");
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError("Error: " + response["message"]);
                    return;
                }
                Debug.Log("Trophy granted.");
                OnTrophy?.Invoke(id, TrophyEventType.Grant);
            }));
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
            string request = $"{baseURL}trophies/remove-achieved/?game_id={gameID}&username={username}&user_token={userToken}&trophy_id={id}";
            Debug.Log("Revoking trophy " + id + "...");
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError("Error: " + response["message"]);
                    return;
                }
                Debug.Log("Trophy revoked.");
                OnTrophy?.Invoke(id, TrophyEventType.Revoke);
            }));
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
            string request = $"{baseURL}trophies/fetch/?game_id={gameID}&username={username}&user_token={userToken}&trophy_id={id}";
            Debug.Log("Fetching trophy data for " + id + "...");
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError("Error: " + response["message"]);
                    return;
                }
                Debug.Log("Fetched trophy data.");
                TrophyData trophyData = ConstructTrophy(response["trophies"].AsArray[0]);
                callback?.Invoke(trophyData);
            }));
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
            string request;
            if (all)
            {
                request = $"{baseURL}trophies/fetch/?game_id={gameID}&username={username}&user_token={userToken}";
            }
            else
            {
                request = $"{baseURL}trophies/fetch/?game_id={gameID}&username={username}&user_token={userToken}&achieved={achieved.ToString().ToLower()}";
            }
            Debug.Log("Fetching trophy list...");
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError("Error: " + response["message"]);
                    return;
                }
                JSONArray trophies = response["trophies"].AsArray;
                List<TrophyData> trophyDatas = new List<TrophyData>();
                if (trophies != null)
                {
                    foreach (JSONObject trophy in trophies)
                    {
                        trophyDatas.Add(ConstructTrophy(trophy));
                    }
                }
                Debug.Log("Fetched trophy list.");
                callback?.Invoke(trophyDatas);
            }));
        }

        /// <summary>
        /// Construct a TrophyData from a JSONNode
        /// </summary>
        /// <param name="trophy"></param>
        /// <returns></returns>
        TrophyData ConstructTrophy(JSONNode trophy)
        {
            TrophyData trophyData = new TrophyData();

            trophyData.id = trophy["id"];
            trophyData.title = trophy["title"];
            trophyData.difficulty = trophy["difficulty"];
            trophyData.description = trophy["description"];
            trophyData.imageURL = trophy["image_url"];
            trophyData.achieved = trophy["achieved"];

            return trophyData;
        }

        public void GetServerTime(System.Action<DateTime> callback = null)
        {
            string request = $"{baseURL}time/?game_id={gameID}";
            Debug.Log("Fetching GameJolt server time...");
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError("Error: " + response["message"]);
                    return;
                }
                Debug.Log("Fetched server time.");
                DateTime serverTime = new DateTime(response["year"].AsInt, response["month"].AsInt, response["day"].AsInt, response["hour"].AsInt, response["minute"].AsInt, response["second"].AsInt);
                callback?.Invoke(serverTime);
            }));
        }

        private IEnumerator GetRequest(string uri, System.Action<UnityWebRequest> callback = null)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            // Debug.Log("Sending web request and waiting for response...");
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
                    // Debug.Log("Response received.");
                    callback?.Invoke(webRequest);
                    break;
            }
        }

        private string AddSignature(string strToSign)
        {
            return strToSign + "&signature=" + Md5Sum(strToSign + gameKey);
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
