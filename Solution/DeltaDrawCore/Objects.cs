using System;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Pantas.DeltaDraw.Core
{
	public enum DDManipulationState { Normal, Edited }

	[ContentProperty("Points")]
	[Serializable]
	public abstract class DDObject : Shape, INotifyPropertyChanged, ICloneable, ISerializable
	{
		public static DDObject TryCreateFromCLR(object clrSource)
		{
			if (!(clrSource is Shape))
				return null;

			DDObject newObject = null;
			if (clrSource is Path)
			{
				if (((clrSource as Path).Data is PathGeometry) &&
					(((clrSource as Path).Data as PathGeometry).Figures.Count > 0))
				{
					if (((clrSource as Path).Data as PathGeometry).Figures[0].IsFilled)
						newObject = new DDPolygon();
					else
						newObject = new DDPolyline();
				}
			}
			if (clrSource is Rectangle)
				newObject = new DDRectangle();
			if (clrSource is Ellipse)
				newObject = new DDEllipse();
				
			if (newObject != null)
				if (newObject.SetFromCLR(clrSource as Shape))
					return newObject;

			return null;
		}
		
		protected Geometry StoredGeometry = new RectangleGeometry();

		#region X Property
		public static readonly DependencyProperty XProperty = DependencyProperty.Register(
			"X", typeof(double), typeof(DDObject), new FrameworkPropertyMetadata(
			0D, AnyBoundsPropertyChangedCallback));

		public double X
		{
			get { return (double)this.GetValue(XProperty); }
			set { this.SetValue(XProperty, value); }
		} 
		#endregion

		#region Y Property
		public static readonly DependencyProperty YProperty = DependencyProperty.Register(
			"Y", typeof(double), typeof(DDObject), new FrameworkPropertyMetadata(
			0D, AnyBoundsPropertyChangedCallback));

		public double Y
		{
			get { return (double)this.GetValue(YProperty); }
			set { this.SetValue(YProperty, value); }
		}
		#endregion

		// Old Width and Height properties are rewritten because their tendency to clip the defining geometry
		// isn't useful for this purpose and might in fact make future maintenance harder.
		// Actual width and height are being maintained automatically by the Shape class and aren't used in this program.

		#region Width Property
		public new static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
			"Width", typeof(double), typeof(DDObject), new FrameworkPropertyMetadata(
			0D, AnyBoundsPropertyChangedCallback, AnySizePropertyCoerceValueCallback));

		public new double Width
		{
			get { return (double)this.GetValue(WidthProperty); }
			set { this.SetValue(WidthProperty, value); }
		}
		#endregion

		#region Height Property
		public new static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
			"Height", typeof(double), typeof(DDObject), new FrameworkPropertyMetadata(
			0D, AnyBoundsPropertyChangedCallback, AnySizePropertyCoerceValueCallback));

		public new double Height
		{
			get { return (double)this.GetValue(HeightProperty); }
			set { this.SetValue(HeightProperty, value); }
		}
		#endregion

		#region ManipulationState Property
		public static DependencyProperty ManipulationStateProperty = DependencyProperty.Register(
			"ManipulationState", typeof(DDManipulationState), typeof(DDObject),
			new PropertyMetadata(DDManipulationState.Normal,
				StatePropertyChangedCallback));

		private static void StatePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDObject sender = (DDObject)d;
			DDManipulationState oldValue = (DDManipulationState)args.OldValue;

			sender.OnManipulationStateChanged(oldValue);
		}

		public DDManipulationState ManipulationState
		{
			get { return (DDManipulationState)this.GetValue(ManipulationStateProperty); }
			set { this.SetValue(ManipulationStateProperty, value); }
		}
		#endregion

		private static object AnySizePropertyCoerceValueCallback(DependencyObject d, object baseValue)
		{
			return Math.Max((double)baseValue, 1D);
		}

		private static void AnyBoundsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDObject sender = (DDObject)d;
			sender.OnBoundsChanged();
		}

		public Point Position
		{
			get { return new Point(X, Y); }
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}

		public Size Size
		{
			get { return new Size(Width, Height); }
			set
			{
				Width = value.Width;
				Height = value.Height;
			}
		}

		public Rect Bounds
		{
			get { return new Rect(X, Y, Width, Height); }
			set
			{
				X = value.X;
				Y = value.Y;
				Width = value.Width;
				Height = value.Height;
			}
		}

		public string ChangeSource { get; protected set; }

		protected DDObject()
		{
			DependencyPropertyDescriptor.FromProperty(FillProperty, typeof(DDObject)).AddValueChanged(this,
				(sender, e) => OnPropertyChanged("Fill"));
			DependencyPropertyDescriptor.FromProperty(StrokeProperty, typeof(DDObject)).AddValueChanged(this,
				(sender, e) => OnPropertyChanged("Stroke"));
			DependencyPropertyDescriptor.FromProperty(StrokeThicknessProperty, typeof(DDObject)).AddValueChanged(this,
				(sender, e) =>  OnPropertyChanged("StrokeThickness"));
		}

		protected virtual void SetupGeometry()
		{
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void OnBoundsChanged()
		{
			OnPropertyChanged("Bounds");
			if (BoundsChanged != null)
				BoundsChanged(this, null);
		}

		protected virtual void OnManipulationStateChanged(DDManipulationState oldState)
		{
			OnPropertyChanged("State");
		}

		public virtual bool StrokeContains(Point point, double tolerance)
		{
			// This is just the first eliminatory test. Inheriting classes must extend this to be more precise.
			Rect bounds = new Rect(X - tolerance, Y - tolerance, Width + tolerance, Height + tolerance);
			return bounds.Contains(point);
		}

		public abstract Point ClosestPoint(Point refPoint);
	
		protected override Geometry DefiningGeometry
		{
			get { return StoredGeometry; }
		}

		protected virtual void CopyToCLR(Shape target)
		{
			Canvas.SetLeft(target, X);
			Canvas.SetTop(target, Y);
			target.Width = Width;
			target.Height = Height;
			if (Fill != null)
				target.Fill = Fill.Clone();
			if (Stroke != null)
				target.Stroke = Stroke.Clone();
			target.StrokeThickness = StrokeThickness;
			//TODO: Add more when needed!
		}

		public virtual bool SetFromCLR(Shape source)
		{
			X = Canvas.GetLeft(source);
			Y = Canvas.GetTop(source);
			Width = source.Width;
			Height = source.Height;
			if (source.Fill != null)
				Fill = source.Fill.Clone();
			else
				Fill = null;
			if (source.Stroke != null)
				Stroke = source.Stroke.Clone();
			else
				Stroke = null;
			StrokeThickness = source.StrokeThickness;
			return true;
			
			//TODO: Add more when needed!
		}

		public event EventHandler BoundsChanged;

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region ICloneable Members

		public abstract object Clone();
		
		protected virtual void CloneTo(DDObject target)
		{
			target.X = X;
			target.Y = Y;
			target.Width = Width;
			target.Height = Height;
			if (Fill != null)
				target.Fill = Fill.Clone();
			if (Stroke != null)
				target.Stroke = Stroke.Clone();
			target.StrokeThickness = StrokeThickness;
			//TODO: Add more when needed!
		}

		public abstract Shape CloneToCLR();

		#endregion

		#region ISerializable Members

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("X", X);
			info.AddValue("Y", Y);
			info.AddValue("Width", Width);
			info.AddValue("Height", Height);

			//TODO: Add more when needed!
		}

		#endregion
	}

	[ContentProperty("Nodes")]
	public abstract class DDNodeObject : DDObject
	{
		#region Nodes Property
		private static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
			"Nodes", typeof(DDNodeCollection), typeof(DDNodeObject),
			new PropertyMetadata(null, PointsPropertyChangedCallback));

		private static void PointsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			DDNodeObject realSender = sender as DDNodeObject;
			if (realSender == null) return;
			
			// Setup callbacks for adding and removing nodes.
			// FreezableCollection could have done this automatically, but then we wouldn't have had
			// advanced features of INotifyCollectionChanged (eg. lists of affected nodes).

			if (args.OldValue != null)
				((DDNodeCollection)args.OldValue).CollectionChanged -= realSender.Nodes_CollectionChanged;

			if (args.NewValue != null)
				((DDNodeCollection) args.NewValue).CollectionChanged += realSender.Nodes_CollectionChanged;

			realSender.SetupGeometry();
		}

		public DDNodeCollection Nodes
		{
			get { return (DDNodeCollection)this.GetValue(NodesProperty); }
			set { this.SetValue(NodesProperty, value); }
		}
		#endregion

		protected bool IsFilled { get; set; }

		protected DDNodeObject()
		{
			Nodes = new DDNodeCollection();
		}

		/// <summary>
		/// Handles the CollectionChanged event of the Nodes control.
		/// </summary>
		/// <remarks>
		/// Maintains two parenthoods:
		/// 1) Canvas that owns DDObject must also own its nodes
		/// 2) Each node must know it is a part of this DDObject (ParentObject property)
		/// 
		/// Also, makes sure nodes are informing DDObject about their internal changes by
		/// attaching/detaching appropriate event handlers.
		/// </remarks>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
		protected void Nodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{					
			if (e.OldItems != null)
				foreach (DDNode node in e.OldItems)
				{
					node.ParentObject = null;
					if (Parent is Canvas)
						(Parent as Canvas).Children.Remove(node);
					node.SegmentModified -= AnyNodeSegmentModified;
					node.SegmentLost -= AnyNodeSegmentLost;
				}
			
			if (e.NewItems != null)
				foreach (DDNode node in e.NewItems)
				{
					node.ParentObject = this;
					if (Parent is Canvas)
						(Parent as Canvas).Children.Add(node);
					node.SegmentModified += AnyNodeSegmentModified;
					node.SegmentLost += AnyNodeSegmentLost;
				}

			OnNodesCollectionChanged();
		}

		/// <summary>
		/// Handles the SegmentModified event of any node.
		/// </summary>
		/// <remarks>
		/// This happens when segment is modified within the node, and THAT happens when one of the defining
		/// points (Point, Control1 or Control2) is moved, or isStroked is changed.
		/// </remarks>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void AnyNodeSegmentModified(object sender, EventArgs e)
		{
			OnNodesChanged();
		}

		/// <summary>
		/// Handles the SegmentLost event of any node.
		/// </summary>
		/// <remarks>
		/// If a segment is lost (set to null from within the node), we need to reform the geometry (without
		/// that segment) and recalculate bounds. This is pretty much what needs to happen when we add
		/// or remove a node, so this is the handler we call.
		/// 
		/// Note that ATM there's no way a user can make a node 'lose' its segment,
		/// but technically, the possibility exists, so we must plan ahead for it.
		/// </remarks>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void AnyNodeSegmentLost(object sender, EventArgs e)
		{
			OnNodesCollectionChanged();
		}

		/// <summary>
		/// Invoked when the parent of this element in the visual tree is changed. Overrides <see cref="M:System.Windows.UIElement.OnVisualParentChanged(System.Windows.DependencyObject)"/>.
		/// </summary>
		/// <remarks>
		/// On adding or removing this object from Canvas, we must also do so with its nodes.
		/// Repeats some of the functionality from Nodes_CollectionChanged (ugly, but necessary).
		/// </remarks>
		/// <param name="oldParent">The old parent element. May be null to indicate that the element did not have a visual parent previously.</param>
		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);

			if (oldParent is Canvas)
				foreach (DDNode node in Nodes)
					(oldParent as Canvas).Children.Remove(node);

			if (VisualParent is Canvas)
				foreach (DDNode node in Nodes)
				{
					(VisualParent as Canvas).Children.Remove(node);
					(VisualParent as Canvas).Children.Add(node);
				}
			
			OnNodesCollectionChanged();
		}

		/// <summary>
		/// Called when nodes collection has changed (as in, a node has been added or removed).
		/// </summary>
		protected virtual void OnNodesCollectionChanged()
		{
			SetupGeometry();
			OnNodesChanged();
		}

		/// <summary>
		/// Called when anything relating to nodes has changed. This includes both node collection
		/// and nodes' individual properties, such as position or control points.
		/// </summary>
		protected virtual void OnNodesChanged()
		{
			if (StoredGeometry != null)
				Bounds = StoredGeometry.Bounds;

			OnPropertyChanged("Nodes");
			if (NodesChanged != null)
				NodesChanged(this, null);
		}

		protected override void OnManipulationStateChanged(DDManipulationState oldState)
		{
			base.OnManipulationStateChanged(oldState);
			
			foreach (DDNode node in Nodes)
				if (node is DDNodeWithHandles)
					switch (ManipulationState)
					{
						case DDManipulationState.Normal:
							(node as DDNodeWithHandles).ShowPointHandle = false;
							(node as DDNodeWithHandles).ShowControlHandles = false;
							break;
						case DDManipulationState.Edited:
							(node as DDNodeWithHandles).ShowPointHandle = true;
							(node as DDNodeWithHandles).ShowControlHandles = true;
							break;
					}
		}

		public override bool StrokeContains(Point point, double tolerance)
		{
			if (!base.StrokeContains(point, tolerance))
				return false;
			else
			{
				Pen pen = new Pen { Thickness = this.StrokeThickness + tolerance };
				return StoredGeometry.StrokeContains(pen, point);
			}
		}

		public override Point ClosestPoint(Point refPoint)
		{
			Point resPoint = new Point();
			double resDistance = double.PositiveInfinity;
			
			for (int i = 0; i < Nodes.Count - 1; i++)
			{
				Point p1, p2, c1, c2;
				if (Nodes[i].Point != null)
					p1 = Nodes[i].Point.Point;
				else
					continue;
				if (Nodes[i + 1].Point != null)
					p2 = Nodes[i + 1].Point.Point;
				else
					continue;
				if (Nodes[i].Control2 != null)
					c1 = Nodes[i].Control2.Point;
				else
					c1 = p1;
				if (Nodes[i + 1].Control1 != null)
					c2 = Nodes[i + 1].Control1.Point;
				else
					c2 = p2;

				double testDistance;
				Point testPoint = DDTools.ClosestPointOnBezierCurve(out testDistance, p1, c1, c2, p2, refPoint);

				if (testDistance < resDistance)
				{
					resPoint = testPoint;
					resDistance = testDistance;
				}
			}

			return resPoint;
		}

		protected PathGeometry ConstructGeometry(Vector? offset)
		{
			PathGeometry g = new PathGeometry();

			if ((Nodes.Count <= 0) || (Nodes[0].Point == null))
				return g;

			g.Figures.Add(new PathFigure { IsFilled = this.IsFilled });
			foreach (DDNode node in Nodes)
				if (node.Segment != null)
				{
					BezierSegment segment = node.Segment;
					if (offset.HasValue)
					{
						segment = segment.Clone();
						segment.Point1 += offset.Value;
						segment.Point2 += offset.Value;
						segment.Point3 += offset.Value;
					}
					g.Figures[0].Segments.Add(segment);
				}
			return g;			
		}
		
		protected override void SetupGeometry()
		{
			base.SetupGeometry();

			StoredGeometry = ConstructGeometry(null);

			InvalidateVisual();
		}

		protected override void CopyToCLR(Shape target)
		{
			base.CopyToCLR(target);

			if (target is Path)
			{
				SetupGeometry();
	
				PathGeometry g = ConstructGeometry(new Vector(-X, -Y));
				(target as Path).Data = g;
				target.ClearValue(FrameworkElement.WidthProperty);
				target.ClearValue(FrameworkElement.HeightProperty);
			}
		}

		public override bool SetFromCLR(Shape source)
		{
			if (!base.SetFromCLR(source))
				return false;

			if (!(source is Path))
				return false;

			Path path = source as Path;
			if (!(path.Data is PathGeometry))
				return false;

			PathGeometry g = path.Data as PathGeometry;
			if (g.Figures.Count == 0)
				return false;

			IsFilled = g.Figures[0].IsFilled;
			
			Vector offset = new Vector(X, Y);
			List<BezierSegment> segmentList = new List<BezierSegment>();
			for (int i = 0; i < g.Figures[0].Segments.Count; i++)
				if (g.Figures[0].Segments[i] is BezierSegment)
				{
					BezierSegment seg = (g.Figures[0].Segments[i] as BezierSegment).Clone();
					seg.Point1 += offset;
					seg.Point2 += offset;
					seg.Point3 += offset;
					segmentList.Add(seg);
				}
				else if (g.Figures[0].Segments[i] is PolyBezierSegment)
				{
					PolyBezierSegment pbs = g.Figures[0].Segments[i] as PolyBezierSegment;
					for (int j = 2; j < pbs.Points.Count; j += 3)
						segmentList.Add(new BezierSegment(
							pbs.Points[j - 2] + offset,
							pbs.Points[j - 1] + offset,
							pbs.Points[j] + offset, pbs.IsStroked));
				}

			DDNode nodeBefore = null;
			Point? savedFirstControl = null;
			foreach (BezierSegment segment in segmentList)
			{
				DDNodeWithHandles node = new DDNodeWithHandles();

				node.Point = new DDPoint(segment.Point3);
				if (segment.Point2 != segment.Point3)
					node.Control1 = new DDPoint(segment.Point2);
				else
					node.Control1 = null;
				if (nodeBefore != null)
				{
					if ((nodeBefore.Point != null) && (segment.Point1 != nodeBefore.Point.Point))
						nodeBefore.Control2 = new DDPoint(segment.Point1);
					else
						nodeBefore.Control2 = null;
				}
				else
					savedFirstControl = segment.Point1;
				node.IsCurved = true;
				node.IsStroked = segment.IsStroked;
				nodeBefore = node;
				Nodes.Add(node);
			}
				
			if ((Nodes.Last != null) && (savedFirstControl.HasValue))
				if (savedFirstControl.Value != Nodes.Last.Point)
					Nodes.Last.Control2 = new DDPoint(savedFirstControl.Value);
				else
					Nodes.Last.Control2 = null;
			
			this.ClearValue(FrameworkElement.WidthProperty);
			this.ClearValue(FrameworkElement.HeightProperty);

			return true;
		}

		public event EventHandler NodesChanged;

		#region ICloneable Members

		protected override void CloneTo(DDObject target)
		{
			base.CloneTo(target);
			foreach (DDNodeWithHandles node in Nodes)
				(target as DDNodeObject).Nodes.Add(node.Clone());
		}
		
		#endregion
	}

	public class DDPolygon : DDNodeObject
	{
		public DDPolygon()
		{
			Nodes.LoopAtStart = true;
			Nodes.LoopAtEnd = true;
			IsFilled = true;
		}

		protected override void SetupGeometry()
		{
			base.SetupGeometry();
			AdjustStartPoint(StoredGeometry, null);
		}

		protected override void OnNodesCollectionChanged()
		{
			base.OnNodesCollectionChanged();

			foreach (DDNode node in Nodes)
				node.SegmentModified -= StartPointBoundSegmentModified;
			
			if (Nodes.Last != null)
				Nodes.Last.SegmentModified += StartPointBoundSegmentModified;
		}

		protected void AdjustStartPoint(Geometry g, Vector? offset)
		{
			if ((g is PathGeometry) && ((g as PathGeometry).Figures.Count > 0) && (Nodes.Last != null) && (Nodes.Last.Point != null))
			{
				(g as PathGeometry).Figures[0].StartPoint = Nodes.Last.Point.Point;
				if (offset.HasValue)
					(g as PathGeometry).Figures[0].StartPoint = (g as PathGeometry).Figures[0].StartPoint + offset.Value;
			}
		}

		void StartPointBoundSegmentModified(object sender, EventArgs e)
		{
			AdjustStartPoint(StoredGeometry, null);
		}

		protected override void CopyToCLR(Shape target)
		{
			base.CopyToCLR(target);

			if (target is Path)
				AdjustStartPoint((target as Path).Data, new Vector(-X, -Y));
		}

		#region ICloneable Members

		public DDPolyline CloneToPolyline()
		{
			DDPolyline ret = new DDPolyline();
			CloneTo(ret);
			return ret;
		}
		
		public override object Clone()
		{
			DDPolygon ret = new DDPolygon();
			CloneTo(ret);
			return ret;
		}

		public override Shape CloneToCLR()
		{
			Path ret = new Path();
			CopyToCLR(ret);
			return ret;
		}

		#endregion
	}

	public class DDPolyline : DDNodeObject
	{
		public DDPolyline()
		{
			Nodes.LoopAtStart = false;
			Nodes.LoopAtEnd = false;
			IsFilled = false;
		}

		protected override void SetupGeometry()
		{
			base.SetupGeometry();
			AdjustStartPoint(StoredGeometry, null);

			if (StoredGeometry is PathGeometry)
				if ((StoredGeometry as PathGeometry).Figures.Count > 0)
				{
					PathSegmentCollection segments = (StoredGeometry as PathGeometry).Figures[0].Segments;
					if ((segments.Count > 0) && (segments[0] is BezierSegment))
					{
						(segments[0] as BezierSegment).Point1 = (segments[0] as BezierSegment).Point3;
						(segments[0] as BezierSegment).Point2 = (segments[0] as BezierSegment).Point3;
					}
				}
		}

		protected override void OnNodesCollectionChanged()
		{
			base.OnNodesCollectionChanged();

			foreach (DDNode node in Nodes)
				node.SegmentModified -= StartPointBoundSegmentModified;

			if (Nodes.First != null)
				Nodes.First.SegmentModified += StartPointBoundSegmentModified;
		}

		protected void AdjustStartPoint(Geometry g, Vector? offset)
		{
			if ((g is PathGeometry) && ((g as PathGeometry).Figures.Count > 0) && (Nodes.Last != null) && (Nodes.Last.Point != null))
			{
				(g as PathGeometry).Figures[0].StartPoint = Nodes.First.Point.Point;
				if (offset.HasValue)
					(g as PathGeometry).Figures[0].StartPoint = (g as PathGeometry).Figures[0].StartPoint + offset.Value;
			}
		}

		void StartPointBoundSegmentModified(object sender, EventArgs e)
		{
			AdjustStartPoint(StoredGeometry, null);
		}

		protected override void CopyToCLR(Shape target)
		{
			base.CopyToCLR(target);

			if (target is Path)
				AdjustStartPoint((target as Path).Data, new Vector(-X, -Y));
		}

		#region ICloneable Members

		public DDPolygon CloneToPolygon()
		{
			DDPolygon ret = new DDPolygon();
			CloneTo(ret);
			return ret;
		}

		public override object Clone()
		{
			DDPolyline ret = new DDPolyline();
			CloneTo(ret);
			return ret;
		}

		public override Shape CloneToCLR()
		{
			Path ret = new Path();
			CopyToCLR(ret);
			return ret;
		}

		#endregion
	}
	
	public class DDRectangle : DDObject
	{
		protected RectangleGeometry StoredRectangleGeometry
		{
			get { return StoredGeometry is RectangleGeometry ? StoredGeometry as RectangleGeometry : null; }
		}
		
		#region CornerRadiusX Property
		public static DependencyProperty CornerRadiusXProperty = DependencyProperty.Register(
			"CornerRadiusX", typeof(double), typeof(DDRectangle),
			new PropertyMetadata(0d, CornerRadiusPropertyChangedCallback));

		public double CornerRadiusX
		{
			get { return (double)this.GetValue(CornerRadiusXProperty); }
			set { this.SetValue(CornerRadiusXProperty, value); }
		}
		#endregion

		#region CornerRadiusY Property
		public static DependencyProperty CornerRadiusYProperty = DependencyProperty.Register(
			"CornerRadiusY", typeof(double), typeof(DDRectangle),
			new PropertyMetadata(0d, CornerRadiusPropertyChangedCallback));

		public double CornerRadiusY
		{
			get { return (double)this.GetValue(CornerRadiusYProperty); }
			set { this.SetValue(CornerRadiusYProperty, value); }
		}
		#endregion

		protected static void CornerRadiusPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			((DDRectangle)d).OnPropertyChanged("CornerRadius");
		}

		protected override void SetupGeometry()
		{
			StoredGeometry = new RectangleGeometry(Bounds);
			
			BindingOperations.SetBinding(StoredRectangleGeometry, RectangleGeometry.RadiusXProperty,
				new Binding("CornerRadiusX") { Source = this });
		
			BindingOperations.SetBinding(StoredRectangleGeometry, RectangleGeometry.RadiusYProperty,
				new Binding("CornerRadiusY") { Source = this });
		}

		public override Point ClosestPoint(Point refPoint)
		{
			return refPoint;
		}

		protected override void OnBoundsChanged()
		{
			base.OnBoundsChanged();
			SetupGeometry();
			InvalidateVisual();
		}

		protected override void CopyToCLR(Shape target)
		{
			base.CopyToCLR(target);

			if (target is Rectangle)
			{
				(target as Rectangle).RadiusX = CornerRadiusX;
				(target as Rectangle).RadiusY = CornerRadiusY;
			}
		}

		public override bool SetFromCLR(Shape source)
		{
			base.SetFromCLR(source);
						
			if (source is Rectangle)
			{
				CornerRadiusX = (source as Rectangle).RadiusX;
				CornerRadiusY = (source as Rectangle).RadiusY;
			}

			return true;
		}

		#region ICloneable Members

		public override object Clone()
		{
			DDRectangle ret = new DDRectangle();
			CloneTo(ret);
			ret.CornerRadiusX = CornerRadiusX;
			ret.CornerRadiusY = CornerRadiusY;
			return ret;
		}

		public override Shape CloneToCLR()
		{
			Rectangle ret = new Rectangle();
			CopyToCLR(ret);
			return ret;
		}

		#endregion
	}

	public class DDEllipse : DDObject
	{
		protected EllipseGeometry StoredEllipseGeometry
		{
			get { return StoredGeometry is EllipseGeometry ? StoredGeometry as EllipseGeometry : null; }
		}

		protected override void SetupGeometry()
		{
			StoredGeometry = new EllipseGeometry(Bounds);
		}

		public override Point ClosestPoint(Point refPoint)
		{
			return refPoint;
		}

		protected override void OnBoundsChanged()
		{
			base.OnBoundsChanged();
			SetupGeometry();
			InvalidateVisual();
		}

		#region ICloneable Members

		public override object Clone()
		{
			DDEllipse ret = new DDEllipse();
			CloneTo(ret);
			return ret;
		}

		public override Shape CloneToCLR()
		{
			Ellipse ret = new Ellipse();
			CopyToCLR(ret);
			return ret;
		}

		#endregion
	}
}