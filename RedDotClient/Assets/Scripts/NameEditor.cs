using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NameEditor : MonoBehaviour
{
  [SerializeField] private InputField _inputField = null;

  private const string KEY = "nickname";

  private static NameEditor Instance { get; set; }
  public static string Name => Instance._inputField.text;

  public void UpdateName(string newName)
  {
    PlayerPrefs.SetString(KEY, newName);
  }

  private void Start()
  {
    Instance = this;
    _inputField.text = PlayerPrefs.GetString(KEY, SystemInfo.deviceName);
  }

}
