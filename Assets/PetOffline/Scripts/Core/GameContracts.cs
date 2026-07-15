using System;

namespace PetOffline.Core
{
    public enum LevelId
    {
        None,
        Day1,
        Day2
    }

    public enum LevelPhase
    {
        None,
        Opening,
        TaskShoes,
        TaskPillow,
        FinalBark,
        Report,
        Ending,
        Complete,
        Start,
        SunFirst,
        CameraCheck,
        Loop,
        DestroyCamera,
        Backup,
        FinalSun,
        Choice,
        End
    }

    public enum FinalChoice
    {
        RestoreConnection,
        KeepQuiet
    }

    public readonly struct CameraUiState
    {
        public CameraUiState(string cameraId, bool active, bool alert)
        {
            CameraId = cameraId;
            Active = active;
            Alert = alert;
        }

        public string CameraId { get; }
        public bool Active { get; }
        public bool Alert { get; }
    }

    public interface ILevelViewModel
    {
        LevelId Level { get; }
        LevelPhase Phase { get; }
        string Objective { get; }
        float Progress01 { get; }
        CameraUiState CameraState { get; }
        ReportDefinitionSO Report { get; }
        event Action Changed;
    }

    public interface ILevelRuntime
    {
        void Bind(GameSession session);
    }

    public interface ICommandSink
    {
        void StartNewGame();
        void ContinueSavedGame();
        void ContinueReport();
        void SubmitChoice(FinalChoice choice);
        void ReturnToTitle();
        void Restart();
    }
}
