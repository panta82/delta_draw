using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Pantas.DeltaDraw.Core;
using System.Collections.Generic;
using System;

namespace Pantas.DeltaDraw.Application
{
	public abstract class DDStretchingRect : Control
	{
		#region StartPoint Property
		public static DependencyProperty StartPointProperty = DependencyProperty.Register(
			"StartPoint", typeof(Point), typeof(DDStretchingRect),
			new PropertyMetadata(new Point(), AnyPropertyChangedCallback));

		public Point StartPoint
		{
			get { return (Point)this.GetValue(StartPointProperty); }
			set { this.SetValue(StartPointProperty, value); }
		}
		#endregion

		#region EndPoint Property
		public static DependencyProperty EndPointProperty = DependencyProperty.Register(
			"EndPoint", typeof(Point), typeof(DDStretchingRect),
			new PropertyMetadata(new Point(), AnyPropertyChangedCallback));

		public Point EndPoint
		{
			get { return (Point)this.GetValue(EndPointProperty); }
			set { this.SetValue(EndPointProperty, value); }
		}
		#endregion

		#region Centered Property
		public static DependencyProperty CenteredProperty = DependencyProperty.Register(
			"Centered", typeof(bool), typeof(DDStretchingRect),
			new PropertyMetadata(false, AnyPropertyChangedCallback));
	
		public bool Centered
		{
			get { return (bool)this.GetValue(CenteredProperty); }
			set { this.SetValue(CenteredProperty, value); }
		}
		#endregion

		#region Proportional Property
		public static DependencyProperty ProportionalProperty = DependencyProperty.Register(
			"Proportional", typeof(bool), typeof(DDStretchingRect),
			new PropertyMetadata(false, AnyPropertyChangedCallback));
	
		public bool Proportional
		{
			get { return (bool)this.GetValue(ProportionalProperty); }
			set { this.SetValue(ProportionalProperty, value); }
		}
		#endregion

		public Rect Bounds
		{
			get
			{
				Point topLeft = new Point(Math.Min(StartPoint.X, EndPoint.X), Math.Min(StartPoint.Y, EndPoint.Y));
				Size size = new Size(Math.Abs(StartPoint.X - EndPoint.X), Math.Abs(StartPoint.Y - EndPoint.Y));
				if (Proportional)
				{
					double maxDim = Math.Max(size.Width, size.Height);
					if (StartPoint.X > EndPoint.X)
						topLeft.X -= (maxDim - size.Width);
					if (StartPoint.Y > EndPoint.Y)
						topLeft.Y -= (maxDim - size.Height);
					size = new Size(maxDim, maxDim);
				}
				if (Centered)
				{
					if (StartPoint.X < EndPoint.X)
						topLeft.X -= size.Width;
					if (StartPoint.Y < EndPoint.Y)
						topLeft.Y -= size.Height;
					size.Width *= 2;
					size.Height *= 2;
				}
				return new Rect(topLeft, size);
			}
			set
			{
				StartPoint = value.TopLeft;
				EndPoint = value.BottomRight;
			}
		}

		private static void AnyPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDStretchingRect sender = (DDStretchingRect)d;
			sender.ResizeToPoints();
		}

		protected virtual void ResizeToPoints()
		{
			Rect bounds = Bounds;
			Canvas.SetLeft(this, bounds.X);
			Canvas.SetTop(this, bounds.Y);
			Width = bounds.Width;
			Height = bounds.Height;
		}

		protected DDStretchingRect()
		{
		}

		protected DDStretchingRect(Point startPoint, Point endPoint)
			: this()
		{
			StartPoint = startPoint;
			EndPoint = endPoint;
		}
	}
	
	public class DDSelectionRect : DDStretchingRect
	{
		static DDSelectionRect()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DDSelectionRect),
				new FrameworkPropertyMetadata(typeof(DDSelectionRect)));
		}
		
		public DDSelectionRect()
			: this(new Point(), new Point())
		{
		}
		
		public DDSelectionRect(Point startPoint, Point endPoint)
			: base(startPoint, endPoint)
		{
			this.Background = new SolidColorBrush(Color.FromArgb(50, 80, 80, 200));
			this.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 80, 80, 200));
			this.BorderThickness = new Thickness(1);
		}
	}

	public class DDNewObjectRect : DDStretchingRect
	{
		static DDNewObjectRect()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DDNewObjectRect),
				new FrameworkPropertyMetadata(typeof(DDNewObjectRect)));
		}
	
		public DDObject Target { get; set; }
		
		public DDNewObjectRect()
			: this(new Point(), new Point())
		{
		}

		public DDNewObjectRect(Point startPoint, DDObject target)
			: this(startPoint, startPoint)
		{
			this.Target = Target;
		}

		public DDNewObjectRect(Point startPoint, Point endPoint)
			: base(startPoint, endPoint)
		{
			Background = Brushes.Transparent;
			BorderBrush = Brushes.Gray;
			BorderThickness = new Thickness(1);
		}

		protected override void ResizeToPoints()
		{
			base.ResizeToPoints();

			if (Target != null)
			{
				Target.Bounds = Bounds;
			}
		}
	}
}