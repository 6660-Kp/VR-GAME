using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR.Extras;

public class Laser : MonoBehaviour
{
    // Start is called before the first frame update
    private SteamVR_LaserPointer laser;
    private GameObject element;
    public bool isEnable = true;

    private void Awake()
    {
        laser = GetComponent<SteamVR_LaserPointer>();
        if (laser != null)
        {
            if (!isEnable)
            {
                laser.enabled = false;
                return;
            }
            else
            {
                laser.PointerIn += LaserOnPointerIn;
                laser.PointerOut += LaserOnPointerOut;
                laser.PointerClick += LaserOnPointerClick;
            }
        }
    }

    private void LaserOnPointerClick(object sender, PointerEventArgs e)
    {
        IPointerClickHandler handler = e.target.gameObject.GetComponent<IPointerClickHandler>();
        if (handler != null)
        {
            handler.OnPointerClick(new PointerEventData(EventSystem.current));
        }
    }

    private void LaserOnPointerOut(object sender, PointerEventArgs e)
    {
        IPointerExitHandler handler = e.target.gameObject.GetComponent<IPointerExitHandler>();
        if (handler != null)
        {
            handler.OnPointerExit(new PointerEventData(EventSystem.current));
        }
    }

    private void LaserOnPointerIn(object sender, PointerEventArgs e)
    {
        IPointerEnterHandler handler = e.target.gameObject.GetComponent<IPointerEnterHandler>();
        if (handler != null)
        {
            handler.OnPointerEnter(new PointerEventData(EventSystem.current));
        }
    }
}
