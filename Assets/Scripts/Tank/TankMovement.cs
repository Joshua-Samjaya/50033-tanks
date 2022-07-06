using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TankMovement : MonoBehaviour
{

    public int m_PlayerNumber = 1;         
    public float m_Speed = 12f;            
    public float m_TurnSpeed = 180f;       
    public AudioSource m_MovementAudio;    
    public AudioClip m_EngineIdling;       
    public AudioClip m_EngineDriving;      
    public float m_PitchRange = 0.2f;


    private string m_MovementAxisName;     
    private string m_TurnAxisName;         
    private Rigidbody m_Rigidbody;         
    private float m_MovementInputValue;    
    private float m_TurnInputValue;        
    private float m_OriginalPitch;        

    private bool dashAvail = true; 
    private bool isDashing = false;
    private float cooldownTimer = 300f;

    public Slider m_Slider; 
    public Image m_FillImage;         
    public Color m_DashAvailColor = Color.blue;  
    public Color m_DashUnavailColor = Color.red;  
    
    private int dirDash;
    private AudioSource  m_dashAudio;



    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_dashAudio = this.transform.GetChild(0).gameObject.GetComponent<AudioSource>();
    }


    private void OnEnable ()
    {
        m_Rigidbody.isKinematic = false;
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;

        //slider implementation for skill cooldown
        m_Slider.value = 100f;
        m_FillImage.color = Color.Lerp(m_DashUnavailColor, m_DashAvailColor, 1);
    }


    private void OnDisable ()
    {
        m_Rigidbody.isKinematic = true;
    }


    private void Start()
    {
        m_MovementAxisName = "Vertical" + m_PlayerNumber;
        m_TurnAxisName = "Horizontal" + m_PlayerNumber;

        m_OriginalPitch = m_MovementAudio.pitch;
    }

    private void Update()
    {
        // Store the player's input and make sure the audio for the engine is playing.
        m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
        m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        EngineAudio ();
    }


    private void EngineAudio()
    {
        // Play the correct audio clip based on whether or not the tank is moving and what audio is currently playing.
        bool isIdle = Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f;
        AudioClip currentClip = isIdle ? m_EngineIdling : m_EngineDriving;

        if (m_MovementAudio.clip != currentClip)
        {
            m_MovementAudio.clip = currentClip;
            m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
            m_MovementAudio.Play();
        }
    }


    private void FixedUpdate()
    {
        // Move and turn the tank.
        Move();
        Turn();
        Dash();
    }

    //dash mechanic
    private void Dash(){
        //check button input and assign dash direction accordingly, but only when it is off cooldown
        if (Input.GetButton("DashForward") && dashAvail){
            dirDash = 1;
            StartCoroutine(Dashing());
            StartCoroutine(Cooldown());
        } 

         if (Input.GetButton("DashBackward") && dashAvail){
            dirDash = -1;
            StartCoroutine(Dashing());
            StartCoroutine(Cooldown());
        } 
    }

    //play the audio, set dash on cooldown, and move the tank rapidly across the map
    IEnumerator Dashing(){
        m_dashAudio.Play();
        dashAvail = false;
        isDashing = true;
        m_Slider.value = 0f;
        m_FillImage.color = Color.Lerp(m_DashUnavailColor, m_DashAvailColor, 0);
        for (int i = 0; i <= 12; i++){
            Vector3 movement = transform.forward * dirDash * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement*4); 
            yield return null;
        }
        isDashing = false;
    }

    //slowly fill up the dash to visualise cooldown
    IEnumerator Cooldown()
    {   
        
        for (float i = 0; i <= cooldownTimer;){
            while(Time.timeScale==0){
                yield return null;
            }
            m_Slider.value += 100f/cooldownTimer;
            m_FillImage.color = Color.Lerp(m_DashUnavailColor, m_DashAvailColor,i/cooldownTimer); 
            i++;
            yield return null;
        } 
        dashAvail = true;
    }


    private void Move()
    {
        // Adjust the position of the tank based on the player's input.
        if (!isDashing){
            Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }
    }


    private void Turn()
    {
        // Adjust the rotation of the tank based on the player's input.
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }
}