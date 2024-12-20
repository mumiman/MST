using System;
using UnityEngine;

namespace MasterServerToolkit.Demos.BasicProfile
{
    [Serializable]
    public class StoreOffer
    {
        public string id;
        public string name;
        public Sprite iconSprite;
        public int price;
        public string currency;
    }
}