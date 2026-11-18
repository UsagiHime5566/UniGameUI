#if DOTWEEN
using DG.Tweening;
using UnityEngine;

namespace UniGameUI
{
    public abstract class UIDOTweenAnimation : MonoBehaviour
    {
        public abstract void Build(Sequence sequence, UIView view, bool entering);
    }
}
#endif
