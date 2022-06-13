using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AccelData
{
    public float Acceleration;
    public float Time;

    public AccelData()
    {
        Acceleration = 0;
        Time = 0;
    }
}

public class PlayerMovementV2 : MonoBehaviour
{
    public Text AccelDisplay;
    public Text GravityDisplay;
    public Text HeightDisplay;
    public Text ForceDisplay;

    public int decimalPlaces;

    private GvrControllerInputDevice theController;
    private int GAxis;
    private float GScaler;
    public bool GravityReady;   //only public so we can manually start the game

    //for jumping
    private AccelData[] PrevAccelsAndTime;
    public int QuantityOfMaxRecords;
    private int QuantityOfSavedRecords;
    public float AccelRange;
    private bool Jumping;
    private Rigidbody rb;
    public int HowFarBackToCheckForStationary;
    public int HowFarBackToCheckForChangeInDirection;
    private bool PlayerStill;
    private Vector3 InitialPosition;

    //for graph
    public float maxXrange;
    public float yRangeMultiplyer;
    public GameObject theGraph;
    public GameObject theGraphMarker;
    private List<GameObject> markers;
    private float spaceBetweenMarkers;

    public bool DevMode;


    void Start()
    {
        theController = GvrControllerInput.GetDevice(GvrControllerHand.Dominant);
        PrevAccelsAndTime = new AccelData[QuantityOfMaxRecords];
        QuantityOfSavedRecords = 0;
        GAxis = 0;
        GScaler = 1;
        GravityReady = false;        

        markers = new List<GameObject>();
        spaceBetweenMarkers = maxXrange / QuantityOfMaxRecords;

        Jumping = false;
        PlayerStill = true;
        InitialPosition = transform.position;
        //timeSinceLastStill = 0;
        rb = GetComponent<Rigidbody>();

        if (!DevMode)
        {
            AccelDisplay.transform.gameObject.SetActive(false);
            GravityDisplay.transform.gameObject.SetActive(false);
            HeightDisplay.transform.gameObject.SetActive(false);
            ForceDisplay.transform.gameObject.SetActive(false);
            theGraph.transform.gameObject.SetActive(false);
        }

        else
        {
            InitializeGraph();
        }
    }

    private void Update()
    {
        if(theController.GetButtonDown(GvrControllerButton.App))// ||
           //theController.GetButtonDown(GvrControllerButton.TouchPadButton))
        {
            InitializeController();
            StartCoroutine(GetAverageStandingAcceleration());
        }

        if(GravityReady)
        {
            UpdateGData();
            UpdateWhenPlayerStill();

            //will take care of the possibility of giving the user a velocity that is too much and make sure the person
            //starts falling at the right time, just a back up plan
            Falling();

            //if (!Jumping && !PlayerStill && PrevAccelsAndTime[QuantityOfSavedRecords - 1].Acceleration > 0)
            if(PrevAccelsAndTime[QuantityOfSavedRecords - 1].Acceleration > 0)
            {
                if(DevMode)
                    print("accel > 0");

                JumpController();
            }

            //displays usefull info
            if(DevMode)
            {
                AccelDisplay.text = "Current G: " + PrevAccelsAndTime[QuantityOfSavedRecords - 1].Acceleration + "\n" +
                                    "time lstf: " + PrevAccelsAndTime[QuantityOfSavedRecords - 1].Time + "\n" +
                                    "GScaler: " + GScaler;

                GravityDisplay.text = "quanity of saved records: " + QuantityOfSavedRecords;

                HeightDisplay.text = "height: " + transform.position.y;

                DrawGraph();
            }
        }
    }

    private void UpdateWhenPlayerStill()
    {
        PlayerStill = true;

        for(int i = QuantityOfSavedRecords - 1; PlayerStill && i >= 0 && i > QuantityOfSavedRecords - 1 - HowFarBackToCheckForStationary; i--)
        {
            if(Mathf.Abs(PrevAccelsAndTime[QuantityOfSavedRecords-1].Acceleration - Physics.gravity.y) > AccelRange)
            {
                PlayerStill = false;
            }
        }

        if (PlayerStill)
        {
            //timeSinceLastStill = Time.time;
            Jumping = false;
            PlayerStill = true;
        }
    }

    //will reset things responsible for jumping
    void OnCollisionEnter(Collision thecollision)
    {
        if (thecollision.gameObject.tag == "Enviornment" && Jumping)
        {
            Jumping = false;
            PlayerStill = true;
        }
    }

    private void Falling()
    {
        if(Jumping && QuantityOfSavedRecords > HowFarBackToCheckForChangeInDirection)
        {
            bool playerFalling;
            bool userFalling;

            playerFalling = true;
            userFalling = true;

            for(int i = QuantityOfSavedRecords-2;
                playerFalling && i > QuantityOfSavedRecords - 1 - HowFarBackToCheckForChangeInDirection; i--)
            {
                if(PrevAccelsAndTime[i].Acceleration < PrevAccelsAndTime[i+1].Acceleration)
                {
                    userFalling = false;
                }
            }

            if(rb.velocity.y > 0)
            {
                playerFalling = false;
            }

            if(userFalling && !playerFalling)
            {
                rb.velocity = new Vector3(0,0,0);
            }
        }
    }

    private void JumpController()
    {
        float totalIntegral;
        bool goingUp;               //if player moved up during the current portion of records
        AccelData MinAccel;         //will not be entirely used like a regular accel data,
                                    //will instead be used to keep track of values for integral

        totalIntegral = 0;
        goingUp = false;
        MinAccel = new AccelData
        {
            Acceleration = PrevAccelsAndTime[QuantityOfSavedRecords - 1].Acceleration,
            Time = PrevAccelsAndTime[QuantityOfSavedRecords - 1].Time
        };

        //determines the minimum accel reached when starting jump and determines the basic integral
        //in just one loop, skips the very first one
        for (int i = QuantityOfSavedRecords - 2; !goingUp && i > 0; i--)
        {
            //will check if there are enough records ahead to check for a future change in direction
            if(i > HowFarBackToCheckForChangeInDirection)
            {
                goingUp = true;
                //will check if there was ever a change in direction of accel, just in case the player was
                //wobbling around
                for(int r = i; goingUp && r > i - HowFarBackToCheckForChangeInDirection + 1; r--)
                {
                    if(PrevAccelsAndTime[r].Acceleration > PrevAccelsAndTime[r-1].Acceleration)
                    {
                        goingUp = false;
                    }
                }
            }

            float tempIntegral;

            tempIntegral = 0;

            //adds portion of integral to total
            tempIntegral = PrevAccelsAndTime[i].Acceleration * PrevAccelsAndTime[i].Time;
            totalIntegral += tempIntegral;
            //removes extra bit from portion
            totalIntegral -= 0.5f * PrevAccelsAndTime[i].Time * Mathf.Abs(PrevAccelsAndTime[i].Acceleration - PrevAccelsAndTime[i-1].Acceleration);

            //keeps track of stuff to finish integral later
            if (MinAccel.Acceleration > PrevAccelsAndTime[i].Acceleration)
            {
                MinAccel.Acceleration = PrevAccelsAndTime[i].Acceleration;
            }
            MinAccel.Time += PrevAccelsAndTime[i].Time;
        }

        totalIntegral -= MinAccel.Acceleration * MinAccel.Time;

        if(totalIntegral > 0)
        {
            //totalIntegral is the velocity of the player going up so it is time to apply
            if(!Jumping)
            {
                Jumping = true;
                rb.velocity = new Vector3(0, totalIntegral, 0);
            }

            //if player was already jumping and jumped again like on their toes or something, will do a portion of a jump
            else
            {
                float miniVelocity = 2*(0.5f*Mathf.Pow(totalIntegral,2)+Physics.gravity.y*(transform.position.y-InitialPosition.y));
                
                //if new jump > lastjump, will apply new jump speed but without the speed used to get them to current height
                if(miniVelocity > 0)
                {
                    miniVelocity = Mathf.Pow(miniVelocity,0.5f);
                    rb.velocity = new Vector3(0, miniVelocity, 0);
                }

                //if new jump < lastjump, will just slow decent down
                else
                {
                    rb.velocity = new Vector3(0, rb.velocity.y + totalIntegral, 0);
                }
            }

            
            if (DevMode)
            {
                ForceDisplay.text = "speed: " + totalIntegral;
            }
        }
    }

    //Will determine which accelarometer axis to rely on for gravity data based on the orientation of the controller
    private void InitializeController()
    {
        float[] AccelValues = {theController.Accel.x, theController.Accel.y, theController.Accel.z};
        GAxis = 0;
        QuantityOfSavedRecords = 0;

        //determine which axis is affected the most by gravity
        for (int i = 1; i < 3; i++)
        {
            if (Mathf.Abs(AccelValues[GAxis]) < Mathf.Abs(AccelValues[i]))
                GAxis = i;
        }
    }

    //will get the average acceleraton for when user is standing
    IEnumerator GetAverageStandingAcceleration()
    {
        float DurationForIdleAccel;
        int QuantityOfRecords;
        float AverageAccel;

        GScaler = 1;
        AverageAccel = 0;
        DurationForIdleAccel = 2;
        QuantityOfRecords = 100;
        GravityReady = false;

        for(int i = 0; i < QuantityOfRecords; i++)
        {
            AverageAccel += GetGValue();
            yield return new WaitForSeconds(DurationForIdleAccel/QuantityOfRecords);
        }

        AverageAccel = AverageAccel/100;
        GScaler =  Physics.gravity.y/AverageAccel;

        GravityReady = true;
    }

    private void UpdateGData()
    {
        if(QuantityOfSavedRecords == QuantityOfMaxRecords)
        {
            for(int i = 0; i < QuantityOfMaxRecords - 1; i++)
            {
                PrevAccelsAndTime[i].Acceleration = PrevAccelsAndTime[i+1].Acceleration;
                PrevAccelsAndTime[i].Time = PrevAccelsAndTime[i+1].Time;
            }

            QuantityOfSavedRecords--;
        }

        else
            PrevAccelsAndTime[QuantityOfSavedRecords] = new AccelData();

        //records current accel and time since last recording
        PrevAccelsAndTime[QuantityOfSavedRecords].Acceleration = GetGValue();
        PrevAccelsAndTime[QuantityOfSavedRecords].Time = Time.deltaTime;
        QuantityOfSavedRecords++;
    }

    //gets the value of gravity from the controller
    private float GetGValue()
    {
        float theGValue;

        //uses the correct axis to get gravity data
        switch (GAxis)
        {
            case 0:
                theGValue = theController.Accel.x;
                break;
            case 1:
                theGValue = theController.Accel.y;
                break;
            case 2:
                theGValue = theController.Accel.z;
                break;
            default:
                theGValue = 0;
                break;
        }

        //makes acceleration consistent with ingame gravity
        theGValue *= GScaler;

        theGValue = RoundNumber(theGValue);

        return theGValue;
    }

    private float RoundNumber(float theNumber)
    {
        theNumber *= Mathf.Pow(10.0f, decimalPlaces);
        theNumber = Mathf.Round(theNumber) / Mathf.Pow(10.0f, decimalPlaces);

        return theNumber;
    }

    private void InitializeGraph()
    {
        //will ensure that the correct amount of markers are present
        for(int i = 0; i < QuantityOfMaxRecords; i++)
        {
            GameObject temp = Instantiate(theGraphMarker, theGraph.transform.position, theGraph.transform.rotation);
            temp.transform.SetParent(theGraph.transform);
            temp.transform.localPosition = new Vector3(i * spaceBetweenMarkers, Physics.gravity.y * yRangeMultiplyer, 0);
            markers.Add(temp);
        }
    }

    private void DrawGraph()
    {
        Vector3 latestPosition;

        //moves all data in graph one back
        for(int i = 0; i < QuantityOfMaxRecords - 1; i++)
        {
            markers[i].transform.localPosition = new Vector3(markers[i].transform.localPosition.x,
                                                             markers[i+1].transform.localPosition.y,
                                                             markers[i].transform.localPosition.z);
        }

        latestPosition = new Vector3(markers[QuantityOfMaxRecords-1].transform.localPosition.x,
                                     PrevAccelsAndTime[QuantityOfSavedRecords-1].Acceleration * yRangeMultiplyer,
                                     markers[QuantityOfMaxRecords-1].transform.localPosition.z);

        markers[QuantityOfMaxRecords-1].transform.localPosition = latestPosition;
    }

    public bool CurrentlyJumping()
    {
        return Jumping;
    }

    public bool PlayerReady()
    {
        return GravityReady;
    }
}
