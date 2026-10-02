using System;
using System.Linq;

namespace CnaCsTemplate;

public static class Program
{
    [STAThread]
    internal static int Main(string[] args)
    {
        try
        {
            int? frameLimit = ResolveFrameLimit(args);
            RunGame(frameLimit);
            return 0;
        }
        catch (FileNotFoundException exception) when (LooksLikeAssemblyIdentity(exception.FileName))
        {
            Console.Error.WriteLine(
                $"Game framework dependency could not be loaded: {exception.FileName ?? exception.Message}");
            Console.Error.WriteLine(
                "Verify the selected engine's managed assembly and native runtime dependencies.");
            return 2;
        }
        catch (DllNotFoundException exception)
        {
            Console.Error.WriteLine($"Native game framework dependency could not be loaded: {exception.Message}");
            return 2;
        }
    }

    private static bool LooksLikeAssemblyIdentity(string? fileName) =>
        fileName?.Contains(", Version=", StringComparison.Ordinal) == true ||
        fileName?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true;

    // Keep framework-dependent type resolution out of Main's JIT pass so a missing configured
    // engine assembly is catchable and can produce the diagnostic above.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void RunGame(int? frameLimit)
    {
        using var game = new HelloGame(frameLimit);
        game.Run();
    }

    private static int? ResolveFrameLimit(string[] args)
    {
        int explicitIndex = Array.IndexOf(args, "--frames");
        if (explicitIndex >= 0)
        {
            if (explicitIndex + 1 >= args.Length ||
                !int.TryParse(args[explicitIndex + 1], out int explicitFrames) ||
                explicitFrames <= 0)
            {
                throw new ArgumentException("--frames requires a positive integer.");
            }

            return explicitFrames;
        }

        if (args.Contains("--stability-test"))
        {
            return ReadEnvironmentFrameLimit(defaultValue: 600);
        }

        return args.Contains("--smoke-test")
            ? ReadEnvironmentFrameLimit(defaultValue: 60)
            : null;
    }

    private static int ReadEnvironmentFrameLimit(int defaultValue) =>
        int.TryParse(Environment.GetEnvironmentVariable("CNA_SMOKE_FRAMES"), out int value) && value > 0
            ? value
            : defaultValue;
}
