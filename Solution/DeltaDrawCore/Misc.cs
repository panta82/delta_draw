using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Windows.Controls;
using System.Collections;
using System.Windows.Shapes;
using System.Xml;
using System.Windows.Markup;
using System.Text;

namespace Pantas.DeltaDraw.Core
{
	#region Custom collections

	#region DDNodeCollection
	public class DDNodeCollection : IList<DDNode>, IList, INotifyPropertyChanged, INotifyCollectionChanged, INotifyPropertyChanging
	{
		readonly List<DDNode> _data = new List<DDNode>();

		public bool LoopAtStart { get; set; }
		public bool LoopAtEnd { get; set; }

		public DDNode First
		{
			get
			{
				return Count > 0 ? _data[0] : null;
			}
		}

		public DDNode Last
		{
			get
			{
				return Count > 0 ? _data[Count - 1] : null;
			}
		}

		public DDNodeCollection()
		{
			LoopAtStart = false;
			LoopAtEnd = false;
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void OnPropertyChanging(string propertyName)
		{
			if (PropertyChanging != null)
				PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, object[] newItems, object[] oldItems)
		{
			if (CollectionChanged != null)
			{
				switch (action)
				{
					case NotifyCollectionChangedAction.Add:
						CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItems));
						break;
					case NotifyCollectionChangedAction.Reset:
					case NotifyCollectionChangedAction.Remove:
						CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, oldItems));
						break;
					case NotifyCollectionChangedAction.Replace:
						CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItems, oldItems));
						break;
				}
			}
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, object newItem, object oldItem)
		{
			object[] newItems = newItem != null ? new object[] { newItem } : new object[] { };
			object[] oldItems = oldItem != null ? new object[] { oldItem } : new object[] { };
			OnCollectionChanged(action, newItems, oldItems);
		}

		public DDNode GetNodeAfter(DDNode refNode)
		{
			int index = IndexOf(refNode);
			if (index >= 0)
				if ((index == Count - 1) && (LoopAtEnd))
					return First;
				else if (index < Count - 1)
					return this[index + 1];
			return null;
		}

		public DDNode GetNodeBefore(DDNode refNode)
		{
			int index = IndexOf(refNode);
			if (index >= 0)
				if ((index == 0) && (LoopAtStart))
					return Last;
				else if (index > 0)
					return this[index - 1];
			return null;
		}

		#region IList<DDNode> Members

		public int IndexOf(DDNode item)
		{
			return _data.IndexOf(item);
		}

		public void Insert(int index, DDNode item)
		{
			OnPropertyChanging("Count");
			_data.Insert(index, item);
			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, null);
			OnPropertyChanged("Count");
		}

		public void RemoveAt(int index)
		{
			OnPropertyChanging("Count");
			DDNode save = _data[index];
			_data.RemoveAt(index);
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, null, save);
			OnPropertyChanged("Count");
		}

		public DDNode this[int index]
		{
			get
			{
				return _data[index];
			}
			set
			{
				OnPropertyChanging("Count");
				DDNode save = _data[index];
				_data[index] = value;
				OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, save);
				OnPropertyChanged("this");
			}
		}

		#endregion

		#region ICollection<DDNode> Members

		public void Add(DDNode item)
		{
			OnPropertyChanging("Count");
			_data.Add(item);
			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, null);
			OnPropertyChanged("Count");
		}

		public void Clear()
		{
			DDNode[] oldItems = new DDNode[_data.Count];
			CopyTo(oldItems, 0);
			_data.Clear();
			OnPropertyChanging("Count");
			// ?: Reset action must be initialized with no changed items. Parameter name: action
			//OnCollectionChanged(NotifyCollectionChangedAction.Reset, null, oldItems);
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, null, oldItems);
		}

		public bool Contains(DDNode item)
		{
			return _data.Contains(item);
		}

		public void CopyTo(DDNode[] array, int arrayIndex)
		{
			_data.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _data.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(DDNode item)
		{
			if (_data.Contains(item))
			{
				OnPropertyChanging("Count");
				if (_data.Remove(item))
				{
					OnCollectionChanged(NotifyCollectionChangedAction.Remove, null, item);
					OnPropertyChanged("Count");
					return true;
				}
			}
			return false;
		}

		#endregion

		#region IList Members

		public int Add(object value)
		{
			if (value is DDNode)
			{
				((IList<DDNode>)this).Add(value as DDNode);
				return ((IList<DDNode>)this).Count - 1;
			}
			else
				return -1;
		}

		public bool Contains(object value)
		{
			if (value is DDNode)
				return ((IList<DDNode>)this).Contains(value as DDNode);
			else
				return false;
		}

		public int IndexOf(object value)
		{
			if (value is DDNode)
				return ((IList<DDNode>)this).IndexOf(value as DDNode);
			else
				return -1;
		}

		public void Insert(int index, object value)
		{
			if (value is DDNode)
				((IList<DDNode>)this).Insert(index, value as DDNode);
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public void Remove(object value)
		{
			if (value is DDNode)
				((IList<DDNode>)this).Remove(value as DDNode);
		}

		object IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				if (value is DDNode)
				 this[index] = value as DDNode;
			}
		}

		#endregion

		#region ICollection Members

		public void CopyTo(Array array, int index)
		{
			//not needed
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { return null; }
		}

		#endregion

		#region IEnumerable<DDNode> Members

		public IEnumerator<DDNode> GetEnumerator()
		{
			return _data.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _data.GetEnumerator();
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region INotifyCollectionChanged Members

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion

		#region INotifyPropertyChanging Members

		public event PropertyChangingEventHandler PropertyChanging;

		#endregion
	} 
	#endregion
	
	#region DDTransformationCacheCollection

	public class DDTransformationCacheCollection : IEnumerable<DDObjectTC>, INotifyPropertyChanged
	{
		readonly List<DDObjectTC> _data = new List<DDObjectTC>();

		Rect? _targetBoundsCache = null;
		protected Rect? TargetBoundsCache
		{
			get { return _targetBoundsCache; }
			set
			{
				bool changed = false;
				if (value != _targetBoundsCache)
					changed = true;
				_targetBoundsCache = value;
				if (changed)
				{
					OnTargetBoundsChanged();
					OnPropertyChanged("TargetBounds");
				}
			}
		}

		public Rect TargetBounds
		{
			get
			{
				if (!TargetBoundsCache.HasValue)
					TargetBoundsCache = CalculateTargetBounds();
				return TargetBoundsCache.Value;
			}
		}

		public int Count
		{
			get
			{
				return _data.Count;
			}
		}

		public DDObject this[int index]
		{
			get 
			{
				if ((index >= 0) && (index < Count))
					return _data[0].Target;
				else
					return null;
			}
		}

		protected Rect CalculateTargetBounds()
		{
			if (_data.Count > 0)
			{
				Rect? res = null;
				for (int i = 0; i < _data.Count; i++)
				{
					if (double.IsInfinity(_data[i].Bounds.Left) ||
						double.IsInfinity(_data[i].Bounds.Top))
						continue;
					if (res.HasValue)
					{
						Rect temp = res.Value;
						temp.Union(_data[i].Bounds);
						res = temp;
					}
					else
						res = _data[i].Bounds;
				}
				return res.HasValue ? res.Value : new Rect(0,0,1,1);
			}
			return new Rect();
		}

		public void Add(DDObject newObject)
		{
			TargetBoundsCache = null;
			if (newObject is DDNodeObject)
				_data.Add(DDNodeObjectTC.CreateInitializedInstance<DDNodeObjectTC>(newObject));
			else
				_data.Add(DDObjectTC.CreateInitializedInstance<DDObjectTC>(newObject));
			OnPropertyChanged("Count");
		}

		public void Clear()
		{
			foreach (DDObjectTC o in _data)
				o.Clear();
			TargetBoundsCache = null;
			_data.Clear();
			OnPropertyChanged("Count");
		}

		public bool Contains(DDObject o)
		{
			foreach (DDObjectTC otc in _data)
				if (otc.Target == o)
					return true;
			return false;
		}

		protected virtual void OnTargetBoundsChanged()
		{
			if (TargetBoundsChanged != null)
				TargetBoundsChanged(this, new EventArgs());
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		#region IEnumerable<DDNodeObjectTransformationState> Members

		public IEnumerator<DDObjectTC> GetEnumerator()
		{
			return _data.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _data.GetEnumerator();
		}

		#endregion

		public event EventHandler TargetBoundsChanged;

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	} 
	#endregion
	
	#endregion

	#region Converters

	#region ConnectedVisibilityConverter
	[ValueConversion(typeof(Visibility), typeof(Visibility))]
	public class ConnectedVisibilityConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			foreach (object o in values)
				if ((Visibility)o != Visibility.Visible)
					return Visibility.Collapsed;
			return Visibility.Visible;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException("This shouldn't happen, it's a one-way binding.");
		}

		#endregion
	}
	#endregion

	#endregion

	#region Interfaces

	public interface IDDChangeTracking
	{
		void StartChange(string changeSource);
		void EndChange();
	}

	#endregion

	#region Exceptions

	public class DDException : ApplicationException
	{
		public DDException(string message)
			: base(message)
		{
		}
	}

	public class DDChangeNotAnnouncedException : DDException
	{
		public DDChangeNotAnnouncedException()
			: base("You can't modify an IDDChangeTracking object without announcing it first by calling StartChange(string source).")
		{
		}
	}

	public class DDInvalidChangeSourceName : DDException
	{
		public DDInvalidChangeSourceName()
			: base("Announced change source can's be an empty string or null.")
		{
		}
	}

	public class DDStarEndChangeOutOfOrder : DDException
	{
		public DDStarEndChangeOutOfOrder()
			: base("StartChange() or EndChange() were called out of order. StartChange() can only be called after EndChange() and vice versa.")
		{
		}
	}

	#endregion

	#region Controls
	public class DDCanvas : Canvas
	{	
		#region IsChanged Property
		public static DependencyProperty IsChangedProperty = DependencyProperty.Register(
			"IsChanged", typeof(bool), typeof(DDCanvas), new PropertyMetadata(false));
	
		public bool IsChanged
		{
			get { return (bool)this.GetValue(IsChangedProperty); }
			set { this.SetValue(IsChangedProperty, value); }
		}
		#endregion
		
		// Courtesy of http://illef.tistory.com/entry/Canvas-supports-ScrollViewer
		
		protected override Size MeasureOverride(Size constraint)
		{
			Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			double maxHeight = 0;
			double maxWidth = 0;

			foreach (UIElement element in base.InternalChildren)
			{
				if (element != null)
				{
					element.Measure(availableSize);
					double left = Canvas.GetLeft(element);
					double top = Canvas.GetTop(element);
					left += element.DesiredSize.Width;
					top += element.DesiredSize.Height;

					maxWidth = maxWidth < left ? left : maxWidth;
					maxHeight = maxHeight < top ? top : maxHeight;
				}
			}

			return new Size { Height = maxHeight, Width = maxWidth };
		}

		public Canvas CloneToCLR(bool shallow)
		{
			Canvas canvas = new Canvas();
			canvas.Width = Width;
			canvas.Height = Height;
			if (Background != null)
				canvas.Background = Background.Clone();
			else
				canvas.Background = null;
			// TODO: Add more if needed

			if (!shallow)
				foreach (UIElement element in Children)
					if (element is DDObject)
						canvas.Children.Add((element as DDObject).CloneToCLR());
			return canvas;
		}

		public void SetFromCanvas(Canvas source)
		{
			Width = source.Width;
			Height = source.Height;
			if (source.Background != null)
				Background = source.Background.Clone();
			else
				Background = null;
			// TODO: Add more if needed

			foreach (UIElement element in source.Children)
			{
				DDObject newObject = DDObject.TryCreateFromCLR(element);
				if (newObject != null)
					Children.Add(newObject);
			}
		}

		protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
		{
			base.OnVisualChildrenChanged(visualAdded, visualRemoved);

			if (visualAdded is DDObject)
			{
				(visualAdded as DDObject).PropertyChanged += DDCanvas_PropertyChanged;
				OnRelevantPropertyChanged(this, new PropertyChangedEventArgs("Children"));
			}
			if (visualRemoved is DDObject)
			{
				(visualRemoved as DDObject).PropertyChanged -= DDCanvas_PropertyChanged;
				OnRelevantPropertyChanged(this, new PropertyChangedEventArgs("Children"));
			}
		}

		private string[] _propertiesToWatch = { "Bounds", "Nodes", "Fill", "Stroke", "StrokeThickness", "CornerRadius" };

		void DDCanvas_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			foreach (string s in _propertiesToWatch)
				if (s == e.PropertyName)
				{
					OnRelevantPropertyChanged(sender, e);
					break;
				}
		}

		protected void OnRelevantPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			IsChanged = true;
			if (RelevantPropertyChanged != null)
				RelevantPropertyChanged(sender, e);
		}

		public event PropertyChangedEventHandler RelevantPropertyChanged;
	}
	#endregion

	#region Misc tools

	#region DDPoint
	/// <summary>
	/// Reference version of standard Point (which is Struct).
	/// 
	/// </summary>
	[TypeConverter(typeof(DDPointConverter))]
	public class DDPoint : INotifyPropertyChanged, IEquatable<DDPoint>
	{
		private double  _x;
		public double X
		{
			get { return _x; }
			set
			{ 
				_x = value;
				_point = new Point(value, Y);
				OnPropertyChanged("X");
				OnPropertyChanged("Point");
			}
		}

		private double  _y;
		public double Y
		{
			get { return _y; }
			set
			{ 
				_y = value;
				_point = new Point(X, value);
				OnPropertyChanged("Y");
				OnPropertyChanged("Point");
			}
		}
				
		private Point _point = new Point();
		public Point Point
		{
			get { return _point; }
			set
			{
				_point = value;
				_x = value.X;
				_y = value.Y;
				OnPropertyChanged("X");
				OnPropertyChanged("Y");
				OnPropertyChanged("Point");
			}
		}

		public DDPoint()
		{
		}

		public DDPoint(double x, double y)
		{
			X = x;
			Y = y;
		}

		public DDPoint(Point p)
		{
			SetFrom(p);
		}

		public DDPoint(DDPoint p)
		{
			SetFrom(p);
		}

		public void SetFrom(Point p)
		{
			X = p.X;
			Y = p.Y;
		}

		public void SetFrom(DDPoint p)
		{
			if (p != null)
			{
				X = p.X;
				Y = p.Y;
			}
		}

        public static implicit operator Point? (DDPoint p)
        {
			return p != null ? new Point(p.X, p.Y) : (Point?)null;
        }

		public static implicit operator DDPoint(Point p)
        {
            return new DDPoint(p.X, p.Y);
        }

		public void Offset(double x, double y)
		{
			X += x;
			Y += y;
		}

		public void Offset(Vector v)
		{
			Offset(v.X, v.Y);
		}

		public override string ToString()
		{
			return string.Format("{0}x{1}", X, Y);
		}

		#region INotifyPropertyChanged Members

		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region IEquatable<DDPoint> Members

		public bool Equals(DDPoint other)
		{
			if (other == null)
				return false;
			else
				return ((X == other.X) && (Y == other.Y));
		}

		#endregion
	}

	public class DDPointConverter : TypeConverter
	{
		private readonly PointConverter _pointConverter = new PointConverter();

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return _pointConverter.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return _pointConverter.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (value is DDPoint)
			{
				Point p = (value as DDPoint).Point;
				return _pointConverter.ConvertTo(context, culture, p, destinationType);
			}
			else
				return null;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			Point p = (Point)_pointConverter.ConvertFrom(context, culture, value);
			return new DDPoint(p);
		}
	}
	
	#endregion

	#region DDTools
	public static class DDTools
	{
		// Calculates union of multiple rects
		// Unused

		public static Rect UnionOfRects(params Rect[] rects)
		{
			Rect res = new Rect();
			foreach (Rect rect in rects)
				res.Union(rect);
			return res;
		}

		// Distance between 2 points in space

		public static double DistanceBetweenPoints(Point p1, Point p2)
		{
			return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
		}

		// Finds the point on a Bezier curve (defined by points P1,P2 and control points C1,C2)
		// closest to the reference point outside the curve.
		// Precision of the search is determined by the sum of distances between the points.
		// Resulting distance is written in the out resultDistance parameter.

		public static Point ClosestPointOnBezierCurve(out double resultDistance, Point p1, Point c1, Point c2, Point p2, Point refPoint)
		{
			double maxLength = DistanceBetweenPoints(p1, c1) + DistanceBetweenPoints(c1, c2) + DistanceBetweenPoints(c2, p2);
			Point testPoint = new Point();
			Point resPoint = new Point();
			double lowestD = double.PositiveInfinity;
			for (double t = 0; t <= 1; t += (1D / maxLength))
			{
				testPoint.X = Math.Pow(1 - t, 3) * p1.X
					+ 3 * Math.Pow(1 - t, 2) * t * c1.X
					+ 3 * (1 - t) * Math.Pow(t, 2) * c2.X
					+ Math.Pow(t, 3) * p2.X;
				testPoint.Y = Math.Pow(1 - t, 3) * p1.Y
					+ 3 * Math.Pow(1 - t, 2) * t * c1.Y
					+ 3 * (1 - t) * Math.Pow(t, 2) * c2.Y
					+ Math.Pow(t, 3) * p2.Y;
				double d = DistanceBetweenPoints(testPoint, refPoint);

				if (d >= lowestD) continue;
				lowestD = d;
				resPoint = testPoint;
			}
			resultDistance = lowestD;
			return resPoint;
		}

		// Version without distance output

		public static Point ClosestPointOnBezierCurve(Point p1, Point c1, Point c2, Point p2, Point refPoint)
		{
			double dump;
			return ClosestPointOnBezierCurve(out dump, p1, c1, c2, p2, refPoint);
		}

		// Color conversion from a standard int (0..255 0..255 0..255 as R G B)
		// Courtesy of http://stackoverflow.com/questions/1133862/converting-int-to-color-in-c-for-silverlights-writeablebitmap

		public static Color ColorFromInt(int colorAsInt)
		{
			return Color.FromArgb(
				(byte)((colorAsInt >> 24) & 0xff), // shift right 3 bytes & take the rightmost byte (A)
				(byte)((colorAsInt >> 16) & 0xff), // shift right 2 bytes & take the rightmost byte (R)
				(byte)((colorAsInt >> 8) & 0xff), // etc... (G)
				(byte)(colorAsInt & 0xff)); // (B)
		}

		// Reverse of the function above

		public static int ColorToInt(Color color)
		{
			return color.A << 24 | color.R << 16 | color.G << 8 | color.B;
		}

		// Returns the current executing application's path
		
		public static string GetApplicationPath()
		{
			return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		}

		public static string WriteToXaml(object o, bool indent)
		{
			StringBuilder sb = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			if (indent)
			{
				settings.Indent = true;
				settings.IndentChars = "\t";
			}
			settings.OmitXmlDeclaration = true;
			settings.Encoding = Encoding.UTF8;
			XamlDesignerSerializationManager dsm = new XamlDesignerSerializationManager(
				XmlWriter.Create(sb, settings));
			XamlWriter.Save(o, dsm);
			return sb.ToString();
		}

		public static object ReadFromXaml(string xaml)
		{
			try
			{
				ParserContext parserContext = new ParserContext();
				parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
				parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
				return XamlReader.Parse(xaml, parserContext);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static object[] ReadMultipleFromXaml(string xaml)
		{
			List<object> ret = new List<object>();
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xaml);
			if (doc.ChildNodes.Count > 0)
				foreach (XmlNode node in doc.ChildNodes[0].ChildNodes)
				{
					object o = ReadFromXaml(node.OuterXml);
					if (o != null)
						ret.Add(o);
				}
			return ret.ToArray();
		}
	}
	#endregion
	
	#endregion
}