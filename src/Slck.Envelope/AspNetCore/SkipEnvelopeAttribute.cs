namespace Slck.Envelope.AspNetCore
{
    /// <summary>
    /// Marker interface. Endpoints whose metadata contains an implementation of this interface
    /// are excluded from <see cref="ApiEnvelopeMiddleware"/> exception handling.
    /// Exceptions propagate normally as if the middleware were not present.
    /// </summary>
    public interface ISkipEnvelopeMetadata { }

    /// <summary>
    /// Apply to a Minimal API endpoint via <c>.WithMetadata(new SkipEnvelopeAttribute())</c>
    /// or to an MVC controller/action to bypass envelope exception handling.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SkipEnvelopeAttribute : Attribute, ISkipEnvelopeMetadata { }
}
