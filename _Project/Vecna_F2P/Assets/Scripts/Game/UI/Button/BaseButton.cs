using UnityEngine;
using UnityEngine.UI;

public abstract class BaseButton : MonoBehaviour
{
    protected Button m_button;

    protected virtual void Awake()
    {
        if (TryGetComponent(out Button lButton))
        {
            m_button = lButton;
            m_button.onClick.AddListener(OnClick);
        }
        else Debug.LogWarning($"name : {gameObject.name} error : scrip {nameof(BaseButton)} is not in Button");
    }
    protected abstract void OnClick();
}
