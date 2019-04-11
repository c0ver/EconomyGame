using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfferTree
{
    public OfferTreeNode Min { get; private set; }
    public OfferTreeNode Max { get; private set; }

    private OfferTreeNode root;

    public OfferTree()
    {

    }

    public void Add(Offer offer)
    {
        if (root == null)
        {
            root = new OfferTreeNode(null, offer);
            Min = root;
            Max = root;
            return;
        }

        OfferTreeNode newNode = root.Add(offer);

        if (Min.Price > newNode.Price)
        {
            Min = newNode;
        }

        if (Max.Price < newNode.Price)
        {
            Max = newNode;
        }
    }
}
