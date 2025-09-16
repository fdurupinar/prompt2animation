using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;

public class LogNetClient : MonoBehaviour
{
    public string url = "https://www.trek.alinenormoyle.com/";
    public bool runLocally = true;
    public bool verbose = true;
    public string localUrl = "http://127.0.0.1:5000/";
    private int _pid = 0;
    private delegate void ResponseCb(String text);
    private bool _ready = true;

    // Start is called before the first frame update
    void Start()
    {
        if (verbose) Debug.Log("Net Client connected to: "+RootUrl());
    }

    private string RootUrl()
    {
        return runLocally? localUrl : url;
    }

    private string Authenticate()
    {
        TextAsset obscure = Resources.Load("password") as TextAsset; 
        string auth = "Bearer " + obscure.text.Trim();
        return auth;
    }

    public void TestConnection()
    {
        string apiUrl = RootUrl() + "hello";
        StartCoroutine(WaitForRequest(apiUrl));
    }

    public void NewParticipant()
    {
        string apiUrl = RootUrl() + "new";
        StartCoroutine(WaitForRequest(apiUrl, SetPid));
    }

    public void SetPid(String response)
    {
        Participant participant;
        participant = JsonUtility.FromJson<Participant>(response);
        _pid = participant.id;
        if (verbose) Debug.Log("Setting partcipant id: "+_pid);
    }

    public void PostLog(String log)
    {
        if (verbose) Debug.Log(log);
        StartCoroutine(Log(log));
    }

    IEnumerator WaitForRequest(string fullUrl, ResponseCb cb = null)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            string authorization = Authenticate();
            www.SetRequestHeader("AUTHORIZATION", authorization);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return WaitForRequest(www, cb);
        }
    }

    IEnumerator Log(String log)
    {
        // Wait if we have already sent a message and haven't received a
        // response yet
        while (!_ready)
        {
            yield return new WaitForSeconds(0.3f);
        }

        // convert json string to byte
        LogMessage comment = new LogMessage();
        comment.pid = _pid;
        comment.time = Time.realtimeSinceStartup;
        comment.logLine = log;

        string apiUrl = RootUrl() + "log";

        // PUT needed to send raw JSON?!?
        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(comment));
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.disposeDownloadHandlerOnDispose = true;
            www.disposeUploadHandlerOnDispose = true;
            www.disposeCertificateHandlerOnDispose = true;

            string authorization = Authenticate(); 
            www.SetRequestHeader("AUTHORIZATION", authorization);
            www.SetRequestHeader("Content-Type", "application/json");
            yield return WaitForRequest(www);
        }
    }

    IEnumerator WaitForRequest(UnityWebRequest www, ResponseCb cb = null)
    {
        _ready = false;
        if (verbose) Debug.Log("apiUrl "+www.url);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("ERROR: " + www.error);
        }
        else
        {
            if (verbose) Debug.Log(www.responseCode + ": " + www.downloadHandler.text);
            if (cb != null) cb(www.downloadHandler.text);
        }
        _ready = true;
    }
}
