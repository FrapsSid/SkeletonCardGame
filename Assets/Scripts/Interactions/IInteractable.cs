using System.Collections.Generic;

namespace Interactions
{
    public interface IInteractable
    {
        IList<Interaction> GetInteractions(Skeleton player);
    }
}