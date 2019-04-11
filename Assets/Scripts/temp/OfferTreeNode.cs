using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfferTreeNode
{
    private OfferTreeNode left, right, parent;

    // can have multiple offers with the same price
    private List<Offer> offers = new List<Offer>();

    public int Price { get; }

    public OfferTreeNode(OfferTreeNode parent, Offer offer)
    {
        offers.Add(offer);
        Price = offer.Price;

        this.parent = parent;
    }

    public int AvailableOfferIndex()
    {
        for(int x = 0; x < offers.Count; x++)
        {
            if (offers[x].QuantityLeft > 0) return x;
        }

        return -1;
    }

    /*public Offer NextAvailableOffer()
    {
        int index = AvailableOfferIndex();

        if (index == -1)
        {

        }
        else
        {
            return offers[index];
        }
    }*/

    // gets the next biggest price that is smaller than this
    public OfferTreeNode GetNextBiggestPrice()
    {
        if (left != null)
        {
            return left.GetBiggest();
        }
        else
        {
            return GetSmallerParent();
        }
    }

    // gets the next smallest price that is bigger than this
    public OfferTreeNode GetNextSmallestPrice()
    {
        if (right != null)
        {
            return right.GetSmallest();
        }
        else
        {
            return GetBiggerParent();
        }
    }

    private OfferTreeNode GetSmallerParent()
    {
        if (parent.right == this)
        {
            return parent;
        }

        return parent.GetSmallerParent();
    }

    private OfferTreeNode GetBiggerParent()
    {
        if (parent.left == this)
        {
            return parent;
        }

        return parent.GetBiggerParent();
    }

    // go down-right as much as possible
    private OfferTreeNode GetBiggest()
    {
        if (right != null)
        {
            return right.GetBiggest();
        }

        return this;
    }

    // go down-left as much as possible
    private OfferTreeNode GetSmallest()
    {
        if (left != null)
        {
            return left.GetSmallest();
        }

        return this;
    }

    public OfferTreeNode Add(Offer newOffer)
    {
        if (Price > newOffer.Price)
        {
            if (left == null)
            {
                left = new OfferTreeNode(this, newOffer);
                return left;
            }

            return left.Add(newOffer);
        } 
        else if (Price < newOffer.Price)
        {
            if (right == null)
            {
                right = new OfferTreeNode(this, newOffer);
                return right;
            }

            return right.Add(newOffer);
        }
        else
        {
            offers.Add(newOffer);
            return this;
        }
    }
}
