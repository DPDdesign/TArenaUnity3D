using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    public Button ThisButton;
    public List<GameObject> AllDisabled;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool dont = false;
            for (int i=0; i < AllDisabled.Count; i++)
            {
                if (AllDisabled[i].active==true)
                {
                    dont = true;
                }
            }
            if( dont==false)
            ThisButton.onClick.Invoke();
        }
    }
}
