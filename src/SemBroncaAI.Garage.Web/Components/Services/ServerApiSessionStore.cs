using System.Collections.Concurrent;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public interface IServerApiSessionStore
{
    void Set(string sessionId, ApiSession session);
    bool TryGet(string sessionId, out ApiSession? session);
    void Remove(string sessionId);
    void Revoke(string sessionId);
    bool IsRevoked(string sessionId);
}

public sealed class ServerApiSessionStore : IServerApiSessionStore
{
    private readonly ConcurrentDictionary<string, ApiSession> _sessions = new();
    private readonly ConcurrentDictionary<string, byte> _revokedSessions = new();

    public void Set(string sessionId, ApiSession session)
    {
        RemoveExpiredSessions(DateTimeOffset.UtcNow);
        _revokedSessions.TryRemove(sessionId, out _);
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

    public void Revoke(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        _revokedSessions[sessionId] = 0;
    }

    public bool IsRevoked(string sessionId) => _revokedSessions.ContainsKey(sessionId);

    private void RemoveExpiredSessions(DateTimeOffset now)
    {
        foreach (var entry in _sessions)
        {
            if (entry.Value.ExpiresAt <= now)
                _sessions.TryRemove(entry.Key, out _);
        }
    }
}
