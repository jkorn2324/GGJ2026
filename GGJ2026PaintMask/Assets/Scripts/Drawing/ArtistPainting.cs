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
            public int FinishedStrokeIndex;

            public readonly bool IsValid => LineIndex != -1;

            public Tape( int lineIndex, int startStrokeIndex, int finishedStrokeIndex)
            {
                LineIndex = lineIndex;
                StartStrokeIndex = startStrokeIndex;
                FinishedStrokeIndex = finishedStrokeIndex;
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

        public static ArtistPainting New()
        {
            var painting = Pool.Get();
            painting.Initialize();
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

        private ActiveLineInfo? _currentLine = null;
        private List<ArtistLineInfo> _lines;
        private List<Tape> _tapeIndices;
        private List<Stroke> _strokeIndices;
        
        public Vector2 PaintingSize => new Vector2(1200.0f, 720.0f);
        
        public Rect PaintingRect => new Rect(Vector2.zero, PaintingSize);

        /// <summary>
        /// The current line.
        /// </summary>
        public ActiveLineInfo? CurrentLine => _currentLine;
        
        public bool IsInitialized { get; private set; } = false;
        
        public int StrokeCount => _strokeIndices?.Count ?? 0;
        
        public int LineCount => _lines?.Count ?? 0;
        
        public int TapeCount => _tapeIndices?.Count ?? 0;
        
        private ArtistPainting() { }

        private void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
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
                _tapeIndices?.Add(new Tape(lineIndex, strokeIndex, -1));
                return new EndLineInfo(tapeIndex, true);
            }
            _strokeIndices?.Add(new Stroke(lineIndex));
            return new EndLineInfo(strokeIndex, false);
        }
        
        
        #region tape

        public Tape GetTape(int index)
        {
            return (_tapeIndices != null && index >= 0 && index < _tapeIndices.Count)
                ? _tapeIndices[index]
                : new Tape(-1, -1, -1);
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
            tape.FinishedStrokeIndex = _strokeIndices.Count - 1;
            _tapeIndices[tapeIndex] = tape;
            return true;
        }
        
        #endregion
        
        // TODO: Compare Paintings
    }
}