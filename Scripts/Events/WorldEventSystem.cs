using Evix.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evix.EventSystems {
  public class WorldEventSystem : EventSystem<WorldEventSystem.Channels> {
    public enum Channels {Basic, LevelFocusUpdates, ChunkActivationUpdates};
  }
}
