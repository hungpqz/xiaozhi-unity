using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class XSpriteChanger : MonoBehaviour
{
    [SerializeField] private Sprite[] _sprites;
    private Image _image;
    private int _currentIndex = -1;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    /// <summary>
    /// Chuyển sang Sprite theo chỉ số chỉ định
    /// </summary>
    /// <param name="index">Chỉ số mục tiêu</param>
    /// <returns>Đã chuyển thành công hay chưa</returns>
    public bool ChangeTo(int index)
    {
        if (_sprites == null || index < 0 || index >= _sprites.Length) return false;
        if (_currentIndex == index) return true;
        _image.sprite = _sprites[index];
        _currentIndex = index;
        return true;
    }

    /// <summary>
    /// Lấy chỉ số Sprite đang hiển thị
    /// </summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>
    /// Lấy số lượng Sprite
    /// </summary>
    public int Count => _sprites?.Length ?? 0;
}
