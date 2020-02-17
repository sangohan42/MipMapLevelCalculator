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
            _showAtlasContent.onValueChanged.AddListener( OnToggleValueChanged );
            _showAtlasContent.gameObject.SetActive(false);
            _showAtlasContent.isOn = false;
            _showColorAtlas = false;
        }
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

        ShowToggleButton();
    }

    public void ShowToggleButton()
    {
        _showAtlasContent.gameObject.SetActive( true );
    }

    public void OnToggleValueChanged( bool value )
    {
        _showColorAtlas = value;
    }

    void OnGUI()
    {
        if (_showColorAtlas)
            _currentTexturePackerCreator?.DrawForDebug(AllTextureParser.TextureType.COLOR);
    }


    public void Update()
    {
        
    }
}
