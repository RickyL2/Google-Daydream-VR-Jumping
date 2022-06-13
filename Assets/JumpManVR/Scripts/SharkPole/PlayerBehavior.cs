using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBehavior : MonoBehaviour
{
    private PlayerMovementV2 playerValues;
    public Text HitCard;
    private int score;

    public GameObject theBrain;
    private Animator brainAnim;

    // Start is called before the first frame update
    void Start()
    {
        score = 0;
        HitCard.text = "0";
        playerValues = GetComponent<PlayerMovementV2>();

        brainAnim = theBrain.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Obstacle") && !playerValues.CurrentlyJumping())
        {
            print("got hit");
            score++;
            HitCard.text = "" + score;
            StartCoroutine(GoToNextAnim());
            print("I recieved the hit");
        }
    }

    IEnumerator GoToNextAnim()
    {
        brainAnim.SetBool("MoveDown", true);
        yield return new WaitForSeconds(0.1f);
        brainAnim.SetBool("MoveDown", false);
    }
}
