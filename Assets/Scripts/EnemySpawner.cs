using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    public GameObject enemy;
    public List<GameObject> spawnPts = new List<GameObject>();
    public int spawnPt = 0;
    public int enemySpawnNum;

    public float spawnRate = 2f;
    //make array of spawn points

    // Update is called once per frame
    void Update()
    {
        //autospawn code
        if (Input.GetKeyDown(KeyCode.O))
        {
            InvokeRepeating("SpawnDudes", 0f, spawnRate);
        }

        //manually spawn code
        if (Input.GetKeyDown(KeyCode.F))
        {
            //for (int i = 0; i < spawnPts.Count; i++) //no, this just spawns five at once. not what i need right now
            {

                //Debug.Log("Point is: " + spawnPts[i]);

                SpawnDudes();

                //would be cool if this was random, but ill do that if i have time and energy
                //if(spawnPt == spawnPts.Count - 1)
                //{
                //    spawnPt = 0;
                //} 
                //else
                //{
                //    spawnPt++;
                //}
            }
        }
        //if key hit, do something
        //clone a game object - instantiate it
    }

    public void SpawnDudes()
    {
        for (int i = 0; i < enemySpawnNum; i++)
        {
            Instantiate(enemy, spawnPts[spawnPt].transform.position, spawnPts[spawnPt].transform.rotation);
            //Debug.Log("Last spawn was at pos: " + spawnPt + "of " + spawnPts.Count);
            spawnPt = Random.Range(0, spawnPts.Count);
        }
    }
}
