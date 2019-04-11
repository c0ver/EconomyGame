using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Offer
{
    // readonly
    public string ItemTag { get; }
    public Entity Trader { get; }
    public int ItemID { get; }
    public bool IsSellOffer { get; }

    public int TotalQuantity { get; private set; }
    public int Price { get; private set; }
    public int QuantityCompleted { get; private set; } = 0;

    public int QuantityLeft { get { return TotalQuantity - QuantityCompleted; } }
    public float FractionCompleted { get { return (float)QuantityCompleted / TotalQuantity; } }

    public Offer(Entity trader, int itemID, int quantity, int price, bool isSellOffer)
    {
        Trader = trader;
        ItemID = itemID;
        ItemTag = Thing.GetTag(itemID);

        TotalQuantity = quantity;
        Price = price;
        IsSellOffer = isSellOffer;

        if (Game.DebugMode) Trader.EntityInfo += "Created " + this;

        if (!isSellOffer)
        {
            trader.LoseMoney(quantity * price);
        }
    }

    public void FillOrder(int amount)
    {
        QuantityCompleted += amount;
    }

    public void AddToQuantity(int amount)
    {
        TotalQuantity += amount;
    }

    // can only be called in MarketPlace
    // add a reference to MarketPlace for each offer to avoid this weird behavior
    public void ChangePrice(int newPrice)
    {
        Price = newPrice;
    }

    /*public void LowerPrice(int lowestPrice)
    {
        if (lowestPrice == -1 || lowestPrice > Price / 2) Price /= 2;
        else Price = lowestPrice - 1;
    }*/

    public string Info()
    {
        if (IsSellOffer)
        {
            return string.Format("Sold {0} out of {1}, {2}%", QuantityCompleted, TotalQuantity, FractionCompleted * 100);
        }
        return string.Format("Bought {0} out of {1}, {2}%", QuantityCompleted, TotalQuantity, FractionCompleted * 100);
    }

    public void ReduceQuantity(int amount)
    {
        TotalQuantity -= amount;

        if (TotalQuantity < QuantityCompleted)
        {
            TotalQuantity = QuantityCompleted;
        }

        if (TotalQuantity == 0)
        {
            Trader.City.MarketPlace.RemoveOffer(this, IsSellOffer);
            Trader.RemoveSellOffer(this);
        }
    }

    public override string ToString()
    {
        if (IsSellOffer)
        {
            return string.Format("{0}'s Sell offer for {1} / {2} of {3} at {4}\n", Trader.Tag, QuantityLeft, TotalQuantity, ItemTag, Price);
        }
        return string.Format("{0}'s Buy offer for {1} / {2} of {3} at {4}\n", Trader.Tag, QuantityLeft, TotalQuantity, ItemTag, Price);
    }
}
