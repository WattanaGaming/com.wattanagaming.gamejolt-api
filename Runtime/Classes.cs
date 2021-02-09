using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DateTime = System.DateTime;
using SimpleJSON;

namespace WattanaGaming.GameJoltAPI
{
    public class TrophyData
    {
        public int id;
        public string title;
        public string difficulty;
        public string description;
        public string imageURL;
        public string achieved;

        public TrophyData(JSONNode JSONData)
        {
            id = JSONData["id"];
            title = JSONData["title"];
            difficulty = JSONData["difficulty"];
            description = JSONData["description"];
            imageURL = JSONData["image_url"];
            achieved = JSONData["achieved"];
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
}
