using UnityEngine;

// Retain the serialized component for existing scenes. Empty slots keep their backgrounds.
public sealed class RelicPanelVisibility : MonoBehaviour
{
    [SerializeField, InspectorName("슬롯 배경 장식")] GameObject decoration;
    void OnEnable(){if(decoration!=null)decoration.SetActive(true);}
}