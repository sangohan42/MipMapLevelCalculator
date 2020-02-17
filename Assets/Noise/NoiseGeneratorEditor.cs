using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoiseGenerator), true)]
public class NoiseGeneratorEditor : Editor
{
    private Transform _instantiateParent;
    private BoxCollider _spawningArea;
    private int _numberOfCopy = 10;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NoiseGenerator noiseGenerator = ( NoiseGenerator )target;

        if( noiseGenerator.instantiateInScene )
        {
            EditorGUI.indentLevel++;

            _instantiateParent = EditorGUILayout.ObjectField( "Parent transform", _instantiateParent, typeof( Transform ), true ) as Transform;
            _spawningArea = EditorGUILayout.ObjectField("Spawning Area", _spawningArea, typeof(BoxCollider), true) as BoxCollider;
            _numberOfCopy = EditorGUILayout.IntField( "Copy Number", _numberOfCopy );

            EditorGUI.indentLevel++;
        }

        if (noiseGenerator.instantiateInScene || noiseGenerator.saveOnDisk)
        {
            if (GUILayout.Button("Generate Textures"))
            {
                noiseGenerator.Generate(_instantiateParent, _spawningArea, _numberOfCopy);
            }
        }

        if (GUILayout.Button("Delete All"))
        {
            noiseGenerator.Delete();
        }

        EditorUtility.SetDirty(target);
    }

    public void OnEnable()
    {
        _instantiateParent = GameObject.Find("SpawningArea").transform;
        _spawningArea = GameObject.Find("SpawningArea").GetComponent<BoxCollider>();
    }
}
