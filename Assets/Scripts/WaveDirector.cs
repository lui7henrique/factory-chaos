using UnityEngine;

/// <summary>
/// One preparation, then one enemy leaves the tunnel. No second wave.
/// </summary>
public class WaveDirector : MonoBehaviour
{
    public const float PrepSeconds = 50f;

    public enum Phase
    {
        Preparing,
        Incoming,
        Cleared,
        Breached
    }

    [SerializeField] float prepSeconds = PrepSeconds;
    [SerializeField] Vector3 spawn = new Vector3(1.6f, 0f, 16.2f);
    [SerializeField] float stopZ = 8f;

    Light tunnelEye;
    WaveEnemy enemy;
    Phase phase = Phase.Preparing;
    float secondsLeft;

    public Phase Current => phase;
    public float SecondsLeft => secondsLeft;
    public WaveEnemy Enemy => enemy;

    public void Configure(Vector3 spawnPoint, float breachZ, Light eye)
    {
        spawn = spawnPoint;
        stopZ = breachZ;
        tunnelEye = eye;
        secondsLeft = prepSeconds;
        SetEye(0.4f);
    }

    void Awake()
    {
        secondsLeft = prepSeconds;
    }

    void Update()
    {
        if (phase == Phase.Preparing)
        {
            secondsLeft = Mathf.Max(0f, secondsLeft - Time.deltaTime);
            if (secondsLeft <= 0f)
                Release();
            return;
        }

        if (phase != Phase.Incoming || enemy == null)
            return;

        if (enemy.IsDead)
        {
            phase = Phase.Cleared;
            SetEye(0.15f);
            return;
        }

        if (enemy.HasArrived)
        {
            phase = Phase.Breached;
            SetEye(1.6f);
        }
    }

    void Release()
    {
        phase = Phase.Incoming;
        secondsLeft = 0f;
        SetEye(1.4f);

        GameObject body = CaveBlockout.CreateEnemy(transform, spawn, stopZ);
        enemy = body.GetComponent<WaveEnemy>();
    }

    void SetEye(float intensity)
    {
        if (tunnelEye == null)
            return;

        tunnelEye.intensity = intensity;
    }
}
