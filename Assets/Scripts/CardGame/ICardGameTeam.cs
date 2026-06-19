using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.CardGame
{
    public interface ICardGameTeam<out T>
    {
        IList<T> Players { get; }
    }
}
