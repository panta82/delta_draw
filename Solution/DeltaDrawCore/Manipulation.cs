using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;

namespace Pantas.DeltaDraw.Core
{
	#region DDManipulationFrame tree
	public abstract class DDManipulationFrameBase : Panel, IDDHandleHolder
	{
		public DDTransformationCacheCollection Targets
		{
			get;
			protected set;
		}

		#region X Property
		public static DependencyProperty XProperty = DependencyProperty.Register(
			"X", typeof(double), typeof(DDManipulationFrameBase),
			new PropertyMetadata(0D, XPropertyChangedCallback));

		private static void XPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDManipulationFrameBase sender = (DDManipulationFrameBase)d;
			double newValue = (double)args.NewValue;
			
			Canvas.SetLeft(sender, newValue);
			sender.Bounds = new Rect(newValue, sender.Bounds.Y, sender.Bounds.Width, sender.Bounds.Height);
		}
	
		public double X
		{
			get { return (double)this.GetValue(XProperty); }
			set { this.SetValue(XProperty, value); }
		}
		#endregion

		#region Y Property
		public static DependencyProperty YProperty = DependencyProperty.Register(
			"Y", typeof(double), typeof(DDManipulationFrameBase),
			new PropertyMetadata(0D, YPropertyChangedCallback));

		private static void YPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDManipulationFrameBase sender = (DDManipulationFrameBase)d;
			double newValue = (double)args.NewValue;
			
			Canvas.SetTop(sender, newValue);
			sender.Bounds = new Rect(sender.Bounds.X, newValue, sender.Bounds.Width, sender.Bounds.Height);
		}
	
		public double Y
		{
			get { return (double)this.GetValue(YProperty); }
			set { this.SetValue(YProperty, value); }
		}
		#endregion

		#region Bounds Property
		public static DependencyProperty BoundsProperty = DependencyProperty.Register(
			"Bounds", typeof(Rect), typeof(DDManipulationFrameBase),
			new PropertyMetadata(new Rect(), BoundsPropertyChangedCallback));

		private static void BoundsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDManipulationFrameBase sender = (DDManipulationFrameBase)d;
			Rect newValue = (Rect)args.NewValue;

			sender.X = newValue.Left;
			sender.Y = newValue.Top;
			sender.Width = newValue.Width;
			sender.Height = newValue.Height;

			sender.OnBoundsChanged();
		}

		public Rect Bounds
		{
			get { return (Rect)this.GetValue(BoundsProperty); }
			set { this.SetValue(BoundsProperty, value); }
		}
		#endregion

		private DDManipulationFrameBase()
		{
			Targets = new DDTransformationCacheCollection();
			Targets.PropertyChanged += new PropertyChangedEventHandler(Targets_PropertyChanged);
			
			DependencyPropertyDescriptor.FromProperty(WidthProperty, typeof(DDManipulationFrameBase)).
				AddValueChanged(this, OnWidthOrHeightChangedHandler);
			DependencyPropertyDescriptor.FromProperty(HeightProperty, typeof(DDManipulationFrameBase)).
				AddValueChanged(this, OnWidthOrHeightChangedHandler);
		}

		protected DDManipulationFrameBase(params DDObject[] targets)
			: this()
		{
			foreach (DDObject target in targets)
				Targets.Add(target);
		}

		void OnWidthOrHeightChangedHandler(object sender, EventArgs e)
		{
			Bounds = new Rect(X, Y, Width, Height);
		}

		void Targets_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Count")
				OnTargetsChanged();
		}

		protected virtual void AdjustFromTargets()
		{
			Bounds = Targets.TargetBounds;
		}

		protected virtual void OnBoundsChanged()
		{
		}

		protected virtual void ApplyToTargets()
		{
		}

		protected virtual void OnTargetsChanged()
		{
			AdjustFromTargets();
		}

		protected override Size MeasureOverride(Size constraint)
		{		
			Size constrainedSize = new Size(
				Math.Min(Width, constraint.Width),
				Math.Min(Height, constraint.Height));
			if (double.IsNaN(constrainedSize.Width * constrainedSize.Height) ||
				double.IsInfinity(constrainedSize.Width * constrainedSize.Height))
				return new Size();
			else
			{
				Size childBox = new Size(double.PositiveInfinity, double.PositiveInfinity);
				foreach (UIElement child in Children)
					child.Measure(childBox);
				return constrainedSize;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{		
			if (Children.Count >= 1)
				PositionChild(Children[0], 0, 0, (DDPanelHandlePosition.Left | DDPanelHandlePosition.Top));
			if (Children.Count >= 2)
				PositionChild(Children[1], finalSize.Width, 0, (DDPanelHandlePosition.Right | DDPanelHandlePosition.Top));
			if (Children.Count >= 3)
				PositionChild(Children[2], finalSize.Width, finalSize.Height, (DDPanelHandlePosition.Right | DDPanelHandlePosition.Bottom));
			if (Children.Count >= 4)
				PositionChild(Children[3], 0, finalSize.Height, (DDPanelHandlePosition.Left | DDPanelHandlePosition.Bottom));
			if (Children.Count >= 5)
				PositionChild(Children[4], finalSize.Width / 2, 0, (DDPanelHandlePosition.Top));
			if (Children.Count >= 6)
				PositionChild(Children[5], finalSize.Width, finalSize.Height / 2, (DDPanelHandlePosition.Right));
			if (Children.Count >= 7)
				PositionChild(Children[6], finalSize.Width / 2, finalSize.Height, (DDPanelHandlePosition.Bottom));
			if (Children.Count >= 8)
				PositionChild(Children[7], 0, finalSize.Height / 2, (DDPanelHandlePosition.Left));
			return finalSize;
		}

		protected void PositionChild(UIElement child, double x, double y, DDPanelHandlePosition position)
		{
			double w = child.DesiredSize.Width;
			double h = child.DesiredSize.Height;
			Rect rect = new Rect(x - w / 2, y - w / 2, w, h);
			child.Arrange(rect);

			if (child is DDPanelHandle)
				(child as DDPanelHandle).Position = position;
		}

		#region IDDHandleHolder Members

		public virtual void ReportHandleManipulation(DDHandle source, Point delta)
		{
		}

		#endregion
	}

	public class DDTranslateAndScaleFrame : DDManipulationFrameBase
	{
		public DDTranslateAndScaleFrame(params DDObject[] targets)
			: base(targets)
		{
			for (int i = 0; i < 8; i++)
				Children.Add(new DDTSFrameHandle(this));
		}

		protected override void OnBoundsChanged()
		{
			base.OnBoundsChanged();

			ApplyToTargets();
		}

		protected override void ApplyToTargets()
		{
			Vector delta = Bounds.TopLeft - Targets.TargetBounds.TopLeft;
			Point factor = new Point(
				Targets.TargetBounds.Width != 0 ? Bounds.Width / Targets.TargetBounds.Width : 1,
				Targets.TargetBounds.Height != 0 ? Bounds.Height / Targets.TargetBounds.Height : 1);

			foreach (DDObjectTC objTC in Targets)
			{
				Vector objDistance = objTC.Bounds.TopLeft - Targets.TargetBounds.TopLeft;
				Vector objDeltaDistance = new Vector(objDistance.X * (factor.X - 1), objDistance.Y * (factor.Y - 1));
				Rect newObjBounds = new Rect(
					objTC.Bounds.X + delta.X + objDeltaDistance.X,
					objTC.Bounds.Y + delta.Y + objDeltaDistance.Y,
					objTC.Bounds.Width * factor.X,
					objTC.Bounds.Height * factor.Y);

				if (objTC is DDNodeObjectTC)
					foreach (DDNodeTC nodeTC in objTC as DDNodeObjectTC)
					{
						if ((nodeTC.Point == null) || (nodeTC.Target == null))
							continue;

						nodeTC.Target.StartChange(this.GetType().Name);
						nodeTC.Target.Point = new Point(
							newObjBounds.Left + factor.X * (nodeTC.Point.X - objTC.Bounds.X),
							newObjBounds.Top + factor.Y * (nodeTC.Point.Y - objTC.Bounds.Y));

						if (nodeTC.Control1 != null)
							nodeTC.Target.Control1 = new Point(
								newObjBounds.Left + factor.X * (nodeTC.Control1.X - objTC.Bounds.X),
								newObjBounds.Top + factor.Y * (nodeTC.Control1.Y - objTC.Bounds.Y));

						if (nodeTC.Control2 != null)
							nodeTC.Target.Control2 = new Point(
								newObjBounds.Left + factor.X * (nodeTC.Control2.X - objTC.Bounds.X),
								newObjBounds.Top + factor.Y * (nodeTC.Control2.Y - objTC.Bounds.Y));
						nodeTC.Target.EndChange();
					}
				else
					objTC.Target.Bounds = newObjBounds;
			}
		}

		public override void ReportHandleManipulation(DDHandle source, Point delta)
		{
			base.ReportHandleManipulation(source, delta);
			
			if (source is DDPanelHandle)
			{
				DDPanelHandle handle = source as DDPanelHandle;
				Rect newBounds = Bounds;
				if ((handle.Position & DDPanelHandlePosition.Left) == DDPanelHandlePosition.Left)
				{
					newBounds.X += delta.X;
					newBounds.Width = Math.Max(newBounds.Width - delta.X, 0);
				}
				if ((handle.Position & DDPanelHandlePosition.Top) == DDPanelHandlePosition.Top)
				{
					newBounds.Y += delta.Y;
					newBounds.Height = Math.Max(newBounds.Height - delta.Y, 0);
				}
				if ((handle.Position & DDPanelHandlePosition.Right) == DDPanelHandlePosition.Right)
					newBounds.Width = Math.Max(newBounds.Width + delta.X, 0);
				if ((handle.Position & DDPanelHandlePosition.Bottom) == DDPanelHandlePosition.Bottom)
					newBounds.Height = Math.Max(newBounds.Height + delta.Y, 0);
				Bounds = newBounds;
			}
		}
	} 
	#endregion

	#region DDTransformationCache tree

	public abstract class DDTransformationCacheBase<T>
		where T : DependencyObject
	{
		public T Target { get; set; }

		protected DDTransformationCacheBase()
		{
		}
		
		protected DDTransformationCacheBase(T target)
		{
			this.Target = target;
		}
		
		public abstract void ReadFromTarget();
		public abstract void ApplyToTarget();

		public virtual void Clear()
		{
			Target = null;
		}

		public static TCacheType CreateInitializedInstance<TCacheType>(T target)
			where TCacheType : DDTransformationCacheBase<T>, new()
		{
			TCacheType instance = new TCacheType();
			instance.Target = target;
			instance.ReadFromTarget();
			return instance;
		}
	}

	public class DDNodeTC : DDTransformationCacheBase<DDNode>
	{
		public DDPoint Point { get; protected set; }
		public DDPoint Control1 { get; protected set; }
		public DDPoint Control2 { get; protected set; }

		public DDNodeTC()
		{
		}
		
		public DDNodeTC(DDNode target)
			: base(target)
		{
		}

		public override void ReadFromTarget()
		{
			Point = Target.Point != null ? new DDPoint(Target.Point) : null;
			Control1 = Target.Control1 != null ? new DDPoint(Target.Control1) : null;
			Control2 = Target.Control2 != null ? new DDPoint(Target.Control2) : null;
		}

		public override void ApplyToTarget()
		{
			Target.StartChange(this.GetType().ToString());
			Target.Point = Point != null ? new DDPoint(Point) : null;
			Target.Control1 = Control1 != null ? new DDPoint(Control1) : null;
			Target.Control2 = Control2 != null ? new DDPoint(Control2) : null;
			Target.EndChange();
		}
	}

	public class DDObjectTC : DDTransformationCacheBase<DDObject>
	{
		public Rect Bounds { get; protected set; }

		public DDObjectTC()
		{
		}
		
		public DDObjectTC(DDObject target)
			: base(target)
		{
		}

		public override void ReadFromTarget()
		{
			Bounds = Target.Bounds;
		}

		public override void ApplyToTarget()
		{
			Target.Bounds = Bounds;
		}
	}

	public class DDNodeObjectTC : DDObjectTC, IEnumerable<DDNodeTC>
	{
		protected List<DDNodeTC> NodeStates = new List<DDNodeTC>();

		public DDNodeObjectTC()
		{
		}
		
		public DDNodeObjectTC(DDNodeObject target)
			: base(target)
		{
		}

		public override void ReadFromTarget()
		{
			base.ReadFromTarget();
			NodeStates.Clear();
			foreach (DDNode node in (Target as DDNodeObject).Nodes)
				NodeStates.Add(DDNodeTC.CreateInitializedInstance<DDNodeTC>(node));
		}

		public override void ApplyToTarget()
		{
			base.ApplyToTarget();
			foreach (DDNodeTC savedState in NodeStates)
				savedState.ApplyToTarget();
		}

		public override void Clear()
		{
			base.Clear();
			NodeStates.Clear();
		}

		#region IEnumerable<DDNodeTransformationState> Members

		public IEnumerator<DDNodeTC> GetEnumerator()
		{
			return NodeStates.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return NodeStates.GetEnumerator();
		}

		#endregion
	}

	#endregion
}