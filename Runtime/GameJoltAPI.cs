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

        public static string gameKey;
        public static string gameID;

        // User credentials are cached for later use after authentication.
        public static string username { get; private set; } = "";
        public static string userToken { get; private set; } = "";
        public static bool isAuthenticated { get; private set; }

        private static string baseURL = "https://api.gamejolt.com/api/game/v1_2/";

        public event System.Action<bool> onAuthenticate;

        void Awake()
        {
            _instance = this;
        }

        public void Authenticate(string name, string token, System.Action callback = null)
        {
            string request = baseURL + "users/auth/?game_id=" + gameID + "&username=" + name + "&user_token=" + token;
            Debug.Log("Attempting to authenticate as " + name + "...");
            StartCoroutine(GetRequest(AddSignature(request), (UnityWebRequest webRequest) =>
            {
                JSONNode response = JSON.Parse(webRequest.downloadHandler.text)["response"];
                if (response["success"] == "false")
                {
                    Debug.LogError("Authentication failed. " + response["message"]);
                    username = userToken = "";
                    isAuthenticated = false;
                    onAuthenticate.Invoke(false);
                    return;
                }
                Debug.Log("Authentication successful.");
                username = name;
                userToken = token;
                isAuthenticated = true;
                callback?.Invoke();
                onAuthenticate.Invoke(true);
            }));
        }

        public void GetServerTime(System.Action<DateTime> callback = null)
        {
            string request = baseURL + "time/?game_id=" + gameID;
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

        IEnumerator GetRequest(string uri, System.Action<UnityWebRequest> callback = null)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Debug.Log("Sending web request and waiting for response...");
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

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
