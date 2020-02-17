using System;
using UnityEngine;

/// <summary>
/// In the context of this class and its related methods and shaders
/// "max" is used to indicate "highest" level of detail
/// which numerically corresponds to the lowest number
/// (0 is the max lod for a texture)
/// </summary>
public class MaxLODComputer
{
    private readonly ComputeShader _computeShader;
    private readonly int _kernelHandle;

    public const int OCCLUDED_TEX_LOD = 100; // init at 100, so that it gets replaced by a smaller LOD for all visible items (= higher resolution)

    public MaxLODComputer()
    {
        if (!SystemInfo.supportsComputeShaders)
            throw new NotSupportedException("Compute shaders not supported on this system!");

        ComputeShader maxLODComputeShader = (ComputeShader) Resources.Load("MaxLODComputer");

        if (!maxLODComputeShader)
            throw new ArgumentException("MaxLODComputer not found");

        _computeShader = maxLODComputeShader;

        try
        {
            _kernelHandle = _computeShader.FindKernel("CSMain");
        }
        catch (Exception)
        {
            throw new NullReferenceException("Cannot find CSMain kernel in shader " + _computeShader.name);
        }
    }

    /// <summary>
    /// "max" is used to indicate "highest" level of detail
    /// but is in fact the lowest numerical value
    /// </summary>
    public int[] GetMaxLOD(Texture tex, int nbTexturesInScene)
    {
        int[] output = new int[nbTexturesInScene];
        for (int j = 0; j < output.Length; j++)
            output[j] = OCCLUDED_TEX_LOD;

        _computeShader.SetTexture(_kernelHandle, "Input", tex);

        ComputeBuffer buffer = new ComputeBuffer(output.Length, sizeof(int), ComputeBufferType.Default);
        buffer.SetData(output);
        _computeShader.SetBuffer(_kernelHandle, "Output", buffer);
        _computeShader.SetFloat("output_size", output.Length);

        _computeShader.Dispatch(_kernelHandle, DispatchCount(tex.width, 8), DispatchCount(tex.height, 8), 1);

        //---------- adding wait here might help if it becomes too slow --------------

        buffer.GetData(output);
        buffer.Dispose();

        return output;
    }

    public float ConvertTextureId(int zeroBasedId, int totalNumberOfTextures)
    {
        if (totalNumberOfTextures <= 0)
            throw new ArgumentException("Parameter must be greater than 0", nameof(totalNumberOfTextures));

        if (zeroBasedId < 0)
            throw new ArgumentException("Parameter must be greater or equal to 0", nameof(zeroBasedId));

        if (zeroBasedId >= totalNumberOfTextures)
            throw new ArgumentException("The id of the texture must be smaller than the total number of textures");

        return zeroBasedId / (float) totalNumberOfTextures;
    }

    /// <summary>
    /// Determine the number of thread groups for the compute shader per dimension
    /// </summary>
    /// <param name="pixelCount">The size of the pixels to process per dimension</param>
    /// <param name="groupThreadSize">The size of the thread group</param>
    /// TODO: get the thread group size via ComputeShader.GetKernelThreadGroupSizes
    /// TODO: move to GraphicTools, and use for all compute shaders in project
    /// <returns></returns>
    private int DispatchCount(int pixelCount, int groupThreadSize)
    {
        return pixelCount / groupThreadSize + (pixelCount % groupThreadSize > 0 ? 1 : 0);
    }
}