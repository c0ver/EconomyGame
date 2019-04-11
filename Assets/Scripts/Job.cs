using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Job : Thing
{
    private const string JOB_FILE = "jobList";

    public static Dictionary<int, Job> jobDict = new Dictionary<int, Job>();
    public static List<Job> jobList;

    public static Job Default { get { return GetJob("FARMER"); } }
    public static Job ERROR { get { return GetJob("BAKER"); } }

    public List<Recipe> Recipes { get; } = new List<Recipe>();

    public int NumbRecipes { get { return Recipes.Count; } }

    public Recipe DefaultRecipe { get { return Recipes[0]; } }

    // assume all jobs are created sequentially after each other
    public Job(string name) : base(name)
    {
        jobDict.Add(ID, this);
    }

    public void AddRecipe(Recipe recipe)
    {
        Recipes.Add(recipe);
    }

    // not sure whether to put Work() in Job or Entity class
    public void Work(Entity entity, Recipe recipe)
    {
        if (!Recipes.Contains(recipe))
        {
            Debug.LogErrorFormat("{0} tried to make {1}, but is a {2}", entity, recipe, this);
            return;
        }

        foreach(var input in recipe.InputInfo)
        {
            entity.RemoveItem(input.Key, input.Value);
        }

        foreach(var output in recipe.OutputInfo)
        {
            entity.AddItem(output.Key, output.Value);
        }
    }

    public static Job GetRandomJob()
    {
        int index = UnityEngine.Random.Range(0, jobList.Count);

        return jobList[index];
    }

    public static Job GetJob(string jobName)
    {
        foreach(Job job in jobDict.Values)
        {
            if(job.Name.Equals(jobName))
            {
                return job;
            }
        }

        Debug.LogErrorFormat("{0} is not a valid job", jobName);

        return null;
    }

    public static void Initialize()
    {
        LoadJobList();
    }

    private static void LoadJobList()
    {
        if (Game.DebugMode) Debug.LogFormat("Loading {0}...", JOB_FILE);

        var jobListText = Resources.Load<TextAsset>(JOB_FILE);

        if(jobListText)
        {
            char[] splitChars = new char[] { '\n', '\r' };
            string[] jobs = jobListText.text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            foreach(string job in jobs)
            {
                new Job(job);
            }
            jobList = jobDict.Values.ToList();
        }
        else
        {
            Debug.LogError(JOB_FILE + " was not loaded");
            return;
        }

        if (Game.DebugMode)
        {
            string jobListInfo = "Job List: \n";
            foreach(Job job in jobDict.Values)
            {
                jobListInfo += job + "\n";
            }
            Debug.Log(jobListInfo);
        }
    }
}
