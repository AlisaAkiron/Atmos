namespace Atmos.Templates;

// ReSharper disable once UnusedTypeParameter - Used for generic type inference
public interface IComponentComposite<TViewModel> where TViewModel : class
{
    public static abstract Type ComponentType { get; }
}
