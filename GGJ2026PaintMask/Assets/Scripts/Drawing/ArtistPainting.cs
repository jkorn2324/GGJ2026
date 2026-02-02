using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace GGJ2026.Painting
{
    public sealed class ArtistPainting
    {
        #region stroke

        public struct Tape
        {
            public readonly int LineIndex;
            public readonly int StartStrokeIndex;
            public int AffectedStrokeCount;
            public bool IsFinished;

            public readonly bool IsValid => LineIndex != -1;

            public Tape( int lineIndex, int startStrokeIndex, int affectedStrokeCount, bool isFinished)
            {
                LineIndex = lineIndex;
                StartStrokeIndex = startStrokeIndex;
                AffectedStrokeCount = affectedStrokeCount;
                IsFinished = isFinished;
            }
        }

        public struct Stroke
        {
            public readonly int LineIndex;
            
            public readonly bool IsValid => LineIndex != -1;

            public Stroke(int lineIndex)
            {
                LineIndex = lineIndex;
            }
        }

        public struct ActiveLineInfo
        {
            public ArtistLineInfo Line;
            public readonly bool IsTape;

            public ActiveLineInfo(ArtistLineInfo inLine, bool inIsTape)
            {
                Line = inLine;
                IsTape = inIsTape;
            }
        }

        public struct EndLineInfo
        {
            public readonly int TypeIndex;
            public readonly bool IsTape;

            public readonly int TapeIndex => IsTape ? TypeIndex : -1;

            public readonly int StrokeIndex => IsTape ? StrokeIndex : -1;

            public readonly bool IsValid => TypeIndex != -1;
            
            public EndLineInfo(int typeIndex, bool isTape)
            {
                TypeIndex = typeIndex;
                IsTape = isTape;
            }
        }
        
        #endregion
        
        #region statics
        
        private static readonly ObjectPool<ArtistPainting> Pool = new ObjectPool<ArtistPainting>(() => new ArtistPainting());

        public static ArtistPainting New(in Vector2 paintingSize)
        {
            var painting = Pool.Get();
            painting.Initialize(paintingSize);
            return painting;
        }

        public static void Release(ref ArtistPainting painting)
        {
            if (painting != null)
            {
                painting.DeInitialize();
                Pool.Release(painting);
            }
            painting = null;
        }
        
        #endregion
        
        #region data_structures

        public struct Listener
        {
            public System.Action<ArtistPainting> OnChanged;
            
            public void Initialize(ArtistPainting painting, bool invokeFunctions = false)
            {
                if (painting == null || !painting.IsInitialized)
                {
                    return;
                }
                painting.OnChanged += OnChanged;
                if (invokeFunctions)
                {
                    OnChanged?.Invoke(painting);
                }
            }
            public void DeInitialize(ArtistPainting painting)
            {
                if (painting == null)
                {
                    return;
                }
                painting.OnChanged -= OnChanged;
            }
        }
        
        #endregion

        /// <summary>
        /// Called when the painting has been changed.
        /// </summary>
        public event System.Action<ArtistPainting> OnChanged;

        private ActiveLineInfo? _currentLine = null;
        private List<ArtistLineInfo> _lines;
        private List<Tape> _tapeIndices;
        private List<Stroke> _strokeIndices;

        public Vector2 PaintingSize { get; private set; }
        
        public Rect PaintingRect => new Rect(Vector2.zero, PaintingSize);

        /// <summary>
        /// The current line.
        /// </summary>
        public ActiveLineInfo? CurrentLine => _currentLine;
        
        public bool IsInitialized { get; private set; } = false;
        
        public int StrokeCount => _strokeIndices?.Count ?? 0;
        
        public int LineCount => _lines?.Count ?? 0;
        
        public int TapeCount => _tapeIndices?.Count ?? 0;

        /// <summary>
        /// The Active Tape Count.
        /// </summary>
        public int ActiveTapeCount
        {
            get
            {
                if (_tapeIndices == null)
                {
                    return 0;
                }
                var totalTapeCount = 0;
                for (var index = 0; index < _tapeIndices.Count; ++index)
                {
                    var tape = _tapeIndices[index];
                    if (tape.IsFinished)
                    {
                        totalTapeCount++;
                    }
                }
                return totalTapeCount;
            }
        }
        
        private ArtistPainting() { }

        private void Initialize(in Vector2 paintingSize)
        {
            if (IsInitialized)
            {
                return;
            }
            PaintingSize = paintingSize;
            _lines = ListPool<ArtistLineInfo>.Get();
            _tapeIndices = ListPool<Tape>.Get();
            _strokeIndices = ListPool<Stroke>.Get();
            IsInitialized = true;
        }

        private void DeInitialize()
        {
            if (!IsInitialized)
            {
                return;
            }
            if (_lines != null)
            {
                ListPool<ArtistLineInfo>.Release(_lines);
                _lines = null;
            }
            if (_tapeIndices != null)
            {
                ListPool<Tape>.Release(_tapeIndices);
                _tapeIndices = null;
            }
            if (_strokeIndices != null)
            {
                ListPool<Stroke>.Release(_strokeIndices);
                _strokeIndices = null;
            }
            IsInitialized = false;
        }

        /// <summary>
        /// Gets the tape at a position.
        /// </summary>
        /// <param name="paintingPosition">The position.</param>
        /// <param name="radiusLeniency">The radius leniency.</param>
        /// <param name="filterOnlyActiveTape">Determines whether to only filter active tape.</param>
        /// <returns>The tape.</returns>
        public int GetClosestTapeIndexAt(in Vector2 paintingPosition, float radiusLeniency = 0.0f, bool filterOnlyActiveTape = true)
        {
            if (!IsInitialized)
            {
                return -1;
            }

            var closestIndex = -1;
            var closestSqrDistance = float.MaxValue;
            for (var tapeIndex = 0; tapeIndex < TapeCount; tapeIndex++)
            {
                var tape = _tapeIndices[tapeIndex];
                var currentLine = _lines[tape.LineIndex];
                if (!currentLine.IsPositionInStroke(paintingPosition, radiusLeniency, out var sqrDistance))
                {
                    continue;
                }
                if (filterOnlyActiveTape && tape.IsFinished)
                {
                    continue;
                }
                if (closestIndex == -1 || sqrDistance < closestSqrDistance)
                {
                    closestIndex = tapeIndex;
                    closestSqrDistance = sqrDistance;
                }
            }
            return closestIndex;
        }
        
        public ArtistLineInfo GetLine(int lineIndex)
        {
            return (_lines != null && lineIndex >= 0 && lineIndex < _lines.Count) 
                ? _lines[lineIndex] : default;
        }

        public Stroke GetStroke(int strokeIndex)
        {
            return (_strokeIndices != null && strokeIndex >= 0 && strokeIndex < _strokeIndices.Count)
                ? _strokeIndices[strokeIndex]
                : new Stroke(-1);
        }
        
        public bool BeginLine(ArtistLineInfo lineInfo, bool inIsTape)
        {
            // Cannot have stroke.
            if (_currentLine != null)
            {
                return false;
            }
            var rect = new Rect(Vector2.zero, PaintingSize);
            if (!rect.Contains(lineInfo.StartPosition))
            {
                return false;
            }
            var artistLineInfo = new ActiveLineInfo(lineInfo, inIsTape);
            _currentLine = artistLineInfo;
            OnChanged?.Invoke(this);
            return true;
        }
        
        public bool UpdateLine(in Vector2 currentPosition)
        {
            if (_currentLine == null)
            {
                return false;
            }
            var line = (ActiveLineInfo)_currentLine;
            var lineInfo = line.Line;
            lineInfo.EndPosition = currentPosition;
            line.Line = lineInfo;
            _currentLine = line;
            OnChanged?.Invoke(this);
            return true;
        }
        
        public EndLineInfo EndLine()
        {
            if (!IsInitialized || _currentLine == null)
            {
                return new EndLineInfo(-1, false);
            }
            var line = (ActiveLineInfo)_currentLine;
            var lineIndex = _lines?.Count ?? 0;
            _lines?.Add(line.Line);
            _currentLine = null;
            var strokeIndex = _strokeIndices?.Count ?? -1;
            if (line.IsTape)
            {
                var tapeIndex = _tapeIndices?.Count ?? -1;
                _tapeIndices?.Add(new Tape(lineIndex, strokeIndex, 0, false));
                OnChanged?.Invoke(this);
                return new EndLineInfo(tapeIndex, true);
            }
            _strokeIndices?.Add(new Stroke(lineIndex));
            OnChanged?.Invoke(this);
            return new EndLineInfo(strokeIndex, false);
        }

        public void Clear()
        {
            if (!IsInitialized)
            {
                return;
            }
            _tapeIndices?.Clear();
            _strokeIndices?.Clear();
            _lines?.Clear();
        }
        
        #region tape

        public Tape GetTape(int index)
        {
            return (_tapeIndices != null && index >= 0 && index < _tapeIndices.Count)
                ? _tapeIndices[index]
                : new Tape(-1, -1, 0, true);
        }

        /// <summary>
        /// Removes the tape.
        /// </summary>
        /// <param name="tapeIndex">The tape index.</param>
        /// <returns>Tries to remove the tape.</returns>
        public bool TryRemoveTape(int tapeIndex)
        {
            if (!IsInitialized || _lines == null || _tapeIndices == null || _strokeIndices == null)
            {
                return false;
            }
            var tape = GetTape(tapeIndex);
            if (!tape.IsValid)
            {
                return false;
            }
            var difference = _strokeIndices.Count - 1 - tape.StartStrokeIndex;
            tape.AffectedStrokeCount = difference;
            tape.IsFinished = true;
            _tapeIndices[tapeIndex] = tape;
            OnChanged?.Invoke(this);
            return true;
        }
        
        #endregion
    }
}