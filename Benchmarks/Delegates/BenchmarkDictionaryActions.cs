using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.Delegates;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BenchmarkDictionaryActions
{
    private class BaseAI
    {
        public Mobile m_Mobile;
        private int _counter;

        public bool DoActionWander()
        {
            _counter++;
            return true;
        }
        public bool DoActionCombat()
        {
            _counter++;
            return true;
        }
        public bool DoActionGuard()
        {
            _counter++;
            return true;
        }
        public bool DoActionFlee()
        {
            _counter++;
            return true;
        }
        public bool DoActionInteract()
        {
            _counter++;
            return true;
        }
        public bool DoActionBackoff()
        {
            _counter++;
            return true;
        }
    }

    private class Mobile
    {
        private int _counter;

        public void OnActionWander() => _counter++;
        public void OnActionCombat() => _counter++;
        public void OnActionGuard() => _counter++;
        public void OnActionFlee() => _counter++;
        public void OnActionInteract() => _counter++;
        public void OnActionBackoff() => _counter++;
    }

    public enum ActionType
    {
        Wander,
        Combat,
        Guard,
        Flee,
        Backoff,
        Interact
    }

    private static readonly Dictionary<ActionType, Func<BaseAI, bool>> _staticActionHandlers = new()
    {
        { ActionType.Wander, ai => { ai.m_Mobile.OnActionWander(); return ai.DoActionWander(); } },
        { ActionType.Combat, ai => { ai.m_Mobile.OnActionCombat(); return ai.DoActionCombat(); } },
        { ActionType.Guard, ai => { ai.m_Mobile.OnActionGuard(); return ai.DoActionGuard(); } },
        { ActionType.Flee, ai => { ai.m_Mobile.OnActionFlee(); return ai.DoActionFlee(); } },
        { ActionType.Interact, ai => { ai.m_Mobile.OnActionInteract(); return ai.DoActionInteract(); } },
        { ActionType.Backoff, ai => { ai.m_Mobile.OnActionBackoff(); return ai.DoActionBackoff(); } }
    };

    private static readonly Mobile _mobile = new();
    private static readonly BaseAI _baseAI = new() { m_Mobile = _mobile };

    [Benchmark]
    public void BenchmarkStaticDictionaryActions()
    {
        for (var i = 0; i < 6; i++)
        {
            var func = (ActionType)i switch
            {
                ActionType.Wander => _staticActionHandlers[ActionType.Wander],
                ActionType.Combat => _staticActionHandlers[ActionType.Combat],
                ActionType.Guard => _staticActionHandlers[ActionType.Guard],
                ActionType.Flee => _staticActionHandlers[ActionType.Flee],
                ActionType.Interact => _staticActionHandlers[ActionType.Interact],
                ActionType.Backoff => _staticActionHandlers[ActionType.Backoff],
                _ => throw new ArgumentOutOfRangeException()
            };

            func(_baseAI);
        }
    }

    [Benchmark]
    public void BenchmarkSwitchActions()
    {
        for (var i = 0; i < 6; i++)
        {
            switch ((ActionType)i)
            {
                case ActionType.Wander:
                    _baseAI.m_Mobile.OnActionWander();
                    _baseAI.DoActionWander();
                    break;
                case ActionType.Combat:
                    _baseAI.m_Mobile.OnActionCombat();
                    _baseAI.DoActionCombat();
                    break;
                case ActionType.Guard:
                    _baseAI.m_Mobile.OnActionGuard();
                    _baseAI.DoActionGuard();
                    break;
                case ActionType.Flee:
                    _baseAI.m_Mobile.OnActionFlee();
                    _baseAI.DoActionFlee();
                    break;
                case ActionType.Interact:
                    _baseAI.m_Mobile.OnActionInteract();
                    _baseAI.DoActionInteract();
                    break;
                case ActionType.Backoff:
                    _baseAI.m_Mobile.OnActionBackoff();
                    _baseAI.DoActionBackoff();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}