using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public class ThreadSpawner : MonoBehaviour
{
    [SerializeField] private string ConfigurationDataFileName;
    private string groupConfigurationFileName;
    private int instances;
    private int threads;
    private float spawnDelay;
    private int maxTriesToSpawn;
    private int attemptsToCreateNewBox;
    
    private int velocityIterations;
    private int positionIterations;

    [SerializeField] private GameObject objectSpawner;
    private int bestBoxesSolution = int.MaxValue;
    private int finishedThreads;
    private int currentThread;
    private List<GameObject> bestThreads;

    private struct Result
    {
        public int boxCount;
        public float timer;
    }

    private List<Result> results;


    [SerializeField] private GameObject box;
    //private float boxArea;
    private float boxHeightForThreadGeneration;

    private float globalTimer = 0f;

    void Awake()
    {
        results = new List<Result>();
        bestThreads = new List<GameObject>();

        ReadConfigurationData();
        SetBoxDimsenions();
        InstantiateThreads();
    }

    private void Update()
    {
        globalTimer += Time.deltaTime;
    }

    private void OnDestroy()
    {
        WriteResults();
    }

    private void SetBoxDimsenions()
    {
        float boxBottomPosition = 0;
        //float boxBase = 0;
        //float boxHeight = 0;

        GameObject boxTemp = Instantiate(box);
        int k = 0;
        foreach (Transform wallGameObject in boxTemp.transform)
        {
            /*if(k == 0)
            {
                boxBase += -(wallGameObject.transform.position.x + wallGameObject.GetComponent<BoxCollider2D>().bounds.extents.x);
            }

            if(k == 1)
            {
                boxBase += wallGameObject.transform.position.x - wallGameObject.GetComponent<BoxCollider2D>().bounds.extents.x;
            }*/

            if (k == 2)
            {
                //boxHeight += -(wallGameObject.transform.position.y + wallGameObject.GetComponent<BoxCollider2D>().bounds.extents.y);
                boxBottomPosition = wallGameObject.transform.position.y;
            }

            if (k == 3)
            {
                //boxHeight += wallGameObject.transform.position.y - wallGameObject.GetComponent<BoxCollider2D>().bounds.extents.y;
                boxHeightForThreadGeneration = wallGameObject.transform.position.y - boxBottomPosition;
                boxHeightForThreadGeneration = boxHeightForThreadGeneration * 1.2f;
            }

            k++;
        }

        //boxArea = boxBase * boxHeight;
        //Debug.Log($"box area thread spawner: {boxArea}");

        Destroy(boxTemp);
    }

    private void InstantiateThread()
    {
        ObjectSpawner objectSpawnerInstance = Instantiate(objectSpawner, new Vector3(0, boxHeightForThreadGeneration * -currentThread, 0), Quaternion.identity).GetComponent<ObjectSpawner>();
        objectSpawnerInstance.groupConfigurationFileName = groupConfigurationFileName;
        objectSpawnerInstance.onFinishedFunction = OnFinish;
        objectSpawnerInstance.SpawnDelay = spawnDelay;
        objectSpawnerInstance.maxTriesToSpawn = maxTriesToSpawn;
        objectSpawnerInstance.attemptsToCreateNewBox = attemptsToCreateNewBox;
        currentThread++;
    }

    private void InstantiateThreads()
    {
        for (int i = 0; i < threads; i++)
        {

            InstantiateThread();
        }
    }

    private void DeleteThreads()
    {
        foreach(GameObject thread in bestThreads)
        {
            Destroy(thread);
        }

        bestThreads.Clear();
    }

    private void OnFinish(GameObject gameobject, int boxCount, float timer)
    {
        finishedThreads++;
        results.Add(new Result
        {
            boxCount = boxCount,
            timer = timer
        });

        if (boxCount < bestBoxesSolution)
        {
            bestBoxesSolution = boxCount;
            DeleteThreads();
            bestThreads.Add(gameobject);
        }
        else if(boxCount == bestBoxesSolution)
        {
            bestThreads.Add(gameobject);
        }
       
            Destroy(gameobject);
        
        if(currentThread < instances)
        {
            InstantiateThread();
        }

        if(finishedThreads == instances)
        {
            Debug.LogWarning($"Best Solution: {bestBoxesSolution} Boxes, Total Time: {globalTimer}");
            WriteResults();
        }
    }

    private void WriteResults()
    {

        StreamWriter file = File.CreateText($"Results/{ConfigurationDataFileName}"); ;
        try
        {
            foreach(Result result in results)
            {
                file.WriteLine($"{result.boxCount},{result.timer}");
            }
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

    private void ReadConfigurationData()
    {
        StreamReader file = null;
        try
        {
            file = File.OpenText(Path.Combine(Application.streamingAssetsPath, ConfigurationDataFileName));

            string currentLine = file.ReadLine();
            while (currentLine != null)
            {
                string[] tokens = currentLine.Split(',');
                string configurationName = tokens[0];

                switch(configurationName)
                {
                    case "GroupConfigurationFileName":
                        groupConfigurationFileName = tokens[1];
                        break;

                    case "Instances":
                        instances = int.Parse(tokens[1]);
                        break;

                    case "Threads":
                        threads = int.Parse(tokens[1]);
                        break;

                    case "SpawnDelay":
                        spawnDelay = float.Parse(tokens[1]);
                        break;

                    case "MaxTriesToSpawn":
                        maxTriesToSpawn = int.Parse(tokens[1]);
                        break;

                    case "AttemptsToCreateNewBox":
                        attemptsToCreateNewBox = int.Parse(tokens[1]);
                        break;

                    case "VelocityIterations":
                        velocityIterations = int.Parse(tokens[1]);
                        break;

                    case "PositionIterations":
                        positionIterations = int.Parse(tokens[1]);
                        break;
                }

                currentLine = file.ReadLine();
            }

            SerializedObject physics2dSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
            SerializedProperty m_VelocityIterations = physics2dSettings.FindProperty("m_VelocityIterations");
            SerializedProperty m_PositionIterations = physics2dSettings.FindProperty("m_PositionIterations");
            m_VelocityIterations.intValue = velocityIterations;
            m_PositionIterations.intValue = positionIterations;
            physics2dSettings.ApplyModifiedProperties();

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
}
