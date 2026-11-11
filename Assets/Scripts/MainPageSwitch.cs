using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class MainPageSwitch : MonoBehaviour
{

    public GameObject MP;
    public GameObject SP;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //public void PageSwitch()
    //{
    //    MP.SetActive(!MP.activeSelf);
    //    SP.SetActive(!SP.activeSelf);
    //}

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void StartGame()
    {

    }

    public void ContinueGame()
    {

    }
}
