using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LogNetClient))]
public class TestNetClient : MonoBehaviour
{
    private bool first = true;

    void Update()
    {
        if (first)
        {
            first = false;
            LogNetClient client = GetComponent<LogNetClient>();
            client.TestConnection();
            client.NewParticipant();
            client.PostLog("UnityTest1");
        }
        
    }
}
