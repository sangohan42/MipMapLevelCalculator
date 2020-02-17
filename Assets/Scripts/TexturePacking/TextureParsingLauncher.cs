using System;
using UnityEngine;
using UnityEngine.UI;

public class TextureParsingLauncher : MonoBehaviour
{
    [SerializeField]
    private Transform _parsingRootTransform;

    [SerializeField]
    private Button _startParsingButton;

    [SerializeField]
    private Toggle _showAtlasContent;

    private bool _showColorAtlas;

    private TexturePackerCreator _currentTexturePackerCreator;

    public void Awake()
    {
        if( _startParsingButton )
            _startParsingButton.onClick.AddListener( OnParseSceneButtonClicked );

        _showColorAtlas = false;
        _showAtlasContent.enabled = false;
    }

    public void OnParseSceneButtonClicked()
    {
        if (!_parsingRootTransform)
        {
            Debug.Log("parsingRootTransform should be set to be able to parse the scene");
            return;
        }

        _startParsingButton.enabled = false;

        _currentTexturePackerCreator = new TexturePackerCreator();
        _currentTexturePackerCreator.ParseSceneAndCreateAtlases( _parsingRootTransform );

        _startParsingButton.enabled = true;
        _showAtlasContent.enabled = true;
    }

    public void OnShowAtlas(bool activated)
    {
        _showColorAtlas = activated;
    }

    public void Update()
    {
        if( _showColorAtlas && _currentTexturePackerCreator != null )
            _currentTexturePackerCreator.DrawForDebug( AllTextureParser.TextureType.COLOR );
    }
}
