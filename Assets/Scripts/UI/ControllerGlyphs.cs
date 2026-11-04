using System;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;
using UnityEngine.Sprites;

public enum ControllerIconStyle { Auto, Xbox, PlayStation }

// Shared, build-safe sprite catalogue. Native device layouts take priority over product names.
public sealed class ControllerGlyphs : ScriptableObject
{
    [Serializable] public sealed class Pair { public Sprite Xbox, PlayStation; public Color XboxColor = Color.white; }
    public Pair[] Buttons;
    public Sprite[] KeyboardFaces;
    public Sprite[] KeyboardDirections;
    [Tooltip("Overworld buttons: Restore (R), mode (E), dismiss (Delete), close (Escape).")]
    public Sprite[] OverworldKeys;
    private static ControllerGlyphs cached;
    public static ControllerGlyphs Catalog => cached ? cached : cached = Resources.Load<ControllerGlyphs>("ControllerGlyphs");
    public static bool DetectPlayStation(Gamepad pad)
    {
        if (pad == null) return false;
        if (pad is DualShockGamepad) return true;
        string identity = (pad.layout + " " + pad.description.product + " " + pad.description.manufacturer).ToLowerInvariant();
        return identity.Contains("dualsense") || identity.Contains("dualshock") || identity.Contains("sony") || identity.Contains("playstation");
    }
    public static bool PlayStation
    {
        get
        {
            var source = GameManager.Instance ? GameManager.Instance.Input : null;
            var style = source ? source.ControllerIcons : ControllerIconStyle.Auto;
            return style == ControllerIconStyle.PlayStation || (style == ControllerIconStyle.Auto && DetectPlayStation(Gamepad.current));
        }
    }
    public static string Adapt(string text) => !PlayStation || text == null ? text : text.Replace("LT", "L2").Replace("RT", "R2")
        .Replace("LB", "L1").Replace("RB", "R1").Replace("View:", "Create/Share:").Replace("A:", "Cross:").Replace("A, B, X, Y", "Cross, Circle, Square, Triangle")
        .Replace("controller B", "controller Circle");
    public static Sprite Resolve(Sprite original, out Color tint)
    {
        tint = Color.white;
        if (!original || !Catalog) return original;
        foreach (var pair in Catalog.Buttons)
            if (pair.Xbox == original) { tint = PlayStation ? Color.white : pair.XboxColor; return PlayStation && pair.PlayStation ? pair.PlayStation : original; }
        return original;
    }
    private static readonly Material[] faceMaterials = new Material[8];
    public static void StyleFace(Image image)
    {
        if (!image || !Catalog) return;
        image.material = null;
        for (int index = 0; index < 4; index++)
        {
            var pair = Catalog.Buttons[index];
            bool sony = image.sprite == pair.PlayStation;
            if (!sony && image.sprite != pair.Xbox) continue;
            int slot = index + (sony ? 4 : 0);
            if (!faceMaterials[slot])
            {
                var shader = Shader.Find("Game/ControllerFace");
                if (!shader) return;
                var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                material.SetColor("_FaceColor", sony ? Color.white : new Color(0.015f, 0.015f, 0.02f));
                material.SetColor("_SymbolColor", sony ? new Color(0.02f, 0.02f, 0.025f) : pair.XboxColor);
                material.SetVector("_SpriteUV", DataUtility.GetOuterUV(image.sprite));
                faceMaterials[slot] = material;
            }
            image.material = faceMaterials[slot];
            image.color = new Color(1, 1, 1, image.color.a);
            return;
        }
    }
    private static readonly Material[] directionMaterials = new Material[8];
    public static void StyleQte(Image image, QteButton input, bool controller)
    {
        if (!controller || input < QteButton.DpadUp) { StyleFace(image); return; }
        int index = (int)input - 4;
        if (!directionMaterials[index])
        {
            var shader = Shader.Find("Game/ControllerDirection"); if (!shader) return;
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material.SetFloat("_Stick", index >= 4 ? 1 : 0); material.SetFloat("_Direction", index % 4);
            material.SetVector("_SpriteUV", DataUtility.GetOuterUV(image.sprite)); directionMaterials[index] = material;
        }
        image.material = directionMaterials[index]; image.color = Color.white;
    }
    public static Sprite Face(QteButton button, bool controller, out Color tint)
    {
        tint = Color.white;
        if (!Catalog) return null;
        int index = (int)button;
        if (index >= 4) return Catalog.KeyboardDirections[index - 4];
        if (!controller) return Catalog.KeyboardFaces[index];
        var pair = Catalog.Buttons[index]; tint = PlayStation ? Color.white : pair.XboxColor;
        return PlayStation ? pair.PlayStation : pair.Xbox;
    }
}
