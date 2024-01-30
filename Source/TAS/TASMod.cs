using Celeste64.TAS.Input;

namespace Celeste64.TAS;

public class TASMod
{
    public static void Initialize()
    {
        CommandAttribute.CollectMethods();
    }

    public static void Deinitialize()
    {

    }

    public static void Update()
    {
        if (TASControls.StartStop.Pressed)
        {
            if (Manager.Running)
                Manager.DisableRun();
            else
                Manager.EnableRun();
        }

        Manager.Update();
    }
}
