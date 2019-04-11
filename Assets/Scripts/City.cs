using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class City : Thing
{
    public static List<City> cityList = new List<City>();

    private const int START_POPULATION = 20;

    private List<Entity> population = new List<Entity>();
    public int Population { get { return population.Count; } }

    public MarketPlace MarketPlace { get; } = new MarketPlace();

    private string endDayInfo = "";

    // Start is called before the first frame update
    public City(string name) : base(name)
    {
        Debug.Log("Making a city");
        for(int x = 0; x < START_POPULATION; x++)
        {
            population.Add(new Entity("Person", this));
        }
    }

    public void Update()
    {
        foreach(Entity entity in population)
        {
            entity.Update();
        }
    }

    // make sell offers
    public void BeginDay()
    {
        if (Game.DebugMode) Debug.Log(string.Format("Begin Day {0} for {1}. Population: {2}\n", Game.DayCount, this, Population));

        MarketPlace.Clear();

        foreach(Entity entity in population)
        {
            entity.BeginDay();
        }

        MarketPlace.MatchMarketOffers();

        Debug.Log("Finished initializing beginning of Day " + Game.DayCount);
    }

    public void EndDay()
    {
        Debug.Log("Reached city EndDay for Day " + Game.DayCount);

        if (Game.DebugMode) endDayInfo = string.Format("End Day {0} for {1}. Population: {2}\n\n", Game.DayCount, Name, population.Count);

        // stuff that every entity needs to do before endDay stuff
        foreach(Entity entity in population)
        {
            entity.MidDay();
        }
        Debug.Log("Finished MidDay for entire population");

        foreach(Entity entity in population)
        {
            entity.EndDay();
        }
        Debug.Log("Finished EndDay for entire population");

        // dead people don't count as people
        for(int x = population.Count - 1; x >= 0; x--)
        {
            if(population[x].IsDead())
            {
                population.RemoveAt(x); 
            }
        }

        if (Game.DebugMode)
        {
            endDayInfo += MarketPlace.Info();
            Debug.Log(endDayInfo);
            Debug.Log("\n");
        }
    }
}
