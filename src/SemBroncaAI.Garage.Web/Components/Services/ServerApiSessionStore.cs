using System.Collections.Concurrent;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public interface IServerApiSessionStore
{
    void Set(string sessionId, ApiSession session);
    bool TryGet(string sessionId, out ApiSession? session);
    void Remove(string sessionId);
}

public sealed class ServerApiSessionStore : IServerApiSessionStore
{
    private readonly ConcurrentDictionary<string, ApiSession> _sessions = new();

    public void Set(string sessionId, ApiSession session)
    {
        RemoveExpiredSessions(DateTimeOffset.UtcNow);
        _sessions[sessionId] = session;
    }

    public bool TryGet(string sessionId, out ApiSession? session)
    {
        if (!_sessions.TryGetValue(sessionId, out session))
            return false;

        if (session.ExpiresAt > DateTimeOffset.UtcNow)
            return true;

        _sessions.TryRemove(sessionId, out _);
        session = null;
        return false;
    }

    public void Remove(string sessionId) => _sessions.TryRemove(sessionId, out _);

    private void RemoveExpiredSessions(DateTimeOffset now)
    {
        foreach (var entry in _sessions)
        {
            if (entry.Value.ExpiresAt <= now)
                _sessions.TryRemove(entry.Key, out _);
        }
    }
}
