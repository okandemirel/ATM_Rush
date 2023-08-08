﻿using Runtime.Enums;
using Runtime.Signals;
using UnityEngine;

namespace Runtime.Managers
{
    public class UIManager : MonoBehaviour
    {
        private void OnEnable()
        {
            CoreGameSignals.Instance.onLevelInitialize += OnLevelInitialize;
            CoreGameSignals.Instance.onReset += OnReset;
            CoreGameSignals.Instance.onLevelFailed += OnLevelFailed;
            CoreGameSignals.Instance.onLevelSuccessful += OnLevelSuccessful;

            OpenStartPanel();
        }

        private void OpenStartPanel()
        {
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Start, 0);
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Level, 1);
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Shop, 2);
        }

        private void OnLevelInitialize(byte levelValue)
        {
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Start, 0);
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Level, 1);
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Shop, 2);
            UISignals.Instance.onSetNewLevelValue?.Invoke(levelValue);
        }

        public void OnPlay()
        {
            CoreGameSignals.Instance.onPlay?.Invoke();
            CoreUISignals.Instance.onClosePanel?.Invoke(0);
            CoreUISignals.Instance.onClosePanel?.Invoke(2);
        }

        private void OnOpenWinPanel()
        {
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Win, 2);
        }

        private void OnOpenFailPanel()
        {
            CoreUISignals.Instance.onOpenPanel?.Invoke(UIPanelTypes.Fail, 2);
        }

        public void OnNextLevel()
        {
            CoreGameSignals.Instance.onNextLevel?.Invoke();
            CoreGameSignals.Instance.onReset?.Invoke();
        }

        public void OnRestartLevel()
        {
            CoreGameSignals.Instance.onRestartLevel?.Invoke();
            CoreGameSignals.Instance.onReset?.Invoke();
        }

        private void OnLevelFailed()
        {
            OnOpenFailPanel();
        }

        private void OnLevelSuccessful()
        {
            OnOpenWinPanel();
        }

        public void OnIncomeUpdate()
        {
            CoreGameSignals.Instance.onClickIncome?.Invoke();
            UISignals.Instance.onSetIncomeLvlText?.Invoke();
        }

        public void OnStackUpdate()
        {
            CoreGameSignals.Instance.onClickStack?.Invoke();
            UISignals.Instance.onSetStackLvlText?.Invoke();
        }

        private void OnDisable()
        {
            CoreGameSignals.Instance.onLevelInitialize -= OnLevelInitialize;
            CoreGameSignals.Instance.onReset -= OnReset;
            CoreGameSignals.Instance.onLevelFailed -= OnLevelFailed;
            CoreGameSignals.Instance.onLevelSuccessful -= OnLevelSuccessful;
        }


        private void OnReset()
        {
            CoreUISignals.Instance.onCloseAllPanels?.Invoke();
        }
    }
}