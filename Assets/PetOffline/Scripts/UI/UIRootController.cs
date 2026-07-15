using PetOffline.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace PetOffline.UI
{
    public sealed class UIRootController : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField, RequiredReference] InputActionAsset actions;
        [SerializeField, RequiredReference] InputSystemUIInputModule inputModule;

        ILevelViewModel model;
        InputActionReference pointReference;
        InputActionReference clickReference;
        InputActionReference moveReference;
        InputActionReference submitReference;
        InputActionReference cancelReference;

        public ILevelViewModel Model => model;
        public bool WaitingForLevel => model == null;

        public void Configure(GameSession owner, InputActionAsset inputActions, InputSystemUIInputModule module)
        {
            session = owner;
            actions = inputActions;
            inputModule = module;
        }

        void Awake()
        {
            ConfigureInputModule();
            if (session != null)
                session.LevelChanged += Bind;
        }

        void OnDestroy()
        {
            if (session != null)
                session.LevelChanged -= Bind;
            Bind(null);
            DestroyInputReferences();
        }

        public void Bind(ILevelViewModel value)
        {
            if (model != null)
                model.Changed -= Refresh;
            model = value;
            if (model != null)
                model.Changed += Refresh;
            Refresh();
        }

        void ConfigureInputModule()
        {
            if (actions == null || inputModule == null)
                return;

            DestroyInputReferences();
            inputModule.actionsAsset = actions;
            inputModule.point = pointReference = InputActionReference.Create(actions.FindAction("UI/Point", true));
            inputModule.leftClick = clickReference = InputActionReference.Create(actions.FindAction("UI/Click", true));
            inputModule.move = moveReference = InputActionReference.Create(actions.FindAction("UI/Navigate", true));
            inputModule.submit = submitReference = InputActionReference.Create(actions.FindAction("UI/Submit", true));
            inputModule.cancel = cancelReference = InputActionReference.Create(actions.FindAction("UI/Cancel", true));
        }

        void DestroyInputReferences()
        {
            DestroyReference(ref pointReference);
            DestroyReference(ref clickReference);
            DestroyReference(ref moveReference);
            DestroyReference(ref submitReference);
            DestroyReference(ref cancelReference);
        }

        static void DestroyReference(ref InputActionReference reference)
        {
            if (reference != null)
                Destroy(reference);
            reference = null;
        }

        void Refresh()
        {
            // Presenters added in Milestone 3 subscribe to this bound model.
        }
    }
}
