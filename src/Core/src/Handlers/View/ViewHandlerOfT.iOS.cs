using Microsoft.Maui.Graphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui.Handlers
{
	public partial class ViewHandler<TVirtualView, TPlatformView> : NSObject, IPlatformViewHandler where TPlatformView : UIView
	{
		public new WrapperView? ContainerView
		{
			get => (WrapperView?)base.ContainerView;
			protected set => base.ContainerView = value;
		}

		public UIViewController? ViewController { get; set; }

		public override void PlatformArrange(Rect rect) =>
			this.PlatformArrangeHandler(rect);

		public override Size GetDesiredSize(double widthConstraint, double heightConstraint) =>
			this.GetDesiredSizeFromHandler(widthConstraint, heightConstraint);

		protected override void SetupContainer()
		{
			if (PlatformView == null || ContainerView != null)
				return;

			var oldParent = (UIView?)PlatformView.Superview;

			var oldIndex = oldParent?.IndexOfSubview(PlatformView);
			PlatformView.RemoveFromSuperview();

			ContainerView ??= new WrapperView(PlatformView.Bounds);
			ContainerView.AddSubview(PlatformView);

			if (oldIndex is int idx && idx >= 0)
				oldParent?.InsertSubview(ContainerView, idx);
			else
				oldParent?.AddSubview(ContainerView);
		}

		protected override void RemoveContainer()
		{
			if (PlatformView == null || ContainerView == null || PlatformView.Superview != ContainerView)
			{
				CleanupContainerView(ContainerView);
				ContainerView = null;
				return;
			}

			var oldParent = (UIView?)ContainerView.Superview;

			var oldIndex = oldParent?.IndexOfSubview(ContainerView);
			CleanupContainerView(ContainerView);
			ContainerView = null;

			if (oldIndex is int idx && idx >= 0)
				oldParent?.InsertSubview(PlatformView, idx);
			else
				oldParent?.AddSubview(PlatformView);

			void CleanupContainerView(UIView? containerView)
			{
				if (containerView is WrapperView wrapperView)
				{
					wrapperView.RemoveFromSuperview();
					wrapperView.Dispose();
				}
			}
		}

		protected void RegisterToNotifications (TPlatformView view, string? notificationName=null) {
			NSNotificationCenter.DefaultCenter.AddObserver (this, new ObjCRuntime.Selector("onNativeNotification:"), notificationName, view);
		}

		// methods that will allow to receive nsnotifications from views, this way
		// we do not need to have a event handler from the view directly.
		// The main idea between this solution is to remove the need to have an event handler
		// in the view that will have a reference to the handler and therefore create a circular ref
		[Export ("onNativeNotification:")]
		private void InternalOnNativeNotification (NSNotification notification)
		{
			// there are two types of notifications we are interested:
			//
			//  1. When the native object is disposed (should be implemented by the inherting maui types)
			//  2. All other notifications.
			//
			// 1. is very important, we want to remove ourselfs from the NSNotification center, 2 we fwd to the virtual method  
			if (notification.Name == "Disposing") {
				NSNotificationCenter.DefaultCenter.RemoveObserver (this);
				OnNativeViewDisposed ();
			} else {
				OnNativeViewChanged (notification);
			}
		}

		protected virtual void OnNativeViewDisposed ()
		{
		}

		protected virtual void OnNativeViewChanged (NSNotification notification)
		{
		}

		public override Dispose()
		{
			// we need to remove ourselfs from the NSNotification center
			NSNotificationCenter.DefaultCenter.RemoveObserver (this);
			base.Dispose();
		}
	}
}
