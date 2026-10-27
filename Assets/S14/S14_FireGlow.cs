using UnityEngine;

// 숯의 발광과 화로 조명이 같은 리듬으로 은은하게 변한다.
public class S14_FireGlow : MonoBehaviour
{
    [SerializeField] private Renderer[] glowingCoals;
    [SerializeField] private Light fireLight;
    [SerializeField] private Color emissionTint = new Color(1f, 0.36f, 0.025f);
    [SerializeField] private float baseEmission = 4.2f;
    [SerializeField] private float baseLightIntensity = 8f;

    private MaterialPropertyBlock propertyBlock;

    public void Configure(Renderer[] coals, Light light)
    {
        glowingCoals = coals;
        fireLight = light;
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        float flicker = Mathf.Lerp(0.72f, 1.16f,
            Mathf.PerlinNoise(Time.time * 3.2f, 0.37f));

        if (fireLight != null)
            fireLight.intensity = baseLightIntensity * flicker;

        if (glowingCoals == null)
            return;

        foreach (Renderer coal in glowingCoals)
        {
            if (coal == null)
                continue;

            coal.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", emissionTint * (baseEmission * flicker));
            coal.SetPropertyBlock(propertyBlock);
        }
    }
}
