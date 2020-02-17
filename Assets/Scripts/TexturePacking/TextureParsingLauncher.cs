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
        if( _showAtlasContent )
        {
            _showAtlasContent.onValueChanged.AddListener( OnShowAtlas );
            _showAtlasContent.enabled = false;
        }
        _showColorAtlas = false;
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

        _showAtlasContent.enabled = true;
    }

    public void OnShowAtlas(bool activated)
    {
        _showColorAtlas = activated;
    }

    public void Update()
    {
        if( _showColorAtlas )
            _currentTexturePackerCreator?.DrawForDebug( AllTextureParser.TextureType.COLOR );
    }
}
