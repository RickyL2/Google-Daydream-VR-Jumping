using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeButtonColor : MonoBehaviour
{
    private Image buttonImage;
    public Color firstColor;
    public Color secondColor;
    private bool theSwitch;

    // Start is called before the first frame update
    void Start()
    {
        buttonImage = GetComponent<Image>();
        theSwitch = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ColorChanger()
    {
        if(theSwitch)
            buttonImage.color = firstColor;

        else
            buttonImage.color = secondColor;

        theSwitch = !theSwitch;
    }

}
