using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.Reflection;
using System.Linq;
using System;

public class Entity : Thing
{
    private const string ENTITY_NEEDS_FILE = "entityNeeds";
    private const string JOB_FILE = "jobList";

    private const int START_MONEY = 100;
    private const int MAX_COMMON_PRICE_BELIEF = 100;
    private const int NEED_PLAN_AHEAD = 1;
    private const int JOB_PLAN_AHEAD = 1;
    private const int VALUE_TO_SPEND_ALL_MONEY = 3;
    private const float NORMAL_SELL_FRACTION = (float)2 / 3;
    private const float ADDITIONAL_EFFICIENCY = 1f;
    private const float MIN_WORK_TIME = 0.5f;
    private const float ITEM_MARKUP = 1.25f;
    private const float MIN_RATIO = 0.25f;
    private const float MAX_PRICE_CUT = (float)2 / 3;
    private const float OCCUPATION_BENEFITS = (float)2 / 3;
    private const float LIQUIDITY_AMOUNT = (float)1 / 2;
    private const float UNNEEDED_USED_REDUCTION = (float)3 / 4;
    private const float UNNEEDED_UNUSED_REDUCTION = (float)1 / 2;
    private const float PREV_PRICE_BELIEF_WEIGHT = (float)1 / 3;

    private readonly static int BREAD_ID = GetID("Bread");
    private readonly static int CLOTHES_ID = GetID("Clothes");
    private readonly static int WOOL_ID = GetID("Wool");
    private readonly static int WHEAT_ID = GetID("Wheat");

    // not being used for now
    private const int MAX_HEALTH = 100;
    private const int SLEEP_HEALTH = MAX_HEALTH / 10;
    private const int MAX_HUNGER = 1;
    private const int HUNGER_COST = MAX_HEALTH / 3;

    // key: itemID, value: usage amount/day
    // hardcoded variable name: careful if name is changed
    // CHANGED!!!!!!!!!!!!!!!!!!!!!!!!!!
    private readonly static Dictionary<int, int> DAILY_CONSUMED_ITEMS = new Dictionary<int, int>
    {
        { BREAD_ID, 1 }
    };
    private readonly static Dictionary<int, int> DAILY_HANDLE_ITEMS = new Dictionary<int, int>
    {
        { CLOTHES_ID, 1 }
    };
    private readonly static Dictionary<int, int> DAILY_NEEDED_ITEMS = new Dictionary<int, int>
    {
        { BREAD_ID, 1 },
        { CLOTHES_ID, 1}
    };

    // key: jobID, value: efficiency of doing the job
    // efficiency = time to do job
    // so smaller is better
    private Dictionary<int, float> jobEfficiency = new Dictionary<int, float>();

    // key: itemID, value: personal belief of the correct price
    private Dictionary<int, float> priceBeliefs = new Dictionary<int, float>();

    // never remove or add to these without calling helper functions that include the MarketPlace
    private Dictionary<int, Offer> SellOffers { get; } = new Dictionary<int, Offer>();
    private Dictionary<int, Offer> BuyOffers { get; } = new Dictionary<int, Offer>();
    
    // key: itemID, value: list of prices exchanged
    private Dictionary<int, List<int>> marketOrders = new Dictionary<int, List<int>>();

    private float prudence = 1;

    private List<Item> inventory = new List<Item>();

    private int daysWorked = 0;
    private float bonusEfficiency = 1;
    private Recipe chosenRecipe;
    private bool hasNeededItems = false;
    private bool createdBuyOffers = false;

    private float timeElapsed = 0;
    public string entityInfo;

    public int Money { get; private set; } = START_MONEY;
    public City City { get; private set; }

    public int HP { get; private set; } = MAX_HEALTH;
    public bool IsAlive { get; private set; } = true;
    public int Hunger { get; private set; } = MAX_HUNGER;

    // may cause confusion since variable name is same as class name
    public Job Job { get; private set; }

    public Entity(string name, City city) : base(name)
    {
        if(Game.DebugMode) entityInfo = "Created " + Tag + '\n';

        City = city;
        AddItem(CLOTHES_ID);
        AddItem(BREAD_ID);
        AddItem(BREAD_ID);
        AddItem(BREAD_ID);

        // generate a random efficiency of working for each job besides farming
        foreach(int jobID in Job.jobDict.Keys)
        {
            jobEfficiency[jobID] = UnityEngine.Random.Range(0, ADDITIONAL_EFFICIENCY) + MIN_WORK_TIME;
            if(Game.DebugMode) entityInfo += string.Format("{0} efficiency: {1}\n", GetTag(jobID), jobEfficiency[jobID]);
        }

        // generate a random priceBelief for each item
        // only do COMMON items for now, because everyone is a peasant
        foreach(Item item in Item.ItemList.Values)
        {
            if (!priceBeliefs.ContainsKey(item.ID))
            {
                if (item.Rarity == Rarity.COMMON)
                {
                    priceBeliefs[item.ID] = UnityEngine.Random.Range(1f, MAX_COMMON_PRICE_BELIEF);
                    if (Game.DebugMode) entityInfo += string.Format("priceBelief of {0}: {1}\n", item, priceBeliefs[item.ID]);

                    foreach(Recipe recipe in item.OutRecipes)
                    {
                        // recalculate pricebelief for inputs since they cannot be higher than outputs
                        foreach(var input in recipe.InputInfo)
                        {
                            if(priceBeliefs.ContainsKey(input.Key) && priceBeliefs[input.Key] < priceBeliefs[item.ID] / input.Value)
                            {
                                continue;
                            }

                            priceBeliefs[input.Key] = UnityEngine.Random.Range(1f, priceBeliefs[item.ID] / input.Value);
                            if (Game.DebugMode) entityInfo += string.Format("priceBelief of {0}: {1}\n", GetTag(input.Key), priceBeliefs[input.Key]);
                        }
                    }

                }
            }
        }
        if (Game.DebugMode) entityInfo += '\n';

        ChooseJob();

        if (Game.DebugMode) Debug.Log(entityInfo);
    }

    /*
     * Priority 1: Buy input items
     * Priority 2: Buy needed items
     * Priority 3: Buy value items
     */
    public void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > jobEfficiency[Job.ID] / bonusEfficiency)
        {
            if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE + "Update:\n\n";

            timeElapsed -= jobEfficiency[Job.ID];

            // Buy input items
            foreach(int itemID in HasOrBuyingInputs(chosenRecipe))
            {
                int surplus = (int)Mathf.Floor(GetInputSurplus(itemID));
                if (surplus >= 0)
                {
                    Debug.LogErrorFormat("{0} was checked to be needed.", GetTag(itemID));
                    Debug.LogError(entityInfo);
                }

                PlaceBuyOffer(itemID, surplus * -1, (int)priceBeliefs[itemID]);
            }

            // Buy needed items
            if (!hasNeededItems)
            {            
                BuyNeededItems();
                hasNeededItems = CheckNeededItems();
                if (Game.DebugMode) entityInfo += Game.BREAK_LINE;
            }

            // Buy value items
            if (hasNeededItems)
            {
                CreateValueBuyOffers();
            }

            Work();

            if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE;
        }
    }

    // item removal occurs only before sell offers are created
    public void BeginDay()
    {
        if (Game.DebugMode) entityInfo = string.Format("{0} has worked {1} days as a {2} with {3} money\n",
            this, daysWorked, Job, Money);
        if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE + "Begin Day:\n\n";

        hasNeededItems = false;
        createdBuyOffers = false;

        timeElapsed = 0;

        BuyOffers.Clear();
        SellOffers.Clear();
        marketOrders.Clear();

        CreateSellOffers();
        CreateInputItemBuyOffers(); // before buying needed items

        if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE;
    }

    /* stuff that needs to be done before EndDay:
     * Use needed items and take them off the market before people start
     * looking at the market to decide their next job
     */
    public void MidDay()
    {
        if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE + "Mid Day:\n\n";

        if (!hasNeededItems)
        {
            if (Game.DebugMode) entityInfo += "Don't have needed items\n";
            LastChanceToBuyNeededItems();
        }

        IsAlive = UseNeededItems();

        if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE;
    }

    // or item removal occurs after all selling has happened
    public void EndDay()
    {
        if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE + "End Day:\n\n";

        UpdatePriceBeliefs();

        // after updating priceBeliefs
        ChooseJob();

        // reclaim money from buy Offers
        while(BuyOffers.Count > 0)
        {
            ReclaimBuyOffer(BuyOffers.Keys.First());
        }

        if (Game.DebugMode) entityInfo += Game.DOUBLE_BREAK_LINE;

        if (Game.DebugMode)
        {
            entityInfo += "Inventory:\n";
            for (int x = 0; x < inventory.Count; x++)
            {
                Item item = inventory[x];
                entityInfo += item.Tag + " " + item.Condition + "\n";
            }

            Debug.Log(entityInfo);
        }
    }

    private void LastChanceToBuyNeededItems()
    {
        // need items to live but still has buy offers with money in them
        while (!hasNeededItems && BuyOffers.Count > 0)
        {
            int itemIDToReclaim = -1;
            foreach (int itemID in BuyOffers.Keys)
            {
                if (Money > 0 || !DAILY_NEEDED_ITEMS.ContainsKey(itemID))
                {
                    itemIDToReclaim = itemID;
                    break;
                }
            }

            // im dead
            if (itemIDToReclaim == -1) break;

            ReclaimBuyOffer(itemIDToReclaim);
            if (!BuyNeededItems())
            {
                break;
            }
            hasNeededItems = CheckNeededItems();
        }

        // need items to live but still has items to sell
        if (!hasNeededItems)
        {
            // sorted in ascending order
            // key: percentage below priceBelief, value: itemID
            SortedList<float, int> sellableItems = new SortedList<float, int>();
            foreach(int itemID in GetUniqueItemsInInventory())
            {
                if (SellOffers.ContainsKey(itemID))
                {
                    RemoveSellOffer(SellOffers[itemID]);
                }

                int surplus = (int)GetNeedSurplus(itemID);
                if (surplus > 0)
                {
                    int highestBuyPrice = City.MarketPlace.HighestAvailableBuyPrice(itemID);
                    if (highestBuyPrice == -1)
                    {
                        if (Game.DebugMode) entityInfo += string.Format("There are no buy offers for {0} - cannot sell\n", GetTag(itemID));
                        continue;
                    }

                    float loss = highestBuyPrice / priceBeliefs[itemID];

                    // ensure there are no duplicate keys. Very unlikely this will happen
                    while(sellableItems.ContainsKey(loss))
                    {
                        loss += (float)1 / 1000;
                    }

                    sellableItems.Add(loss, itemID);
                }
            }

            if (Game.DebugMode)
            {
                entityInfo += "Sellable items:\n";
                foreach(var sellableItem in sellableItems)
                {
                    entityInfo += string.Format("{0} at a loss at {1}", GetTag(sellableItem.Value), sellableItem.Key);
                }
                entityInfo += "\n";
            }

            // sell items with least loss
            while (!hasNeededItems && sellableItems.Count > 0)
            {
                int lastIndex = sellableItems.Count - 1;
                float price = sellableItems.Last().Key;
                int itemID = sellableItems.Last().Value;

                if (SellOffers.ContainsKey(itemID))
                {
                    RemoveSellOffer(SellOffers[itemID]);
                }

                int highestBuyPrice = City.MarketPlace.HighestAvailableBuyPrice(itemID);
                if (highestBuyPrice == -1)
                {
                    sellableItems.RemoveAt(lastIndex);
                    continue;
                }

                int surplus = (int)GetNeedSurplus(itemID);
                if (surplus < 1)
                {
                    sellableItems.RemoveAt(lastIndex);
                    continue;
                }

                PlaceSellOffer(itemID, surplus, highestBuyPrice);
                BuyNeededItems();
            }
        }
    }

    private void ReclaimBuyOffer(int itemID)
    {
        Offer offer = BuyOffers[itemID];

        if (Game.DebugMode) entityInfo += offer;

        Money += offer.QuantityLeft * offer.Price;
        if (Game.DebugMode) entityInfo += string.Format("Reclaimed {0} for a total of {1}\n",
            offer.QuantityLeft * offer.Price, Money);

        RemoveBuyOffer(offer);
    }

    // newPriceBelief = highest value item exchanged for
    private void UpdatePriceBeliefs()
    {
        if (Game.DebugMode) entityInfo += "Updating priceBeliefs for " + this + '\n';
        string changeFormat = "{0}: {1} -> {2}, a change of {3}%\n\n";

        List<int> itemIDList = new List<int>(priceBeliefs.Keys);
        foreach (int itemID in itemIDList)
        {
            float marketPrice = City.MarketPlace.MarketPrice(itemID);
            int quantityExchanged = City.MarketPlace.QuantitySold(itemID);
            float prevPriceBelief = priceBeliefs[itemID];

            if (Game.DebugMode)
            {
                entityInfo += string.Format("{0}:\nMarket Price: {1}\nQuantity Sold: {2}\n", GetTag(itemID), marketPrice, quantityExchanged);
            }

            // this item was exchanged on the market
            // also implies marketPrice != -1
            if (quantityExchanged > 0)
            {
                // no items were left on the market so 1.5x the price since it apparently wasn't high enough
                int lowestPrice = City.MarketPlace.LowestAvailableSellPrice(itemID);
                if (lowestPrice == -1)
                {
                    marketPrice *= (float)3 / 2;
                    if (Game.DebugMode) entityInfo += string.Format("Sold out -> New Market Price: {0}\n", marketPrice);
                }

                // trust the marketprice more if quantityExchanged is high compared with the city population
                // weight must be at least 0.5
                float marketPriceWeight = quantityExchanged > City.Population ? 1 : (float) quantityExchanged / City.Population;
                marketPriceWeight = marketPriceWeight / 2 + (float)1 / 2;
                float prevBeliefWeight = 1 - marketPriceWeight;

                priceBeliefs[itemID] = prevPriceBelief * prevBeliefWeight + marketPrice * marketPriceWeight;

                if (Game.DebugMode) entityInfo += string.Format("prevBeliefWeight: {0} | marketPriceWeight: {1}\n", prevBeliefWeight, marketPriceWeight);
            }
            else // make the new priceBelief based on buy/sell offers made if any
            {
                int lowestSellOfferPrice = City.MarketPlace.LowestAvailableSellPrice(itemID);
                int highestBuyOfferPrice = City.MarketPlace.HighestAvailableBuyPrice(itemID);

                if (Game.DebugMode)
                {
                    entityInfo += string.Format("Lowest sell Price: {0}\n", lowestSellOfferPrice);
                    entityInfo += string.Format("Highest buy Price: {0}\n", highestBuyOfferPrice);
                }

                // market price is probably the average of the two
                if (lowestSellOfferPrice != -1 && highestBuyOfferPrice != -1)
                {
                    marketPrice = (float) (lowestSellOfferPrice + highestBuyOfferPrice) / 2;
                }
                // lower priceBelief if lowest sell price is below it
                else if (lowestSellOfferPrice != -1 && lowestSellOfferPrice < priceBeliefs[itemID])
                {
                    marketPrice = lowestSellOfferPrice - 1;
                }
                // raise priceBelief if highest buy price is above it
                else if (highestBuyOfferPrice != -1 && highestBuyOfferPrice > priceBeliefs[itemID])
                {
                    marketPrice = highestBuyOfferPrice + 1;
                }
                if (Game.DebugMode) entityInfo += string.Format("New Market Price: {0}\n", marketPrice);

                // take the simple average of the two
                priceBeliefs[itemID] = (prevPriceBelief + marketPrice) / 2;
            }

            if (Game.DebugMode)
            {
                float percentChange = 100 * (priceBeliefs[itemID] - prevPriceBelief) / prevPriceBelief;
                entityInfo += string.Format(changeFormat, GetTag(itemID), prevPriceBelief,
                    priceBeliefs[itemID], percentChange);
            }

            if (float.IsNaN(priceBeliefs[itemID]))
            {
                Debug.LogError("This belief is NaN");
                Debug.LogError(entityInfo);
            }
        }

        if (Game.DebugMode) entityInfo += Game.BREAK_LINE;
    }

    private void Work()
    {
        if (Game.DebugMode) entityInfo += string.Format("{0} is trying to {1}\n", this, chosenRecipe);

        if (!HasInputs(chosenRecipe))
        {
            if (Game.DebugMode) entityInfo += string.Format("{0} does not have enough inputs\n", this);
            UpdateChosenRecipe();
            return;
        }

        MakeRecipe(chosenRecipe);

        foreach(int itemID in chosenRecipe.OutputInfo.Keys)
        {
            PlaceSellOffer(itemID);
        }

        if (Game.DebugMode) entityInfo += Game.BREAK_LINE;
    }

    private void MakeRecipe(Recipe recipe)
    {
        foreach(var inputInfo in recipe.InputInfo)
        {
            RemoveItem(inputInfo.Key, inputInfo.Value);
        }
        foreach(var outputInfo in recipe.OutputInfo)
        {
            AddItem(outputInfo.Key, outputInfo.Value);
        }
    }

    private bool UseNeededItems()
    {
        // for consumed items
        foreach(KeyValuePair<int, int> consumable in DAILY_CONSUMED_ITEMS)
        {
            int inInventory = GetTotalInInventory(consumable.Key);

            // don't have enough in inventory
            if(inInventory < consumable.Value)
            {
                if (Game.DebugMode)
                {
                    entityInfo += string.Format("Has only {0} of {1}; needs to consume {2}\n",
                        inInventory, Item.ItemList[consumable.Key], consumable.Value);
                    Debug.LogWarningFormat("{0} died due to a lack of {1}\n", this, GetTag(consumable.Key)); ;
                    Debug.LogWarning(entityInfo);
                }
                return false;
            }

            if (Game.DebugMode)
            {
                entityInfo += string.Format("{0} ate {1} for today.\n", this, Item.ItemList[consumable.Key]);
            }

            RemoveItem(consumable.Key, consumable.Value);
        }

        // for item use
        foreach(var item in DAILY_HANDLE_ITEMS)
        {
            int inInventory = GetTotalInInventory(item.Key);

            // don't have enough in inventory
            if(inInventory < item.Value)
            {
                if (Game.DebugMode)
                {
                    entityInfo += string.Format("Has only {0} of {1}; needs {2}\n",
                        inInventory, Item.ItemList[item.Key], item.Value);
                    Debug.LogWarningFormat("{0} died due to a lack of {1}\n", this, GetTag(item.Key)); ;
                    Debug.LogWarning(entityInfo);
                }
                return false;
            }

            if (Game.DebugMode) entityInfo += string.Format("{0} used {1} for today.\n", this, Item.ItemList[item.Key]);
            ItemUseDecay(item.Key);
        }

        return true;
    }

    private bool CheckNeededItems()
    {
        if (Game.DebugMode) entityInfo += "Checking for need:\n";

        foreach (int itemID in DAILY_NEEDED_ITEMS.Keys)
        {
            if (GetNeedSurplus(itemID) < 0)
            {
                return false;
            }
            if (Game.DebugMode) entityInfo += "\n";
        }
        return true;
    }

    private bool IsBeingUsed(int itemID)
    {
        return DAILY_NEEDED_ITEMS.ContainsKey(itemID) || chosenRecipe.InputInfo.ContainsKey(itemID);
    }

    private float GetNeedSurplus(int itemID)
    {
        float surplus = GetTotalInInventory(itemID);
        if (Game.DebugMode) entityInfo += string.Format("Has {0} of {1} in inventory\n", surplus, GetTag(itemID));

        // subtract needed items
        if (DAILY_NEEDED_ITEMS.ContainsKey(itemID))
        {
            float usePerDay = DAILY_NEEDED_ITEMS[itemID];

            if (Item.ItemList[itemID].GetType() != typeof(Consumable))
            {
                usePerDay *= Item.ItemList[itemID].DecayFactor;
            }
            surplus -= usePerDay * NEED_PLAN_AHEAD;

            if (Game.DebugMode) entityInfo += string.Format("UsePerDay: {0} -> Surplus of {1}\n", usePerDay, surplus);
        }

        // itemID is being used in a recipe to make the needed item
        /*else if (chosenRecipe.InputInfo.ContainsKey(itemID)) 
        {
            int usePerCycle = chosenRecipe.InputInfo[itemID];
            int numbCyclesPerDay = 1;
            surplus -= usePerCycle * numbCyclesPerDay* JOB_PLAN_AHEAD;

            if (Game.DebugMode) entityInfo += string.Format("UsePerCycle: {0} -> Surplus of {1}\n", usePerCycle, surplus);
        }*/

        surplus = RemoveFromSellOffer(itemID, surplus);

        if (Game.DebugMode) entityInfo += string.Format("Need Surplus of {0}\n", surplus);

        return surplus;
    }

    private float GetInputSurplus(int itemID)
    {
        float surplus = GetTotalInInventory(itemID);

        // subtract if input item
        if (chosenRecipe.InputInfo.ContainsKey(itemID))
        {
            int usePerCycle = chosenRecipe.InputInfo[itemID];
            int numbCyclesPerDay = (int) (Game.DAY_LENGTH / (jobEfficiency[Job.ID] / bonusEfficiency));
            surplus -= usePerCycle * numbCyclesPerDay* JOB_PLAN_AHEAD;

            if (Game.DebugMode) entityInfo += string.Format("UsePerCycle: {0}, numbCycles: {1} -> Surplus of {2}\n", usePerCycle, numbCyclesPerDay, surplus);
        }
        else
        {
            Debug.LogErrorFormat("{0} is not an input", GetTag(itemID));
            Debug.LogError(entityInfo);
        }

        surplus = RemoveFromSellOffer(itemID, surplus);

        if (Game.DebugMode) entityInfo += string.Format("Input Surplus of {0}\n", surplus);

        return surplus;
    }

    private float RemoveFromSellOffer(int itemID, float surplus)
    {
        if (!SellOffers.ContainsKey(itemID))
        {
            return surplus;
        }

        int currentlySelling = SellOffers[itemID].QuantityLeft;
        surplus -= currentlySelling;

        if (Game.DebugMode) entityInfo += string.Format("Selling {0} -> Surplus of {1}\n", currentlySelling, surplus);

        // if we need it and we're selling it...
        if (surplus < 0)
        {
            // assuming negative numbers round down, ex: -1.67 -> -2
            int amount = ((int)surplus) * -1;
            SellOffers[itemID].ReduceQuantity(amount);
            if (Game.DebugMode) entityInfo += string.Format("{0} is decreasing sell amount by {1}\n", this, amount);

            currentlySelling = SellOffers[itemID].QuantityLeft;
            surplus += currentlySelling;
            if (Game.DebugMode) entityInfo += string.Format("Selling {0} -> Surplus of {1}\n", currentlySelling, surplus);
        }

        return surplus;
    }

    private void ItemUseDecay(int itemID)
    {
        foreach(Item item in inventory)
        {
            if (itemID == item.ID)
            {
                float breakChance = UnityEngine.Random.Range(0f, 1f);

                if(breakChance < item.DecayFactor)
                {
                    if (Game.DebugMode) entityInfo += string.Format("{0} lost a {1} due to decay\n", this, item);
                    RemoveItem(itemID);
                }

                // only decay for one of given item
                return;
            }
        }

        Debug.LogErrorFormat("{0} was not found in {1}'s inventory", GetTag(itemID), this);
            Debug.LogError(entityInfo);
    }

    // has inputs available in inventory or market
    private bool HasAccessToItem(int itemID)
    {
        if (itemID == -1)
        {
            return true;
        }

        if(HasItem(itemID))
        {
            return true;
        } 
        if (Game.DebugMode) entityInfo += string.Format("Does not have input: {0} in inventory\n", GetTag(itemID));

        int lowestPrice = City.MarketPlace.LowestAvailableSellPrice(itemID);
        if(lowestPrice <= Money && lowestPrice != -1)
        {
            return true;
        }
        if (Game.DebugMode) entityInfo += string.Format("Cannot buy or find {0} on the market\n", GetTag(itemID));

        if (Game.DebugMode) entityInfo += string.Format("No access to {0}\n", GetTag(itemID));
        return false;
    }

    // TODO: input may have more than 1 of input item
    private bool HasAccessToInputItems(Recipe recipe)
    {
        foreach(var inputInfo in recipe.InputInfo)
        {
            if(!HasAccessToItem(inputInfo.Key))
            {
                return false;
            }
        }
        return true;
    }

    /* Switch: 
     * The new job has more value based on priceBelief and marketPrice
     * Daily needed items are on yesterday's market
     * Can buy those items
     */
    private void ChooseJob()
    {
        if (Game.DebugMode) entityInfo += "Previous job: " + Job + "\n\n";

        daysWorked++;
        Job prevJob = Job;

        Job bestJob = null;
        float bestJobValue = float.MinValue;

        /* look through needed items
         * if needed, make sure its on yesterday's market
         * if not on yesterday's market, that is the job to do if inputs are available
         */
        if (Game.DebugMode) entityInfo += string.Format("Looking at jobs for need:\nMoney: {0}\n", Money);
        foreach(int itemID in DAILY_NEEDED_ITEMS.Keys)
        {
            float surplus = GetNeedSurplus(itemID);

            if (surplus >= 0)
            {
                continue;
            }

            int lowestPrice = City.MarketPlace.LowestAvailableSellPrice(itemID);
            if (Game.DebugMode) entityInfo += string.Format("LowestPriceAvailableToday: {0}\n", lowestPrice);

            // I need the item and I can't get it from the marketplace
            if(lowestPrice == -1 || lowestPrice > Money)
            {
                List<Recipe> recipes = Item.ItemList[itemID].OutRecipes;
                foreach (Recipe recipe in recipes)
                {
                    if (HasAccessToInputItems(recipe))
                    {
                        Job = recipe.GetJob();
                        chosenRecipe = recipe;

                        if (!Job.Equals(prevJob))
                        {
                            daysWorked = 0;
                        }

                        if (Game.DebugMode) entityInfo += "Job due to need: " + Job + "\n";
                        return;
                    }
                    else if (Game.DebugMode) entityInfo += "No access to input items for " + GetTag(itemID) + '\n';

                    if (Game.DebugMode) entityInfo += '\n';
                }
            }
        }

        // look at jobs for value
        if (Game.DebugMode) entityInfo += "\nLooking at jobs for value:\n";
        Recipe bestRecipe = null;
        foreach(Job potentialJob in Job.jobDict.Values)
        {
            if (Game.DebugMode) entityInfo += "Looking at " + potentialJob + "\n";

            Tuple<float, Recipe> result = GetHighestRecipeValue(potentialJob);
            float value = result.Item1;
            Recipe recipe = result.Item2;

            if(value > bestJobValue)
            {
                bestJobValue = value;
                bestJob = potentialJob;
                bestRecipe = recipe;
            }
        }

        chosenRecipe = bestRecipe;
        Job = bestJob;

        if (!Job.Equals(prevJob))
        {
            daysWorked = 0;
        }

        if (Game.DebugMode) entityInfo += string.Format("Job due to value: {0}\nRecipe: {1}\n\n", Job, chosenRecipe);
    }

    private Recipe UpdateChosenRecipe()
    {
        if (Job.Recipes.Count == 1)
        {
            return chosenRecipe;
        }

        // later choose on value when there are multiple recipes / job
        return chosenRecipe;
    }

    // get the highest recipe and recipe value of the job
    private Tuple<float, Recipe> GetHighestRecipeValue(Job potentialJob)
    {
        float bestValue = float.MinValue;
        Recipe bestRecipe = null;

        if (Game.DebugMode) entityInfo += string.Format("Finding best value for {0}\n", potentialJob);

        foreach (Recipe recipe in potentialJob.Recipes)
        {
            if (Game.DebugMode) entityInfo += string.Format("Looking at recipe: {0}\n", recipe);

            float recipeValue = 0;

            // reward value
            float rewardValue = 0;
            foreach(var output in recipe.OutputInfo)
            {
                int quantity = output.Value;
                int itemID = output.Key;

                float outputItemValue = priceBeliefs[itemID];

                float marketPrice = City.MarketPlace.MarketPrice(itemID);
                if (marketPrice != -1) outputItemValue = (outputItemValue + marketPrice) / 2;

                outputItemValue *= quantity;

                rewardValue += outputItemValue;
            }

            // TODO: Why is this second? Make it first and check for all inputs first
            float costValue = 0;
            foreach(var input in recipe.InputInfo)
            {
                int quantity = input.Value;
                int itemID = input.Key;

                // check if item is available
                if(!HasAccessToItem(itemID))
                {
                    rewardValue = 0;
                    costValue = float.NaN;
                    break;
                }

                float inputItemValue = priceBeliefs[itemID];

                float marketPrice = City.MarketPlace.MarketPrice(itemID);
                if (marketPrice != -1) inputItemValue = (inputItemValue + marketPrice) / 2;

                inputItemValue *= quantity;

                costValue += inputItemValue;
            }

            // bonusEfficiency if entity has worked at this job for some time
            // encourages staying at the same job
            // sigmoid function with max of double efficiency (2)
            // min of 1 at daysWorked = 0
            bonusEfficiency = 1;
            if (Job != null && Job.Equals(potentialJob))
            {
                bonusEfficiency = 2 / (1 + Mathf.Exp(-0.5f * daysWorked));
                if (Game.DebugMode) entityInfo += "Bonus Efficiency: " + bonusEfficiency + '\n';
            }

            int numbCyclesPerDay = (int) (Game.DAY_LENGTH / (jobEfficiency[potentialJob.ID] / bonusEfficiency));

            recipeValue = (rewardValue - costValue) * numbCyclesPerDay;

            if (Game.DebugMode)
            {
                entityInfo += string.Format("Cost: {0}, Reward: {1}, Efficiency: {2}, Value: {3}\n",
                    costValue, rewardValue, jobEfficiency[potentialJob.ID], recipeValue);
            }

            if (recipeValue > bestValue)
            {
                bestValue = recipeValue;
                bestRecipe = recipe;
            }
        }

        if (Game.DebugMode)
        {
            entityInfo += string.Format("Best value: {0}\n\n", bestValue);
        }

        return Tuple.Create(bestValue, bestRecipe);
    }

    // returns list of inputs that are needed and has no buy offer
    private List<int> HasOrBuyingInputs(Recipe recipe)
    {
        List<int> missingInputs = MissingInputs(recipe);
        for (int x = missingInputs.Count - 1; x >= 0; x--)
        {
            int itemID = missingInputs[x];

            if (BuyOffers.ContainsKey(itemID))
            {
                missingInputs.RemoveAt(x);
            }
        }

        return missingInputs;
    }

    // has inputs in inventory
    private bool HasInputs(Recipe recipe)
    {
        foreach (var input in recipe.InputInfo)
        {
            int itemID = input.Key;
            int amount = input.Value;

            if( GetTotalInInventory(itemID) < amount)
            {
                return false;
            }
        }
        return true;
    }

    private List<int> MissingInputs(Recipe recipe)
    {
        List<int> missingInputs = new List<int>();
        foreach (var input in recipe.InputInfo)
        {
            int itemID = input.Key;
            int amount = input.Value;

            if (GetTotalInInventory(itemID) < amount)
            {
                missingInputs.Add(itemID);
            }
        }

        return missingInputs;
    }

    // returns true if all buyOffers were made successfully
    private bool BuyNeededItems()
    {
        if (Game.DebugMode) entityInfo += "Buying for need:\n";

        bool success = true;
        foreach(var need in DAILY_NEEDED_ITEMS)
        {
            int itemID = need.Key;
            int surplus = (int) Mathf.Floor(GetNeedSurplus(itemID));
            if (surplus < 0)
            {
                if(!CreateNeededItemBuyOffer(itemID, surplus * -1))
                {
                    success = false;
                    break;
                }
            }

            if (Game.DebugMode) entityInfo += '\n';
        }

        if (Game.DebugMode) entityInfo += Game.BREAK_LINE;

        return success;
    }

    /* 
     * Recursive function to buy inputs for needed item if applicable
     *
     * if item is a output in a recipe, buy its materials if cheaper than item itself
     * anyone can use any recipe, but being job matched with recipe gives greater efficiency (later)
     */
    private bool CreateNeededItemBuyOffer(int itemID, int neededAmount)
    {
        if (Game.DebugMode) entityInfo += string.Format("Need {0} of {1}\n", neededAmount, GetTag(itemID));

        if (Money == 0)
        {
            if (Game.DebugMode) entityInfo += "Have no money\n";
            return false;
        }

        if (BuyOffers.ContainsKey(itemID))
        {
            ReclaimBuyOffer(itemID);
        }

        // spend less money near the beginning of the day vs. the end
        float willingToSpend = priceBeliefs[itemID] + (Money - priceBeliefs[itemID]) * Game.TimeOfDay;

        // if priceBelief > Money, be willing to spend all Money
        if (priceBeliefs[itemID] > Money) willingToSpend = Money;

        PlaceBuyOffer(itemID, neededAmount, (int) willingToSpend);

        return true;

        /*
        int itemPrice = City.MarketPlace.LowestAvailablePriceToday(itemID, neededAmount);
        if (itemPrice == -1) itemPrice = int.MaxValue;

        // if we are making our need, consider buying inputs to make it
        int inputPrice = int.MaxValue;
        if (chosenRecipe.OutputInfo.ContainsKey(itemID))
        {
            // TODO: consider own inventory when looking at market as well
            inputPrice = City.MarketPlace.CostForRecipe(chosenRecipe);

            if (inputPrice == -1) inputPrice = int.MaxValue;
            else inputPrice *= neededAmount / chosenRecipe.OutputInfo[itemID];
        }

        // no immediate need to buy the item
        if (itemPrice > willingToSpend && inputPrice > willingToSpend)
        {
            return false;
        }

        // buy inputs over the outputs
        if(inputPrice < itemPrice)
        {
            foreach(var input in chosenRecipe.InputInfo)
            {
                int inputSurplus = (int)Mathf.Floor(GetNeedSurplus(input.Key));

                // don't need this input
                if (inputSurplus >= 0) continue;

                bool successfulBuy = BuyNeededItem(input.Key, inputSurplus * -1);
                if (!successfulBuy) return false;
            }
        }
        */
    }

    // assumes we already checked for the value of doing recipe
    private void CreateInputItemBuyOffers()
    {
        if (Game.DebugMode) entityInfo += "Buying input items for " + chosenRecipe + "\n";

        foreach(var input in chosenRecipe.InputInfo)
        {
            int itemID = input.Key;

            int surplus = (int)Mathf.Floor(GetInputSurplus(itemID));
            if (surplus < 0)
            {
                PlaceBuyOffer(itemID, surplus * -1, (int)priceBeliefs[itemID]);
            }
        }
        if (Game.DebugMode) entityInfo += Game.BREAK_LINE;
    }

    // Depreceated !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    /*private bool BuyItem(int itemID, int totalAmountToBuy)
    {
        LinkedList<Offer> offers = City.MarketPlace.GetSellOffers(itemID);

        if (offers == null)
        {
            if (Game.DebugMode) entityInfo += "No listings for item: " + GetTag(itemID) + "\n\n";
            Debug.LogErrorFormat("No listings for needed item after checking: {0}", GetTag(itemID));

            return false;
        }

        int amountLeftToBuy = totalAmountToBuy;
        foreach(Offer offer in offers)
        {
            int quantityLeft = offer.QuantityLeft;
            Entity seller = offer.Trader;
            if (seller == this || quantityLeft == 0) continue;

            if (Game.DebugMode) entityInfo += "Looking at " + offer;

            int amountToBuy = Money / offer.Price;
            if (Game.DebugMode) entityInfo += string.Format("{0} can afford to buy {1} of {2}\n",
                this, amountToBuy, GetTag(itemID));

            if (amountToBuy == 0) return false;

            if (amountToBuy > amountLeftToBuy)
            {
                amountToBuy = amountLeftToBuy;
                return false;
            }

            // can't buy more than the offer is selling
            if (amountToBuy > quantityLeft) amountToBuy = quantityLeft;

            Buy(offer, amountToBuy);

            amountLeftToBuy -= amountToBuy;

            if (amountLeftToBuy == 0) return true;
        }

        return false;
    }*/

    // create buy offers for needed/used items
    // Buy offers at 75%* of priceBelief
    // Quantity of 1 for now, increase if filled
    private void CreateValueBuyOffers()
    {
        if (Game.DebugMode) entityInfo += "Creating value buy offers for USED items:\n";

        // create combined list of used items and needed items
        List<int> usedItems = new List<int>(DAILY_NEEDED_ITEMS.Keys);
        foreach(int inputID in chosenRecipe.InputInfo.Keys)
        {
            usedItems.Add(inputID);
        }

        foreach(int itemID in usedItems)
        {
            if (!BuyOffers.ContainsKey(itemID))
            {
                int buyPrice = (int)(priceBeliefs[itemID] * UNNEEDED_USED_REDUCTION);
                if (buyPrice > 0)
                {
                    PlaceBuyOffer(itemID, 1, buyPrice);
                }
            }
        }

        if (Game.DebugMode) entityInfo += "\nCreating value buy offers for UNUSED items:\n";

        // create buy offers for unused unneeded items at 50%* of priceBelief
        foreach (int itemID in Item.ItemList.Keys)
        {
            if (!usedItems.Contains(itemID) && !BuyOffers.ContainsKey(itemID))
            {
                int buyPrice = (int)(priceBeliefs[itemID] * UNNEEDED_UNUSED_REDUCTION);
                if (buyPrice > 0 && Money >= buyPrice * 1)
                {
                    PlaceBuyOffer(itemID, 1, buyPrice);
                }
            }
        }

        if (Game.DebugMode) entityInfo += Game.BREAK_LINE;
    }

    private void PlaceBuyOffer(int itemID, int amount, int price)
    {
        if (amount < 1 || price < 1)
        {
            Debug.LogErrorFormat("{0} tried to buy {1} of {2} for {3} each", this, amount, GetTag(itemID), price);
            return;
        }

        if (BuyOffers.ContainsKey(itemID))
        {
            Debug.LogWarningFormat("There is already a buy offer:\n{2}", this, GetTag(itemID), BuyOffers[itemID]);
            ReclaimBuyOffer(itemID);
        }

        int actualAmount = amount;
        if (Money < amount * price)
        {
            actualAmount = Money / price;
        }

        if (actualAmount == 0)
        {
            if (Game.DebugMode) entityInfo += string.Format("Tried to buy {0} of {1} for {2} but only have {3}\n",
                amount, GetTag(itemID), amount * price, Money);
            return;
        }

        Offer offer = new Offer(this, itemID, actualAmount, price, false);
        BuyOffers[itemID] = offer;
        City.MarketPlace.AddOffer(offer);
    }

    // used at the beginning of the day
    // don't sell inputs for a job since working increases their value anyways
    private void CreateSellOffers()
    {
        if (Game.DebugMode) entityInfo += "Creating sell offers:\n";

        HashSet<Item> uniqueItems = new HashSet<Item>(inventory);

        foreach(Item item in uniqueItems)
        {
            // if not using the item
            if(!chosenRecipe.InputInfo.ContainsKey(item.ID)) PlaceSellOffer(item.ID);
        }

        if (Game.DebugMode) entityInfo += Game.BREAK_LINE;
    }

    private void PlaceSellOffer(int itemID, int surplus = -1, int sellPrice = -1)
    {
        bool customOffer = true;
        if (surplus == -1)
        {
            surplus = (int) Mathf.Floor(GetNeedSurplus(itemID));
            customOffer = false;
        }

        if(surplus > 0)
        {
            // sell offer already exists, just add to it
            if(SellOffers.ContainsKey(itemID))
            {
                if (customOffer)
                {
                    Debug.LogErrorFormat("Tried to create a custom sell offer for {0} at {1} for {2} but one already exists",
                        GetTag(itemID), sellPrice, surplus);
                }

                if (Game.DebugMode) entityInfo += string.Format("Adding to {0} to {1}", surplus, SellOffers[itemID]);

                SellOffers[itemID].AddToQuantity(surplus);

                if (Game.DebugMode) entityInfo += SellOffers[itemID];
                return;
            }

            if (sellPrice == -1)
            {
                sellPrice = (int)priceBeliefs[itemID];
                // sell at a markup above the priceBelief to make money
                if (!chosenRecipe.OutputInfo.ContainsKey(itemID) && IsBeingUsed(itemID))
                {
                    if (Game.DebugMode) entityInfo +=
                            string.Format("Marking up the price from {0} since I am also using it and not making it\n", priceBeliefs[itemID]);
                    sellPrice = (int)(sellPrice * ITEM_MARKUP);
                }

                if (sellPrice == 0) sellPrice = 1;
            }

            Offer offer = new Offer(this, itemID, surplus, sellPrice, true);
            SellOffers[itemID] = offer;
            City.MarketPlace.AddOffer(offer);
        }
    }

    // get total count of item in inventory
    private int GetTotalInInventory(int itemID)
    {
        int total = 0;
        for(int x = 0; x < inventory.Count; x++)
        {
            if(inventory[x].ID == itemID)
            {
                //total += inventory[x].GetCondition();
                total += 1;
            }
        }
        return total;
    }

    private int GetIndexInInventory(int itemID)
    {
        for(int x = 0; x < inventory.Count; x++)
        {
            if(inventory[x].ID == itemID)
            {
                return x;
            }
        }
        return -1;
    }

    private List<int> GetUniqueItemsInInventory()
    {
        List<int> uniqueItemIDs = new List<int>();
        foreach(Item item in inventory)
        {
            if(!uniqueItemIDs.Contains(item.ID))
            {
                uniqueItemIDs.Add(item.ID);
            }
        }
        return uniqueItemIDs;
    }

    public void RemoveBuyOffer(Offer offer)
    {
        City.MarketPlace.RemoveOffer(offer, false);
        BuyOffers.Remove(offer.ItemID);
    }

    public void RemoveSellOffer(Offer offer)
    {
        City.MarketPlace.RemoveOffer(offer, true);
        SellOffers.Remove(offer.ItemID);
    }

    private void AddItem(int itemID)
    {
        inventory.Add(Item.ItemList[itemID]);

        if (Game.DebugMode) entityInfo += string.Format("{0} acquired a {1}\n", this, GetTag(itemID));
    }

    public void AddItem(int itemID, int count)
    {
        for(int x = 0; x < count; x++)
        {
            AddItem(itemID);
        }
    }

    // returns false if could not find item in inventory
    private bool RemoveItem(int itemID)
    {
        foreach(Item item in inventory)
        {
            if(item.ID == itemID)
            {
                inventory.Remove(item);

                if (Game.DebugMode)
                {
                    entityInfo += string.Format("{0} lost a {1}\n",
                        Tag, GetTag(itemID));
                }
                return true;
            }
        }

        Debug.LogErrorFormat("{0} could not remove {1}\n{2}", this, GetTag(itemID), entityInfo);
        Debug.LogError(entityInfo);
        return false;
    }

    public bool RemoveItem(int itemID, int count)
    {
        for(int x = 0; x < count; x++)
        {
            if (!RemoveItem(itemID)) return false;
        }

        return true;
    }

    public bool GiveItems(Entity other, int itemID, int count)
    {
        for (int x = 0; x < count; x++)
        {
            if (!RemoveItem(itemID)) return false;
            other.AddItem(itemID);
        }
        return true;
    }

    public bool HasItem(int itemID)
    {
        if (itemID == -1)
        {
            return true;
        }

        foreach (Item item in inventory)
        {
            if (item.ID == itemID)
            {
                return true;
            }
        }
        return false;
    }

    public void GiveMoney(Entity other, int amount)
    {
        LoseMoney(amount);
        other.GainMoney(amount);
    }

    public void GainMoney(int amount)
    {
        Money += amount;

        if (Game.DebugMode) entityInfo += string.Format("{0} gained {1} money and now has {2}\n", this, amount, Money);
    }
    
    public bool LoseMoney(int amount)
    {
        Money -= amount;

        if(Money < 0)
        {
            Money += amount;

            Debug.LogError(this + " tried to spend more money then it could!");
            Debug.LogError(entityInfo);
            return false;
        }

        if (Game.DebugMode) entityInfo += string.Format("{0} lost {1} money and now has {2}\n", this, amount, Money);

        return true;
    }

    public bool IsDead()
    {
        return !(IsAlive);
    }

    public static void Initialize() { }

    // read from a json file to set the needs of every child class of Entity
    // use when adding different species
    private static void CreateEntityNeeds()
    {
        if (Game.DebugMode) Debug.Log("Loading " + ENTITY_NEEDS_FILE);

        var entityNeedsJson = Resources.Load<TextAsset>(ENTITY_NEEDS_FILE);

        if(entityNeedsJson)
        {
            JSONArray entityNeeds = (JSONArray) JSON.Parse(entityNeedsJson.text);
            foreach(JSONNode classNeed in entityNeeds)
            {
                string className = classNeed["class"];
                System.Type classType = System.Type.GetType(className);
                FieldInfo prop = classType.GetField("DAILY_NEEDED_ITEMS",
                    BindingFlags.NonPublic | BindingFlags.Static);

                JSONArray needs = classNeed["needs"].AsArray;
                Dictionary<int, int> dailyNeeds = new Dictionary<int, int>();
                foreach(JSONNode itemNeed in needs)
                {
                    foreach(KeyValuePair<string, JSONNode> node in itemNeed)
                    {
                        int itemID = Item.GetID(node.Key);
                        dailyNeeds.Add(itemID, node.Value);
                    }
                }

                prop.SetValue(null, dailyNeeds);
            }
        }
        else
        {
            Debug.LogError(ENTITY_NEEDS_FILE + " was not loaded");
        }
    }
}
