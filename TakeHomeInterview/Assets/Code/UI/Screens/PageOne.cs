using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PageOne : MonoBehaviour
{
    //------------------------------------------------------------------------------------
    // Data
    //------------------------------------------------------------------------------------
    [SerializeField]
    PageOneWidget[] pageOneWidgets = null;

    [SerializeField]
    Button nextBtn;

    [SerializeField]
    Button backBtn;

    [SerializeField]
    GameObject loading;

    bool mIsLoading;

    //------------------------------------------------------------------------------------
    // Static Data
    //------------------------------------------------------------------------------------
    public static string ENDPOINT_URL = "https://jsonplaceholder.typicode.com/photos/";

    //------------------------------------------------------------------------------------
    // Functions
    //------------------------------------------------------------------------------------

    public void Start()
    {
        nextBtn.onClick.AddListener(ChangeScene);
        backBtn.onClick.AddListener(LastScene);
        mIsLoading = true;
        Setup();
    }

    //------------------------------------------------------------------------------------
    public void Setup()
    {
        if (pageOneWidgets != null)
        {
            for (int i = 0; i < pageOneWidgets.Length; i++)
            {
                pageOneWidgets[i].Setup(i + 1);
            }
        }
        mIsLoading = false;

        GameUtilities.SetActive(loading, mIsLoading);
    }

    //------------------------------------------------------------------------------------
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    //------------------------------------------------------------------------------------
    public void ChangeScene()
    {
        SceneManager.LoadScene("PageTwo");
    }

    //------------------------------------------------------------------------------------
    public void LastScene()
    {
        SceneManager.LoadScene("PageThree");
    }

}
