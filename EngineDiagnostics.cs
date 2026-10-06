using Microsoft.Xna.Framework.Graphics;
#if ENGINE_CNA
using CNA.XnaCompat.Extensions;
#endif

namespace CnaDotnetTemplate;

/// <summary>Keeps engine-specific diagnostics outside the portable game implementation.</summary>
internal static class EngineDiagnostics
{
    internal readonly record struct Capabilities(string RendererName, bool Supports3D, bool SupportsDepth);

    internal static Capabilities Inspect(GraphicsDevice graphicsDevice)
    {
#if ENGINE_CNA
        return new Capabilities(
            graphicsDevice.GetCnaRendererName(),
            graphicsDevice.SupportsCnaCapability(CnaGraphicsCapability.ThreeD),
            graphicsDevice.SupportsCnaCapability(CnaGraphicsCapability.DepthStencilBuffer));
#else
        // XNA-family desktop backends expose 3D and a depth/stencil buffer as profile features.
        return new Capabilities(graphicsDevice.Adapter.Description, Supports3D: true, SupportsDepth: true);
#endif
    }
}
