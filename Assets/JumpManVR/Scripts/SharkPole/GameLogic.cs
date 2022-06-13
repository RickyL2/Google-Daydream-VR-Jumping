using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public GameObject Spinner;
    private Animator SpinnerAnim;
    private float initialAnimSpeed;

    public GameObject ThePlayer;
    private PlayerMovementV2 playerScript;
    private PlayerBehavior playerBehaviorScript;

    // Start is called before the first frame update
    void Start()
    {
        SpinnerAnim = Spinner.GetComponent<Animator>();
        initialAnimSpeed = SpinnerAnim.speed;

        playerScript = ThePlayer.GetComponent<PlayerMovementV2>();
        playerBehaviorScript = ThePlayer.GetComponent<PlayerBehavior>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!playerScript.PlayerReady())
        {
            SpinnerAnim.speed = 0;
        }

        else
        {
            SpinnerAnim.speed = initialAnimSpeed;
        }
    }
}
