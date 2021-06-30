using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ObjectSpawner : MonoBehaviour
{
    public string groupConfigurationFileName;// = "data.csv";
    public UnityAction<GameObject, int, float> onFinishedFunction;

    #region SpawnObjects
    protected struct Figure
    {
        public GameObject gameObject;
        public Rigidbody2D rb2d;
        public Collider2D collider2D;
    }

    [SerializeField] private List<GameObject> objects2Spawn;
    private List<List<int>> groupObjects2Spawn = new List<List<int>>();
    protected List<List<Figure>> intantiatedObjects = new List<List<Figure>>();
    protected List<int> currentGroup;
    private int currentGroupTotalFigures;
    protected int currentGroupInstantiatedFigures;
    protected bool hasToCompleteGroup = false;
    #endregion


    #region BoxesAndwallsa
    [SerializeField] private GameObject box;
    protected GameObject currentBox;
    protected List<Figure> currentBoxFigures = new List<Figure>();
    private int boxCounter = 0;
    private int currentAttemptsToCreateNewBox = 0;
    public int attemptsToCreateNewBox;// = 100;
    public int maxTriesToSpawn;// = 100;

    private class Wall
    {
        public Vector2 position;
        public Vector2 colliderExtents;

        public Wall(Vector3 position, BoxCollider2D collider)
        {
            this.position = new Vector2(position.x, position.y);
            this.colliderExtents = new Vector2(collider.bounds.extents.x, collider.bounds.extents.y);
        }
    }

    private Wall[] walls = new Wall[4];
    #endregion


    #region timers
    private float spawnDelayTimer = 0f;
    public float SpawnDelay;// = 0.05f;

    private float globalTimer = 0f;
    #endregion

    private const float deleteTolerance = 0.1f;

    // Awake is called before the first frame
    private void Start()
    {
        LoadGroups();
        InstantiateFigures();
        CreateBox();
    }

    private void Update()
    {
        globalTimer += Time.deltaTime;
        spawnDelayTimer += Time.deltaTime;

        if (spawnDelayTimer >= SpawnDelay)
        {
            spawnDelayTimer = 0;
            ChangeToNextGroup();
            InvokeGroup();
        }
    }

    protected List<int> FindSmallerGroup()
    {

        for (int i = 0; i < groupObjects2Spawn.Count; i++)
        {
            if(groupObjects2Spawn[i] == currentGroup)
            {
                continue;
            }

            if (GroupFiguresCount(groupObjects2Spawn[i]) < currentGroupTotalFigures)
            {
                return groupObjects2Spawn[i];
            }
        }

        return null;
    }

    virtual protected void CreateBox()
    {
        foreach (Figure figure in currentBoxFigures)
        {
            Destroy(figure.rb2d);
            Destroy(figure.collider2D);
        }
        currentBoxFigures.Clear();

        if (currentBox == null)
        {
            currentBox = Instantiate(box, transform);
        }
        else
        {
            currentBox = Instantiate(box, new Vector3(2 * walls[1].position.x - walls[0].position.x, walls[0].position.y, 0), Quaternion.identity);
            currentBox.transform.parent = transform;
        }

        boxCounter++;

        int i = 0;
        foreach (Transform wallGameObject in currentBox.transform)
        {
            Wall wall = new Wall(wallGameObject.transform.position, wallGameObject.GetComponent<BoxCollider2D>());
            walls[i] = wall;
            i++;
        }
    }

    private int GetFigure2SpawnIndex()
    {
        List<int> availableFigureKindIndexes = new List<int>();

        for (int i = 0; i < objects2Spawn.Count; i++)
        {
            if (currentGroup[i] > 0)
            {
                availableFigureKindIndexes.Add(i);
            }
        }

        if (availableFigureKindIndexes.Count == 0)
        {
            return -1;
        }

        if (availableFigureKindIndexes.Count == 1)
        {
            return availableFigureKindIndexes[0];
        }

        for (int i = 0; i < availableFigureKindIndexes.Count; i++)
        {
            int temp = availableFigureKindIndexes[i];
            int randomIndex = UnityEngine.Random.Range(i, availableFigureKindIndexes.Count);
            availableFigureKindIndexes[i] = availableFigureKindIndexes[randomIndex];
            availableFigureKindIndexes[randomIndex] = temp;
        }

        return availableFigureKindIndexes[0];
    }

    private Vector2 GetRandomPosition(float polygonExtentsX, float polygonExtentsY)
    {

        float spawnPositionX = UnityEngine.Random.Range(walls[0].position.x + walls[0].colliderExtents.x + polygonExtentsX + deleteTolerance,
                 walls[1].position.x - walls[1].colliderExtents.x - polygonExtentsX - deleteTolerance);

        float spawnPositionY = 0;


        if (currentAttemptsToCreateNewBox >= attemptsToCreateNewBox / 2)
        {
            spawnPositionY = UnityEngine.Random.Range(walls[2].position.y + 2 * (walls[3].position.y - walls[2].position.y) / 3 + polygonExtentsY + deleteTolerance,
                walls[3].position.y - walls[3].colliderExtents.y - polygonExtentsY - deleteTolerance);
        }
        else
        {
            spawnPositionY = UnityEngine.Random.Range(walls[2].position.y + walls[2].colliderExtents.y + polygonExtentsY + deleteTolerance,
                walls[3].position.y - walls[3].colliderExtents.y - polygonExtentsY - deleteTolerance);
        }

        return new Vector2(spawnPositionX, spawnPositionY);
    }

    private void InvokeGroup()
    {
        if (currentGroup == null)
        {
            return;
        }


        int figure2SpawnIndex = GetFigure2SpawnIndex();
        List<Figure> instantiatedObjectList = intantiatedObjects[figure2SpawnIndex];
        Figure instantiatedFigure = instantiatedObjectList[0];
        GameObject instantiatedObject = instantiatedFigure.gameObject;
        instantiatedObject.SetActive(true);

        PolygonCollider2D polygonCollider = instantiatedObject.GetComponent<PolygonCollider2D>();
        float polygonExtentsX = polygonCollider.bounds.extents.x;
        float polygonExtentsY = polygonCollider.bounds.extents.y;


        Vector2 spawnPosition;
        float tests = 0;
        Collider2D colliderOverlap;

        do
        {
            spawnPosition = GetRandomPosition(polygonExtentsX, polygonExtentsY);
            instantiatedObject.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0);
            tests++;
            colliderOverlap = Physics2D.OverlapArea(
            new Vector2(spawnPosition.x - polygonExtentsX, spawnPosition.y + polygonExtentsY),
            new Vector2(spawnPosition.x + polygonExtentsX, spawnPosition.y - polygonExtentsY));
        }
        while (colliderOverlap != null && tests < maxTriesToSpawn);

        if (tests == maxTriesToSpawn)
        {
            instantiatedObject.SetActive(false);
            currentAttemptsToCreateNewBox++;

            if (currentAttemptsToCreateNewBox == attemptsToCreateNewBox)
            {
                currentAttemptsToCreateNewBox = 0;
                CreateBox();
            }
        }
        else
        {
            currentAttemptsToCreateNewBox = 0;
            /*debugRect = new Rect(
                spawnPosition.x - polygonExtentsX - deleteTolerance, 
                spawnPosition.y - polygonExtentsY - deleteTolerance, 
                2 * polygonExtentsX + deleteTolerance, 
                2 * polygonExtentsY + deleteTolerance);*/

            instantiatedObject.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0);
            currentGroup[figure2SpawnIndex]--;
            instantiatedObjectList.Remove(instantiatedFigure);
            instantiatedObject.transform.SetParent(currentBox.transform, true);
            currentBoxFigures.Add(instantiatedFigure);
            currentGroupInstantiatedFigures++;
        }
    }

    private int GroupFiguresCount(List<int> group)
    {
        int availableFigures = 0;
        foreach (int figureSpawnCount in group)
        {
            availableFigures += figureSpawnCount;
        }

        return availableFigures;
    }

    protected bool CheckCurrentGroupCompleted()
    {
        return GroupFiguresCount(currentGroup) == 0;
    }

    protected void SetCurrentGroup(List<int> newGroup)
    {
        
        currentGroup = newGroup;
        currentGroupTotalFigures = GroupFiguresCount(currentGroup);
        currentGroupInstantiatedFigures = 0;
    }

    private void ChangeToNextGroup()
    {
        if (currentGroup == null)
        {
            return;
        }

        if (CheckCurrentGroupCompleted())
        {
            groupObjects2Spawn.Remove(currentGroup);
            hasToCompleteGroup = false;
            if (groupObjects2Spawn.Count > 0)
            {
                SetCurrentGroup(groupObjects2Spawn[0]);
            }
            else
            {
                currentGroup = null;
                Debug.Log($"boxes: {boxCounter}, timer: {globalTimer}");
                onFinishedFunction(gameObject, boxCounter, globalTimer);
                enabled = false;
            }
        }
    }

    private void LoadGroups()
    {
        StreamReader file = null;
        try
        {
            file = File.OpenText(Path.Combine(Application.streamingAssetsPath, groupConfigurationFileName));
            string currentLine = file.ReadLine();
            while (currentLine != null)
            {
                int[] group = Array.ConvertAll(currentLine.Split(','), int.Parse);
                groupObjects2Spawn.Add(new List<int>(group));
                currentLine = file.ReadLine();
            }

            SetCurrentGroup(groupObjects2Spawn[0]);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            if (file != null)
            {
                file.Close();
            }
        }
    }

    private void InstantiateFigures()
    {
        for(int n = 0; n < objects2Spawn.Count; n++)
        {
            intantiatedObjects.Add(new List<Figure>());
        }

        foreach (List<int> group in groupObjects2Spawn)
        {
            int j = 0;
            foreach (int numberObjects in group)
            {
                for (int k = 0; k < numberObjects; k++)
                {
                    Figure figure = new Figure();
                    figure.gameObject = Instantiate(objects2Spawn[j], transform);
                    figure.rb2d = figure.gameObject.GetComponent<Rigidbody2D>();
                    figure.collider2D = figure.gameObject.GetComponent<Collider2D>();
                    figure.gameObject.SetActive(false);
                    intantiatedObjects[j].Add(figure);
                }
                j++;
            }
        }
    }

    /*
     * Debug
     */

    /*private Rect debugRect = new Rect(); 
    void OnDrawGizmos()
    {
        // Green
        Gizmos.color = new Color(0.0f, 1.0f, 0.0f);
        DrawRect(debugRect);
    }

    void DrawRect(Rect rect)
    {
        Gizmos.DrawWireCube(new Vector3(rect.center.x, rect.center.y, 0.01f), new Vector3(rect.size.x, rect.size.y, 0.01f));
    }*/
}
