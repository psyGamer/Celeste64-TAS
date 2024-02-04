using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste64.Source.Scenes.SubMenus
{
    /// <summary>
    /// If you want to add Submenu, extend this class
    /// <list>
    /// <item>Name: Name of Title and Option in Parent</item>
    /// <item>isTopLevel: whether to place the Menu in the top Menu</item>
    /// <item>Items: List of (SubMenuItem, Option, Toggle, Slider, Spacer)</item>
    /// </list>
    /// </summary>
    public abstract class CustomSubMenu : Menu
    {
        public static Menu pauseMenu;
        public static List<Menu> SubMenus = new List<Menu>();


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
        public abstract string Name { get; }
        public abstract List<Item> Items { get; }
    }
    /// <summary>
    /// Adds the Submenu to this Menu
    /// </summary>
    /// <typeparam name="T">The SubMenu you want to add</typeparam>
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
