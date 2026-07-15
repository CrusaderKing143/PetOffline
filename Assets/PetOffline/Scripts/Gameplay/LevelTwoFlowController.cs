using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelTwoFlowController : LevelFlowController
    {
        const string MainObjective = "让拿铁晒满20秒太阳";

        public bool IsBound { get; private set; }
        public LevelPhase Phase => Context != null ? Context.Phase : LevelPhase.None;

        public override void Bind(GameSession session)
        {
            IsBound = true;
            Context?.Configure(LevelId.Day2, MainObjective);
            Context?.SetState(LevelPhase.Start, MainObjective, 0f, new CameraUiState(string.Empty, false, false));
            session?.SetGameplayMode(true);
        }
    }
}
