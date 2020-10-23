using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class PageOneWidget : MonoBehaviour
{
    //------------------------------------------------------------------------------------
    // Data
    //------------------------------------------------------------------------------------
    [Header("Details")]
    public TextMeshProUGUI rightTitleText;
    public TextMeshProUGUI leftTitleText;
    public GameObject rightImageContainer;
    public GameObject leftImageContainer;
    public RawImage rightRawImage;
    public RawImage leftRawImage;

    private string mURL;
    bool mIsEven;

    //------------------------------------------------------------------------------------
    // Functions
    //------------------------------------------------------------------------------------

    void Awake()
    {
        
    }

    //------------------------------------------------------------------------------------
    public void Setup(int inID)
    {
        bool is_even = inID % 2 == 0;
        mIsEven = is_even;
        GameUtilities.SetActive(rightImageContainer, is_even);
        GameUtilities.SetActive(leftImageContainer, !is_even);
        if (Client.Instance.Responses.Count == 0 && Client.Instance.TextureCache.Count == 0)
        {
            mURL = $"{PageOne.ENDPOINT_URL}{inID}";
            RequestHeader contentTypeHeader = new RequestHeader
            {
                Key = "Content-Type",
                Value = "application/json"
            };
            
            StartCoroutine(Client.Instance.HttpGet(mURL, (r) => StartCoroutine(OnRequestComplete(r))));
        }
        else
        {
            if (mIsEven)
            {
                GameUtilities.SetOptionalText(rightTitleText, Client.Instance.Responses[inID - 1].title);
                rightRawImage.texture = Client.Instance.TextureCache[inID - 1];
                rightRawImage.texture.filterMode = FilterMode.Point;
                Client.Instance.TextureCache.Add(rightRawImage.texture);
            }
            else
            {
                GameUtilities.SetOptionalText(leftTitleText, Client.Instance.Responses[inID - 1].title);
                leftRawImage.texture = Client.Instance.TextureCache[inID - 1];
                leftRawImage.texture.filterMode = FilterMode.Point;
                Client.Instance.TextureCache.Add(leftRawImage.texture);
            }
        }
        
    }

    //------------------------------------------------------------------------------------
    IEnumerator OnRequestComplete(Response result)
    {
        Debug.Log($"Status Code: {result.StatusCode}");
        Debug.Log($"Data: {result.Data}");
        Debug.Log($"Error: {result.Error}");

        if (string.IsNullOrEmpty(result.Error) && !string.IsNullOrEmpty(result.Data))
        {
            PhotoResponse photo_response = JsonUtility.FromJson<PhotoResponse>(result.Data);
            Client.Instance.Responses.Add(photo_response);

            UnityWebRequest request = UnityWebRequestTexture.GetTexture(photo_response.url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(request.error);
                yield break;
            }

            if (mIsEven)
            {
                GameUtilities.SetOptionalText(rightTitleText, photo_response.title);
                rightRawImage.texture = DownloadHandlerTexture.GetContent(request);
                rightRawImage.texture.filterMode = FilterMode.Point;
                Client.Instance.TextureCache.Add(rightRawImage.texture);
            }
            else
            {
                GameUtilities.SetOptionalText(leftTitleText, photo_response.title);
                leftRawImage.texture = DownloadHandlerTexture.GetContent(request);
                leftRawImage.texture.filterMode = FilterMode.Point;
                Client.Instance.TextureCache.Add(leftRawImage.texture);
            }
        }
    }

    void Update()
    {

    }
}
