using Celeste64.Scenes.SubMenus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste64.Scenes.SubMenus
{
    internal class TASSettingsMenu : CustomSubMenu
    {
        public override bool isTopLevel => true;
        public override string Name => "TAS Settings";
        public override List<Item> Items => new List<Item>([
            new Menu.Toggle("Simplified Graphics", Save.Instance.ToggleSimplifiedGraphics, () => Save.Instance.SimplifiedGraphics),
        ]);
    }
}
