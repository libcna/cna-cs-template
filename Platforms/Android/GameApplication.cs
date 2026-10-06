namespace CnaDotnetTemplate;

/// <summary>The Android application: CNA.NET's host runs the game's own Main on SDL's thread.</summary>
[Android.App.Application]
public class GameApplication : CNA.Android.CnaGameApplication
{
    public GameApplication(IntPtr handle, Android.Runtime.JniHandleOwnership transfer)
        : base(handle, transfer)
    {
    }

    protected override void RunGame() => Program.Main([]);
}
