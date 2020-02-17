using System;
using System.Collections.Generic;
using UnityEngine;

class Packer : IPacker
{
    private static int packerIndex = 0;
    private bool _horizontalSplit = true;

    //list of zones in packing that are free to use
    private List<Rect> _availableZones;

    //list of rects that are assigned (used for rendering a text)
    private Dictionary<Map, Rect> _assignedRects;
    private int _maxSize;
    private bool _lastActionWasRemove = false;
    private LODProcessor _lodProcessor = null;

    private int _totalWidth;
    private int _totalHeight;

    private Texture2D _packed;
    private Transform _rootTransform;

    #region Properties


    public bool SuccessfullyPacked { get; private set; } = true;

    public Texture2D GetTexture()
    {
        if (_packed != null)
            return _packed;

        if (_totalHeight * _totalHeight == 0)
        {
            Debug.Log("Nothing in packer...");
            _packed = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            return _packed;
        }

        RenderTexture _renderTexture = RenderTexture.GetTemporary(_totalWidth, _totalHeight, 0, RenderTextureFormat.ARGB32); //, RenderTextureReadWrite.Linear );
        _renderTexture.filterMode = FilterMode.Point;
        _renderTexture.anisoLevel = 0;

        //draw all elements
        RenderTexture.active = _renderTexture;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, _renderTexture.width, _renderTexture.height, 0);
        GL.Clear(true, true, new Color(1, 1, 1, 0));

        foreach (var pair in _assignedRects)
        {
            Texture2D tex = pair.Key.Texture;
            FilterMode stored = tex.filterMode;
            tex.filterMode = FilterMode.Point;
            Graphics.DrawTexture(pair.Value, tex);
            tex.filterMode = stored;
        }

        GL.PopMatrix();
        RenderTexture.active = null;

        if (_packed == null)
            _packed = new Texture2D(_totalWidth, _totalHeight, TextureFormat.ARGB32, false); //, false );
        if (_packed.width != _totalWidth || _packed.height != _totalHeight)
            _packed.Resize(_totalWidth, _totalHeight);

        //blit back to ram
        RenderTexture.active = _renderTexture;
        _packed.ReadPixels(new Rect(0, 0, _totalWidth, _totalHeight), 0, 0);
        RenderTexture.active = null;
        _packed.Apply();

        RenderTexture.ReleaseTemporary(_renderTexture);
        return _packed;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Packs objects in elts list in an optimal rect
    /// </summary>
    /// <param name="elts">list of elements to pack</param>
    /// <param name="maxSize">maximum output pack size</param>
    /// <param name="type">The type of the textures in the packer</param>
    public Packer(List<Map> elts, int maxSize, AllTextureParser.TextureType type, Transform rootTransform)
    {
        _rootTransform = rootTransform;

        //remove empty elements from list
        for (int i = elts.Count - 1; i >= 0; i--)
            if (elts[i] == null)
                elts.RemoveAt(i);

        //check that list doesn't contain duplicates
        for (int i = 0; i < elts.Count; i++)
        {
            Map t = elts[i];
            for (int j = elts.Count - 1; j > i; j--)
            {
                if (elts[j] == t)
                    elts.RemoveAt(j);
            }
        }

        _maxSize = maxSize;
        _type = type;

        SuccessfullyPacked = TryPackingTextures(elts);
    }

    private AllTextureParser.TextureType _type;

    /// <summary>
    /// Declare a packer without any elements
    /// </summary>
    /// <param name="horizontalSplit"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="maxSize"></param>
    public Packer(int width, int height, int maxSize)
    {
        _maxSize = maxSize;
        _totalWidth = width;
        _totalHeight = height;
        Clear();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// return TRUE if this element has a rect assigned in the packing
    /// </summary>
    /// <param name="elt"></param>
    /// <returns></returns>
    public bool IsAssigned(Map elt)
    {
        return _assignedRects.ContainsKey(elt);
    }

    public Rect GetRect(Map elt)
    {
        if (_assignedRects.ContainsKey(elt))
            return _assignedRects[elt];
        else
        {
            return new Rect();
        }
    }

    public Rect GetNormalizedRectWithCenterOffset(Map elt)
    {
        Rect r = GetRect(elt);
        return new Rect((r.min.x + 0.5f) / _totalWidth,
            1f - (r.min.y + 0.5f + r.height - 1) / _totalHeight,
            (r.width - 1) / _totalWidth,
            (r.height - 1) / _totalHeight);
    }

    /// <summary>
    /// Try to add elt to packer
    /// Return TRUE only if a space was found for it
    /// </summary>
    /// <param name="elt"></param>
    /// <returns></returns>
    public bool Add(Map elt)
    {
        if (elt != null && !_assignedRects.ContainsKey(elt))
        {
            if (_lastActionWasRemove)
            {
                _lastActionWasRemove = false;
                Defragment();
            }

            //try to find an empty spot
            if (!FindAreaForElement(elt))
            {
                Defragment();
                if (!FindAreaForElement(elt))
                {
                    //doesn't fit, we'll need to expand and repack
                    List<Map> list = new List<Map>(_assignedRects.Keys);
                    list.Add(elt);
                    SuccessfullyPacked = TryPackingTextures(list, true);
                    return SuccessfullyPacked;
                }
            }
        }

        return SuccessfullyPacked;
    }

    private bool TryPackingTextures(List<Map> elts, bool expandOnly = false)
    {
        if (SortAndPack(elts, expandOnly))
            return true;
        if (CalculateLOD(elts) && SortAndPack(elts, expandOnly))
            return true;

        return false;
    }

    /// <summary>
    /// Remove an elt from the packer, freeing its used space for later reuse
    /// </summary>
    /// <param name="elt"></param>
    public void Remove(Map elt)
    {
        if (_assignedRects.ContainsKey(elt))
        {
            Rect r = _assignedRects[elt];
            _assignedRects.Remove(elt);
            if (r.width * r.height > 1)
            {
                _availableZones.Add(r);
                _lastActionWasRemove = true;
            }
        }
    }

    /// <summary>
    /// Remove all elts from packer and reset its empty space to a single large rect
    /// </summary>
    public void Clear()
    {
        _assignedRects = new Dictionary<Map, Rect>();
        _availableZones = new List<Rect>();
        _availableZones.Add(new Rect(0, 0, _totalWidth, _totalHeight));
    }

    /// <summary>
    /// Used for debugging in Unity Editor
    /// </summary>
    public void OnDrawGizmos()
    {
        Rect r;
        for (int i = 0; i < _availableZones.Count; i++)
        {
            r = _availableZones[i];
            Gizmos.color = new Color(i % 2, (i / 2) % 2, (i % 4) % 4, 1f);
            Gizmos.DrawCube(r.center, new Vector3(r.width, r.height, 0));
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(r.center, new Vector3(r.width, r.height, 0));
        }
    }

    public override string ToString()
    {
        return "Packer texture count=" + _assignedRects.Values.Count;
    }

    #endregion

    #region Private Helper Methods

    bool CalculateLOD(List<Map> elts)
    {
        Debug.Log("Start MipMap Calculation");
        if (_lodProcessor == null)
        {
            try
            {
                _lodProcessor = new LODProcessor( Screen.width, Screen.height );
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }
        }

        if (_lodProcessor != null)
        {
            try
            {
                Dictionary<Texture2D, Vector2Int> sizes = _lodProcessor.CalculateTexturesLOD(_type, _rootTransform);
                if (sizes.Count == 0)
                    return false;

                foreach (var map in elts)
                {
                    if (sizes.TryGetValue(map.Texture, out Vector2Int size))
                    {
                        Debug.Log("Replacing size from ( " + map.MetaData.MipMapWidth + ", " + map.MetaData.MipMapHeight + " ) to ( " + size.x + ", " + size.y + " )");
                        map.MetaData.MipMapWidth = size.x;
                        map.MetaData.MipMapHeight = size.y;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
                return false;
            }
        }

        return false;
    }

    bool SortAndPack( List<Map> elts, bool expandOnly )
    {
        Debug.Log("SortAndPack");
        DateTime startTime = DateTime.Now;
        EstimateNeededSize(elts, expandOnly);

        //let's sort objects
        IComparer<Map> cmp;
        if (_horizontalSplit)
            cmp = new HorizontalSplitSizeComparer();
        else
            cmp = new VerticalSplitSizeComparer();

        elts.Sort(cmp);

        Map elt = null;
        bool everythingFits = false;

        while (!everythingFits)
        {
            Clear();
            everythingFits = true;

            //process elts from bigger to smaller
            for (int i = elts.Count - 1; i >= 0; i--)
            {
                elt = elts[i];

                if (!FindAreaForElement(elt))
                {
                    everythingFits = false;
                    if (_totalWidth < _maxSize)
                    {
                        if (_totalWidth <= _totalHeight)
                            _totalWidth *= 2;
                        else
                            _totalHeight *= 2;
                    }
                    else if (_totalHeight < _maxSize)
                        _totalHeight *= 2;
                    else
                    {
                        Debug.LogError(_type + ": Sort and pack done in " + DateTime.Now.Subtract(startTime).TotalMilliseconds +
                                       "ms, no fit! Missing textures " + i + " out of " + elts.Count);
                        return false;
                    }

                    break;
                }
            }
        }

        Debug.LogWarning(_type + ": Sort and pack, fits " + everythingFits + "! Done in " +
                         DateTime.Now.Subtract(startTime).TotalMilliseconds + "ms. Textures added " +
                         _assignedRects.Count);

        return everythingFits;
    }

    void Defragment()
    {
        //sort by increasing Ymin
        PositionYComparer comparer = new PositionYComparer();
        _availableZones.Sort(comparer);

        //go through our list, look at consecutive elements, if they are "attached", replace them other split only is it increases max size
        int index = 0;

        while (index < _availableZones.Count - 1)
        {
            Rect r0 = _availableZones[index];
            Rect r1 = _availableZones[index + 1];

            //are they connected?
            if (r0.yMin == r1.yMin && r0.xMax == r1.xMin)
            {
                if (r0.height == r1.height)
                {
                    //merge
                    _availableZones[index] = new Rect(r0.xMin, r0.yMin, r0.width + r1.width, r0.height);
                    _availableZones.RemoveAt(index + 1);
                    index--;
                }
                else
                {
                    float currentMaxSize = Mathf.Max(r0.width * r0.height, r1.width * r1.height);
                    float newMaxSize = Mathf.Max((r0.width + r1.width) * Mathf.Min(r0.height, r1.height),
                        r0.height < r1.height
                            ? r1.width * (r1.height - r0.height)
                            : r0.width * (r0.height - r1.height));
                    if (newMaxSize > currentMaxSize)
                    {
                        //swap
                        _availableZones[index] = new Rect(r0.xMin, r0.yMin, r0.width + r1.width,
                            Mathf.Min(r0.height, r1.height));
                        Rect newRect = r0.height < r1.height
                            ? new Rect(r1.xMin, r1.yMin + r0.height, r1.width, r1.height - r0.height)
                            : new Rect(r0.xMin, r0.yMin + r1.height, r0.width, r0.height - r1.height);

                        //let's see where to integrate it
                        _availableZones.RemoveAt(index + 1);
                        int newIndex = index + 1;
                        bool inserted = false;
                        while (newIndex < _availableZones.Count)
                        {
                            int res = comparer.Compare(newRect, _availableZones[newIndex]);
                            if (res <= 0)
                            {
                                //insert here
                                _availableZones.Insert(newIndex, newRect);
                                inserted = true;
                                break;
                            }

                            newIndex++;
                        }

                        if (!inserted)
                            _availableZones.Add(newRect);
                        index--;
                    }
                }
            }

            index++;
        }

        //we now have consolidated horizontal bars, let's try to merge vertically
        _availableZones.Sort(new PositionXComparer());
        index = 0;
        while (index < _availableZones.Count - 1)
        {
            Rect r0 = _availableZones[index];
            Rect r1 = _availableZones[index + 1];
            if (r0.xMin == r1.xMin && r0.xMax == r1.xMax && r0.yMax == r1.yMin)
            {
                _availableZones[index] = new Rect(r0.xMin, r0.yMin, r0.width, r0.height + r1.height);
                _availableZones.RemoveAt(index + 1);
            }
            else
                index++;
        }

        //notice that to have a fully completed defragmentation, we would need to loop the whole method until no more merging happens
        //in practice this is not needed since defragment is being called regularly
    }

    bool FindAreaForElement(Map elt)
    {
        int foundIndex = -1;
        int eltWidth = elt.MetaData.MipMapWidth;
        int eltHeight = elt.MetaData.MipMapHeight;

        //find an area where this elt fits
        for (int i = 0; i < _availableZones.Count; i++)
        {
            if (foundIndex == -1 && eltWidth <= _availableZones[i].width && eltHeight <= _availableZones[i].height)
                foundIndex = i;
            else
            {
                //try to find an exact fit
                if (eltWidth == _availableZones[i].width && eltHeight == _availableZones[i].height)
                {
                    foundIndex = i;
                    break;
                }
            }
        }

        if (foundIndex == -1)
            return false;

        //found it! Add it to rects
        Rect zone = _availableZones[foundIndex];
        _assignedRects[elt] = new Rect(zone.xMin, zone.yMin, eltWidth, eltHeight);

        //let's remove this available zone and add two split zones
        if (_horizontalSplit)
        {
            if (zone.width > eltWidth)
            {
                _availableZones[foundIndex] =
                    new Rect(zone.xMin + eltWidth, zone.yMin, zone.width - eltWidth, eltHeight);
                if (zone.height > eltHeight)
                    _availableZones.Insert(foundIndex + 1,
                        new Rect(zone.xMin, zone.yMin + eltHeight, zone.width, zone.height - eltHeight));
            }
            else
            {
                if (zone.height > eltHeight)
                    _availableZones[foundIndex] = new Rect(zone.xMin, zone.yMin + eltHeight, zone.width,
                        zone.height - eltHeight);
                else
                    _availableZones.RemoveAt(foundIndex);
            }
        }
        else
        {
            Debug.LogError("Not implemented");
        }

        return true;
    }

    void EstimateNeededSize(List<Map> elts, bool expandOnly)
    {
        //we have our list of items to pack, we can approximate the required texture size
        long area = 0;
        int count = elts.Count;

        //in tmp we will store the max height (if horizontal is TRUE) or width (if FALSE)
        int maxWidth = 0, maxHeight = 0;
        for (int i = 0; i < count; i++)
        {
            int eltWidth = elts[i].MetaData.MipMapWidth;
            int eltHeight = elts[i].MetaData.MipMapHeight;

            area += eltWidth * eltHeight;
            maxWidth = Mathf.Max(maxWidth, eltWidth);
            maxHeight = Mathf.Max(maxHeight, eltHeight);
        }

        int targetWidth = Mathf.NextPowerOfTwo(maxWidth);
        if (expandOnly && targetWidth < _totalWidth)
            targetWidth = _totalWidth;

        int targetHeight = Mathf.NextPowerOfTwo(maxHeight);
        if (expandOnly && targetHeight < _totalHeight)
            targetHeight = _totalHeight;
        while (targetWidth * targetHeight < area && (targetWidth < _maxSize || targetHeight < _maxSize))
        {
            //try to keep it square
            if (targetWidth < targetHeight)
                targetWidth *= 2;
            else
                targetHeight *= 2;
        }

        //we now have a plausible area for storing all our elements
        //we assume that packing in only expanding

        _totalWidth = targetWidth;
        _totalHeight = targetHeight;

        //Debug.LogWarning("Estimated needed size " + _totalWidth + "," + _totalHeight);
    }

    bool IncreaseArea()
    {
        //try to keep it square
        if (_horizontalSplit && _totalWidth < _maxSize)
        {
            for (int i = 0; i < _availableZones.Count; i++)
            {
                if (_availableZones[i].xMax == _totalWidth)
                {
                    Rect r = _availableZones[i];
                    _availableZones[i] = new Rect(r.xMin, r.yMin, r.width + _totalWidth, r.height);
                }
            }

            _totalWidth *= 2;
            //Debug.LogWarning("Packer "+_packerIndex+" Increase Packing to " + _totalWidth + "," + _totalHeight);
            return true;
        }
        else if (_totalHeight < _maxSize)
        {
            for (int i = 0; i < _availableZones.Count; i++)
            {
                if (_availableZones[i].yMax == _totalHeight)
                {
                    Rect r = _availableZones[i];
                    _availableZones[i] = new Rect(r.xMin, r.yMin, r.width, r.height + _totalHeight);
                }
            }

            _totalHeight *= 2;
            //Debug.LogWarning("Increase Packing to " + _totalWidth + "," + _totalHeight);
            return true;
        }
        else
            return false;
    }

    #endregion

    #region Classes

    class HorizontalSplitSizeComparer : IComparer<Map>
    {
        public int Compare(Map x, Map y)
        {
            //we assume that what really matters is the overall size first
            int r = (x.MetaData.MipMapHeight * x.MetaData.MipMapWidth).CompareTo(
                y.MetaData.MipMapHeight * y.MetaData.MipMapWidth);
            if (r == 0)
                r = x.MetaData.MipMapHeight.CompareTo(y.MetaData.MipMapHeight);
            if (r == 0)
                r = x.MetaData.MipMapWidth.CompareTo(y.MetaData.MipMapWidth);
            return r;
        }
    }

    class VerticalSplitSizeComparer : IComparer<Map>
    {
        public int Compare(Map x, Map y)
        {
            int r = x.MetaData.MipMapWidth.CompareTo(y.MetaData.MipMapWidth);
            if (r == 0)
                r = x.MetaData.MipMapHeight.CompareTo(y.MetaData.MipMapHeight);
            return r;
        }
    }

    class PositionYComparer : IComparer<Rect>
    {
        public int Compare(Rect r0, Rect r1)
        {
            int r = r0.yMin.CompareTo(r1.yMin);
            if (r == 0)
                r = r0.xMin.CompareTo(r1.xMin);
            return r;
        }
    }

    class PositionXComparer : IComparer<Rect>
    {
        public int Compare(Rect r0, Rect r1)
        {
            int r = r0.xMin.CompareTo(r1.xMin);
            if (r == 0)
                r = r0.yMin.CompareTo(r1.yMin);
            return r;
        }
    }

    class SizeComparer : IComparer<Rect>
    {
        public int Compare(Rect r0, Rect r1)
        {
            return (r0.width * r0.height).CompareTo(r1.width * r1.height);
        }
    }

    #endregion
}