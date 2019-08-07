using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMap : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }
    public GameObject HexPrefab;
    // Update is called once per frame
    void GenerateMap()
    {
        for (int col= 0; col<10;col++)
        {
            for (int row = 0; row < 10; row++)
            {
                HexClass h = new HexClass(col, row);
                Instantiate(
                    HexPrefab,
                    h.Position(),
                    //new Vector3(col, 0, row),
                    Quaternion.identity,
                    this.transform
                    ) ;
            }
        }
    }
}
