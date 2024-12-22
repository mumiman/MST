using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CCengine
{
    [System.Serializable]
    public enum PlayerRole { Carry, Mid, Offlane, Sup1, Sup2 }
    //Represent the current state of a player during the game (data only)

    [System.Serializable]
    public class Player
    {
        public int playerId;
        public string username;
        public string avatar;
        public string deck; // Could represent the deck configuration or ID

        public bool is_ai = false;
        public int ai_level;

        public bool connected = false; // Connected to server and game
        public bool ready = false;     // Sent all player data, ready to play

        // Athletes owned by the player
        public List<Athlete> athletes = new List<Athlete>();

        public Dictionary<string, Card> cards_all = new Dictionary<string, Card>(); //Dictionnary for quick access to any card by UID

        // Cards in various zones
        public List<Card> cards_deck = new List<Card>();    // Cards in the player's deck
        public List<Card> cards_hand = new List<Card>();    // Cards in the player's hand
        public List<Card> cards_discard = new List<Card>(); // Cards in the player's discard pile

        // History of actions performed by the player
        public List<ActionHistory> history_list = new List<ActionHistory>();

        public Player(int id)
        {
            playerId = id;
            username = $"Player{id}";
        }

        public bool IsReady()
        {
            return ready && cards_all.Count > 0; ;
        }

        public bool IsConnected()
        {
            return connected || is_ai;
        }

        #region ---- Athlete Management ----

        // Add an athlete to the player's roster
        public void AddAthlete(Athlete athlete)
        {
            if (!athletes.Contains(athlete))
            {
                athletes.Add(athlete);
            }
        }

        // Remove an athlete from the player's roster
        public void RemoveAthlete(Athlete athlete)
        {
            athletes.Remove(athlete);
        }

        // Get an athlete by ID
        public Athlete GetAthlete(string athleteId)
        {
            return athletes.Find(a => a.Id == athleteId);
        }

        #endregion

        #region ---- Card Management ----

        // Add a card to the player's deck
        public void AddCardToDeck(Card card)
        {
            cards_deck.Add(card);
        }
        public void RemoveCardFromDeck(List<Card> card_list, Card card)
        {
            card_list.Remove(card);
        }

        public virtual void RemoveCardFromAllGroups(Card card)
        {
            cards_deck.Remove(card);
            cards_hand.Remove(card);
            cards_discard.Remove(card);
        }

        // Draw a card from the deck to the hand
        public Card DrawCard()
        {
            if (cards_deck.Count > 0)
            {
                Card card = cards_deck[0];
                cards_deck.RemoveAt(0);
                cards_hand.Add(card);
                return card;
            }
            return null;
        }

        // Discard a card from the hand
        public void DiscardCard(Card card)
        {
            if (cards_hand.Contains(card))
            {
                cards_hand.Remove(card);
                cards_discard.Add(card);
            }
        }

        // Shuffle the deck
        public void ShuffleDeck(System.Random random)
        {
            int n = cards_deck.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                Card temp = cards_deck[i];
                cards_deck[i] = cards_deck[j];
                cards_deck[j] = temp;
            }
        }

        public virtual Card GetRandomCard(List<Card> card_list, System.Random rand)
        {
            if (card_list.Count > 0)
                return card_list[rand.Next(0, card_list.Count)];
            return null;
        }

        public bool HasCard(List<Card> card_list, Card card)
        {
            return card_list.Contains(card);
        }

        public Card GetHandCard(string uid)
        {
            foreach (Card card in cards_hand)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetDeckCard(string uid)
        {
            foreach (Card card in cards_deck)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetDiscardCard(string uid)
        {
            foreach (Card card in cards_discard)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetCard(string uid)
        {
            if (uid != null)
            {
                bool valid = cards_all.TryGetValue(uid, out Card card);
                if (valid)
                    return card;
            }
            return null;
        }

        #endregion

        #region ---- History ---------

        public void AddHistory(ushort type, Card card)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, Card target)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_uid = target.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, Player target)
        {
            ActionHistory order = new ActionHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_id = target.playerId;
            history_list.Add(order);
        }


        #endregion

        //Clone all player variables into another var, used mostly by the AI when building a prediction tree
        public static void Clone(Player source, Player dest)
        {
            dest.playerId = source.playerId;
            dest.is_ai = source.is_ai;
            dest.ai_level = source.ai_level;

            //Commented variables are not needed for ai predictions
            //dest.username = source.username;
            //dest.avatar = source.avatar;
            //dest.deck = source.deck;
            //dest.connected = source.connected;
            //dest.ready = source.ready;

            Card.CloneDict(source.cards_all, dest.cards_all);

            Card.CloneListRef(dest.cards_all, source.cards_hand, dest.cards_hand);
            Card.CloneListRef(dest.cards_all, source.cards_deck, dest.cards_deck);
            Card.CloneListRef(dest.cards_all, source.cards_discard, dest.cards_discard);

        }
    }

    [System.Serializable]
    public class ActionHistory
    {
        public ushort type;
        public string card_id;
        public string card_uid;
        public string target_uid;
        public string ability_id;
        public int target_id;

    }
}