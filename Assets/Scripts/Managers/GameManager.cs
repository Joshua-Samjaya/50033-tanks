using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 5;            
    public float m_StartDelay = 3f;             
    public float m_EndDelay = 3f;               
    public CameraControl m_CameraControl;       
    public Text m_MessageText;         
    public Text m_Timer;         
    public GameObject[] m_TankPrefabs;
    public TankManager[] m_Tanks;               
    public List<Transform> wayPointsForAI;

    private int m_RoundNumber;                  
    private WaitForSeconds m_StartWait;         
    private WaitForSeconds m_EndWait;           
    private TankManager m_RoundWinner;          
    private TankManager m_GameWinner;      

    public bool isTimeout = false;
    public float countdownTime=60f;

    public GameObject pauseMenuUI;
    public AudioMixer mixer; 

    private void Start()
    {   
        pauseMenuUI.SetActive(false);
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);

        SpawnAllTanks();
        SetCameraTargets();

        StartCoroutine(GameLoop());
    }


    private void SpawnAllTanks()
    {
        m_Tanks[0].m_Instance =
            Instantiate(m_TankPrefabs[0], m_Tanks[0].m_SpawnPoint.position, m_Tanks[0].m_SpawnPoint.rotation) as GameObject;
        m_Tanks[0].m_PlayerNumber = 1;
        m_Tanks[0].SetupPlayerTank();

        for (int i = 1; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].m_Instance =
                Instantiate(m_TankPrefabs[i], m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].SetupAI(wayPointsForAI);
        }
    }


    private void SetCameraTargets()
    {
        Transform[] targets = new Transform[m_Tanks.Length];

        for (int i = 0; i < targets.Length; i++)
            targets[i] = m_Tanks[i].m_Instance.transform;

        m_CameraControl.m_Targets = targets;
    }


    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting());
        yield return StartCoroutine(RoundPlaying());
        yield return StartCoroutine(RoundEnding());

        if (m_GameWinner != null) SceneManager.LoadScene(0);
        else StartCoroutine(GameLoop());
    }


    private IEnumerator RoundStarting()
    {
        ResetAllTanks();
        DisableTankControl();

        m_CameraControl.SetStartPositionAndSize();

        m_RoundNumber++;
        m_MessageText.text = $"ROUND {m_RoundNumber}";
        m_Timer.text = "";
        countdownTime = 60f;
        isTimeout = false;
        yield return m_StartWait;
    }


    private IEnumerator RoundPlaying()
    {
        EnableTankControl();
        StartCoroutine(RoundTimer());
        m_MessageText.text = string.Empty;
        Debug.Log(!OneTankLeft());

        while (!OneTankLeft() && !isTimeout) yield return null;
    }

    // Reduce countdown time and display the text on the screen
    private IEnumerator RoundTimer(){
        while (countdownTime > 0)
        {
            countdownTime -= Time.deltaTime;
            m_Timer.text = string.Format ("Time left: {0}s", countdownTime.ToString("0.00"));
            yield return null;
        }
        isTimeout = true;
    }

    // checks for tank with highest health
    // randomise in the case of equal highest health
    private int HighestHealth()
    {   
        int maxHealthID=0;
        float maxHealth=0;
        for (int i = 0; i < m_Tanks.Length; i++)
        {   
            float tankHealth = m_Tanks[i].m_Instance.gameObject.GetComponent<TankHealth>().m_CurrentHealth;
            if (tankHealth==maxHealth){
                int[] maxArray = new int[] { maxHealthID, i };
                maxHealthID = maxArray[Random.Range(0,2)];
            } else if (tankHealth>maxHealth){
                maxHealthID = i;
                maxHealth = tankHealth;
            }
        }
        return maxHealthID;
    }


    private IEnumerator RoundEnding()
    {
        DisableTankControl();
        m_RoundWinner = null;

        m_RoundWinner = GetRoundWinner();
        if (m_RoundWinner != null) m_RoundWinner.m_Wins++;

        m_GameWinner = GetGameWinner();

        string message = EndMessage();
        m_MessageText.text = message;

        yield return m_EndWait;
    }


    private bool OneTankLeft()
    {
        int numTanksLeft = 0;

        for (int i = 0; i < m_Tanks.Length; i++)
        {   
            if (m_Tanks[i].m_Instance.activeSelf) numTanksLeft++;
        }

        return numTanksLeft <= 1;
    }

    private TankManager GetRoundWinner()
    {   
        if (isTimeout){
            return m_Tanks[HighestHealth()];
        }
        countdownTime=0;
        m_Timer.text="";
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                return m_Tanks[i];
        }

        return null;
    }

    private TankManager GetGameWinner()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }

        return null;
    }


    private string EndMessage()
    {
        var sb = new StringBuilder();

        //add text to indicate victory by timeout
        if (isTimeout){
            isTimeout = false;
            sb.Append("TIMEOUT!\n\n");
        }

        if (m_RoundWinner != null) sb.Append($"{m_RoundWinner.m_ColoredPlayerText} WINS THE ROUND!");
        else sb.Append("DRAW!");

        sb.Append("\n\n\n\n");

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            sb.AppendLine($"{m_Tanks[i].m_ColoredPlayerText}: {m_Tanks[i].m_Wins} WINS");
        }

        if (m_GameWinner != null)
            sb.Append($"{m_GameWinner.m_ColoredPlayerText} WINS THE GAME!");

        return sb.ToString();
    }


    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].Reset();
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].EnableControl();
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].DisableControl();
    }

    private void Update()
    {   // implementation of pause
        if (Input.GetButtonUp("Pause")){
            // if game is playing, pause
            if (Time.timeScale ==1){
                pauseMenuUI.SetActive(true);
                m_MessageText.text = "Paused";
                mixer.SetFloat("volume", -80.0f);
                Time.timeScale = 0;
            // else if game is paused, resume
            } else{
                pauseMenuUI.SetActive(false);
                m_MessageText.text = "";
                mixer.SetFloat("volume", 0f);
                Time.timeScale = 1;
            }
        }
    }
}