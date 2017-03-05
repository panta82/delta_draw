using System;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Pantas.DeltaDraw.Core
{
	public interface IDDHandleHolder
	{
		void ReportHandleManipulation(DDHandle handle, Point coords);
	}
	
	public abstract class DDHandle : Control
	{
		#region IsBeingDragged Property
		public static DependencyPropertyKey IsBeingDraggedPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsBeingDragged", typeof(bool), typeof(DDHandle), new PropertyMetadata(false));

		public static DependencyProperty IsBeingDraggedProperty = IsBeingDraggedPropertyKey.DependencyProperty;

		public bool IsBeingDragged
		{
			get { return (bool)this.GetValue(IsBeingDraggedProperty); }
			private set { this.SetValue(IsBeingDraggedPropertyKey, value); }
		}
		#endregion
		
		public IDDHandleHolder Holder { get; set; }
		
		protected DDHandle(IDDHandleHolder holder)
		{
			this.Holder = holder;
		}

		protected void StartDrag()
		{
			CaptureMouse();
			IsBeingDragged = true;
		}

		protected void EndDrag()
		{
			IsBeingDragged = false;
			ReleaseMouseCapture();
		}

		public virtual void ReportDrag(Point p)
		{
			p.X -= Width / 2;
			p.Y -= Height / 2;
			OnManipulationByUser(p);
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			if ((e.LeftButton == MouseButtonState.Pressed) && (e.RightButton == MouseButtonState.Released))
				StartDrag();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			
			if ((e.LeftButton == MouseButtonState.Pressed) && (e.RightButton == MouseButtonState.Released))
			{
				if (IsBeingDragged)
				{
					Point p = e.GetPosition(this);
					p.X -= Width / 2;
					p.Y -= Height / 2;
					OnManipulationByUser(p);
				}
			}
			else
				EndDrag();
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			
			if (e.LeftButton == MouseButtonState.Released)
				EndDrag();
		}

		protected virtual void OnManipulationByUser(Point delta)
		{
		}
	}

	[Flags]
	public enum DDPanelHandlePosition
	{
		None = 0x0,
		Left = 0x1,
		Top = 0x2,
		Right = 0x4,
		Bottom = 0x8
	};
	
	public abstract class DDPanelHandle : DDHandle
	{
		public DDPanelHandlePosition Position { get; set; }

		protected DDPanelHandle(IDDHandleHolder holder)
			: base(holder)
		{
			Position = DDPanelHandlePosition.None;
		}

		protected override void OnManipulationByUser(Point delta)
		{
			Holder.ReportHandleManipulation(this, delta);
		}
	}

	public class DDTSFrameHandle : DDPanelHandle
	{	
		static DDTSFrameHandle()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DDTSFrameHandle),
				new FrameworkPropertyMetadata(typeof(DDTSFrameHandle)));
		}
		
		public DDTSFrameHandle(IDDHandleHolder holder)
			: base(holder)
		{
			Position = DDPanelHandlePosition.None;
			Width = 10;
			Height = 10;
		}
	}

	public class DDPositionableHandle : DDHandle
	{
		#region Position Property
		public static DependencyProperty PositionProperty = DependencyProperty.Register(
			"Position", typeof(DDPoint), typeof(DDPositionableHandle),
			new PropertyMetadata(null, PositionPropertyChangedCallback));

		private static void PositionPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDPositionableHandle sender = d as DDPositionableHandle;
			sender.OnPositionChanged();
		}

		public DDPoint Position
		{
			get { return (DDPoint)this.GetValue(PositionProperty); }
			set { this.SetValue(PositionProperty, value); }
		}
		#endregion
		
		protected DDPositionableHandle(IDDHandleHolder holder)
			: base(holder)
		{
		}

		protected override void OnManipulationByUser(Point delta)
		{
			if (Position != null)
				Position = new DDPoint(Position.X + delta.X, Position.Y + delta.Y);
		}

		protected virtual void OnPositionChanged()
		{
			if (Holder == null)
				return;

			if ((Width > 0) && (Height > 0) && (Position != null))
			{
				Canvas.SetLeft(this, Position.X - Width / 2f);
				Canvas.SetTop(this, Position.Y - Height / 2f);
				Visibility = Visibility.Visible;
				Holder.ReportHandleManipulation(this, Position.Point);
			}
			else
				Visibility = Visibility.Hidden;
		}
	}

	public class DDPointHandle : DDPositionableHandle
	{
		static DDPointHandle()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DDPointHandle),
				new FrameworkPropertyMetadata(typeof(DDPointHandle)));
		}
		
		public DDPointHandle(IDDHandleHolder holder)
			: base(holder)
		{
			Width = 9;
			Height = 9;
		}
	}

	public class DDControl1Handle : DDPositionableHandle
	{
		static DDControl1Handle()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DDControl1Handle),
				new FrameworkPropertyMetadata(typeof(DDControl1Handle)));
		}
		
		public DDControl1Handle(IDDHandleHolder holder)
			: base(holder)
		{
			Width = 7;
			Height = 7;
		}
	}

	public class DDControl2Handle : DDPositionableHandle
	{
		static DDControl2Handle()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DDControl2Handle),
				new FrameworkPropertyMetadata(typeof(DDControl2Handle)));
		}
	
		public DDControl2Handle(IDDHandleHolder holder)
			: base(holder)
		{
			Width = 6;
			Height = 6;
		}
	}

	public class DDBoundControlLine : Shape
	{
		protected LineGeometry StoredGeometry = new LineGeometry();

		#region StartObject Property
		public static DependencyProperty StartObjectProperty = DependencyProperty.Register(
			"StartObject", typeof(DependencyObject), typeof(DDBoundControlLine),
			new PropertyMetadata(null, AnyPropertyChangedCallback));

		public DependencyObject StartObject
		{
			get { return (DependencyObject)this.GetValue(StartObjectProperty); }
			set { this.SetValue(StartObjectProperty, value); }
		}
		#endregion

		#region StartProperty Property
		public static DependencyProperty StartPropertyProperty = DependencyProperty.Register(
			"StartProperty", typeof(string), typeof(DDBoundControlLine),
			new PropertyMetadata("", AnyPropertyChangedCallback));

		public string StartProperty
		{
			get { return (string)this.GetValue(StartPropertyProperty); }
			set { this.SetValue(StartPropertyProperty, value); }
		}
		#endregion

		#region EndNode Property
		public static DependencyProperty EndObjectProperty = DependencyProperty.Register(
			"EndObject", typeof(DependencyObject), typeof(DDBoundControlLine),
			new PropertyMetadata(null, AnyPropertyChangedCallback));

		public DependencyObject EndObject
		{
			get { return (DependencyObject)this.GetValue(EndObjectProperty); }
			set { this.SetValue(EndObjectProperty, value); }
		}
		#endregion

		#region EndProperty Property
		public static DependencyProperty EndPropertyProperty = DependencyProperty.Register(
			"EndProperty", typeof(string), typeof(DDBoundControlLine),
			new PropertyMetadata("", AnyPropertyChangedCallback));

		public string EndProperty
		{
			get { return (string)this.GetValue(EndPropertyProperty); }
			set { this.SetValue(EndPropertyProperty, value); }
		}
		#endregion

		private static void AnyPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			(sender as DDBoundControlLine).SetupGeometry();
		}

		public DDBoundControlLine()
		{
			Stroke = Brushes.SkyBlue;
			StrokeThickness = 1;
			StrokeDashArray = new DoubleCollection { 1, 1 };
			StrokeDashCap = PenLineCap.Round;
			IsHitTestVisible = false;
		}

		public DDBoundControlLine(DependencyObject startObject, string startProperty,
			DependencyObject endObject, string endProperty)
			: this()
		{
			this.StartObject = startObject;
			this.StartProperty = startProperty;
			this.EndObject = endObject;
			this.EndProperty = endProperty;
		}

		protected void SetupGeometry()
		{
			BindingOperations.ClearAllBindings(this);
			
			StoredGeometry = new LineGeometry();

			if ((StartObject != null) && (EndObject != null)
				&& (!string.IsNullOrEmpty(StartProperty))
				&& (!string.IsNullOrEmpty(EndProperty)))
			{
				// Must use BindingOperations since Geometry doesn't have SetBinding.
				// Why? Performance penalty?

				Binding b1 = new Binding(StartProperty) { Source = StartObject };
				BindingOperations.SetBinding(StoredGeometry, LineGeometry.StartPointProperty, b1);

				Binding b2 = new Binding(EndProperty) { Source = EndObject };
				BindingOperations.SetBinding(StoredGeometry, LineGeometry.EndPointProperty, b2);

				// If any of the nodes is hidden or collapsed, we shouldn't see the line either.

				MultiBinding visBindingGroup = new MultiBinding();
				visBindingGroup.Bindings.Add(new Binding
					{
						Source = StartObject,
						Path = new PropertyPath("Visibility")
					});
				visBindingGroup.Bindings.Add(new Binding
				{
					Source = EndObject,
					Path = new PropertyPath("Visibility")
				});
				visBindingGroup.Converter = new ConnectedVisibilityConverter();
				this.SetBinding(VisibilityProperty, visBindingGroup);
			}
		}

		protected override Geometry DefiningGeometry
		{
			get { return StoredGeometry; }
		}
	}
}