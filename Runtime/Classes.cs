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
        public string diffuculty;
        public string description;
        public string imageURL;
        public bool achieved;
        public DateTime achievedDate;
    }
}
