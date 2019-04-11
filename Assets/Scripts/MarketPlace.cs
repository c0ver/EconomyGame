using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarketPlace
{
    // sorted from lowest price to highest
    private Dictionary<int, List<Offer>> buyOffers = new Dictionary<int, List<Offer>>();
    private Dictionary<int, List<Offer>> sellOffers = new Dictionary<int, List<Offer>>();
    private Dictionary<int, List<Offer>> completedOffers = new Dictionary<int, List<Offer>>();

    // key: itemID, value: List of prices at which this item was sold
    private Dictionary<int, List<int>> completedOrders = new Dictionary<int, List<int>>();

    private Dictionary<int, bool> updateAverage = new Dictionary<int, bool>();

    private Dictionary<int, float> completedOrderAverages = new Dictionary<int, float>();
    private Dictionary<int, int> highestPriceExchanged = new Dictionary<int, int>();


    public MarketPlace()
    {
        Clear();
    }

    public void Clear()
    {
        buyOffers.Clear();
        sellOffers.Clear();
        completedOffers.Clear();
        completedOrderAverages.Clear();
        completedOrders.Clear();

        foreach(int itemID in Item.ItemList.Keys)
        {
            updateAverage[itemID] = false;
            highestPriceExchanged[itemID] = -1;
        }
    }

    public float MarketPrice(int itemID)
    {
        if (updateAverage[itemID] == true)
        {
            FindMarketAverage(itemID);
        }

        if(!completedOrderAverages.ContainsKey(itemID))
        {
            return -1;
        }

        return completedOrderAverages[itemID];
    }

    public void FindMarketAverages()
    {
        // get averages for every item sold
        foreach (int itemID in completedOrders.Keys)
        {
            FindMarketAverage(itemID);
        }
    }

    private void FindMarketAverage(int itemID)
    {
        updateAverage[itemID] = false;

        int total = 0;
        foreach (int price in completedOrders[itemID])
        {
            total += price;
        }
        completedOrderAverages[itemID] = (float)total / completedOrders[itemID].Count;
    }

    public int QuantitySold(int itemID)
    {
        if (!completedOrders.ContainsKey(itemID)) return 0;

        return completedOrders[itemID].Count;
    }

    public List<Offer> GetSellOffers(int itemID)
    {
        if(sellOffers.ContainsKey(itemID))
        {
            return sellOffers[itemID];
        }

        return null;
    }
    
    public void MatchMarketOffers()
    {
        foreach (int itemID in sellOffers.Keys)
        {
            if (!buyOffers.ContainsKey(itemID))
            {
                continue;
            }

            MatchItemOffers(itemID);
        }
    }
    
    public void MatchItemOffers(int itemID)
    {
        if (!buyOffers.ContainsKey(itemID) || !sellOffers.ContainsKey(itemID)) return;

        List<Offer> itemBuyOffers = buyOffers[itemID];
        List<Offer> itemSellOffers = sellOffers[itemID];
        while (itemSellOffers.Count != 0 && itemBuyOffers.Count != 0)
        {
            Offer buyOffer = itemBuyOffers[itemBuyOffers.Count - 1];
            Offer sellOffer = itemSellOffers[0];

            // highest buy Offer is less than lowest sell offer for this item
            if (buyOffer.Price < sellOffer.Price)
            {
                break;
            }

            // a transaction was made so the average must be recalculated the next time it is called
            updateAverage[itemID] = true;

            MatchOffers(buyOffer, sellOffer);

            // remove from the active offer list but keep it in history
            if (buyOffer.QuantityLeft == 0)
            {
                buyOffer.Trader.RemoveBuyOffer(buyOffer);

                if (buyOffer.TotalQuantity != 0)
                {
                    if (!completedOffers.ContainsKey(itemID))
                    {
                        completedOffers[itemID] = new List<Offer>();
                    }
                    completedOffers[itemID].Add(buyOffer);
                }
            }

            if (sellOffer.QuantityLeft == 0)
            {
                sellOffer.Trader.RemoveSellOffer(sellOffer);

                if (sellOffer.TotalQuantity != 0)
                {
                    if (!completedOffers.ContainsKey(itemID))
                    {
                        completedOffers[itemID] = new List<Offer>();
                    }
                    completedOffers[itemID].Add(sellOffer);
                }
            }
        }
    }

    private void MatchOffers(Offer buyOffer, Offer sellOffer)
    {
        int itemID = buyOffer.ItemID;
        int amountExchanged = buyOffer.QuantityLeft < sellOffer.QuantityLeft ? buyOffer.QuantityLeft : sellOffer.QuantityLeft;
        int priceExchanged = (buyOffer.Price + sellOffer.Price) / 2;

        Entity seller = sellOffer.Trader;
        Entity buyer = buyOffer.Trader;

        if (amountExchanged == 0)
        {
            Debug.LogWarningFormat("{0} tried to buy 0 of {1} from {2}", buyer, Thing.GetTag(itemID), seller);
            Debug.LogWarning(buyer.entityInfo);
            Debug.LogWarning(seller.entityInfo);
            return;
        }

        if (Game.DebugMode)
        {
            seller.entityInfo += string.Format("Selling {0} of {1} to {2} for a total of {3}\n", amountExchanged, buyOffer.ItemTag, buyer, amountExchanged * priceExchanged);
            buyer.entityInfo += string.Format("Buying {0} of {1} from {2} for a total of {3}\n", amountExchanged, buyOffer.ItemTag, seller, amountExchanged * priceExchanged);
        }

        // buyer already lost money when placing order
        seller.GainMoney(priceExchanged * amountExchanged);
        
        // since they actual buy price was less than the buy offer
        buyer.GainMoney((buyOffer.Price - priceExchanged) * amountExchanged);

        if (!seller.GiveItems(buyer, itemID, amountExchanged))
        {
            if (Game.DebugMode)
            {
                seller.entityInfo += "Transaction did not go through\n";
                buyer.entityInfo += "Transaction did not go through\n";
            }
            return;
        }

        buyOffer.FillOrder(amountExchanged);
        sellOffer.FillOrder(amountExchanged);

        if (highestPriceExchanged[itemID] < priceExchanged) highestPriceExchanged[itemID] = priceExchanged;

        if (!completedOrders.ContainsKey(itemID))
        {
            completedOrders.Add(itemID, new List<int>());
        }
        for (int x = 0; x < amountExchanged; x++)
        {
            completedOrders[itemID].Add(priceExchanged);
        }
    }

    // assume that an entity can only have one offer for each buying/selling for each itemID
    public void ModifyOffer(Entity trader, int itemID, int newPrice, bool isSellOffer)
    {
        Offer offer = null;
        int offerIndex = -1;

        List<Offer> offers;
        string action;

        if (isSellOffer)
        {
            offers = sellOffers[itemID];
            action = "selling";
        }
        else
        {
            offers = buyOffers[itemID];
            action = "buying";
        }

        for (int x = 0; x < offers.Count; x++)
        {
            if (offers[x].Trader == trader)
            {
                offerIndex = x;
                offer = offers[x];

                break;
            }
        }

        if (offerIndex == -1 || offer == null)
        {
            Debug.LogErrorFormat("{0} was not found {1} {2}", trader, action, Thing.GetTag(itemID));
            return;
        }

        offer.ChangePrice(newPrice);

        offers.RemoveAt(offerIndex);
        AddOffer(offer);
    }

    // sort offer list by increasing price
    public void AddOffer(Offer offer)
    {
        int itemID = offer.ItemID;
        Dictionary<int, List<Offer>> offers;
        if (offer.IsSellOffer)
        {
            offers = sellOffers;
        }
        else
        {
            offers = buyOffers;
        }

        if (!offers.ContainsKey(itemID))
        {
            offers.Add(itemID, new List<Offer>());
        }

        bool insertedOffer = false;
        for (int x = 0; x < offers[itemID].Count; x++)
        {
            if (offer.Price <= offers[itemID][x].Price)
            {
                offers[itemID].Insert(x, offer);
                insertedOffer = true;
                break;
            }
        }

        // the sell offer has a higher price than all other offers
        if (!insertedOffer)
        {
            offers[itemID].Add(offer);
        }

        MatchItemOffers(itemID);
    }

    // returns -1 if recipe inputs are not available on the market
    public int CostForRecipe(Recipe recipe)
    {
        int totalCost = 0;

        foreach(var input in recipe.InputInfo)
        {
            int itemID = input.Key;
            int amount = input.Value;

            int itemCost = LowestAvailableSellPrice(itemID, amount);

            if (itemCost == -1)
            {
                return -1;
            }

            totalCost += itemCost;
        }

        return totalCost;
    }

    public int HighestAvailableBuyPrice(int itemID, int amountWanted = 1)
    {
        if(buyOffers.ContainsKey(itemID))
        {
            int totalPrice = 0;

            // start from the end since it is sorted from least to most price
            for (int x = buyOffers[itemID].Count - 1; x >= 0; x--)
            {
                int amountAtPrice = buyOffers[itemID][x].QuantityLeft;
                if (amountWanted <= amountAtPrice)
                {
                    totalPrice += buyOffers[itemID][x].Price * amountWanted;
                    return totalPrice;
                }

                // not enough quantity from this offer
                amountWanted -= amountAtPrice;
                totalPrice += amountAtPrice * buyOffers[itemID][x].Price;
            }
            // if this loops ends without returning, there is not enough amountWanted
        }

        return -1;
    }

    public int LowestAvailableSellPrice(int itemID, int amountWanted = 1)
    {
        if(sellOffers.ContainsKey(itemID))
        {
            int totalPrice = 0;

            for (int x = 0; x < sellOffers[itemID].Count; x++)
            {
                int amountAtPrice = sellOffers[itemID][x].QuantityLeft;
                if (amountWanted <= amountAtPrice)
                {
                    totalPrice += sellOffers[itemID][x].Price * amountWanted;
                    return totalPrice;
                }

                // not enough quantity from this offer
                amountWanted -= amountAtPrice;
                totalPrice += amountAtPrice * sellOffers[itemID][x].Price;
            }
            // if this loops ends without returning, there is not enough amountWanted
        }

        return -1;
    }

    public Offer LowestAvailableSellOffer(int itemID)
    {
        if (!sellOffers.ContainsKey(itemID) || sellOffers[itemID].Count == 0)
        {
            return null;
        }
        return sellOffers[itemID][0];
    }

    public int HighestPriceExchanged(int itemID)
    {
        return highestPriceExchanged[itemID];
    }

    public void RemoveOffer(Offer offer, bool isSellOffer)
    {
        int itemID = offer.ItemID;
        if (isSellOffer)
        {
            sellOffers[itemID].Remove(offer);
        }
        else
        {
            buyOffers[itemID].Remove(offer);
        }
    }

    public string Info()
    {
        string info = "";

        // prints all the remaining sell offers for each item
        foreach (var itemOffers in sellOffers)
        {
            info += string.Format("Remaining Sell Offers for {0}:\n", Thing.GetTag(itemOffers.Key));

            foreach (Offer offer in itemOffers.Value)
            {
                info += offer;
            }

            info += '\n';
        }

        // prints all the remaining buy offers for each item
        foreach (var itemOffers in buyOffers)
        {
            info += string.Format("Remaining Buy Offers for {0}:\n", Thing.GetTag(itemOffers.Key));

            // print from largest to smallest
            for (int x = itemOffers.Value.Count - 1; x >= 0; x--)
            {
                info += itemOffers.Value[x];
            }

            info += '\n';
        }

        // prints all completed offers
        foreach (var itemOffers in completedOffers)
        {
            info += string.Format("Completed Offers for {0}:\n", Thing.GetTag(itemOffers.Key));

            foreach (Offer offer in itemOffers.Value)
            {
                info += offer;
            }

            info += '\n';
        }


        foreach (var completedOrder in completedOrders)
        {
            foreach (int price in completedOrder.Value)
            {
                info += string.Format("Exchanged {0} for {1}\n", Thing.GetTag(completedOrder.Key), price);
            }
        }

        info += "\nMarket Prices:\n";
        foreach(var avg in completedOrderAverages)
        {
            info += string.Format("{0}: {1}\n", Thing.GetTag(avg.Key), avg.Value);
        }

        return info;
    }
}
