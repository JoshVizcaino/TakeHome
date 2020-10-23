using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PageTwo : MonoBehaviour
{
    //------------------------------------------------------------------------------------
    // Data
    //------------------------------------------------------------------------------------
    [SerializeField]
    PageTwoWidget pageTwoWidget;

    [SerializeField]
    GameObject widgetObject;

    [SerializeField]
    Camera camera;

    [SerializeField]
    Button nextBtn;

    [SerializeField]
    Button backBtn;

    [Header("Laptop")]
    public Animator anim;
    public GameObject laptop;
    public GameObject screen;

    //------------------------------------------------------------------------------------
    // Functions
    //------------------------------------------------------------------------------------
    void Start()
    {
        nextBtn.onClick.AddListener(NextScene);
        backBtn.onClick.AddListener(PrvScene);
        anim = laptop.gameObject.GetComponent<Animator>();
    }

    //------------------------------------------------------------------------------------
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Transform object_hit = hit.transform;
                Debug.Log(object_hit);
                if (object_hit.tag == "Laptop")
                {
                    anim.SetTrigger("ChangeState");
                    if (anim.GetBool("IsOpen"))
                    {
                        anim.SetBool("IsOpen", false);
                    }
                    else
                    {
                        anim.SetBool("IsOpen", true);
                    }
                }
            }  
        }
        GameUtilities.SetActive(screen, anim.GetBool("IsOpen") && anim.GetCurrentAnimatorStateInfo(0).IsName("LaptopOpen"));
        GameUtilities.SetActive(widgetObject, anim.GetBool("IsOpen") && anim.GetCurrentAnimatorStateInfo(0).IsName("LaptopOpen"));
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    //------------------------------------------------------------------------------------
    void NextScene()
    {
        SceneManager.LoadScene("PageThree");
    }

    //------------------------------------------------------------------------------------
    void PrvScene()
    {
        SceneManager.LoadScene("PageOne");
    }
}
