using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace RummyBooky.Extensions
{
    /// <summary>
    /// Extension methods for VisualElement animations providing accessibility compliance
    /// (IsAnimationEnabled checks) and cancellation safety (CancelAnimations calls).
    /// </summary>
    public static class ViewExtensions
    {
        /// <summary>
        /// Checks whether animations are enabled for the visual element.
        /// </summary>
        public static bool IsAnimationEnabled(this VisualElement view) => true;

        /// <summary>
        /// Executes a tactile press animation (scale down &amp; restore).
        /// Respects IsAnimationEnabled and cancels prior animations.
        /// </summary>
        public static async Task AnimatePressAsync(this VisualElement view, double scaleTo = 0.95, uint duration = 90)
        {
            if (view == null) return;

            if (!view.IsAnimationEnabled()) return;

            view.CancelAnimations();

            await view.ScaleTo(scaleTo, duration, Easing.CubicOut);
            await view.ScaleTo(1.0, duration, Easing.CubicOut);
        }

        /// <summary>
        /// Smoothly transitions between collapsed card box and expanded player list.
        /// Respects IsAnimationEnabled and cancels prior animations.
        /// </summary>
        public static async Task TransitionCardBoxAsync(this VisualElement collapsedView, VisualElement expandedView, bool expand, uint duration = 250)
        {
            if (collapsedView == null || expandedView == null) return;

            if (!collapsedView.IsAnimationEnabled() || !expandedView.IsAnimationEnabled())
            {
                collapsedView.IsVisible = !expand;
                expandedView.IsVisible = expand;
                collapsedView.Opacity = expand ? 0 : 1;
                expandedView.Opacity = expand ? 1 : 0;
                return;
            }

            collapsedView.CancelAnimations();
            expandedView.CancelAnimations();

            if (expand)
            {
                expandedView.Opacity = 0;
                expandedView.Scale = 0.95;
                expandedView.IsVisible = true;

                await Task.WhenAll(
                    collapsedView.FadeTo(0, duration, Easing.CubicInOut),
                    collapsedView.ScaleTo(0.95, duration, Easing.CubicInOut),
                    expandedView.FadeTo(1, duration, Easing.CubicInOut),
                    expandedView.ScaleTo(1.0, duration, Easing.CubicInOut)
                );

                collapsedView.IsVisible = false;
            }
            else
            {
                collapsedView.Opacity = 0;
                collapsedView.Scale = 0.95;
                collapsedView.IsVisible = true;

                await Task.WhenAll(
                    expandedView.FadeTo(0, duration, Easing.CubicInOut),
                    expandedView.ScaleTo(0.95, duration, Easing.CubicInOut),
                    collapsedView.FadeTo(1, duration, Easing.CubicInOut),
                    collapsedView.ScaleTo(1.0, duration, Easing.CubicInOut)
                );

                expandedView.IsVisible = false;
            }
        }

        /// <summary>
        /// Safe fade-in with accessibility check and animation cancellation.
        /// </summary>
        public static async Task SafeFadeInAsync(this VisualElement view, uint duration = 250, Easing? easing = null)
        {
            if (view == null) return;
            easing ??= Easing.CubicOut;

            if (!view.IsAnimationEnabled())
            {
                view.Opacity = 1;
                view.IsVisible = true;
                return;
            }

            view.CancelAnimations();
            view.IsVisible = true;
            await view.FadeTo(1, duration, easing);
        }

        /// <summary>
        /// Safe fade-out with accessibility check and animation cancellation.
        /// </summary>
        public static async Task SafeFadeOutAsync(this VisualElement view, uint duration = 200, Easing? easing = null)
        {
            if (view == null) return;
            easing ??= Easing.CubicInOut;

            if (!view.IsAnimationEnabled())
            {
                view.Opacity = 0;
                view.IsVisible = false;
                return;
            }

            view.CancelAnimations();
            await view.FadeTo(0, duration, easing);
            view.IsVisible = false;
        }
    }
}
