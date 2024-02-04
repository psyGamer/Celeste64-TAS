using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste64.Scenes.SubMenus
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
        public static Menu? pauseMenu;
        public static List<Menu> TopLevelMenus = new List<Menu>();


        public CustomSubMenu() : base()
        {
            Title = Name;
            foreach (var item in Items)
            {
                Add(item);
            }
            Add(new Menu.Option("Close", () => CloseOpen(pauseMenu)));
            if (isTopLevel)
            {
                TopLevelMenus.Add(this);
            }
        }

        public static void CloseOpen(Menu pauseMenu)
        {
            pauseMenu.CloseSubMenus();
        }



        public abstract bool isTopLevel { get; }
        public abstract string Name { get; }
        public abstract List<Item> Items { get; }
    }
    /// <summary>
    /// Adds the Submenu to this Menu
    /// </summary>
    /// <typeparam name="T">The SubMenu you want to add</typeparam>
    public class SubMenuOpeningOption<T> : Menu.Submenu
        where T : CustomSubMenu, new()
    {
        public SubMenuOpeningOption() : this(new T())
        {

        }
        public SubMenuOpeningOption(Menu submenu) : base(submenu.Title, CustomSubMenu.pauseMenu, submenu)
        {

        }
    }
}
