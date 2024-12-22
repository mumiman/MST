using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CCengine
{
    //Represent the current state of a card during the game (data only)

    [System.Serializable]
    public class Card
    {
        public string card_id;
        public string uid;
        public int player_id;
        public string variant_id;

        public string name;
        public string description;
        public CardType cardType;
        public int cost;

        // If the card assigns a task
        public Mission assignedTask;

        // If the card boosts athlete stats
        public StatBoost statBoost;

        [System.NonSerialized] private int hash = 0;
        [System.NonSerialized] private CardData data = null;
        // [System.NonSerialized] private VariantData vdata = null;

        public Card(string cardId, string uid, int playerId)
        {
            this.card_id = cardId;
            this.uid = uid;
            this.player_id = playerId;
            // Initialize other properties as needed
        }


        public virtual void SetCard(CardData icard //,VariantData cvariant
        )
        {
            data = icard;
            card_id = icard.id;
            // variant_id = cvariant.id;

        }

        public CardData CardData
        {
            get
            {
                if (data == null || data.id != card_id)
                    data = CardData.Get(card_id); //Optimization, store for future use
                return data;
            }
        }

        // public VariantData VariantData
        // {
        //     get
        //     {
        //         if (vdata == null || vdata.id != variant_id)
        //             vdata = VariantData.Get(variant_id); //Optimization, store for future use
        //         return vdata;
        //     }
        // }

        public CardData Data => CardData; //Alternate name

        public int Hash
        {
            get
            {
                if (hash == 0)
                    hash = Mathf.Abs(uid.GetHashCode()); //Optimization, store for future use
                return hash;
            }
        }

        public static Card Create(CardData icard, Player player, string uid)
        {
            Card card = new Card(icard.id, uid, player.playerId);
            card.SetCard(icard);
            // card.SetCard(icard, ivariant);
            player.cards_all[card.uid] = card;
            return card;
        }

        // public static Card Create(CardData icard, VariantData ivariant, Player player)
        // {
        //     return Create(icard, ivariant, player, GameTool.GenerateRandomID(11, 15));
        // }

        // public static Card Create(CardData icard, VariantData ivariant, Player player, string uid)
        // {
        //     Card card = new Card(icard.id, uid, player.player_id);
        //     card.SetCard(icard, ivariant);
        //     player.cards_all[card.uid] = card;
        //     return card;
        // }

        public static Card CloneNew(Card source)
        {
            Card card = new Card(source.card_id, source.uid, source.player_id);
            Clone(source, card);
            return card;
        }

        //Clone all card variables into another var, used mostly by the AI when building a prediction tree
        public static void Clone(Card source, Card dest)
        {
            dest.card_id = source.card_id;
            dest.uid = source.uid;
            dest.player_id = source.player_id;

            dest.variant_id = source.variant_id;

        }

        //Clone a var that could be null
        public static void CloneNull(Card source, ref Card dest)
        {
            //Source is null
            if (source == null)
            {
                dest = null;
                return;
            }

            //Dest is null
            if (dest == null)
            {
                dest = CloneNew(source);
                return;
            }

            //Both arent null, just clone
            Clone(source, dest);
        }

        //Clone dictionary completely
        public static void CloneDict(Dictionary<string, Card> source, Dictionary<string, Card> dest)
        {
            foreach (KeyValuePair<string, Card> pair in source)
            {
                bool valid = dest.TryGetValue(pair.Key, out Card val);
                if (valid)
                    Clone(pair.Value, val);
                else
                    dest[pair.Key] = CloneNew(pair.Value);
            }
        }

        //Clone list by keeping references from ref_dict
        public static void CloneListRef(Dictionary<string, Card> ref_dict, List<Card> source, List<Card> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Card scard = source[i];
                bool valid = ref_dict.TryGetValue(scard.uid, out Card rcard);
                if (valid)
                {
                    if (i < dest.Count)
                        dest[i] = rcard;
                    else
                        dest.Add(rcard);
                }
            }

            if (dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }

    }
    public enum CardType
    {
        TaskAssignment,
        StatBoost,
    }

    public class StatBoost
    {
        public int attackIncrease;
        public int defenseIncrease;
        public int speedIncrease;

        public override string ToString()
        {
            return $"Attack +{attackIncrease}, Defense +{defenseIncrease}, Speed +{speedIncrease}";
        }
    }
}


