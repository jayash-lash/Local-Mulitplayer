using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerHealthUI : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new Vector3(0, 2f, 0);
    
    private TextMeshProUGUI _healthText;
    private Image _backgroundImage;
    private PlayerHealth _playerHealth;
    private Camera _mainCamera;
    private Canvas _canvas;
    
    private void Start()
    {
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("Canvas not found on scene!");
            return;
        }
        
        CreateHealthText();
        
        _playerHealth = GetComponent<PlayerHealth>();
        if (_playerHealth == null)
        {
            Debug.LogError("PlayerHealth component not found!");
            return;
        }
        
        _playerHealth.Health.OnValueChanged += OnHealthChanged;
        
        _mainCamera = Camera.main;
    }
    
    private void CreateHealthText()
    {
        GameObject backgroundObject = new GameObject("HealthBackground");
        backgroundObject.transform.SetParent(_canvas.transform, false);
        
        _backgroundImage = backgroundObject.AddComponent<Image>();
        _backgroundImage.color = new Color(0, 0, 0, 0.7f);
        _backgroundImage.sprite = CreateRoundedSprite();
        
        RectTransform bgRect = _backgroundImage.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(80, 25);
        
        GameObject textObject = new GameObject("HealthText");
        textObject.transform.SetParent(backgroundObject.transform, false);
        
        _healthText = textObject.AddComponent<TextMeshProUGUI>();
        _healthText.text = "100/100";
        _healthText.fontSize = 14;
        _healthText.color = Color.white;
        _healthText.alignment = TextAlignmentOptions.Center;
        _healthText.fontStyle = FontStyles.Bold;
        
        RectTransform textRect = _healthText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
    
    private Sprite CreateRoundedSprite()
    {
        Texture2D texture = new Texture2D(32, 16);
        Color[] pixels = new Color[texture.width * texture.height];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
    
    private void Update()
    {
        if (_backgroundImage != null && _mainCamera != null)
        {
            Vector3 worldPosition = transform.position + _offset;
            Vector3 screenPosition = _mainCamera.WorldToScreenPoint(worldPosition);
        
            if (screenPosition.z > 0)
            {
                Vector3 currentPos = _backgroundImage.transform.position;
                _backgroundImage.transform.position = Vector3.Lerp(currentPos, screenPosition, Time.deltaTime * 10f);
                _backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                _backgroundImage.gameObject.SetActive(false);
            }
        }
    }
    
    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (_healthText != null)
        {
            _healthText.text = $"{newValue}/{_playerHealth.MaxHealth}";
            
            float healthPercent = (float)newValue / _playerHealth.MaxHealth;
            if (healthPercent > 0.6f)
                _healthText.color = Color.green;
            else if (healthPercent > 0.3f)
                _healthText.color = Color.yellow;
            else
                _healthText.color = Color.red;
        }
    }
    
    public override void OnDestroy()
    {
        if (_playerHealth != null && _playerHealth.Health != null)
        {
            _playerHealth.Health.OnValueChanged -= OnHealthChanged;
        }
        
        if (_backgroundImage != null)
        {
            Destroy(_backgroundImage.gameObject);
        }
    }
}