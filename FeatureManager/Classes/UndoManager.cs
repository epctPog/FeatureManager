using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FeatureManager.Models;

namespace FeatureManager.Classes.Undo
{
    public class UndoManager
    {
        private readonly Stack<List<FeatureEntry>> undoStack = new();
        private readonly Stack<List<FeatureEntry>> redoStack = new();

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public void SaveState(IEnumerable<FeatureEntry> current)
        {
            undoStack.Push(current.Select(f => f.Clone()).ToList());
            redoStack.Clear();
        }

        public List<FeatureEntry> Undo(IEnumerable<FeatureEntry> current)
        {
            if (!CanUndo) return current.ToList();

            redoStack.Push(current.Select(f => f.Clone()).ToList());
            return undoStack.Pop();
        }

        public List<FeatureEntry> Redo(IEnumerable<FeatureEntry> current)
        {
            if (!CanRedo) return current.ToList();

            undoStack.Push(current.Select(f => f.Clone()).ToList());
            return redoStack.Pop();
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}