using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using System.Globalization;

public class NetworkManager : MonoBehaviour
{
    [SerializeField] public string ip_eduroamDesktop;
    [SerializeField] public string ip_LAN_Desktop;
    [SerializeField] public string ip_LAN_Laptop;
    [SerializeField] public string ip_HomeWifi_Laptop;
    [SerializeField] public string port;

    public string ip;//
    private string[] ipList = new string[4];

    public string serverEndpoint;

    private ProjectionManager projectionManager;
    private CameraManager cameraManager;
    public GameObject UILoading;

    private void Awake()
    {
        ipList[0] = ip_eduroamDesktop;
        ipList[1] = ip_LAN_Desktop;
        ipList[2] = ip_LAN_Laptop;
        ipList[3] = ip_HomeWifi_Laptop;

        projectionManager = GameObject.Find("MixedRealityPlayspace/Projection Manager").GetComponent<ProjectionManager>();
        cameraManager = GameObject.Find("MixedRealityPlayspace/Camera Manager").GetComponent<CameraManager>();
    }

    private void Start()
    {
        StartCoroutine(Connect(0));
        //StartCoroutine(Connect(1));
        //StartCoroutine(Connect(2));
        //StartCoroutine(Connect(3));
    }

    public IEnumerator Connect(int ip_attempts)
    {
        string ip_connect = ipList[ip_attempts];
        Debug.Log($"Trying to connect to {ip_connect}");
        string serverEndpoint_connect = "http://" + ip_connect + ":" + port;

        WWWForm webform = new WWWForm();
        using (UnityWebRequest uwr = UnityWebRequest.Get(serverEndpoint_connect))
        {
            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
            else
            {
                Debug.Log("Received: " + uwr.downloadHandler.text);
                Debug.Log($"Connected to server. IP = {ip_connect}");
                ip = ip_connect;
                serverEndpoint = serverEndpoint_connect;
            }
        }
    }

    public IEnumerator SendImage(byte[] imageBytes)
    {
        serverEndpoint = "http://" + ip + ":" + port;

        WWWForm webform = new WWWForm();
        using (UnityWebRequest uwr = UnityWebRequest.Post(serverEndpoint, webform))
        {

            uwr.SetRequestHeader("Content-Type", "application/octet-stream");

            //I think the request-type might have to match the funtion name including caps
            uwr.SetRequestHeader("Request-Type", "SendImage");
            uwr.timeout = 10;

            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.uploadHandler = new UploadHandlerRaw(imageBytes);
            uwr.uploadHandler.contentType = "application/octet-stream";
            Debug.Log("Sending image to server.");
            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
            else
            {
                Debug.Log("Received:" + uwr.downloadHandler.text);

                List<decimal[]> data = unpackMatrix(uwr.downloadHandler.text);

                projectionManager.projectPoints(data);
                UILoading.transform.Find("Canvas").gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator CacheImage(byte[] imageBytes)
    {
        serverEndpoint = "http://" + ip + ":" + port;

        WWWForm webform = new WWWForm();
        using (UnityWebRequest uwr = UnityWebRequest.Post(serverEndpoint, webform))
        {

            uwr.SetRequestHeader("Content-Type", "application/octet-stream");

            //I think the request-type might have to match the funtion name including caps
            uwr.SetRequestHeader("Request-Type", "CacheImage");
            uwr.timeout = 10;

            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.uploadHandler = new UploadHandlerRaw(imageBytes);
            uwr.uploadHandler.contentType = "application/octet-stream";
            Debug.Log("Sending image to server.");
            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
            else
            {
                UILoading.transform.Find("Canvas").gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator FindWaterBottle()
    {
        serverEndpoint = "http://" + ip + ":" + port;

        WWWForm webform = new WWWForm();
        using (UnityWebRequest uwr = UnityWebRequest.Post(serverEndpoint, webform))
        {
            uwr.SetRequestHeader("Request-Type", "FindWaterBottle");

            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
            else
            {
                Debug.Log("Received:" + uwr.downloadHandler.text);
                List<decimal[]> data = unpackMatrix(uwr.downloadHandler.text);
                projectionManager.projectPoints(data);
                UILoading.transform.Find("Canvas").gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator FindBolt()
    {
        serverEndpoint = "http://" + ip + ":" + port;

        WWWForm webform = new WWWForm();
        using (UnityWebRequest uwr = UnityWebRequest.Post(serverEndpoint, webform))
        {
            uwr.SetRequestHeader("Request-Type", "FindBolt");

            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
            else
            {
                Debug.Log("Received:" + uwr.downloadHandler.text);
                List<decimal[]> data = unpackMatrix(uwr.downloadHandler.text);
                projectionManager.projectPoints(data);
                UILoading.transform.Find("Canvas").gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator FindCalculator()
    {
        serverEndpoint = "http://" + ip + ":" + port;

        WWWForm webform = new WWWForm();
        using (UnityWebRequest uwr = UnityWebRequest.Post(serverEndpoint, webform))
        {
            uwr.SetRequestHeader("Request-Type", "FindCalculator");

            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
            else
            {
                Debug.Log("Received:" + uwr.downloadHandler.text);
                List<decimal[]> data = unpackMatrix(uwr.downloadHandler.text);
                projectionManager.projectPoints(data);
                UILoading.transform.Find("Canvas").gameObject.SetActive(false);
            }
        }
    }
    List<decimal[]> unpackMatrix(string data_text)
    {
        int level = 0;
        string tempString = "";
        List<decimal[]> res = new List<decimal[]>();
        foreach (char c in data_text)
        {
            if (c == '[')
            {
                level++;
            }
            else if (c == ']')
            {
                level--;
                if (level == 1) //if after going down a level we end up at level 2, we need to reset the temp string and parse it's contents
                {
                    tempString = tempString.Substring(1); //remove the first [
                    string[] contents = tempString.Split(',');
                    decimal[] arr = new decimal[contents.Length];  // dynamically allocate array based on the number of elements
                    for (int i = 0; i < contents.Length; i++)
                    {
                        arr[i] = decimal.Parse(contents[i], CultureInfo.InvariantCulture);
                    }
                    //Add this result to the running list
                    res.Add(arr);
                    tempString = "";
                }
            }
            if (level == 2)
            {
                tempString += c;
            }
        }
        return res;
    }
}
