using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    public interface IStandalonePopupAction
    {
        string Header { get; }

        void Init();
        void NextItem();
        void PrevItem();
        void ExecuteCurrentItem();
    }
}