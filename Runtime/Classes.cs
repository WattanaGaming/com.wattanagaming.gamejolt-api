using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DateTime = System.DateTime;

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
