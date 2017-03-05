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
using System.Windows.Markup;
using System.Xml;

namespace Pantas.DeltaDraw.Application
{
	[Flags]
	public enum DDMouseButtonFlags {
		None = 0x0,
		Clicked = 0x1,
		StartedDrag = 0x2,
		Dragging = 0x4,
		EndedDrag = 0x8
	}

	public struct DDMouseButtonRecord
	{
		public MouseButtonState State;
		public DDMouseButtonFlags Flags;
		public DateTime TimeStamp;
		public Point DragRefPoint;

		public void Reset()
		{
			State = MouseButtonState.Released;
			Flags = DDMouseButtonFlags.None;
			TimeStamp = new DateTime();
			DragRefPoint = new Point();
		}
	}
	
	public class DDToolManager
	{
		static readonly public Cursor NeutralCursor = Cursors.Arrow;
		public static readonly TimeSpan ClickTime = new TimeSpan(3000000); // 300ms, 0.3 sec
		public static readonly double ClickArea = 100; // square pixels
		
		protected DDMouseButtonRecord[] MouseButtonRecords = new DDMouseButtonRecord[3];

		DDTool _activeTool = null;
		public DDTool ActiveTool
		{
			get { return _activeTool; }
			set
			{
				DeactivateTool();
				_activeTool = value;
				ActivateTool();
			}
		}

		FrameworkElement _drawingArea;
		protected FrameworkElement DrawingArea
		{ 
			get { return _drawingArea; }
			set
			{
				DeactivateTool();
				if (_drawingArea != null)
				{
					_drawingArea.MouseMove -= DrawingArea_MouseMove;
					_drawingArea.MouseDown -= DrawingArea_MouseDown;
					_drawingArea.MouseUp -= DrawingArea_MouseUp;
				}

				_drawingArea = value;
				
				if (_drawingArea != null)
				{
					_drawingArea.MouseMove += DrawingArea_MouseMove;
					_drawingArea.MouseDown += DrawingArea_MouseDown;
					_drawingArea.MouseUp += DrawingArea_MouseUp;
				}
				ActivateTool();
			}
		}

		FrameworkElement _inputArea;
		protected FrameworkElement InputArea
		{
			get { return _inputArea; }
			set
			{
				DeactivateTool();
				if (_inputArea != null)
				{
					_inputArea.KeyDown -= InputArea_KeyDown;
					_inputArea.KeyUp -= InputArea_KeyUp;
				}

				_inputArea = value;
				
				if (_inputArea != null)
				{
					_inputArea.KeyDown += InputArea_KeyDown;
					_inputArea.KeyUp += InputArea_KeyUp;
				}
				ActivateTool();
			}
		}

		Canvas _drawingCanvas;
		protected Canvas DrawingCanvas
		{
			get { return _drawingCanvas; }
			set
			{
				DeactivateTool();
				_drawingCanvas = value;
				ActivateTool();
			}
		}

		public bool IsMouseAvailable
		{
			get { return (Mouse.Captured == null || Mouse.Captured == DrawingArea); }
		}

		public DDToolManager(FrameworkElement drawingArea, Canvas drawingCanvas, FrameworkElement inputArea)
		{
			this.DrawingArea = drawingArea;
			this.DrawingCanvas = drawingCanvas;
			this.InputArea = inputArea;
		}

		void DrawingArea_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if ((ActiveTool != null) && (DrawingCanvas != null) && (IsMouseAvailable))
			{
				UpdateButtonRecords(e.GetPosition(DrawingCanvas), e.LeftButton, e.MiddleButton, e.RightButton);
				ActiveTool.ReportMouseDown(DrawingCanvas, e);
				CheckAndClearButtonFlags(e);
			}
		}

		void DrawingArea_MouseMove(object sender, MouseEventArgs e)
		{
			if ((ActiveTool != null) && (DrawingCanvas != null) && (IsMouseAvailable))
			{
				UpdateButtonRecords(e.GetPosition(DrawingCanvas), e.LeftButton, e.MiddleButton, e.RightButton);
				ActiveTool.ReportMouseMove(DrawingCanvas, e);
				CheckAndClearButtonFlags(e);
			}
		}

		void DrawingArea_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if ((ActiveTool != null) && (DrawingCanvas != null) && (IsMouseAvailable))
			{
				UpdateButtonRecords(e.GetPosition(DrawingCanvas), e.LeftButton, e.MiddleButton, e.RightButton);
				ActiveTool.ReportMouseUp(DrawingCanvas, e);
				CheckAndClearButtonFlags(e);
			}
		}

		void InputArea_KeyDown(object sender, KeyEventArgs e)
		{
			if (ActiveTool != null)
				ActiveTool.ReportKeyDown(DrawingCanvas, e);
		}

		void InputArea_KeyUp(object sender, KeyEventArgs e)
		{
			if (ActiveTool != null)
				ActiveTool.ReportKeyUp(DrawingCanvas, e);
		}

		void DeactivateTool()
		{
			if (DrawingArea != null)
			{
				BindingOperations.ClearBinding(DrawingArea, FrameworkElement.CursorProperty);
				DrawingArea.ReleaseMouseCapture();
			}
			foreach (DDMouseButtonRecord record in MouseButtonRecords)
				record.Reset();
		}

		void ActivateTool()
		{
			if (ActiveTool != null)
			{
				if (DrawingArea != null)
				{
					DrawingArea.SetBinding(FrameworkElement.CursorProperty,
						new Binding("ToolCursor") { Source = ActiveTool });
				}
			}
		}

		protected void UpdateButtonRecords(Point mousePos, params MouseButtonState[] states)
		{
			DateTime now = DateTime.Now;
			int numOfPressedButtons = 0;
			
			for (int i = 0; i < Math.Min(states.Length, MouseButtonRecords.Length); i++)
			{
				double deltaArea = Math.Abs(MouseButtonRecords[i].DragRefPoint.X - mousePos.X) *
					Math.Abs(MouseButtonRecords[i].DragRefPoint.Y- mousePos.Y);
				TimeSpan deltaTime = now - MouseButtonRecords[i].TimeStamp;
				if (states[i] == MouseButtonState.Pressed)
				{
					if (MouseButtonRecords[i].State == MouseButtonState.Released)
					{
						MouseButtonRecords[i].DragRefPoint = mousePos;
						MouseButtonRecords[i].TimeStamp = now;
					}
					if (!MouseButtonRecords[i].Flags.HasFlag(DDMouseButtonFlags.Dragging))
					{
						if ((deltaArea > ClickArea) || (deltaTime > ClickTime))
						{
							MouseButtonRecords[i].Flags |= DDMouseButtonFlags.StartedDrag;
							MouseButtonRecords[i].Flags |= DDMouseButtonFlags.Dragging;
						}
					}
					numOfPressedButtons++;
				}
				else
				{
					if (MouseButtonRecords[i].State == MouseButtonState.Pressed)
					{
						if ((deltaArea <= ClickArea) && (deltaTime <= ClickTime))
							MouseButtonRecords[i].Flags |= DDMouseButtonFlags.Clicked;
						if (MouseButtonRecords[i].Flags.HasFlag(DDMouseButtonFlags.Dragging))
							MouseButtonRecords[i].Flags |= DDMouseButtonFlags.EndedDrag;
					}
				}
				MouseButtonRecords[i].State = states[i];
			}
			
			if (DrawingArea != null)
				if ((numOfPressedButtons > 0) && (Mouse.Captured == null))
					DrawingArea.CaptureMouse();
				else if ((numOfPressedButtons == 0) && (DrawingArea.IsMouseCaptured))
					DrawingArea.ReleaseMouseCapture();
		}

		protected void CheckAndClearButtonFlags(MouseEventArgs e)
		{
			if ((ActiveTool == null) || (DrawingCanvas == null))
				return;
			
			for (int i = 0; i < MouseButtonRecords.Length; i++)
			{
				MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice,
					e.Timestamp, (MouseButton)i, e.StylusDevice);
				if (MouseButtonRecords[i].Flags.HasFlag(DDMouseButtonFlags.Clicked))
				{
					ActiveTool.ReportMouseClick(DrawingCanvas, args);
					MouseButtonRecords[i].Flags ^= DDMouseButtonFlags.Clicked;
				}
				if (MouseButtonRecords[i].Flags.HasFlag(DDMouseButtonFlags.StartedDrag))
				{
					ActiveTool.ReportStartDrag(DrawingCanvas, args);
					MouseButtonRecords[i].Flags ^= DDMouseButtonFlags.StartedDrag;
				}
				if (MouseButtonRecords[i].Flags.HasFlag(DDMouseButtonFlags.Dragging))
				{
					ActiveTool.ReportMouseDrag(DrawingCanvas, args);
					if (MouseButtonRecords[i].Flags.HasFlag(DDMouseButtonFlags.EndedDrag))
						MouseButtonRecords[i].Flags ^= DDMouseButtonFlags.Dragging;
				}
				if (MouseButtonRecords[i].Flags.HasFlag(DDMouseButtonFlags.EndedDrag))
				{
					ActiveTool.ReportEndDrag(DrawingCanvas, args);
					MouseButtonRecords[i].Flags ^= DDMouseButtonFlags.EndedDrag;
				}
			}
		}
	}

	public abstract class DDTool : DependencyObject, IDisposable
	{
		#region ToolCursor Property
		public static DependencyProperty ToolCursorProperty = DependencyProperty.Register(
			"ToolCursor", typeof(Cursor), typeof(DDTool), new PropertyMetadata(Cursors.Arrow));

		public Cursor ToolCursor
		{
			get { return (Cursor)this.GetValue(ToolCursorProperty); }
			set { this.SetValue(ToolCursorProperty, value); }
		}
		#endregion

		private readonly List<DependencyObject> _hitList = new List<DependencyObject>();
		
		#region AllowIntersectionHits Property
		public static DependencyProperty AllowIntersectionHitsProperty = DependencyProperty.Register(
			"AllowIntersectionHits", typeof(bool), typeof(DDTool), new PropertyMetadata(true));

		public bool AllowIntersectionHits
		{
			get { return (bool)this.GetValue(AllowIntersectionHitsProperty); }
			set { this.SetValue(AllowIntersectionHitsProperty, value); }
		}
		#endregion
			
		protected DDTool()
		{
			this.ToolCursor = Cursors.Arrow;
		}

		internal virtual void ReportMouseMove(Canvas drawingCanvas, MouseEventArgs args)
		{
		}

		internal virtual void ReportMouseDown(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
		}

		internal virtual void ReportMouseUp(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
		}

		internal virtual void ReportMouseClick(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
		}

		internal virtual void ReportStartDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
		}

		internal virtual void ReportMouseDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
		}

		internal virtual void ReportEndDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
		}

		internal virtual void ReportKeyDown(Canvas drawingCanvas, KeyEventArgs args)
		{
		}

		internal virtual void ReportKeyUp(Canvas drawingCanvas, KeyEventArgs args)
		{
		}	

		protected virtual DependencyObject[] PerformHitTest(Canvas drawingCanvas, Point p)
		{
			_hitList.Clear();

			if (drawingCanvas != null)
				VisualTreeHelper.HitTest(drawingCanvas, null, HitTestResult, new PointHitTestParameters(p));

			DependencyObject[] result = new DependencyObject[_hitList.Count];
			_hitList.CopyTo(result);
			return result;
		}

		protected virtual DependencyObject[] PerformHitTest(Canvas drawingCanvas, Rect r)
		{
			_hitList.Clear();

			if (drawingCanvas != null)
				VisualTreeHelper.HitTest(drawingCanvas, null, GeometryHitTestResult, new GeometryHitTestParameters(new RectangleGeometry(r)));

			DependencyObject[] result = new DependencyObject[_hitList.Count];
			_hitList.CopyTo(result);
			return result;
		}


		protected HitTestResultBehavior HitTestResult(HitTestResult result)
		{
			_hitList.Add(result.VisualHit);
			return HitTestResultBehavior.Continue;
		}


		protected HitTestResultBehavior GeometryHitTestResult(HitTestResult result)
		{
			IntersectionDetail id = (result as GeometryHitTestResult).IntersectionDetail;
			if (!AllowIntersectionHits && (id != IntersectionDetail.FullyInside))
				return HitTestResultBehavior.Continue;
			
			// Insert instead on Add to preserve order of elements from canvas
			_hitList.Insert(0, result.VisualHit);
			return HitTestResultBehavior.Continue;
		}

		protected virtual DependencyObject GetTopmostAtPointOfType(Canvas drawingCanvas, Type targetType, Point p)
		{
			DependencyObject[] hits = PerformHitTest(drawingCanvas, p);
			foreach (DependencyObject obj in hits)
				if (targetType.IsAssignableFrom(obj.GetType()))
					return obj;
			return null;
		}

		protected virtual DDObject[] GetAllInsideRectOfDDObject(Canvas drawingCanvas, Rect r)
		{
			DependencyObject[] hits = PerformHitTest(drawingCanvas, r);
			List<DDObject> results = new List<DDObject>();
			foreach (DependencyObject obj in hits)
				if (typeof(DDObject).IsAssignableFrom(obj.GetType()))
					results.Add(obj as DDObject);
			return results.ToArray();
		}

		#region IDisposable Members

		public virtual void Dispose()
		{
			_hitList.Clear();
		}

		#endregion
	}

	public delegate void DDTSFrameNotificationDelegate(DDTranslateAndScaleFrame refFrame);
	
	public class DDObjectClipboardFormat : List<string>
	{
		public static readonly string DataFormat = "DDObjectClipboardFormat";
	}
	
	public class DDPickTool : DDTool
	{	
		public static readonly string DataFormat = "DDObjectClipboardFormat";

		protected DDTranslateAndScaleFrame TSFrame = null;
		protected DDSelectionRect SelectionRect = null;

		protected DDTSFrameHandle GrabbedHandle = null;
		protected Point? DraggingPoint = null;

		protected Cursor PickCursor = new Cursor(System.IO.Path.Combine(DDTools.GetApplicationPath(), "Cursors", "Pick.cur"));
		
		public DDPickTool()
		{
			ToolCursor = PickCursor;
		}

		internal override void ReportMouseClick(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportMouseClick(drawingCanvas, args);

			if ((drawingCanvas == null) || (args.ChangedButton != MouseButton.Left))
				return;
			
			Point mousePos = args.GetPosition(drawingCanvas);
			DDObject affectedObject = GetTopmostAtPointOfType(drawingCanvas, typeof(DDObject), mousePos) as DDObject;
			SelectObjects(drawingCanvas, affectedObject);
		}

		internal override void ReportStartDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportStartDrag(drawingCanvas, args);
			if ((drawingCanvas == null) || (args.ChangedButton != MouseButton.Left))
				return;

			Point mousePos = args.GetPosition(drawingCanvas);
			DDObject affectedObject = GetTopmostAtPointOfType(drawingCanvas, typeof(DDObject), mousePos) as DDObject;

			if (affectedObject == null)
			{
				SelectionRect = new DDSelectionRect(mousePos, mousePos);
				drawingCanvas.Children.Add(SelectionRect);
			}
			else
			{
				if ((TSFrame == null) || (!TSFrame.Targets.Contains(affectedObject)))
					SelectObjects(drawingCanvas, affectedObject);
				if (TSFrame != null)
					DraggingPoint = new Point(mousePos.X - TSFrame.X, mousePos.Y - TSFrame.Y);
			}
		}

		internal override void ReportMouseDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportMouseDrag(drawingCanvas, args);
			if ((drawingCanvas == null) || (args.ChangedButton != MouseButton.Left))
				return;
			
			Point mousePos = args.GetPosition(drawingCanvas);
			if ((DraggingPoint.HasValue) && (TSFrame != null))
			{
				TSFrame.X = mousePos.X - DraggingPoint.Value.X;
				TSFrame.Y = mousePos.Y - DraggingPoint.Value.Y;
			}
			else if (SelectionRect != null)
			{
				SelectionRect.EndPoint = mousePos;
			}
		}

		internal override void ReportEndDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportEndDrag(drawingCanvas, args);
			if ((drawingCanvas == null) || (args.ChangedButton != MouseButton.Left))
				return;

			DraggingPoint = null;
			if (SelectionRect != null)
			{
				SelectObjects(drawingCanvas, GetAllInsideRectOfDDObject(drawingCanvas, SelectionRect.Bounds));
				drawingCanvas.Children.Remove(SelectionRect);
				SelectionRect = null;
			}
		}

		internal override void ReportKeyDown(Canvas drawingCanvas, KeyEventArgs args)
		{
			base.ReportKeyDown(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			if ((args.Key.HasFlag(Key.LeftShift)) || (args.Key.HasFlag(Key.RightShift)))
				if (SelectionRect != null)
					SelectionRect.Centered = true;
			if ((args.Key.HasFlag(Key.LeftCtrl)) || (args.Key.HasFlag(Key.RightCtrl)))
				if (SelectionRect != null)
					SelectionRect.Proportional = true;
		}

		internal override void ReportKeyUp(Canvas drawingCanvas, KeyEventArgs args)
		{
			base.ReportKeyUp(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			if ((args.Key.HasFlag(Key.LeftShift)) || (args.Key.HasFlag(Key.RightShift)))
				if (SelectionRect != null)
					SelectionRect.Centered = false;
			if ((args.Key.HasFlag(Key.LeftCtrl)) || (args.Key.HasFlag(Key.RightCtrl)))
				if (SelectionRect != null)
					SelectionRect.Proportional = false;
		}

		protected void ClearSelection()
		{
			if (TSFrame != null)
			{
				if (TSFrame.Parent is Canvas)
				{
					(TSFrame.Parent as Canvas).Children.Remove(TSFrame);
					TSFrame = null;
				}
				DraggingPoint = null;
				if (FrameChanged != null)
					FrameChanged(null);
			}
			
			if (SelectionRect != null)
			{
				if (SelectionRect.Parent is Canvas)
				{
					(SelectionRect.Parent as Canvas).Children.Remove(SelectionRect);
					SelectionRect = null;
				}
			}
		}

		public void SelectObjects(Canvas drawingCanvas, params DDObject[] targets)
		{
			if (drawingCanvas == null)
				return;

			ClearSelection();

			if ((targets != null) && (targets.Length > 0) && (targets[0] != null))
			{
				TSFrame = new DDTranslateAndScaleFrame(targets);
				drawingCanvas.Children.Add(TSFrame);
				if (FrameChanged != null)
					FrameChanged(TSFrame);
			}
		}

		public void DeleteSelection()
		{
			if ((TSFrame == null) || (TSFrame.Targets.Count <= 0))
				return;
			
			foreach (DDObjectTC otc in TSFrame.Targets)
				if (otc.Target.Parent is Canvas)
					(otc.Target.Parent as Canvas).Children.Remove(otc.Target);
			ClearSelection();

			if (ObjectsDeleted != null)
				ObjectsDeleted(this, new EventArgs());
		}

		public void CopyToClipboard()
		{
			if (TSFrame == null)
				return;
			
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<DDClipboardRoot>");
			foreach (DDObjectTC otc in TSFrame.Targets)
				sb.AppendLine(DDTools.WriteToXaml(otc.Target.CloneToCLR(), false));
			sb.AppendLine("</DDClipboardRoot>");
			Clipboard.SetData(DataFormat, sb.ToString());
		}

		public void CutToClipboard()
		{
			CopyToClipboard();
			DeleteSelection();
		}

		public void PasteFromClipboard(DDCanvas targetCanvas)
		{
			if (targetCanvas == null)
				return;

			if (Clipboard.ContainsData(DataFormat))
			{
				ClearSelection();
				string xaml = Clipboard.GetData(DataFormat) as string;
				object[] objects = DDTools.ReadMultipleFromXaml(xaml);
				
				List<DDObject> pastedObjects = new List<DDObject>();
				foreach (object o in objects)
				{
					DDObject ddo = DDObject.TryCreateFromCLR(o);
					if (ddo != null)
					{
						pastedObjects.Add(ddo);
						targetCanvas.Children.Add(ddo);
					}
				}

				SelectObjects(targetCanvas, pastedObjects.ToArray());
				
				if (pastedObjects.Count > 0)
					if (ObjectsAdded != null)
						ObjectsAdded(this, new EventArgs());
			}
		}

		public void CopyToClipboardAsXaml(Canvas carrierCanvas)
		{
			if (TSFrame == null)
				return;
			
			foreach (DDObjectTC otc in TSFrame.Targets)
				carrierCanvas.Children.Add(otc.Target.CloneToCLR());
			string xaml = DDTools.WriteToXaml(carrierCanvas, true);
			Clipboard.SetText(xaml);
		}

		public event DDTSFrameNotificationDelegate FrameChanged;
		public EventHandler ObjectsDeleted;
		public EventHandler ObjectsAdded;

		#region IDisposable Members

		public override void Dispose()
		{
			base.Dispose();
			ClearSelection();
			ToolCursor = null;
			PickCursor.Dispose();
			FrameChanged = null;
		}

		#endregion
	}

	public class DDEditTool : DDTool
	{	
		protected DDNodeObject TargetObject = null;
		protected Cursor EditCursor = new Cursor(System.IO.Path.Combine(DDTools.GetApplicationPath(), "Cursors", "Edit.cur"));
		
		public DDEditTool()
		{
			ToolCursor = EditCursor;
		}

		protected void SetTarget(DDNodeObject target)
		{
			if (TargetObject != null)
			{
				TargetObject.ManipulationState = DDManipulationState.Normal;
				TargetObject = null;
			}
			
			if (target != null)
			{
				TargetObject = target;
				TargetObject.ManipulationState = DDManipulationState.Edited;
			}
		}

		internal override void ReportMouseClick(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportMouseClick(drawingCanvas, args);

			if ((drawingCanvas == null) || (args.ChangedButton != MouseButton.Left))
				return;
			
			Point mousePos = args.GetPosition(drawingCanvas);
			DDNodeObject affectedObject = GetTopmostAtPointOfType(drawingCanvas, typeof(DDNodeObject), mousePos) as DDNodeObject;

			if (affectedObject != null)
			{
				if (affectedObject == TargetObject)
					SetTarget(null);
				else
					SetTarget(affectedObject);
			}
			else
				SetTarget(null);
		}

		public override void Dispose()
		{
			base.Dispose();

			SetTarget(null);
			ToolCursor = null;
			EditCursor.Dispose();
		}
	}

	public abstract class DDNewObjectTool : DDTool
	{	
		protected DDNewObjectRect NORect = null;
		
		public DDNewObjectTool()
		{
			ToolCursor = Cursors.Cross;
		}

		protected abstract DDObject CreateTargetObject();
		
		protected void ClearRect()
		{
			if (NORect != null)
			{
				if ((NORect.Parent != null) && (NORect.Parent is Canvas))
					(NORect.Parent as Canvas).Children.Remove(NORect);
				NORect.Target = null;
				NORect = null;

				if (ObjectCreated != null)
					ObjectCreated(this, new EventArgs());
			}
		}
		
		protected void NewRect(Canvas drawingCanvas, Point startPoint)
		{
			if (drawingCanvas == null)
				return;
			
			ClearRect();
			DDObject target = CreateTargetObject();
			drawingCanvas.Children.Add(target);
			NORect = new DDNewObjectRect(startPoint, target);
			NORect.Target = target;
			drawingCanvas.Children.Add(NORect);
		}

		internal override void ReportStartDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportStartDrag(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			Point mousePos = args.GetPosition(drawingCanvas);
			NewRect(drawingCanvas, mousePos);
		}

		internal override void ReportMouseDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportMouseDrag(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			if (NORect != null)
				NORect.EndPoint = args.GetPosition(drawingCanvas);
		}

		internal override void ReportEndDrag(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportEndDrag(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			ClearRect();
		}

		internal override void ReportKeyDown(Canvas drawingCanvas, KeyEventArgs args)
		{
			base.ReportKeyDown(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			if ((args.Key.HasFlag(Key.LeftShift)) || (args.Key.HasFlag(Key.RightShift)))
				if (NORect != null)
					NORect.Centered = true;
			if ((args.Key.HasFlag(Key.LeftCtrl)) || (args.Key.HasFlag(Key.RightCtrl)))
				if (NORect != null)
					NORect.Proportional = true;
		}

		internal override void ReportKeyUp(Canvas drawingCanvas, KeyEventArgs args)
		{
			base.ReportKeyUp(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			if ((args.Key.HasFlag(Key.LeftShift)) || (args.Key.HasFlag(Key.RightShift)))
				if (NORect != null)
					NORect.Centered = false;
			if ((args.Key.HasFlag(Key.LeftCtrl)) || (args.Key.HasFlag(Key.RightCtrl)))
				if (NORect != null)
					NORect.Proportional = false;
		}

		public override void Dispose()
		{
			base.Dispose();

			ClearRect();
		}

		public event EventHandler ObjectCreated;
	}

	public class DDRectangleTool : DDNewObjectTool
	{
		public static DDRectangle PrototypeRectangle = new DDRectangle();

		static DDRectangleTool()
		{
			PrototypeRectangle.Fill = Brushes.White;
			PrototypeRectangle.Stroke = Brushes.Black;
			PrototypeRectangle.StrokeThickness = 1;
		}
		
		protected override DDObject CreateTargetObject()
		{
			return PrototypeRectangle.Clone() as DDObject;
		}
	}

	public class DDEllipseTool : DDNewObjectTool
	{
		public static DDEllipse PrototypeEllipse = new DDEllipse();

		static DDEllipseTool()
		{
			PrototypeEllipse.Fill = Brushes.White;
			PrototypeEllipse.Stroke = Brushes.Black;
			PrototypeEllipse.StrokeThickness = 1;
		}

		protected override DDObject CreateTargetObject()
		{
			return PrototypeEllipse.Clone() as DDObject;
		}
	}

	public class DDPenGuideLine : Shape
	{
		protected LineGeometry StoredGeometry;
		
		#region StartPoint Property
		public static DependencyProperty StartPointProperty = DependencyProperty.Register(
			"StartPoint", typeof(Point), typeof(DDPenGuideLine),
			new PropertyMetadata(new Point(), AnyPropertyChangedCallback));

		public Point StartPoint
		{
			get { return (Point)this.GetValue(StartPointProperty); }
			set { this.SetValue(StartPointProperty, value); }
		}
		#endregion

		#region EndPoint Property
		public static DependencyProperty EndPointProperty = DependencyProperty.Register(
			"EndPoint", typeof(Point), typeof(DDPenGuideLine),
			new PropertyMetadata(new Point(), AnyPropertyChangedCallback));

		public Point EndPoint
		{
			get { return (Point)this.GetValue(EndPointProperty); }
			set { this.SetValue(EndPointProperty, value); }
		}
		#endregion

		private static void AnyPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDPenGuideLine sender = (DDPenGuideLine)d;
			sender.InvalidateVisual();
		}

		public DDPenGuideLine()
		{
			Stroke = Brushes.Black;
			StrokeThickness = 2;
			StrokeDashArray = new DoubleCollection { 2, 2 };
			StrokeEndLineCap = PenLineCap.Round;

			StoredGeometry = new LineGeometry();
			BindingOperations.SetBinding(StoredGeometry, LineGeometry.StartPointProperty,
				new Binding("StartPoint") { Source = this });
			BindingOperations.SetBinding(StoredGeometry, LineGeometry.EndPointProperty,
				new Binding("EndPoint") { Source = this });
		}

		protected void UpdateGeometry()
		{
			StoredGeometry.StartPoint = StartPoint;
			StoredGeometry.EndPoint = EndPoint;
		}

		protected override Geometry DefiningGeometry
		{
			get { return StoredGeometry; }
		}
	}
	
	public class DDPenTool : DDTool
	{	
		public static DDNodeObject PrototypeNodeObject = new DDPolyline();
		static DDPenTool()
		{
			PrototypeNodeObject.Fill = Brushes.White;
			PrototypeNodeObject.Stroke = Brushes.Black;
			PrototypeNodeObject.StrokeThickness = 1;
		}

		#region IsDrawing Property
		public static DependencyProperty IsDrawingProperty = DependencyProperty.Register(
			"IsDrawing", typeof(bool), typeof(DDPenTool),
			new PropertyMetadata(false, IsDrawingPropertyChangedCallback));

		private static void IsDrawingPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDPenTool sender = (DDPenTool)d;
			bool newValue = (bool)args.NewValue;

			if (newValue)
				sender.ToolCursor = sender.PenCursor;
			else
				sender.ToolCursor = sender.EditCursor;
		}
	
		public bool IsDrawing
		{
			get { return (bool)this.GetValue(IsDrawingProperty); }
			set { this.SetValue(IsDrawingProperty, value); }
		}
		#endregion

		#region CreateClosed Property
		public static DependencyProperty CreateClosedProperty = DependencyProperty.Register(
			"CreateClosed", typeof(bool), typeof(DDPenTool), new PropertyMetadata(true));

		public bool CreateClosed
		{
			get { return (bool)this.GetValue(CreateClosedProperty); }
			set { this.SetValue(CreateClosedProperty, value); }
		}
		#endregion

		protected DDNodeObject TargetObject = null;
		protected DDPenGuideLine GuideLine = null;
		
		protected Cursor PenCursor = new Cursor(System.IO.Path.Combine(DDTools.GetApplicationPath(), "Cursors", "Pen.cur"));
		protected Cursor EditCursor = new Cursor(System.IO.Path.Combine(DDTools.GetApplicationPath(), "Cursors", "Edit.cur"));

		public DDPenTool()
		{
			ToolCursor = EditCursor;
		}

		protected DDNodeObject CreateNewObject()
		{
			if (CreateClosed)
				return (PrototypeNodeObject as DDPolyline).CloneToPolygon();
			else
				return (PrototypeNodeObject as DDPolyline).Clone() as DDPolyline;
		}

		protected void EndDrawing()
		{
			if (TargetObject != null)
			{
				if (TargetObject.Nodes.Count < 2)
				{
					if (TargetObject.Parent is Canvas)
						(TargetObject.Parent as Canvas).Children.Remove(TargetObject);
				}
				else
					if (ObjectCreated != null)
						ObjectCreated(this, new EventArgs());
				TargetObject = null;
			}
			
			if ((GuideLine != null) && (GuideLine.Parent is Canvas))
			{
				(GuideLine.Parent as Canvas).Children.Remove(GuideLine);
				GuideLine = null;
			}
			
			IsDrawing = false;
		}

		protected void StartDrawing(Canvas drawingCanvas)
		{
			if (drawingCanvas == null)
				return;

			IsDrawing = true;
			
			if (TargetObject == null)
			{
				TargetObject = CreateNewObject();
				drawingCanvas.Children.Add(TargetObject);
			}

			if (GuideLine == null)
			{
				GuideLine = new DDPenGuideLine();
				if (TargetObject.Nodes.Count > 0)
				{
					GuideLine.StartPoint = TargetObject.Nodes.Last.Point.Point;
					GuideLine.EndPoint = TargetObject.Nodes.Last.Point.Point;
				}
				drawingCanvas.Children.Add(GuideLine);
			}
		}

		protected void DrawNode(Canvas drawingCanvas, Point p)
		{
			if (drawingCanvas == null)
				return;

			StartDrawing(drawingCanvas);

			DDNodeWithHandles newNode = new DDNodeWithHandles(drawingCanvas, new DDPoint(p));
			TargetObject.Nodes.Add(newNode);
			GuideLine.StartPoint = p;
			GuideLine.EndPoint = p;
		}

		internal override void ReportMouseClick(Canvas drawingCanvas, MouseButtonEventArgs args)
		{
			base.ReportMouseClick(drawingCanvas, args);
			if (drawingCanvas == null)
				return;
			
			Point mousePos = args.GetPosition(drawingCanvas);
			if (args.ChangedButton == MouseButton.Left)
			{
				DrawNode(drawingCanvas, mousePos);
			}
			else if (args.ChangedButton == MouseButton.Right)
			{
				if (GuideLine != null)
					EndDrawing();
				else
				{
					DDNodeObject affectedObject = GetTopmostAtPointOfType(drawingCanvas, typeof(DDNodeObject), mousePos) as DDNodeObject;
					if (affectedObject != null)
					{
						TargetObject = affectedObject;
						StartDrawing(drawingCanvas);
					}
				}
			}
			else if (args.ChangedButton == MouseButton.Right)
			{

			}
		}

		internal override void ReportMouseMove(Canvas drawingCanvas, MouseEventArgs args)
		{
			base.ReportMouseMove(drawingCanvas, args);
			if (drawingCanvas == null)
				return;

			if (GuideLine != null)
			{
				Point mousePos = args.GetPosition(drawingCanvas);
				GuideLine.EndPoint = mousePos;
			}
		}

		public event EventHandler ObjectCreated;

		public override void Dispose()
		{
			base.Dispose();

			EndDrawing();
			PenCursor.Dispose();
			EditCursor.Dispose();
		}
	}
}