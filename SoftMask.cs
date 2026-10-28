using System.Collections.Generic;
using FairyGUI;
using UnityEngine;
public class SoftMask : MonoBehaviour
{
    private class MaskedEntry
    {
        public bool changed;
        public bool maskedMaterial;
        public DisplayObject displayObject;
    }

    public string maskedShader = "SoulGames/SoftMask";

    [Range(1, 3)] public float softness = 1.4f;
    public Texture2D maskTex;
    public Vector4 maskRect = new Vector4(150, 150, 0, 0);

    private Transform _cachedTransform;

    public Transform cachedTransform
    {
        get
        {
            if (_cachedTransform == null) _cachedTransform = transform;
            return _cachedTransform;
        }
    }

    private MaterialPropertyBlock materialPropertyBlock;
    private readonly List<MaskedEntry> maskedEntries = new();
    private readonly Dictionary<Texture, Material> overridedMaterials = new();

    private static readonly int Softness = Shader.PropertyToID("_Softness");
    private static readonly int SoftMaskTex = Shader.PropertyToID("_SoftMaskTex");
    private static readonly int SoftMaskRect = Shader.PropertyToID("_SoftMaskRect");
    private static readonly int SoftMaskMatrix = Shader.PropertyToID("_SoftMaskMatrix");

    public void SetMaskRect(float width, float height, Vector2 pivot)
    {
        maskRect.x = width;
        maskRect.y = height;
        maskRect.z = pivot.x;
        maskRect.w = pivot.y;
    }

    public void MaskGObject(GObject gObject) =>
        MaskDisplayObject(gObject.displayObject);

    public void MaskDisplayObject(DisplayObject displayObject)
    {
        if (displayObject == null) return;
        var maskedEntry = new MaskedEntry
        {
            changed = true,
            displayObject = displayObject,
        };
        maskedEntries.Add(maskedEntry);
        if (displayObject.gOwner is GComponent cmp)
        {
            for (var i = 0; i < cmp.numChildren; i++)
            {
                var child = cmp.GetChildAt(i);
                if (child.displayObject != null)
                    MaskDisplayObject(child.displayObject);
            }
        }
    }

    public void RemoveGObject(GObject gObject) =>
        RemoveDisplayObject(gObject.displayObject);

    public void RemoveDisplayObject(DisplayObject displayObject)
    {
        if (displayObject == null) return;
        if (displayObject.gOwner is GComponent cmp)
        {
            for (int i = 0; i < cmp.numChildren; i++)
            {
                var child = cmp.GetChildAt(i);
                var childDisplayObject = child.displayObject;
                if (childDisplayObject != null)
                {
                    RemoveDisplayObject(childDisplayObject);
                }
            }
        }

        var count = maskedEntries.Count;
        for (int i = 0; i < count; i++)
        {
            var maskedEntry = maskedEntries[i];
            if (maskedEntry.displayObject.Equals(displayObject))
            {
                maskedEntries.RemoveAt(i);
                break;
            }
        }
    }

    private void UpdateMaskedObjects()
    {
        if (maskTex == null)
            maskTex = ResourceManager.LoadTexture("Assets/Art/TextureSingle/UI/soft.png");

        materialPropertyBlock ??= new MaterialPropertyBlock();
        var matrix = cachedTransform.worldToLocalMatrix;
        var count = maskedEntries.Count;
        for (var i = 0; i < count; i++)
        {
#if UNITY_EDITOR
            // debug 调试用
            // if (maskedMaterial)
            // maskedMaterial.SetFloat(Softness, softness);
#endif
            
            var maskedEntry = maskedEntries[i];
            if (!maskedEntry.changed) continue;
            var displayObject = maskedEntry.displayObject;
            var graphics = displayObject.graphics;
            if (graphics == null) continue;
            if (maskedEntry.displayObject.gameObject.name.Contains("NumberTxt"))
            {
                Debug.LogError($"numberTxt {graphics} {displayObject.paintingGraphics}");
            }

            var meshRenderer = graphics.meshRenderer;
            if (meshRenderer == null) continue;
            if (!maskedEntry.maskedMaterial)
            {
                var mainTexture = graphics.material.mainTexture;
                if (!overridedMaterials.TryGetValue(mainTexture, out var sharedMaterial))
                {
                    Shader shader;
#if UNITY_EDITOR
                    shader = Shader.Find(maskedShader);
#else
                    shader = ResourceManager.LoadResource("Assets/EditorShader/SoulShader/Other/SoftMask.shader",
                        typeof(Shader)) as Shader;
#endif
                    sharedMaterial = new Material(shader);
                    overridedMaterials[mainTexture] = sharedMaterial;
                }

                graphics.SetMaterial(sharedMaterial);
                maskedEntry.maskedMaterial = true;
            }

            if (maskedEntry.maskedMaterial)
            {
                //Todo Unity6.3以上 使用Renderer.SetShaderUserValue
                meshRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetTexture(SoftMaskTex, maskTex);
                materialPropertyBlock.SetFloat(Softness, softness);
                materialPropertyBlock.SetVector(SoftMaskRect, maskRect);
                materialPropertyBlock.SetMatrix(SoftMaskMatrix, matrix);
                meshRenderer.SetPropertyBlock(materialPropertyBlock);
                maskedEntry.changed = false;
            }
        }
    }

    private void Update()
    {
        UpdateMaskedObjects();

        if (cachedTransform.hasChanged)
            foreach (var entry in maskedEntries)
                entry.changed = true;
    }

    private void OnDestroy()
    {
        ResourceManager.UnloadTexture(maskTex);
        maskedEntries.Clear();
        overridedMaterials.Clear();
    }
}
