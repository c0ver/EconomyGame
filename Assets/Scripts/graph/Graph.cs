using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeMonkey.Utils;
using System;

// Code from https://unitycodemonkey.com/video.php?v=CmU5-v-v1Qo

public class Graph : MonoBehaviour
{
    private const float SPACE_BETWEEN_POINTS = 50f;

    private const float LEVEL_LEGEND_COLOR_TEXT = 7f;

    private RectTransform graphContainer;

    // serialized to be able to set in editor
    [SerializeField] private Sprite circleSprite;

    private Dictionary<int, List<Vector2>> marketHistory = new Dictionary<int, List<Vector2>>();
    private float largestPrice = 200;
    private float graphHeight;

    private Color[] colors = new Color[6]
    {
        Color.blue,
        Color.cyan,
        Color.green,
        Color.magenta,
        Color.red,
        Color.yellow,
    };

    private void Awake()
    {
        graphContainer = transform.Find("graphContainer").GetComponent<RectTransform>();
        graphHeight = graphContainer.sizeDelta.y;
    }

    private void Start()
    {
        if (Item.ItemList.Count > colors.Length)
        {
            Debug.LogError("There are more items than available colors to graph");
        }

        foreach (int itemID in Item.ItemList.Keys)
        {
            marketHistory.Add(itemID, new List<Vector2>());
        }

        CreateLegend();
    }

    // <itemID, marketPrice>
    public void AddDay(Dictionary<int, float> marketPrices)
    {
        foreach(int itemID in marketHistory.Keys)
        {
            float price;
            if (marketPrices.ContainsKey(itemID) && marketPrices[itemID] != -1)
            {
                price = marketPrices[itemID];
                if (price > largestPrice)
                {
                    Debug.LogWarningFormat("Highest price changed from {0} to {1}", largestPrice, price);
                    largestPrice = price;
                }
            }
            else
            {
                price = 0;
            }

            float xPos = SPACE_BETWEEN_POINTS + Game.DayCount * SPACE_BETWEEN_POINTS;
            float yPos = (price / largestPrice) * graphHeight;

            Vector2 pos = CreatePoint(xPos, yPos, itemID, colors[itemID - Item.FirstItemID]);
            marketHistory[itemID].Add(pos);
        }
    }

    private void CreateLegend()
    {
        int index = 1;
        float spacing = 20; 

        Font defaultFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");


        foreach(int itemID in marketHistory.Keys)
        {
            // create color key
            GameObject colorKey = new GameObject("key", typeof(Image));
            colorKey.transform.SetParent(graphContainer);

            Image image = colorKey.GetComponent<Image>();
            image.sprite = circleSprite;
            image.color = colors[itemID - Item.FirstItemID];

            RectTransform colorRectTransform = colorKey.GetComponent<RectTransform>();
            colorRectTransform.anchoredPosition = new Vector2(0, -1 * spacing * index);
            colorRectTransform.anchorMin = new Vector2(0, 1);
            colorRectTransform.anchorMax = new Vector2(0, 1);
            colorRectTransform.sizeDelta = new Vector2(14, 14);


            // create text
            GameObject textKey = new GameObject(Thing.GetTag(itemID), typeof(Text));
            textKey.transform.SetParent(colorKey.transform);

            // Set Text component properties.
            Text text = textKey.GetComponent<Text>();
            text.font = defaultFont;
            text.text = Thing.GetTag(itemID);
            text.fontSize = 12;
            text.color = Color.black;

            RectTransform textRectTransform = textKey.GetComponent<RectTransform>();
            textRectTransform.anchoredPosition = new Vector2(spacing, 0);
            textRectTransform.sizeDelta = new Vector2(50, 14);

            index++;
        }
    }

    private Vector2 CreatePoint(float xPos, float yPos, int itemID, Color color) {
        GameObject obj = new GameObject(Game.DayCount + Thing.GetTag(itemID), typeof(Image));
        obj.transform.SetParent(graphContainer);
        Image image = obj.GetComponent<Image>();
        image.sprite = circleSprite;
        image.color = colors[itemID - Item.FirstItemID];

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        Vector2 pos = new Vector2(xPos, yPos);
        rectTransform.anchoredPosition = pos;
        rectTransform.sizeDelta = new Vector2(11, 11);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        return pos;
    }

    /*
    private void UpdateGraph()
    {
        foreach (var history in marketHistory)
        {
            int itemID = history.Key;
            List<float> itemHistory = history.Value;
            ShowGraph(itemHistory);
        }
    }

    private void ShowGraph(List<float> valueList) {

        GameObject lastCircleGameObject = null;
        for (int i = 0; i < valueList.Count; i++) {
            float xPosition = SPACE_BETWEEN_POINTS + i * SPACE_BETWEEN_POINTS;
            float yPosition = (valueList[i] / largestPrice) * graphHeight;
            GameObject circleGameObject = CreatePoint(new Vector2(xPosition, yPosition));
            if (lastCircleGameObject != null) {
                CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, circleGameObject.GetComponent<RectTransform>().anchoredPosition);
            }
            lastCircleGameObject = circleGameObject;
        }
    }

    private void CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB) {
        GameObject gameObject = new GameObject("dotConnection", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);

        // white with half transparency
        gameObject.GetComponent<Image>().color = new Color(1,1,1, .5f);

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 dir = (dotPositionB - dotPositionA).normalized;
        float distance = Vector2.Distance(dotPositionA, dotPositionB);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 3f);
        rectTransform.anchoredPosition = dotPositionA + dir * distance * .5f;
        rectTransform.localEulerAngles = new Vector3(0, 0, UtilsClass.GetAngleFromVectorFloat(dir));
    }*/
}
