using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

public class SoftMask : MonoBehaviour
{
    private class MaskedEntry
    {
        public bool changed;
        public DisplayObject displayObject;
        public Material maskedMaterial;
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

    private readonly List<MaskedEntry> maskedEntries = new List<MaskedEntry>();

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

        Matrix4x4 matrix = default;
        var count = maskedEntries.Count;
        for (var i = 0; i < count; i++)
        {
            var maskedMaterial = maskedEntries[i].maskedMaterial;
#if UNITY_EDITOR
            // debug 调试用
            if (maskedMaterial != null)
                maskedMaterial.SetFloat(Softness, softness);
#endif
            if (!maskedEntries[i].changed) continue;
            if (maskedMaterial == null)
            {
                var displayObject = maskedEntries[i].displayObject;
                var graphics = displayObject.graphics;
                if (maskedEntries[i].displayObject.gameObject.name.Contains("NumberTxt"))
                {
                    Debug.LogError($"numberTxt {graphics} {displayObject.paintingGraphics}");
                }
                if (graphics != null)
                {
                    Shader shader;
#if UNITY_EDITOR
                    shader = Shader.Find(maskedShader);
#else
                    shader = ResourceManager.LoadResource("Assets/EditorShader/SoulShader/Other/SoftMask.shader",
                        typeof(Shader)) as Shader;
#endif
                    maskedMaterial = new Material(shader);
                    graphics.SetMaterial(maskedMaterial);
                    maskedMaterial.SetTexture(SoftMaskTex, maskTex);
                    maskedEntries[i].maskedMaterial = maskedMaterial;
                }
            }

            if (maskedMaterial != null)
            {
                if (matrix == default) matrix = cachedTransform.worldToLocalMatrix;
                maskedMaterial.SetFloat(Softness, softness);
                maskedMaterial.SetVector(SoftMaskRect, maskRect);
                maskedMaterial.SetMatrix(SoftMaskMatrix, matrix);
                maskedEntries[i].changed = false;
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
    }
}