using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using Newtonsoft.Json;

public class Game : MonoBehaviour
{
    public const int DAY_LENGTH = 5; // in seconds
    private const int TOTAL_CITIES = 1;
    public const string BREAK_LINE = "--------------------------------\n";
    public const string DOUBLE_BREAK_LINE = "------------------------------------------------------------------\n";

    // public to be seen in Unity's inspector
    public int dayCount = 0;
    public int totalPopulation;
    public float totalTimeElapsed = 0;
    public float timeOfDay = 0;
    public bool debugMode;
    public Graph graph;

    // so we can call game properties statically
    public static Game Instance { get; private set; }
    public static bool DebugMode { get { return Instance.debugMode; } }
    public static int DayCount { get { return Instance.dayCount; } }
    public static Graph Graph { get { return Instance.graph; } }
    public static float TimeOfDay
    {
        get
        {
            if (Instance.timeOfDay > DAY_LENGTH) return 1;
            else return Instance.timeOfDay / DAY_LENGTH;
        }
    }

    public List<City> cityList = new List<City>();

    private void Awake()
    {
        Instance = this;
        GameObject obj = GameObject.Find("Graph");
        graph = (Graph)obj.GetComponent(typeof(Graph));

        Item.Initialize();
        Entity.Initialize();
    }

    private void Start()
    {
        for(int x = 0; x < TOTAL_CITIES; x++)
        {
            cityList.Add(new City("Default"));
        }
        Debug.Log("Everything has been initialized properly!");

        BeginDay();
    }

    private void Update()
    {
        totalTimeElapsed = Time.realtimeSinceStartup;
        timeOfDay += Time.deltaTime;

        foreach(City city in cityList)
        {
            city.Update();
        }

        if(timeOfDay > DAY_LENGTH)
        {
            EndDay();

            dayCount += 1;
            timeOfDay -= DAY_LENGTH;
            BeginDay();
        }
    }

    private void EndDay()
    {
        totalPopulation = 0;

        foreach(City city in cityList)
        {
            city.EndDay();
            totalPopulation += city.Population;
        }
    }

    private void BeginDay()
    {
        foreach(City city in cityList)
        {
            city.BeginDay();
        }
    }

    private void CreateGraph()
    {

    }

    private void UpdateGraph()
    {

    }
}
