using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Celeste64.TAS.StudioCommunication;

[Flags]
public enum Actions {
    None = 0,
    Jump = 1 << 0,
}

public static class ActionsUtils {
    public static readonly ReadOnlyDictionary<char, Actions> Chars = new(
        new Dictionary<char, Actions> {
            {'J', Actions.Jump},
        });

    public static bool TryParse(char c, out Actions actions) {
        return Chars.TryGetValue(c, out actions);
    }
}
