using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DateTime = System.DateTime;
using DateTimeOffset = System.DateTimeOffset;
using SimpleJSON;

namespace WattanaGaming.GameJoltAPI
{
    public class UserData
    {
        public int Id;
        public string AvatarURL;
        public string UserName;
        public string DisplayName;
        public string Description;
        public string Website;

        public string SignedUp;
        public DateTime SignedUpTime;
        public string LastLoggedIn;
        public DateTime LastLoggedInTime;

        public string Status;

        public UserData(JSONNode JSONData)
        {
            Id = JSONData["id"];
            AvatarURL = JSONData["avatar_url"];
            UserName = JSONData["username"];
            DisplayName = JSONData["developer_name"];
            Description = JSONData["developer_description"];
            Website = JSONData["developer_website"];

            SignedUp = JSONData["signed_up"];
            SignedUpTime = DateTimeOffset.FromUnixTimeSeconds(JSONData["signed_up_timestamp"].AsLong).DateTime;
            LastLoggedIn = JSONData["last_logged_in"];
            LastLoggedInTime = DateTimeOffset.FromUnixTimeSeconds(JSONData["last_logged_in_timestamp"].AsLong).DateTime;

            Status = JSONData["status"];
        }
    }
    
    public class TrophyData
    {
        public int Id;
        public string Title;
        public string Difficulty;
        public string Description;
        public string ImageURL;
        public string Achieved;

        public TrophyData(JSONNode JSONData)
        {
            Id = JSONData["id"];
            Title = JSONData["title"];
            Difficulty = JSONData["difficulty"];
            Description = JSONData["description"];
            ImageURL = JSONData["image_url"];
            Achieved = JSONData["achieved"];
        }

        public override string ToString()
        {
            return $"ID: {Id}, Title: {Title}, Difficulty: {Difficulty}, Description: {Description}, Achieved: {Achieved}";
        }
    }

    public enum TrophyEventType
    {
        Grant,
        Revoke
    }

    public enum APIVersion
    {
        v1_2
    }

    public class APIError : System.Exception
    {
        public APIError(string message) : base(message) { }
    }
}
