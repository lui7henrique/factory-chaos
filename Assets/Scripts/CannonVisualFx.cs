using UnityEngine;

/// <summary>
/// Lamp state from round count and a short barrel-only recoil kick.
/// </summary>
public class CannonVisualFx : MonoBehaviour
{
    [SerializeField] Transform barrelVisual;
    [SerializeField] Renderer lampRenderer;

    CannonController cannon;
    Vector3 barrelRestLocal;
    float recoilUntil;
    bool lampOn = true;
    MaterialPropertyBlock block;
    static readonly Color LampOn = new Color(0.349f, 0.937f, 0.380f, 1f);
    static readonly Color LampOff = new Color(0.12f, 0.2f, 0.14f, 1f);

    const float RecoilDistance = 0.07f;
    const float RecoilDuration = 0.09f;

    public void Bind(Transform barrel, Renderer lamp)
    {
        barrelVisual = barrel;
        lampRenderer = lamp;
        if (barrelVisual != null)
            barrelRestLocal = barrelVisual.localPosition;

        cannon = GetComponent<CannonController>();
        ApplyLamp(cannon != null && cannon.Rounds > 0);
    }

    public void PlayRecoil()
    {
        if (barrelVisual == null)
            return;

        barrelVisual.localPosition = barrelRestLocal + new Vector3(0f, 0f, -RecoilDistance);
        recoilUntil = Time.time + RecoilDuration;
    }

    void LateUpdate()
    {
        if (barrelVisual != null && recoilUntil > 0f)
        {
            if (Time.time >= recoilUntil)
            {
                barrelVisual.localPosition = barrelRestLocal;
                recoilUntil = 0f;
            }
            else
            {
                float t = 1f - ((recoilUntil - Time.time) / RecoilDuration);
                barrelVisual.localPosition = Vector3.Lerp(
                    barrelRestLocal + new Vector3(0f, 0f, -RecoilDistance),
                    barrelRestLocal,
                    t);
            }
        }

        if (cannon == null)
            cannon = GetComponent<CannonController>();

        bool hasRounds = cannon != null && cannon.Rounds > 0;
        if (hasRounds != lampOn)
            ApplyLamp(hasRounds);
    }

    void ApplyLamp(bool on)
    {
        lampOn = on;
        if (lampRenderer == null)
            return;

        if (block == null)
            block = new MaterialPropertyBlock();

        lampRenderer.GetPropertyBlock(block);
        Color color = on ? LampOn : LampOff;
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", on ? LampOn * 0.45f : Color.black);
        lampRenderer.SetPropertyBlock(block);
    }
}
