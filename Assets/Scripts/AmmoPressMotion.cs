using UnityEngine;

/// <summary>
/// Moves the visual press head while the ammo machine is processing.
/// It does not touch physical items.
/// </summary>
public class AmmoPressMotion : MonoBehaviour
{
    [SerializeField] AmmoMachine machine;
    [SerializeField] float raisedY = 1.58f;
    [SerializeField] float loweredY = 1.18f;
    [SerializeField] float speed = 0.9f;

    public void Bind(AmmoMachine source, float raised, float lowered)
    {
        machine = source;
        raisedY = raised;
        loweredY = lowered;
        Vector3 position = transform.localPosition;
        position.y = raisedY;
        transform.localPosition = position;
    }

    void Update()
    {
        float target = machine != null && machine.IsPressing ? loweredY : raisedY;
        Vector3 position = transform.localPosition;
        position.y = Mathf.MoveTowards(position.y, target, speed * Time.deltaTime);
        transform.localPosition = position;
    }
}
