using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Controls;

namespace Pantas.DeltaDraw.Core
{
	public class DDNode : FrameworkElement, INotifyPropertyChanged, IDDChangeTracking, ICloneable
	{
		#region NodeBefore property
		DDNode _nodeBefore = null;
		public DDNode NodeBefore
		{
			get
			{
				if (_nodeBefore != null)
					return _nodeBefore;
				else
				{
					if (ParentObject != null)
					{
						_nodeBefore = ParentObject.Nodes.GetNodeBefore(this);
						return _nodeBefore;
					}
					else
						return null;
				}
			}
		}
		#endregion

		#region NodeAfter property
		DDNode _nodeAfter = null;
		public DDNode NodeAfter
		{
			get
			{
				if (_nodeAfter != null)
					return _nodeAfter;
				else
				{
					if (ParentObject != null)
					{
						_nodeAfter = ParentObject.Nodes.GetNodeAfter(this);
						return _nodeAfter;
					}
					else
						return null;
				}
			}
		}
		#endregion

		public BezierSegment Segment
		{
			get;
			protected set;
		}

		DDNodeObject _parentObject = null;
		public DDNodeObject ParentObject
		{
			get { return _parentObject; }
			internal set
			{
				if (_parentObject != null)
					_parentObject.NodesChanged -= ParentObjectOtherNodesChanged;
				_parentObject = value;
				if (_parentObject != null)
					_parentObject.NodesChanged += ParentObjectOtherNodesChanged;

				TrySetupDefaultControlPoints();
				if (NodeBefore != null)
					NodeBefore.TrySetupDefaultControlPoints();
				if (NodeAfter != null)
					NodeAfter.TrySetupDefaultControlPoints();
			}
		}

		public bool IsCurved
		{
			get;
			internal set;
		}

		#region Point Property
		public static readonly DependencyProperty PointProperty = DependencyProperty.Register(
			"Point", typeof(DDPoint), typeof(DDNode),
			new PropertyMetadata(null, AnyPropertyChangedCallback));

		public DDPoint Point
		{
			get { return (DDPoint)this.GetValue(PointProperty); }
			set { this.SetValue(PointProperty, value); }
		}
		#endregion

		#region Control1 Property
		public static readonly DependencyProperty Control1Property = DependencyProperty.Register(
			"Control1", typeof(DDPoint), typeof(DDNode),
			new PropertyMetadata(null, AnyPropertyChangedCallback));

		public DDPoint Control1
		{
			get { return (DDPoint)this.GetValue(Control1Property); }
			set { this.SetValue(Control1Property, value); }
		}
		#endregion

		#region Control2 Property
		public static readonly DependencyProperty Control2Property = DependencyProperty.Register(
			"Control2", typeof(DDPoint), typeof(DDNode),
			new PropertyMetadata(null, AnyPropertyChangedCallback));

		public DDPoint Control2
		{
			get { return (DDPoint)this.GetValue(Control2Property); }
			set { this.SetValue(Control2Property, value); }
		}
		#endregion

		#region IsStroked Property
		public static readonly DependencyProperty IsStrokedProperty = DependencyProperty.Register(
			"IsStroked", typeof(bool), typeof(DDNode),
			new PropertyMetadata(true, AnyPropertyChangedCallback));

		public bool IsStroked
		{
			get { return (bool)this.GetValue(IsStrokedProperty); }
			set { this.SetValue(IsStrokedProperty, value); }
		}
		#endregion

		private static void AnyPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			DDNode p = (sender as DDNode);
			p.OnNodeChanged();
			p.OnPropertyChanged(args.Property.Name);
		}

		public bool Control1ManuallySet { get; set; }
		public bool Control2ManuallySet { get; set; }

		public string ChangeSource { get; protected set; }

		protected DDNode()
		{
			Segment = null;
			IsCurved = true;
			Control1ManuallySet = false;
			Control2ManuallySet = false;
			ChangeSource = null;
		}

		protected DDNode(DDPoint point)
			: this()
		{
			StartChange("Internal");
			this.Point = point;
			EndChange();
		}

		protected DDNode(DDPoint point, DDPoint control1, DDPoint control2, bool isStroked)
			: this()
		{
			StartChange("Internal");
			this.Point = point;
			this.Control1 = control1;
			this.Control2 = control2;
			this.IsStroked = isStroked;
			EndChange();
		}

		void ParentObjectOtherNodesChanged(object sender, EventArgs e)
		{
			_nodeBefore = null;
			_nodeAfter = null;
		}

		virtual protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		virtual protected void OnParentObjectChanged()
		{
		}

		virtual protected void OnNodeChanged()
		{
			if (Control1 == null)
				Control1ManuallySet = false;
			if (Control2 == null)
				Control2ManuallySet = false;

			RefreshSegment();
			if (NodeAfter != null)
				NodeAfter.RefreshSegment();
			
			if (ChangeSource == null)
				OnExternalChange();
		}

		protected virtual void OnSegmentCreated()
		{
			if (SegmentCreated != null)
				SegmentCreated(this, new EventArgs());
		}

		protected virtual void OnSegmentModified()
		{
			if (SegmentModified != null)
				SegmentModified(this, new EventArgs());
		}

		protected virtual void OnSegmentLost()
		{
			if (SegmentLost != null)
				SegmentLost(this, new EventArgs());
		}

		protected virtual void OnExternalChange()
		{
			if (ExternalChange != null)
				ExternalChange(this, new EventArgs());
			
			Control1ManuallySet = (Control1 != null);
			Control2ManuallySet = (Control2 != null);
		}

		protected void RefreshSegment()
		{		
			if (Point == null)
			{
				bool lost = (Segment != null);
				Segment = null;
				if (lost)
					OnSegmentLost();
			}
			else
			{
				if (Segment == null)
				{
					Segment = new BezierSegment();
					OnSegmentCreated();
				}

				/* 
				 * Bezier points in WPF are arranged like this (Nx stands for Node[index], Px is for Point[num]):
				 *  
				 *   ...[N1P3]----(N2P1)----------(N2P2)----[N2P3]----(N3P1)----------(N3P2)----[N3P3]----(N4P1)...
				 * 
				 * Control points are weighting the line between their own and PREVIOUS node,
				 * with Point1 being nearer to the PREVIOUS node, Point2 weighting its own node and
				 * Point3 defining the node itself.
				 * 
				 * How we'd want (and expect) them to behave is like this (Nx stands for node, C for control point):
				 * 
				 *   ...[N1]----(N1C2)----------(N2C1)----[N2]----(N2C2)----------(N3C1)----[N3]----(N3C2)...
				 * 
				 * ... with each node controlling the weight immediately before and after itself along the line.
				 * 
				 * To achieve this, segment control is shifted one point along the line.
				 * This node controls two points of its own segment (Point2 <= Control1 and
				 * Point3 <= Point) and Point1 of the segment before (Point1 <= NodeBefore.Control2).
				 * Its own Control2 property will be utilized by the next node in line.
				 */

				if (NodeBefore != null)
				{
					if ((NodeBefore.Control2 != null) && (IsCurved))
						Segment.Point1 = NodeBefore.Control2.Point;
					else if (NodeBefore.Point != null)
						Segment.Point1 = NodeBefore.Point.Point;
					else
						Segment.Point1 = Point.Point;
				}
				else
					Segment.Point1 = Point.Point;

				if ((Control1 != null) && (IsCurved))
					Segment.Point2 = Control1.Point;
				else
					Segment.Point2 = Point.Point;

				Segment.Point3 = Point.Point;

				Segment.IsStroked = IsStroked;

				OnSegmentModified();
			}
		}

		public void TrySetupDefaultControlPoints()
		{
			if ((ParentObject == null) || (Point == null))
				return;

			StartChange("Internal");
			Point p1 = Point.Point;
			if (!Control1ManuallySet && (NodeBefore != null) && (NodeBefore.Point != null))
			{
				Point p2 = NodeBefore.Point.Point;
				Control1 = new Point(p1.X + (p2.X - p1.X) * 0.25, p1.Y + (p2.Y - p1.Y) * 0.25);
			}
			if (!Control2ManuallySet && (NodeAfter != null) && (NodeAfter.Point != null))
			{
				Point p2 = NodeAfter.Point.Point;
				Control2 = new Point(p1.X + (p2.X - p1.X) * 0.25, p1.Y + (p2.Y - p1.Y) * 0.25);
			}
			EndChange();
		}

		public event EventHandler SegmentCreated;
		public event EventHandler SegmentModified;
		public event EventHandler SegmentLost;
		public event EventHandler ExternalChange;

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region IDDChangeTracking Members

		public void StartChange(string changeSource)
		{
			if (string.IsNullOrEmpty(changeSource))
				throw new DDInvalidChangeSourceName();
			if (ChangeSource != null)
				throw new DDStarEndChangeOutOfOrder();
			ChangeSource = changeSource;
		}

		public void EndChange()
		{
			if (ChangeSource == null)
				throw new DDStarEndChangeOutOfOrder();
			ChangeSource = null;
		}

		#endregion

		#region ICloneable Members

		protected virtual void CloneTo(DDNode target)
		{
			if (this.Point != null)
				target.Point = new DDPoint(this.Point);
			else
				target.Point = null;
			if (this.Point != null)
				target.Control1 = new DDPoint(this.Control1);
			else
				target.Control1 = null;
			if (this.Control2 != null)
				target.Control2 = new DDPoint(this.Control2);
			else
				target.Control2 = null;
			target.IsStroked = IsStroked;
			target.Control1ManuallySet = Control1ManuallySet;
			target.Control2ManuallySet = Control2ManuallySet;
		}
		
		public object Clone()
		{
			DDNode ret = new DDNode();
			CloneTo(ret);
			return ret;
		}

		#endregion
	}
	
	public class DDNodeWithHandles : DDNode, IDDHandleHolder
	{
		#region ControlCanvas Property
		private Canvas _controlCanvas = null;
		public Canvas ControlCanvas
		{
			get { return _controlCanvas; }
			set
			{
				if (_controlCanvas != null)
				{
					if (PointHandle != null)
						_controlCanvas.Children.Remove(PointHandle);
					if (Control1Handle != null)
						_controlCanvas.Children.Remove(Control1Handle);
					if (Control2Handle != null)
						_controlCanvas.Children.Remove(Control2Handle);
					if (Control1Line != null)
						_controlCanvas.Children.Remove(Control1Line);
					if (Control2Line != null)
						_controlCanvas.Children.Remove(Control2Line);
				}
				_controlCanvas = value;
				if (_controlCanvas != null)
				{
					if (PointHandle != null)
						_controlCanvas.Children.Add(PointHandle);
					if (Control1Handle != null)
						_controlCanvas.Children.Add(Control1Handle);
					if (Control2Handle != null)
						_controlCanvas.Children.Add(Control2Handle);
					if (Control1Line != null)
						_controlCanvas.Children.Add(Control1Line);
					if (Control2Line != null)
						_controlCanvas.Children.Add(Control2Line);
				}
			}
		}
		#endregion

		#region PointHandle Property
		private DDPointHandle _pointHandle = null;
		protected DDPointHandle PointHandle
		{
			get { return _pointHandle; }
			set
			{
				if ((_pointHandle != null) && (ControlCanvas != null))
				{
					ControlCanvas.Children.Remove(_pointHandle);
				}
				_pointHandle = value;
				if ((_pointHandle != null) && (ControlCanvas != null))
				{
					ControlCanvas.Children.Add(_pointHandle);
					if (Point != null)
						_pointHandle.Position = Point;
				}
			}
		}
		#endregion

		#region Control1Handle Property
		private DDControl1Handle _control1Handle = null;
		protected DDControl1Handle Control1Handle
		{
			get { return _control1Handle; }
			set
			{
				if ((_control1Handle != null) && (ControlCanvas != null))
				{
					ControlCanvas.Children.Remove(_control1Handle);
				}
				_control1Handle = value;
				if ((_control1Handle != null) && (ControlCanvas != null))
				{
					ControlCanvas.Children.Add(_control1Handle);
					if (Control1 != null)
						_control1Handle.Position = Control1;
				}
			}
		}
		#endregion

		#region Control2Handle Property
		private DDControl2Handle _control2Handle = null;
		protected DDControl2Handle Control2Handle
		{
			get { return _control2Handle; }
			set
			{
				if ((_control2Handle != null) && (ControlCanvas != null))
				{
					ControlCanvas.Children.Remove(_control2Handle);
				}
				_control2Handle = value;
				if ((_control2Handle != null) && (ControlCanvas != null))
				{
					ControlCanvas.Children.Add(_control2Handle);
					if (Control2 != null)
						_control2Handle.Position = Control2;
				}
			}
		}
		#endregion

		#region Control1Line Property
		private DDBoundControlLine _control1Line = null;
		protected DDBoundControlLine Control1Line
		{
			get { return _control1Line; }
			set
			{
				if ((_control1Line != null) && (ControlCanvas != null))
					ControlCanvas.Children.Remove(_control1Line);
				_control1Line = value;
				if ((_control1Line != null) && (ControlCanvas != null))
					ControlCanvas.Children.Add(_control1Line);
			}
		}
		#endregion

		#region Control2Line Property
		private DDBoundControlLine _control2Line = null;
		protected DDBoundControlLine Control2Line
		{
			get { return _control2Line; }
			set
			{
				if ((_control2Line != null) && (ControlCanvas != null))
					ControlCanvas.Children.Remove(_control2Line);
				_control2Line = value;
				if ((_control2Line != null) && (ControlCanvas != null))
					ControlCanvas.Children.Add(_control2Line);
			}
		}
		#endregion

		#region ShowPointHandle Property
		public static readonly DependencyProperty ShowPointHandleProperty = DependencyProperty.Register(
			"ShowPointHandle", typeof(bool), typeof(DDNodeWithHandles),
			new PropertyMetadata(false, AnyShowHandlePropertyChangedCallback));
	
		public bool ShowPointHandle
		{
			get { return (bool)this.GetValue(ShowPointHandleProperty); }
			set { this.SetValue(ShowPointHandleProperty, value); }
		}
		#endregion

		#region ShowControlHandles Property
		public static readonly DependencyProperty ShowControlHandlesProperty = DependencyProperty.Register(
			"ShowControlHandles", typeof(bool), typeof(DDNodeWithHandles),
			new PropertyMetadata(false, AnyShowHandlePropertyChangedCallback));
	
		public bool ShowControlHandles
		{
			get { return (bool)this.GetValue(ShowControlHandlesProperty); }
			set { this.SetValue(ShowControlHandlesProperty, value); }
		}
		#endregion

		private static void AnyShowHandlePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDNodeWithHandles sender = (DDNodeWithHandles) d;
			sender.RefreshHandleVisibility();
		}

		public DDNodeWithHandles()
			: this(null, null, null, null, true)
		{
		}
		
		public DDNodeWithHandles(Canvas controlCanvas)
			: this(controlCanvas, null, null, null, true)
		{
		}

		public DDNodeWithHandles(Canvas controlCanvas, DDPoint point)
			: this(controlCanvas, point, null, null, true)
		{
		}

		public DDNodeWithHandles(Canvas controlCanvas, DDPoint point, DDPoint control1, DDPoint control2, bool isStroked)
			: base(point, control1, control2, isStroked)
		{
			ControlCanvas = controlCanvas;
			RefreshHandleVisibility();
		}

		protected void RefreshHandleVisibility()
		{
			if (ShowControlHandles)
			{
				if ((Control1Line == null) && (PointHandle != null) && (Control1Handle != null))
					Control1Line = new DDBoundControlLine(PointHandle, "Position.Point", Control1Handle, "Position.Point");
				else if ((Control1Line != null) && ((PointHandle == null) || (Control1Handle == null)))
					Control1Line = null;
				if ((Control2Line == null) && (PointHandle != null) && (Control2Handle != null))
					Control2Line = new DDBoundControlLine(PointHandle, "Position.Point", Control2Handle, "Position.Point");
				else if ((Control2Line != null) && ((PointHandle == null) || (Control2Handle == null)))
					Control2Line = null;
				
				if ((Control1Handle == null) && (Control1 != null))
					Control1Handle = new DDControl1Handle(this);
				else if ((Control1Handle != null) && (Control1 == null))
					Control1Handle = null;
				if ((Control2Handle == null) && (Control2 != null))
					Control2Handle = new DDControl2Handle(this);
				else if ((Control2Handle != null) && (Control2 == null))
					Control2Handle = null;
			}
			else
			{
				Control1Handle = null;
				Control2Handle = null;
				Control1Line = null;
				Control2Line = null;
			}
			
			if (ShowPointHandle)
			{
				if ((PointHandle == null) && (Point != null))
					PointHandle = new DDPointHandle(this);
				else if ((PointHandle != null) && (Point == null))
					PointHandle = null;
			}
			else
				PointHandle = null;
		}

		protected override void OnNodeChanged()
		{
			base.OnNodeChanged();
			
			RefreshHandleVisibility();

			//if (ChangeSource == "Manipulation") return;

			//StartChange("Internal");
			if ((PointHandle != null) && (Point != null))
			{
				if ((PointHandle.Position == null) || (!PointHandle.Position.Equals(Point)))
					PointHandle.Position = new DDPoint(Point);
			}
			if ((Control1Handle != null) && (Control1 != null))
			{
				if ((Control1Handle.Position == null) || (!Control1Handle.Position.Equals(Control1)))
					Control1Handle.Position = new DDPoint(Control1);
			}
			if ((Control2Handle != null) && (Control2 != null))
			{
				if ((Control2Handle.Position == null) || (!Control2Handle.Position.Equals(Control2)))
					Control2Handle.Position = new DDPoint(Control2);
			}
			//EndChange();
		}

		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);

			if ((this.Parent == null) && (ControlCanvas != null))
				ControlCanvas = null;
			else if ((this.Parent is Canvas) && (this.Parent != ControlCanvas))
				ControlCanvas = this.Parent as Canvas;
		}

		#region IDDHandleHolder Members

		public void ReportHandleManipulation(DDHandle handle, Point newPosition)
		{
//			StartChange("Manipulation");
			if (handle is DDPointHandle)
			{
				if ((Point != null) && (Control1 != null))
				{
					Vector delta = Point.Point - Control1.Point;
					Control1 = newPosition - delta;
				}
				if ((Point != null) && (Control2 != null))
				{
					Vector delta = Point.Point - Control2.Point;
					Control2 = newPosition - delta;
				}
				Point = newPosition;
			}
			else if (handle is DDControl1Handle)
			{
				Control1 = newPosition;
				Control1ManuallySet = true;
			}
			else if (handle is DDControl2Handle)
			{
				Control2 = newPosition;
				Control2ManuallySet = true;
			}
//			EndChange();
		}

		#endregion

		#region ICloneable Members

		protected override void CloneTo(DDNode target)
		{
			base.CloneTo(target);
			
			if (target is DDNodeWithHandles)
			{
				DDNodeWithHandles localTarget = target as DDNodeWithHandles;
				localTarget.ShowPointHandle = ShowPointHandle;
				localTarget.ShowControlHandles = ShowControlHandles;
			}
		}
		
		public new object Clone()
		{
			DDNodeWithHandles ret = new DDNodeWithHandles();
			CloneTo(ret);
			return ret;
		}

		#endregion
	}
}