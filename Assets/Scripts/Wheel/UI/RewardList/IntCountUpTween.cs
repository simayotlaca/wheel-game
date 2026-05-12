using UnityEngine;

public struct IntCountUpTween
{
    private int _from;
    private int _to;
    private int _current;
    private float _startTime;
    private bool _active;
    private bool _initialized;

    public int Current => _current;
    public bool IsActive => _active;

    public bool SetTarget(int target, float now)
    {
        if (!_initialized)
        {
            _current = target;
            _from = target;
            _to = target;
            _initialized = true;
            _active = false;
            return true;
        }
        if (target == _to && _active) return false;
        if (target == _current && !_active) return false;

        if (_initialized && _current <= 0 && target > 0)
        {
            _from = target;
            _to = target;
            _current = target;
            _active = false;
            return true;
        }

        _from = _current;
        _to = target;
        _startTime = now;
        _active = _current != target;
        return false;
    }

    public bool Tick(float now, float duration)
    {
        if (!_active) return false;
        float t = duration > 0f ? Mathf.Clamp01((now - _startTime) / duration) : 1f;

        float eased = t * t * (3f - 2f * t);
        int v = t >= 1f ? _to : Mathf.RoundToInt(Mathf.Lerp(_from, _to, eased));
        bool changed = v != _current;
        _current = v;
        if (t >= 1f) _active = false;
        return changed;
    }
}
