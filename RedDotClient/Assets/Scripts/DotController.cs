using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DotController : EventTrigger
{
  public long ID { private get; set; }
  
  private bool _isRed;
  public bool IsRed
  {
    set
    {
      _isRed = value;
      GetComponent<Image>().color = _isRed ? Color.red : Color.black;
    }
  }

  public override void OnPointerDown(PointerEventData eventData)
  {
    RedDotTcpClient.Instance.TouchDot(ID);
  }
}
