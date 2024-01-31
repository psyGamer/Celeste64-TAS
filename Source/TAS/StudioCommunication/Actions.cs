using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Celeste64.TAS.StudioCommunication;

[Flags]
public enum Actions {
    None = 0,
    Jump = 1 << 0,
    Jump2 = 1 << 1,
    Dash = 1 << 2,
    Dash2 = 1 << 3,
    Climb = 1 << 4,
    Pause = 1 << 5,
    Confirm = 1 << 6,
}

public static class ActionsUtils {
    public static readonly ReadOnlyDictionary<char, Actions> Chars = new(
        new Dictionary<char, Actions> {
            {'J', Actions.Jump},
            {'K', Actions.Jump2},
            {'X', Actions.Dash},
            {'C', Actions.Dash2},
            {'G', Actions.Climb},
            {'S', Actions.Pause},
            {'O', Actions.Confirm},
        });

    public static bool TryParse(char c, out Actions actions) {
        return Chars.TryGetValue(c, out actions);
    }
}
