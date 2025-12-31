using System.Collections.Generic;

namespace ProjetDelta.UI
{
    /// <summary>
    /// UI facultative pour recevoir des lignes de log segmentées (clics, metadata).
    /// Les implementations non interessees peuvent ignorer.
    /// </summary>
    public interface IUILogTarget
    {
        void AddLogLine(IEnumerable<UILogSegment> segments);
    }
}
