using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace XiaoZhi.Unity
{
    public class AnimationCtrl
    {
        private enum State
        {
            Idle,
            Stand,
            Greetings,
            Talk,
            Dance,
        }

        private const float StandTickIntervalMin = 10.0f;
        private const float StandTickIntervalMax = 60.0f;

        private readonly Animator _animator;
        private readonly AppPresets.AnimationLib _lib;
        private readonly Talk _talk;
        private readonly AnimatorProxy _animProxy;

        private State _state;
        private bool _readyToSpeak;
        private Talk.State _lastTalkState;
        private string _currentAnim;
        private float _nextStandTime = float.MaxValue;
        private bool _externalAnim;

        public AnimationCtrl(Animator animator, AppPresets.AnimationLib lib, Talk talk)
        {
            _animator = animator;
            _lib = lib;
            _talk = talk;
            _talk.OnStateUpdate += OnTalkStateUpdate;
            _talk.OnChatUpdate += OnTalkChatUpdate;
            _animProxy = _animator.GetBehaviour<AnimatorProxy>();
            _animProxy.StateUpdate += OnAnimStateUpdate;
        }

        public void Dispose()
        {
            _animProxy.StateUpdate -= OnAnimStateUpdate;
            _talk.OnStateUpdate -= OnTalkStateUpdate;
            _talk.OnChatUpdate -= OnTalkChatUpdate;
        }

        public void Animate(params string[] labels)
        {
            _externalAnim = true;
            Labels2Animation(labels);
        }

        private void OnTalkStateUpdate(Talk.State state)
        {
            switch (state)
            {
                case Talk.State.Speaking:
                    _readyToSpeak = true;
                    break;
                case Talk.State.Listening:
                    SetState(_lastTalkState != Talk.State.Speaking ? State.Greetings : State.Idle);
                    break;
                case Talk.State.Unknown:
                case Talk.State.Starting:
                case Talk.State.Idle:
                case Talk.State.Connecting:
                case Talk.State.Activating:
                case Talk.State.Error:
                default:
                    _externalAnim = false;
                    SetState(State.Idle);
                    break;
            }

            _lastTalkState = state;
        }

        private void OnTalkChatUpdate(string chat)
        {
            if (_readyToSpeak)
            {
                _readyToSpeak = false;
                SetState(State.Talk);
            }
        }

        private void SetState(State state)
        {
            if (_state == state) return;
            _state = state;
            if (!_externalAnim)
                State2Animation();
        }

        private void State2Animation()
        {
            var labels = UnityEngine.Pool.ListPool<string>.Get();
            State2Labels(labels);
            Labels2Animation(labels);
            UnityEngine.Pool.ListPool<string>.Release(labels);
            if (_state == State.Idle && _talk.Stat == Talk.State.Listening)
                _nextStandTime = Time.time + Random.Range(StandTickIntervalMin, StandTickIntervalMax);
        }

        private void Labels2Animation(IEnumerable<string> labels)
        {
            var metas = _lib.MatchAll(labels);
            var meta = AppPresets.AnimationLib.Random(metas);
            var anim = meta?.Name ?? "idle1";
            if (_currentAnim != anim) _animator.CrossFadeInFixedTime(anim, 0.7f, 0);
            else _animator.Play(anim, 0, 0.0f);
            _currentAnim = anim;
        }

        private void State2Labels(List<string> labels)
        {
            switch (_state)
            {
                case State.Idle:
                    labels.Add("idle");
                    break;
                case State.Greetings:
                    labels.Add("greet");
                    break;
                case State.Talk:
                    labels.Add("talk");
                    break;
                case State.Stand:
                    labels.Add("stand");
                    break;
                case State.Dance:
                    labels.Add("dance");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnAnimStateUpdate(AnimatorStateInfo stateInfo)
        {
            if (!string.IsNullOrEmpty(_currentAnim) &&
                stateInfo.IsName(_currentAnim))
            {
                if (_externalAnim && stateInfo.normalizedTime >= 0.999f)
                {
                    _externalAnim = false;
                    State2Animation();
                }
                else if (stateInfo is { loop: false, normalizedTime: >= 0.999f })
                {
                    if (_state is State.Greetings or State.Stand) SetState(State.Idle);
                    else State2Animation();
                }
                else if (_state == State.Idle && _talk.Stat == Talk.State.Listening && stateInfo.loop &&
                         Time.time >= _nextStandTime)
                {
                    _nextStandTime = float.MaxValue;
                    SetState(State.Stand);
                }
            }
        }
    }
}