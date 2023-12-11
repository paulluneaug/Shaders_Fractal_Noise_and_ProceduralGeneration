using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScriptExecutionTimeRecorder
{
    private readonly struct ScriptEvent
    {
        private readonly string m_eventName;
        private readonly double m_eventDuration;
        private readonly int m_frame;

        public ScriptEvent(string eventName, double eventDuration)
        {
            m_eventName = eventName;
            m_eventDuration = eventDuration;
            m_frame = Time.frameCount;
        }

        public override readonly string ToString()
        {
            return $"[Recorder] Event \"{m_eventName}\" took {m_eventDuration} ms | Frame {m_frame}";
        }
    }

    private List<ScriptEvent> m_events = null;

    private DateTime m_firstEventTime;
    private DateTime m_previousEventTime;

    public ScriptExecutionTimeRecorder()
    {
        m_events = new List<ScriptEvent>();
        ReinitTimes();
    }

    public void AddEvent(string eventName, bool logImmediatly = true)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError($"[Recorder] This event name is null or empty, it is not a valid name");
            return;
        }

        DateTime now = DateTime.Now;
        ScriptEvent newEvent = new ScriptEvent
        (
            eventName,
            (now - m_previousEventTime).TotalMilliseconds
        );
        m_events.Add(newEvent);

        if (logImmediatly)
        {
            Debug.Log(newEvent.ToString());
        }

        m_previousEventTime = now;
    }

    public void LogLastEvent()
    {
        if (m_events.Count > 0)
        {
            Debug.Log(m_events.Last().ToString());
        }
        else
        {
            Debug.LogWarning("[Recorder] No events to log");
        }
    }

    public void LogAllEvents()
    {
        string log = string.Empty;
        foreach (ScriptEvent e in m_events)
        {
            log += e.ToString() + "\n";
        }
        log += GetAllEventsTimeSpan();

        Debug.Log(log);
    }

    public void LogAllEventsTimeSpan()
    {
        Debug.Log(GetAllEventsTimeSpan());
    }

    public void Reset()
    {
        m_events.Clear();
        ReinitTimes();
    }

    private void ReinitTimes()
    {
        m_firstEventTime = DateTime.Now;
        m_previousEventTime = m_firstEventTime;
    }

    private string GetAllEventsTimeSpan()
    {
        return $"[Recorder] All the logged events took {(m_previousEventTime - m_firstEventTime).TotalMilliseconds} ms so far";
    }
}
