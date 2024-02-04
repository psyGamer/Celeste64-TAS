using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste64.Source.Scenes.SubMenus
{
    public abstract class CustomSubMenu : Menu
    {
        public static Menu pauseMenu;
        public CustomSubMenu() : base()
        {
            Title = Name;
            foreach (var item in Items)
            {
                Add(item);
            }
            Add(new Menu.Option("Close", () => pauseMenu.CloseOpen()));
            if (isTopLevel)
            {
                SubMenus.Add(this);
            }
        }
        public abstract bool isTopLevel { get; }

        public static List<Menu> SubMenus = new List<Menu>();
        public abstract string Name { get; }
        public abstract List<Item> Items { get; }
    }

    public class SubMenuItem<T> : Menu.Submenu
        where T : CustomSubMenu, new()
    {
        public SubMenuItem(Menu of) : this(new T().Name, of, new T())
        {

        }
        public SubMenuItem(string label, Menu? rootMenu, Menu? submenu = null) : base(label, rootMenu, submenu)
        {

        }
    }
}
