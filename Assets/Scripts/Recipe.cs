using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recipe : Thing
{
    private const string RECIPE_FILE = "recipeList";

    public static List<Recipe> recipeList = new List<Recipe>();

    // key: occupation, value: list of recipes that the occupation can use
    private static Dictionary<Job, List<Recipe>> OccupationRecipes = new Dictionary<Job, List<Recipe>>();

    // key: itemID, value: quantity needed/created
    public Dictionary<int, int> InputInfo { get; } = new Dictionary<int, int>();
    public Dictionary<int, int> OutputInfo { get; } = new Dictionary<int, int>();

    public int NumbOutputs { get; }
    public int NumbInputs { get; }

    public List<Job> Jobs { get; } = new List<Job>();

    [Newtonsoft.Json.JsonConstructor]
    public Recipe(string name, List<string> outputItems, List<int> outputQuantities, List<string> inputItems, List<int> inputQuantities, List<string> jobs) : base(name)
    {
        // make sure that output and input items are valid
        Debug.AssertFormat(outputItems.Count == outputQuantities.Count,
            "# of outputItems: {0}, # of outputQuantities: {1}", outputItems.Count, outputQuantities.Count);
        Debug.AssertFormat(inputItems.Count == inputQuantities.Count,
            "# of inputItems: {0}, # of inputQuantities: {1}", inputItems.Count, inputQuantities.Count);
        NumbOutputs = outputItems.Count;
        NumbInputs = inputItems.Count;

        foreach (string jobName in jobs) {
            Job job = Job.GetJob(jobName);
            job.AddRecipe(this);
            Jobs.Add(job);
        }

        for(int x = 0; x < NumbInputs; x++)
        {
            InputInfo.Add(GetID(inputItems[x]), inputQuantities[x]);
        }

        for(int x = 0; x < NumbOutputs; x++)
        {
            OutputInfo.Add(GetID(outputItems[x]), outputQuantities[x]);
        }
    }

    public bool IsOutput(int itemID)
    {
        return OutputInfo.ContainsKey(itemID);
    }

    public Job GetJob()
    {
        if(Jobs.Count == 0)
        {
            Debug.LogErrorFormat("{0} does not have a job that can make it", this);
            return null;
        }
        return Jobs[0];
    }

    public override string Info()
    {
        string recipe = base.Info();
        recipe += "Inputs: ";
        foreach(var input in InputInfo)
        {
            recipe += GetTag(input.Key) + ": " + input.Value + '\n';
        }

        recipe += "Outputs: ";
        foreach(var output in OutputInfo)
        {
            recipe += GetTag(output.Key) + ": " + output.Value + '\n';
        }
        
        return recipe;
    }

    public static HashSet<Job> GetCrafters(int itemID)
    {
        HashSet<Job> jobs = new HashSet<Job>();

        // look through every recipe
        foreach(Recipe recipe in recipeList)
        {
            // look through every output for that recipe
            foreach(int outputID in recipe.OutputInfo.Keys)
            {
                if(outputID == itemID)
                {
                    // add every job that uses the recipe
                    foreach(Job job in recipe.Jobs)
                    {
                        jobs.Add(job);
                    }
                }
            }
        }

        return jobs;
    }

    public static List<Recipe> GetRecipes(Job occupation)
    {
        return OccupationRecipes[occupation];
    }

    public static void Initialize()
    {
        LoadRecipeData();
    }

    private static void LoadRecipeData()
    {
        if (Game.DebugMode) Debug.LogFormat("Loading {0}...", RECIPE_FILE);

        var recipeListJson = Resources.Load<TextAsset>(RECIPE_FILE);

        if(recipeListJson)
        {
            recipeList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Recipe>>(recipeListJson.text);
        }
        else
        {
            Debug.LogError(RECIPE_FILE + " was not loaded");
            return;
        }

        foreach(Recipe recipe in recipeList)
        {
            foreach (Job occupation in recipe.Jobs) {
                if(!OccupationRecipes.ContainsKey(occupation))
                {
                    OccupationRecipes[occupation] = new List<Recipe>();
                }
                OccupationRecipes[occupation].Add(recipe);
            }
        }

        if (Game.DebugMode)
        {
            string objs = "Recipe List: \n";
            foreach(Recipe recipe in recipeList)
            {
                objs += recipe + "\n";
            }
            Debug.Log(objs);
        }
    }
}
