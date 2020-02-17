using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UniformTextureGenerator), true)]
public class UniformTextureGeneratorEditor : Editor
{
    private Transform _instantiateParent;
    private BoxCollider _spawningArea;
    private int _textureNumber = 20;
    private int _maxTextureSize = 2048;
    private int _numberOfCopy = 10;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UniformTextureGenerator uniformTextureGenerator = (UniformTextureGenerator) target;

        if( uniformTextureGenerator.instantiateInScene )
        {
            EditorGUI.indentLevel++;

            _instantiateParent = EditorGUILayout.ObjectField("Parent transform", _instantiateParent, typeof(Transform), true) as Transform;
            _spawningArea = EditorGUILayout.ObjectField("Spawning Area", _spawningArea, typeof(BoxCollider), true) as BoxCollider;
            _numberOfCopy = EditorGUILayout.IntField("Copy Number", _numberOfCopy);

            EditorGUI.indentLevel--;
        }

        _textureNumber = EditorGUILayout.IntField("Texture Number", _textureNumber);
        _maxTextureSize = EditorGUILayout.IntField("Maximum Texture Size", _maxTextureSize);

        if( uniformTextureGenerator.instantiateInScene || uniformTextureGenerator.saveOnDisk )
        {
            if( GUILayout.Button( "Generate Textures" ) )
            {
                uniformTextureGenerator.Generate( _instantiateParent, _spawningArea, _textureNumber, _numberOfCopy, _maxTextureSize );
            }
        }
        
        if (GUILayout.Button("Delete All"))
        {
            uniformTextureGenerator.Delete();
        }
    }

    public void OnEnable()
    {
        _instantiateParent = GameObject.Find("SpawningArea").transform;
        _spawningArea = GameObject.Find("SpawningArea").GetComponent<BoxCollider>();
    }
}
