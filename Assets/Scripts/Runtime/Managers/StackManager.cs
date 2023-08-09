using System.Collections.Generic;
using DG.Tweening;
using Runtime.Commands.Stack;
using Runtime.Data.UnityObject;
using Runtime.Data.ValueObject;
using Runtime.Signals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Managers
{
    public class StackManager : MonoBehaviour
    {
        #region Self Variables

        #region Public Variables

        public StackJumperCommand StackJumperCommand => _stackJumperCommand;
        public StackTypeUpdaterCommand StackTypeUpdaterCommand => _stackTypeUpdaterCommand;
        public ItemAdderOnStackCommand AdderOnStackCommand => _itemAdderOnStackCommand;

        public bool LastCheck
        {
            get => _lastCheck;
            set => _lastCheck = value;
        }

        #endregion

        #region Seralized Veriables

        [SerializeField] private GameObject money;

        #endregion

        #region Private Variables

        [ShowInInspector] private StackData _data;
        private List<GameObject> _collectableStack = new List<GameObject>();
        private bool _lastCheck;

        private StackMoverCommand _stackMoverCommand;
        private ItemAdderOnStackCommand _itemAdderOnStackCommand;
        private ItemRemoverOnStackCommand _itemRemoverOnStackCommand;
        private StackAnimatorCommand _stackAnimatorCommand;
        private StackJumperCommand _stackJumperCommand;
        private StackTypeUpdaterCommand _stackTypeUpdaterCommand;
        private StackInteractionWithConveyorCommand _stackInteractionWithConveyorCommand;
        private StackInitializerCommand _stackInitializerCommand;

        private readonly string _stackDataPath = "Data/CD_Stack";

        #endregion

        #endregion

        private void Awake()
        {
            _data = GetStackData();
            Init();
        }

        private void Init()
        {
            _stackMoverCommand = new StackMoverCommand(ref _data);
            _itemAdderOnStackCommand = new ItemAdderOnStackCommand(this, ref _collectableStack, ref _data);
            _itemRemoverOnStackCommand = new ItemRemoverOnStackCommand(this, ref _collectableStack);
            _stackAnimatorCommand = new StackAnimatorCommand(this, _data, ref _collectableStack);
            _stackJumperCommand = new StackJumperCommand(_data, ref _collectableStack);
            _stackInteractionWithConveyorCommand = new StackInteractionWithConveyorCommand(this, ref _collectableStack);
            _stackTypeUpdaterCommand = new StackTypeUpdaterCommand(ref _collectableStack);
            _stackInitializerCommand = new StackInitializerCommand(this, ref money);
        }

        private StackData GetStackData()
        {
            return Resources.Load<CD_Stack>(_stackDataPath).Data;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            StackSignals.Instance.onInteractionCollectable += OnInteractionWithCollectable;
            StackSignals.Instance.onInteractionObstacle += _itemRemoverOnStackCommand.Execute;
            StackSignals.Instance.onInteractionATM += OnInteractionWithATM;
            StackSignals.Instance.onInteractionConveyor +=
                _stackInteractionWithConveyorCommand.Execute;
            StackSignals.Instance.onStackFollowPlayer += OnStackMove;
            StackSignals.Instance.onUpdateType += _stackTypeUpdaterCommand.Execute;
            CoreGameSignals.Instance.onPlay += OnPlay;
            CoreGameSignals.Instance.onReset += OnReset;
        }

        private void OnStackMove(Vector2 direction)
        {
            transform.position = new Vector3(0, gameObject.transform.position.y, direction.y + 2f);
            if (gameObject.transform.childCount > 0)
            {
                _stackMoverCommand.Execute(direction.x, _collectableStack);
            }
        }

        private void OnInteractionWithATM(GameObject collectableGameObject)
        {
            // ScoreSignals.Instance.onSetAtmScore?.Invoke((int)collectableGameObject.GetComponent<CollectableManager>()
            //     .CollectableTypeValue + 1);
            if (_lastCheck == false)
            {
                _itemRemoverOnStackCommand.Execute(collectableGameObject);
            }
            else
            {
                collectableGameObject.SetActive(false);
            }
        }

        private void OnInteractionWithCollectable(GameObject collectableGameObject)
        {
            DOTween.Complete(_stackJumperCommand);
            _itemAdderOnStackCommand.Execute(collectableGameObject);
            StartCoroutine(_stackAnimatorCommand.Execute());
            _stackTypeUpdaterCommand.Execute();
        }

        private void OnPlay()
        {
            _stackInitializerCommand.Execute();
        }


        private void UnSubscribeEvents()
        {
            StackSignals.Instance.onInteractionCollectable -= OnInteractionWithCollectable;
            StackSignals.Instance.onInteractionObstacle -= _itemRemoverOnStackCommand.Execute;
            StackSignals.Instance.onInteractionATM -= OnInteractionWithATM;
            StackSignals.Instance.onInteractionConveyor -=
                _stackInteractionWithConveyorCommand.Execute;
            StackSignals.Instance.onStackFollowPlayer -= OnStackMove;
            StackSignals.Instance.onUpdateType -= _stackTypeUpdaterCommand.Execute;
            CoreGameSignals.Instance.onPlay -= OnPlay;
            CoreGameSignals.Instance.onReset -= OnReset;
        }

        private void OnDisable()
        {
            UnSubscribeEvents();
        }

        private void OnReset()
        {
            _lastCheck = false;
            _collectableStack.Clear();
            _collectableStack.TrimExcess();
        }
    }
}