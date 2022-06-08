using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Artanim
{

    [RequireComponent(typeof(Text))]
    public class FpsUI : MonoBehaviour
    {
        private Text _TextField;
        private Text TextField
        {
            get
            {
                if (!_TextField)
                    _TextField = GetComponent<Text>();
                return _TextField;
            }
        }


        void Update()
        {
                TextField.text = string.Format("{0:0} FPS", FPSMetrics.FpsAvg);
        }

    }
}