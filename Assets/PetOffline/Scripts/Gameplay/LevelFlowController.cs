using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    public class LevelFlowController : MonoBehaviour
    {
        [SerializeField, RequiredReference] LevelSceneContext context;

        public LevelSceneContext Context => context;
        public void Configure(LevelSceneContext value) => context = value;
    }
}
