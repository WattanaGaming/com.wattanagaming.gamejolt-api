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
        public int id;
        public string avatarURL;
        public string userName;
        public string displayName;
        public string description;
        public string website;
        public Type type;

        public string signedUp;
        public DateTime signedUpTime;
        public string lastLoggedIn;
        public DateTime lastLoggedInTime;

        public Status status;

        public UserData(JSONNode JSONData)
        {
            id = JSONData["id"];
            avatarURL = JSONData["avatar_url"];
            userName = JSONData["username"];
            displayName = JSONData["developer_name"];
            description = JSONData["developer_description"];
            website = JSONData["developer_website"];

            {
                string typeString = JSONData["type"];
                type = typeString switch
                {
                    "User" => Type.User,
                    "Developer" => Type.Developer,
                    "Moderator" => Type.Moderator,
                    "Administrator" => Type.Administrator,
                    _ => throw new APIError("Unknown user type.")
                };
            }

            signedUp = JSONData["signed_up"];
            signedUpTime = DateTimeOffset.FromUnixTimeSeconds(JSONData["signed_up_timestamp"].AsLong).DateTime;
            lastLoggedIn = JSONData["last_logged_in"];
            lastLoggedInTime = DateTimeOffset.FromUnixTimeSeconds(JSONData["last_logged_in_timestamp"].AsLong).DateTime;

            {
                string statusString = JSONData["status"];
                status = statusString switch
                {
                    "Active" => Status.Active,
                    "Banned" => Status.Banned,
                    _ => throw new APIError("Unknown user status.")
                };
            }
        }

        public enum Type
        {
            User,
            Developer,
            Moderator,
            Administrator
        }

        public enum Status
        {
            Active,
            Banned
        }
    }
    
    public class TrophyData
    {
        public int id;
        public string title;
        public Difficulty difficulty;
        public string description;
        public string imageURL;
        public string achieved;

        public TrophyData(JSONNode JSONData)
        {
            id = JSONData["id"];
            title = JSONData["title"];
            
            {
                string difficultyString = JSONData["difficulty"];
                difficulty = difficultyString switch
                {
                    "Bronze" => Difficulty.Bronze,
                    "Silver" => Difficulty.Silver,
                    "Gold" => Difficulty.Gold,
                    "Platinum" => Difficulty.Platinum,
                    _ => throw new APIError("Unknown trophy difficulty.")
                };
            }

            description = JSONData["description"];
            imageURL = JSONData["image_url"];
            achieved = JSONData["achieved"];
        }

        public enum Difficulty
        {
            Bronze,
            Silver,
            Gold,
            Platinum
        }

        public override string ToString()
        {
            return $"ID: {id}, Title: {title}, Difficulty: {difficulty}, Description: {description}, Achieved: {achieved}";
        }
    }

    public enum TrophyEventType
    {
        Grant,
        Revoke
    }

    public enum SessionStatus
    {
        Active,
        Idle
    }

    public enum APIVersion
    {
        v1_2
    }

    public class APIError : System.Exception
    {
        public APIError(string message) : base(message) { }
    }

    public class AuthError : System.Exception
    {
        public AuthError(string message) : base(message) { }
    }
}
