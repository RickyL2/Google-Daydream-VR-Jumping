using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Spinner : MonoBehaviour
{
    private PlayerMovementV2 playerValues;
    public Text ScoreCard;
    private int score;

    // Start is called before the first frame update
    void Start()
    {
        score = 0;
        ScoreCard.text = "0";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void IncrementScore()
    {
        score++;
        ScoreCard.text = "" + score;
    }
}
