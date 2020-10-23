using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PageThree : MonoBehaviour
{
    //------------------------------------------------------------------------------------
    // Data
    //------------------------------------------------------------------------------------

    [SerializeField]
    PageThreeWidget[] pageThreeWidgets;

    [SerializeField]
    Button backBtn;

    [SerializeField]
    Button nextBtn;

    //------------------------------------------------------------------------------------
    // Functions
    //------------------------------------------------------------------------------------

    void Start()
    {
        backBtn.onClick.AddListener(PrvScene);
        nextBtn.onClick.AddListener(NextScene);
        Setup();
    }

    //------------------------------------------------------------------------------------
    public void Setup()
    {
        if (Client.Instance.TextureCache != null)
        {
            if (pageThreeWidgets != null)
            {
                for (int i = 0; i < pageThreeWidgets.Length; i++)
                {
                    pageThreeWidgets[i].SetImage(Client.Instance.TextureCache[i]);
                }
            }
        }
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
    void PrvScene()
    {
        SceneManager.LoadScene("PageTwo");
    }

    //------------------------------------------------------------------------------------
    void NextScene()
    {
        SceneManager.LoadScene("PageOne");
    }

}
