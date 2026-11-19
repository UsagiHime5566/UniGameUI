using System;
using System.Threading;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace UniGameUI
{
    public enum UITransitionPreset { None, Fade, Slide, RotateFade, Custom, Zoom }
    public enum UITransitionDirection { Left, Right, Up, Down }
    public enum UITransitionEase { Linear, InQuad, OutQuad, InOutQuad, OutBack }

    [Serializable]
    public sealed class UITransition
    {
        [SerializeField] internal UITransitionPreset preset;
        [SerializeField, Min(0)] internal float duration = 0.25f;
        [SerializeField] internal UITransitionEase ease = UITransitionEase.OutQuad;
        [SerializeField] internal UITransitionDirection direction = UITransitionDirection.Right;
        [SerializeField, Min(0)] internal float distance = 120f;
        [SerializeField] internal float angle = 12f;
        [SerializeField, Min(0.01f)] internal float zoomScale = 0.85f;

        public UITransitionPreset Preset => preset;
        public float Duration => duration;
        public UITransitionEase Ease => ease;
        public UITransitionDirection Direction => direction;
        public float Distance => distance;
        public float Angle => angle;
        public float ZoomScale => zoomScale;

        public static bool IsDOTweenReady
        {
            get
            {
#if DOTWEEN
                return true;
#else
                return false;
#endif
            }
        }

        internal static async Awaitable PlayAsync(
            UIView view, UITransition transition, bool entering, CancellationToken cancellation)
        {
            await Awaitable.MainThreadAsync();
            cancellation.ThrowIfCancellationRequested();
            if (transition.preset == UITransitionPreset.None ||
                (transition.preset != UITransitionPreset.Custom && transition.duration <= 0))
                return;

#if DOTWEEN
            var pose = new UIViewPose(view);
            Sequence sequence = null;
            CancellationTokenRegistration registration = default;
            var completed = false;
            var killed = false;
            try
            {
                sequence = CreateSequence(view, transition, entering, pose);
                var completion = new AwaitableCompletionSource();
                sequence.SetAutoKill(true);
                sequence.OnComplete(() =>
                {
                    completed = true;
                    completion.TrySetResult();
                });
                sequence.OnKill(() =>
                {
                    killed = true;
                    if (!completed)
                        completion.TrySetCanceled();
                });
                // Navigation starts on Unity's main thread. DOTween always updates there.
                registration = cancellation.Register(() => completion.TrySetCanceled());
                cancellation.ThrowIfCancellationRequested();
                sequence.Play();
                await completion.Awaitable;
            }
            finally
            {
                registration.Dispose();
                await Awaitable.MainThreadAsync();
                if (!completed && !killed)
                    sequence?.Kill();
                pose.Restore(view);
            }
#else
            // Without DOTween the transition switches instantly so navigation keeps working.
            if (!warnedMissingDOTween)
            {
                warnedMissingDOTween = true;
                Debug.LogWarning("UniGameUI transitions switch instantly until DOTween is imported and " +
                    "Setup DOTween has run.", view);
            }
#endif
        }

#if !DOTWEEN
        private static bool warnedMissingDOTween;
#endif

#if DOTWEEN
        internal static Sequence CreateSequence(UIView view, UITransition transition, bool entering, UIViewPose pose)
        {
            var sequence = DOTween.Sequence().Pause();
            try
            {
                sequence.SetUpdate(true);
                if (transition.preset == UITransitionPreset.Custom)
                {
                    var custom = view.GetComponent<UIDOTweenAnimation>();
                    if (!custom)
                        throw new InvalidOperationException($"{view.name} needs a UIDOTweenAnimation component.");
                    custom.Build(sequence, view, entering);
                    return sequence;
                }

                var tweenEase = transition.ease switch
                {
                    UITransitionEase.InQuad => DG.Tweening.Ease.InQuad,
                    UITransitionEase.OutQuad => DG.Tweening.Ease.OutQuad,
                    UITransitionEase.InOutQuad => DG.Tweening.Ease.InOutQuad,
                    UITransitionEase.OutBack => DG.Tweening.Ease.OutBack,
                    _ => DG.Tweening.Ease.Linear
                };
                if (transition.preset == UITransitionPreset.Fade ||
                    transition.preset == UITransitionPreset.RotateFade)
                {
                    view.Group.alpha = entering ? 0 : pose.Alpha;
                    sequence.Join(DOTween.To(() => view.Group.alpha, value => view.Group.alpha = value,
                        entering ? pose.Alpha : 0, transition.duration).SetEase(tweenEase));
                }

                if (transition.preset == UITransitionPreset.Slide)
                {
                    var direction = transition.direction switch
                    {
                        UITransitionDirection.Left => Vector2.left,
                        UITransitionDirection.Up => Vector2.up,
                        UITransitionDirection.Down => Vector2.down,
                        _ => Vector2.right
                    };
                    var offset = pose.Position + direction * transition.distance;
                    view.Motion.anchoredPosition = entering ? offset : pose.Position;
                    sequence.Join(DOTween.To(() => view.Motion.anchoredPosition,
                        value => view.Motion.anchoredPosition = value,
                        entering ? pose.Position : offset, transition.duration).SetEase(tweenEase));
                }

                if (transition.preset == UITransitionPreset.RotateFade)
                {
                    var offset = pose.Rotation * Quaternion.Euler(0, 0, transition.angle);
                    view.Motion.localRotation = entering ? offset : pose.Rotation;
                    sequence.Join(view.Motion.DOLocalRotateQuaternion(
                        entering ? pose.Rotation : offset, transition.duration).SetEase(tweenEase));
                }

                if (transition.preset == UITransitionPreset.Zoom)
                {
                    var scaled = pose.Scale * transition.zoomScale;
                    view.Motion.localScale = entering ? scaled : pose.Scale;
                    sequence.Join(view.Motion.DOScale(entering ? pose.Scale : scaled, transition.duration)
                        .SetEase(tweenEase));
                }

                return sequence;
            }
            catch
            {
                sequence.Kill();
                throw;
            }
        }
#endif
    }

    internal readonly struct UIViewPose
    {
        public readonly Vector2 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
        public readonly float Alpha;

        public UIViewPose(UIView view)
        {
            Position = view.Motion.anchoredPosition;
            Rotation = view.Motion.localRotation;
            Scale = view.Motion.localScale;
            Alpha = view.Group.alpha;
        }

        public void Restore(UIView view)
        {
            if (!view || !view.Motion || !view.Group)
                return;
            view.Motion.anchoredPosition = Position;
            view.Motion.localRotation = Rotation;
            view.Motion.localScale = Scale;
            view.Group.alpha = Alpha;
        }
    }
}
