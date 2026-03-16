using System.Collections.Generic;
using UnityEngine;

public class FACS_Visualizer_2D : MonoBehaviour 
{
    public FACS myFACS = null;
    Transform panelRoot = null;

    void Start()
    {
        panelRoot = transform.Find("Transform");
    }   

    void ClearControls()
    {
        foreach (Transform t in panelRoot)
        {
            t.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        ClearControls();
        List<ActionUnit> aus = myFACS.AUList;
        foreach (ActionUnit au in aus)
        {
            if (au.Intensities[au.currInd] > 0)
            {
                string name = $"AU{au.AU:00}";
                //Debug.Log(name);
                Transform auSprite = panelRoot.Find(name);
                if (auSprite != null) auSprite.gameObject.SetActive(true);
            }
        }
    } 
}
