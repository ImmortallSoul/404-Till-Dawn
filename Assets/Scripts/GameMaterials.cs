using UnityEngine;

public static class GameMaterials
{
    public static Material Create(Color color, Color? emission = null)
    {
        return Create(color, emission, null, Vector2.one, null, 0.28f, 0f);
    }

    /// <summary>
    /// Матеріал із реальною CC0-текстурою. Старі виклики без текстури лишаються робочими.
    /// </summary>
    public static Material Create(Color color, Color? emission, Texture2D texture, Vector2 tiling,
        Texture2D normalMap = null, float smoothness = 0.28f, float metallic = 0f)
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Diffuse");

        var material = new Material(shader);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        material.color = color;

        if (texture != null)
        {
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            SetScale(material, "_MainTex", tiling);
            SetScale(material, "_BaseMap", tiling);
        }

        if (normalMap != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 0.85f);
            SetScale(material, "_BumpMap", tiling);
        }

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Roughness"))
            material.SetFloat("_Roughness", 1f - smoothness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);

        if (emission.HasValue)
        {
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            if (material.HasProperty("_EmissionMap") && texture != null)
                material.SetTexture("_EmissionMap", texture);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return material;
    }

    /// <summary>
    /// Напівпрозоре світіння для XP, пасток, мuzzle flash і UI-елементів у світі.
    /// </summary>
    public static Material CreateGlow(Color color, Texture2D texture, bool additive = true)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        material.color = color;
        if (texture != null)
        {
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
        }

        if (additive)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_ZWrite", 0);
        }
        else
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
        }

        material.EnableKeyword("_ALPHABLEND_ON");
        if (material.HasProperty("_Cull"))
            material.SetInt("_Cull", 0); // двобічні sprite-частки полум'я
        material.renderQueue = additive ? 3050 : 3000;
        return material;
    }

    static void SetScale(Material material, string property, Vector2 tiling)
    {
        if (material.HasProperty(property))
            material.SetTextureScale(property, tiling);
    }
}
