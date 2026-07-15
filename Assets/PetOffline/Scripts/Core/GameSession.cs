using System;
using UnityEngine;

namespace PetOffline.Core
{
    public sealed class GameSession : MonoBehaviour, ICommandSink
    {
        [SerializeField, RequiredReference] SceneFlowService sceneFlow;
        [SerializeField, RequiredReference] SaveService saveService;
        [SerializeField, RequiredReference] InputRouter inputRouter;

        ILevelViewModel currentLevel;

        public ILevelViewModel CurrentLevel => currentLevel;
        public event Action<ILevelViewModel> LevelChanged;
        public event Action ContinueReportRequested;
        public event Action<FinalChoice> ChoiceRequested;

        public void Configure(SceneFlowService flow, SaveService save, InputRouter input)
        {
            sceneFlow = flow;
            saveService = save;
            inputRouter = input;
            sceneFlow.Configure(this);
        }

        void Awake()
        {
            sceneFlow?.Configure(this);
            inputRouter?.SetGameplayMode(false);
        }

        public void BindLevel(ILevelViewModel level)
        {
            currentLevel = level;
            inputRouter?.SetGameplayMode(level != null);
            LevelChanged?.Invoke(level);
        }

        public void StartNewGame() => sceneFlow.LoadWorld(LevelId.Day1);
        public void ContinueSavedGame() => sceneFlow.LoadWorld(saveService.DayOneCompleted ? LevelId.Day2 : LevelId.Day1);
        public void ContinueReport() => ContinueReportRequested?.Invoke();
        public void SubmitChoice(FinalChoice choice) => ChoiceRequested?.Invoke(choice);
        public void ReturnToTitle() => sceneFlow.ReturnToTitle();
        public void Restart() => sceneFlow.LoadWorld(LevelId.Day1);
    }
}
