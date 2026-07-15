using System;
using UnityEngine;

namespace PetOffline.Core
{
    public sealed class GameSession : MonoBehaviour, ICommandSink
    {
        [SerializeField, RequiredReference] SceneFlowService sceneFlow;
        [SerializeField, RequiredReference] SaveService saveService;
        [SerializeField, RequiredReference] InputRouter inputRouter;
        [SerializeField, RequiredReference] DialogueDirector dialogueDirector;

        ILevelViewModel currentLevel;

        public ILevelViewModel CurrentLevel => currentLevel;
        public InputRouter Input => inputRouter;
        public DialogueDirector Dialogue => dialogueDirector;
        public SaveService Save => saveService;
        public event Action<ILevelViewModel> LevelChanged;
        public event Action ContinueReportRequested;
        public event Action<FinalChoice> ChoiceRequested;

        public void Configure(SceneFlowService flow, SaveService save, InputRouter input) =>
            Configure(flow, save, input, dialogueDirector);

        public void Configure(SceneFlowService flow, SaveService save, InputRouter input, DialogueDirector dialogue)
        {
            sceneFlow = flow;
            saveService = save;
            inputRouter = input;
            dialogueDirector = dialogue;
            sceneFlow?.Configure(this);
        }

        void Awake()
        {
            sceneFlow?.Configure(this);
            SetGameplayMode(false);
        }

        public void BindLevel(ILevelViewModel level)
        {
            currentLevel = level;
            if (level == null)
                SetGameplayMode(false);
            LevelChanged?.Invoke(level);
        }

        public void SetGameplayMode(bool enabled) => inputRouter?.SetGameplayMode(enabled);

        public void CompleteDayOne()
        {
            saveService?.MarkDayOneCompleted();
            sceneFlow?.LoadWorld(LevelId.Day2);
        }

        public void StartNewGame() => sceneFlow.LoadWorld(LevelId.Day1);
        public void ContinueSavedGame() => sceneFlow.LoadWorld(saveService.DayOneCompleted ? LevelId.Day2 : LevelId.Day1);
        public void ContinueReport() => ContinueReportRequested?.Invoke();
        public void SubmitChoice(FinalChoice choice) => ChoiceRequested?.Invoke(choice);
        public void ReturnToTitle() => sceneFlow.ReturnToTitle();
        public void Restart() => sceneFlow.LoadWorld(LevelId.Day1);
    }
}
