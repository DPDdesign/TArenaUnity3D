using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void End()
    {
        SceneManager.LoadScene("MainMenu_Scene");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
