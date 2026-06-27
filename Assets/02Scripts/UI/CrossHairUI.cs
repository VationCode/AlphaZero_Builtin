using UnityEngine;

public class CrossHairUI : UIWindow
{
    public override void Open()
    {
        base.Open();

        Refresh();
    }

    private void Refresh()
    {
        // 갱신
    }
    public override void Close()
    {
        base.Close();
    }
}
