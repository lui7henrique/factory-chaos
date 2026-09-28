using UnityEngine;

/// <summary>
/// Soft flame motion while the furnace is processing. It only moves existing shapes.
/// </summary>
public class FurnaceFire : MonoBehaviour
{
    [SerializeField] OreMachine machine;

    Transform[] flames;
    Vector3[] restPosition;
    Vector3[] restScale;

    public void Bind(OreMachine source)
    {
        machine = source;
        Cache();
    }

    void Awake()
    {
        Cache();
    }

    void Cache()
    {
        int count = transform.childCount;
        flames = new Transform[count];
        restPosition = new Vector3[count];
        restScale = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            flames[i] = transform.GetChild(i);
            restPosition[i] = flames[i].localPosition;
            restScale[i] = flames[i].localScale;
        }
    }

    void Update()
    {
        if (flames == null)
            return;

        bool hot = machine != null && machine.IsProcessing;
        for (int i = 0; i < flames.Length; i++)
        {
            if (flames[i] == null)
                continue;

            float wave = hot ? Mathf.Sin(Time.time * 5.5f + i * 1.7f) : 0f;
            float scale = hot ? 1f + wave * 0.14f : 1f;
            flames[i].localScale = restScale[i] * scale;
            flames[i].localPosition = restPosition[i] + new Vector3(0f, hot ? wave * 0.025f : 0f, 0f);
        }
    }
}
