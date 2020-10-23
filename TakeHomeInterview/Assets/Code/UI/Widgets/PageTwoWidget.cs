using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class PageTwoWidget : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI text;

    static string lyrics = "                                  We're no strangers to love You know the rules and so do I A full commitment's what I'm thinking of You wouldn't get this from any other guy I just want to tell you how I'm feeling Gotta make you understand Never gonna give you up, never gonna let you down Never gonna run around and desert you Never gonna make you cry, never gonna say goodbye Never gonna tell a lie and hurt you We've known each other for so long Your heart's been aching but you're too shy to say it Inside we both know what's been going on We know the game and we're gonna play it And if you ask me how I'm feeling Don't tell me you're too blind to see Never gonna give you up, never gonna let you down Never gonna run around and desert you Never gonna make you cry, never gonna say goodbye Never gonna tell a lie and hurt you Never gonna give you up, never gonna let you down Never gonna run around and desert you Never gonna make you cry, never gonna say goodbye Never gonna tell a lie and hurt you We've known each other for so long Your heart's been aching but you're too shy to say it Inside we both know what's been going on We know the game and we're gonna play it I just want to tell you how I'm feeling Gotta make you understand Never gonna give you up, never gonna let you down Never gonna run around and desert you Never gonna make you cry, never gonna say goodbye Never gonna tell a lie and hurt you";

    [SerializeField]
    float mScrollSpeed;

    RectTransform mRectTrans;
    float mWidth;
    Vector3 mStartPosition;
    float mScrollPosition = 0;
    

    void Start()
    {
        mRectTrans = text.GetComponent<RectTransform>();

        GameUtilities.SetOptionalText(text, lyrics);

        mWidth = text.preferredWidth;

        mStartPosition = mRectTrans.position;

    }

    void Update()
    {
        mRectTrans.position = new Vector3(-mScrollPosition % mWidth, mStartPosition.y, mStartPosition.z);

        mScrollPosition += mScrollSpeed * Time.deltaTime;
    }
}
