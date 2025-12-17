using System;
using Twinny.Core;
using UnityEngine;

namespace Twinny.XR
{
    [Serializable]
    public class TwinnyXRRuntime : TwinnyRuntime
    {
        [Header("Safe Area")]
        [SerializeField]
        public bool showSafeArea = true;
        [SerializeField]
        public Vector2 safeAreaSize = new Vector2(2.5f, 1.5f);
        [SerializeField]
        public bool allowClickSafeAreaOutside = false;
        [Header("Acnhoring")]
        public float anchoringDistanceOffset = -1f;

    }

}
