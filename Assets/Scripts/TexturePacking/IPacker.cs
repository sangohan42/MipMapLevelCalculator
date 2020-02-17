using UnityEngine;

interface IPacker
{
    bool Add(Map t);
    void Remove(Map t);
    Rect GetRect(Map t);
    Rect GetNormalizedRectWithCenterOffset(Map t);

    Texture2D GetTexture();
    bool SuccessfullyPacked { get; }
}
