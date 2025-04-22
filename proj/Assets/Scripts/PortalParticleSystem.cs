using UnityEngine;

public class PortalParticleSystem : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private float emissionRate = 2f;
    [SerializeField] private float minParticleSpeed = 0.03f;
    [SerializeField] private float maxParticleSpeed = 0.1f;
    [SerializeField] private float minParticleSize = 0.05f;
    [SerializeField] private float maxParticleSize = 0.15f;
    [SerializeField] private float particleLifetime = 4f;
    [SerializeField] private float hoverDistance = 0.8f;
    [SerializeField] private Color particleColor = new Color(0.5f, 0.1f, 0.8f, 0.8f);
    [SerializeField] private Color particleColor2 = new Color(0.7f, 0.3f, 1f, 0.6f);
    
    private ParticleSystem gudparticleSystem;
    private Transform portalTransform;
    
    private Vector3 portalSize;

    public void Initialize(Transform portalTransform, Vector3 size)
    {
        this.portalTransform = portalTransform;
        portalSize = size;
        SetupParticleSystem();
    }

    private void SetupParticleSystem()
    {
        // Create and configure particle system
        gudparticleSystem = gameObject.AddComponent<ParticleSystem>();
        
        // Main module configuration
        var main = gudparticleSystem.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(minParticleSpeed, maxParticleSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);
        main.startColor = new ParticleSystem.MinMaxGradient(particleColor, particleColor2);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        
        // Emission module
        var emission = gudparticleSystem.emission;
        emission.rateOverTime = emissionRate;
        
        // Shape module - rectangle matching portal size
        var shape = gudparticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(portalSize.x * 0.8f, portalSize.y * 0.8f, 0.01f);
        
        // Velocity over lifetime - particles move out then get sucked back in
        var velocityOverLifetime = gudparticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        
        // Create animation curves for the velocity
        AnimationCurve xCurve = new AnimationCurve();
        xCurve.AddKey(0.0f, 0.0f);  // Start with no horizontal velocity
        xCurve.AddKey(0.5f, 0.0f);  // Hover
        xCurve.AddKey(1.0f, -1.5f); // Get sucked back in faster
        
        AnimationCurve yCurve = new AnimationCurve();
        yCurve.AddKey(0.0f, 0.0f);
        yCurve.AddKey(0.5f, 0.0f);
        yCurve.AddKey(1.0f, -1.2f);
        
        AnimationCurve zCurve = new AnimationCurve();
        zCurve.AddKey(0.0f, hoverDistance * 2); // Appear about two blocks away
        zCurve.AddKey(0.5f, hoverDistance);     // Gradually move closer
        zCurve.AddKey(1.0f, -hoverDistance * 2); // Get sucked in faster and disappear
        
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(1.0f, xCurve);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1.0f, yCurve);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(2.0f, zCurve);
        
        // Size over lifetime - start small, grow, then shrink as they get sucked in
        var sizeOverLifetime = gudparticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.2f);
        sizeCurve.AddKey(0.2f, 1.0f);
        sizeCurve.AddKey(0.7f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.0f);
        
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);
        
        // Color over lifetime - fade in and out
        var colorOverLifetime = gudparticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(particleColor, 0.0f),
                new GradientColorKey(particleColor2, 0.5f),
                new GradientColorKey(particleColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.2f),
                new GradientAlphaKey(1.0f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        
        colorOverLifetime.color = colorGradient;
        
        // Rotation over lifetime - make particles spin slowly
        var rotationOverLifetime = gudparticleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        AnimationCurve rotationCurve = new AnimationCurve();
        rotationCurve.AddKey(0.0f, -45f);
        rotationCurve.AddKey(1.0f, 45f);
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(1.0f, rotationCurve);
        
        // Texture Sheet Animation - use for animated particles if needed
        var textureSheetAnimation = gudparticleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.numTilesX = 4;
        textureSheetAnimation.numTilesY = 4;
        textureSheetAnimation.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheetAnimation.frameOverTime = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f));
        
        // Create material for particles
        Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        particleMaterial.SetColor("_Color", particleColor);
        
        // Try to load a portal particle texture if available
        Texture2D particleTexture = Resources.Load<Texture2D>("portal_particle");
        if (particleTexture != null)
        {
            particleMaterial.mainTexture = particleTexture;
        }
        
        // Assign material to particle system renderer
        var renderer = gudparticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
    }
}