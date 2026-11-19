namespace OffDutyCounter;

internal class ShadowForm : Form
{
    // Let Windows draw a subtle shadow outside the borderless window region.
    protected override CreateParams CreateParams
    {
        get
        {
            const int dropShadow = 0x00020000; // CS_DROPSHADOW
            var parameters = base.CreateParams;
            parameters.ClassStyle |= dropShadow;
            return parameters;
        }
    }
}
