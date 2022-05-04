
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class TextureRefresh : UdonSharpBehaviour
{
    public MeshRenderer source;
    public MeshRenderer target;
    public bool fromPropertyBlock;

    public int targetIndex = 0;
    public Texture defaultTexture;
    public string sourceTexProp;
    public string targetTexProp;
    public float interval = 0.5f;

    MaterialPropertyBlock block;

    void Start()
    {
        block = new MaterialPropertyBlock();
        _Update();
    }

    public void _Update()
    {
        if (!Utilities.IsValid(source) || !Utilities.IsValid(target))
            return;

        Texture tex = defaultTexture;
        if (fromPropertyBlock)
        {
            source.GetPropertyBlock(block);
            tex = block.GetTexture(sourceTexProp);
            if (!Utilities.IsValid(tex))
                tex = defaultTexture;
        } else
        {
            Material mat = source.sharedMaterial;
            if (Utilities.IsValid(mat))
            {
                tex = mat.GetTexture(sourceTexProp);
                if (!Utilities.IsValid(tex))
                    tex = defaultTexture;
            }
        }

        target.GetPropertyBlock(block, targetIndex);
        block.SetTexture(targetTexProp, tex);
        target.SetPropertyBlock(block, targetIndex);

        SendCustomEventDelayedSeconds("_Update", interval);
    }
}
