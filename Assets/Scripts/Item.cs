using Newtonsoft.Json;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Rarity
{
    COMMON = 0,
    UNCOMMON = 1,
    RARE = 2
}

public enum Type
{
    MISC = 0,
    CONSUMABLE = 1,
    TOOL = 2,
    CLOTHING = 3
}

public class Item : Thing
{
    private const string ITEM_FILE = "itemList";
    public static Dictionary<int, Item> ItemList = new Dictionary<int, Item>();

    public string Description { get; }
    public Rarity Rarity { get; }
    public Type Type { get; }
    public float DecayFactor { get; }
    public float Condition { get; private set; }

    // to keep a unique list
    public HashSet<Job> Crafters { get; set; }

    // recipes that use this item
    public List<Recipe> InRecipes { get; } = new List<Recipe>();

    // recipes that make this item
    public List<Recipe> OutRecipes { get; } = new List<Recipe>();

    [JsonConstructor]
    public Item(string name, string desc, Rarity rarity, Type type, float decayFactor) : base(name)
    {
        Description = desc;
        Rarity = rarity;
        Type = type;
        DecayFactor = decayFactor;
        Condition = 1;

        ItemList.Add(ID, this);
    }

    public void Decay()
    {
        Condition -= DecayFactor;
    }

    public override string Info()
    {
        string info = base.Info() + Description + "\n" + Rarity + "\n" + Type + "\n" + DecayFactor + '\n';

        info += "Crafters: ";
        foreach(Job crafter in Crafters)
        {
            info += crafter + ", ";
        }

        info += "\nInRecipes: \n";
        foreach(Recipe recipe in InRecipes)
        {
            info += recipe + ", ";
        }

        info += "\nOutRecipes: \n";
        foreach(Recipe recipe in OutRecipes)
        {
            info += recipe + ", ";
        }

        return info + '\n';
    }

    private void AddOutRecipe(Recipe recipe)
    {
        OutRecipes.Add(recipe);
    }

    private void AddInRecipe(Recipe recipe)
    {
        InRecipes.Add(recipe);
    }

    public static Item GetItem(int itemID)
    {
        return ItemList[itemID];
    }

    public static void Initialize()
    {
        LoadItemData();

        Job.Initialize();
        Recipe.Initialize();

        FindCrafters();
        FindRecipes();

        if (Game.DebugMode)
        {
            string itemListInfo = "Item List: \n";
            foreach(Item item in ItemList.Values)
            {
                itemListInfo += item.Info() + "\n";
            }
            Debug.Log(itemListInfo);
        }
    }

    private static void LoadItemData()
    {
        if (Game.DebugMode) Debug.LogFormat("Loading {0}...", ITEM_FILE);

        var itemListJson = Resources.Load<TextAsset>(ITEM_FILE);

        if(itemListJson)
        {
            JSONArray array = (JSONArray)JSON.Parse(itemListJson.text);
            foreach (JSONNode itemJson in array)
            {
                string json = itemJson.Info();
                string className = itemJson["type"];
                switch (className)
                {
                    case "CONSUMABLE":
                        JsonConvert.DeserializeObject<Consumable>(json);
                        break;
                    default:
                        JsonConvert.DeserializeObject<Item>(json);
                        break;
                }
            }
        }
        else
        {
            Debug.LogError(ITEM_FILE + " was not loaded");
            return;
        }
    }

    private static void FindCrafters()
    {
        foreach(Item item in ItemList.Values)
        {
            item.Crafters = Recipe.GetCrafters(item.ID);
        }
    }

    private static void FindRecipes()
    {
        foreach(Recipe recipe in Recipe.recipeList)
        {
            foreach(int itemID in recipe.OutputInfo.Keys)
            {
                ItemList[itemID].AddOutRecipe(recipe);
            }

            foreach(int itemID in recipe.InputInfo.Keys)
            {
                ItemList[itemID].AddInRecipe(recipe);
            }
        }
    }
}
