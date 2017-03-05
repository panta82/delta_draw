using System.Windows;
using System.Collections.Generic;
using System;

namespace Pantas.DeltaDraw.Application
{
	public class DDUndoManager<T> where T : class
	{
		private const int MaxUndoSteps = 20;

		readonly LinkedList<T> _data = new LinkedList<T>();
		LinkedListNode<T> _currentStep = null;

		public bool CanUndo()
		{
			return _currentStep != null;
		}

		public bool CanRedo()
		{
			if (_currentStep != null)
				return (_currentStep.Next != null);
			else
				return (_data.First != null);
		}

		public void AddStep(T value)
		{
			while (_data.Last != _currentStep)
				_data.RemoveLast();
			if (_data.Count >= MaxUndoSteps)
				_data.RemoveFirst();
			_data.AddLast(value);
			_currentStep = _data.Last;
		}

		public void Clear()
		{
			_data.Clear();
			_currentStep = null;
		}

		public T Undo(T currentState)
		{
			if (!CanUndo())
				return null;
			T save = _currentStep.Value;
			_currentStep.Value = currentState;
			_currentStep = _currentStep.Previous;
			return save;
		}

		public T Redo(T currentState)
		{
			if (!CanRedo())
				return null;
			if (_currentStep != null)
				_currentStep = _currentStep.Next;
			else
				_currentStep = _data.First;
			T save = _currentStep.Value;
			_currentStep.Value = currentState;
			return save;
		}
	}
}