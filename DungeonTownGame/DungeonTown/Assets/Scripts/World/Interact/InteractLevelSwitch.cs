using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractLevelSwitch : Interactable
{

    public string levelToLoad;
    public float levelSwitchTime;

    private float currLevelSwitchTime;

    public void Update()
    {
        if (bExpended)
        {
            currLevelSwitchTime -= Time.deltaTime;
            if(currLevelSwitchTime <= 0)
            {
                SceneManager.LoadScene(levelToLoad);
            }
        }
    }

    public override void Interact()
    {
        bExpended = true;
        currLevelSwitchTime = levelSwitchTime;
    }
}
