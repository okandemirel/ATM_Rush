using System.Collections.Generic;
using Runtime.Data.UnityObject;
using Runtime.Keys;
using Runtime.Signals;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runtime.Managers
{
    public class InputManager : MonoBehaviour
    {
        #region Self Variables

        #region Private Variables

        private float _positionValuesX;

        private bool _isTouching;

        private float _currentVelocity; //ref type
        private Vector2? _mousePosition; //ref type
        private Vector3 _moveVector; //ref type

        [Header("Data")] private CD_Input _data;
        [ShowInInspector] private bool _isFirstTimeTouchTaken;
        [ShowInInspector] private bool _isAvailableForTouch;

        #endregion

        #endregion

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            CoreGameSignals.Instance.onReset += OnReset;
            CoreGameSignals.Instance.onPlay += OnPlay;
            InputSignals.Instance.onChangeInputState += OnChangeInputState;
        }

        private void OnPlay()
        {
            _isAvailableForTouch = true;
        }


        private void OnChangeInputState(bool state)
        {
            _isAvailableForTouch = state;
        }

        private void UnSubscribeEvents()
        {
            CoreGameSignals.Instance.onReset -= OnReset;
            CoreGameSignals.Instance.onPlay -= OnPlay;
            InputSignals.Instance.onChangeInputState -= OnChangeInputState;
        }

        private void OnDisable()
        {
            UnSubscribeEvents();
        }


        private void Update()
        {
            if (!_isAvailableForTouch) return;

            if (Input.GetMouseButtonUp(0) && !IsPointerOverUIElement())
            {
                _isTouching = false;

                InputSignals.Instance.onInputReleased?.Invoke();
                InputSignals.Instance.onChangeInputState?.Invoke(false);
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUIElement())
            {
                _isTouching = true;
                InputSignals.Instance.onInputTaken?.Invoke();
                if (!_isFirstTimeTouchTaken)
                {
                    _isFirstTimeTouchTaken = true;
                    InputSignals.Instance.onFirstTimeTouchTaken?.Invoke();
                }

                _mousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0) && !IsPointerOverUIElement())
            {
                if (_isTouching)
                {
                    if (_mousePosition != null)
                    {
                        Vector2 mouseDeltaPos = (Vector2)Input.mousePosition - _mousePosition.Value;


                        if (mouseDeltaPos.x > _data.Data.HorizontalInputSpeed)
                            _moveVector.x = _data.Data.HorizontalInputSpeed / 10f * mouseDeltaPos.x;
                        else if (mouseDeltaPos.x < -_data.Data.HorizontalInputSpeed)
                            _moveVector.x = -_data.Data.HorizontalInputSpeed / 10f * -mouseDeltaPos.x;
                        else
                            _moveVector.x = Mathf.SmoothDamp(_moveVector.x, 0f, ref _currentVelocity,
                                _data.Data.HorizontalInputClampStopValue);

                        _mousePosition = Input.mousePosition;

                        InputSignals.Instance.onInputDragged?.Invoke(new HorizontalnputParams()
                        {
                            HorizontalInputValue = _moveVector.x,
                            HorizontalInputClampSides = _data.Data.HorizontalInputClampNegativeSides,
                        });
                    }
                }
            }
        }


        private bool IsPointerOverUIElement()
        {
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        private void OnReset()
        {
            _isTouching = false;
            _isFirstTimeTouchTaken = false;
        }
    }
}