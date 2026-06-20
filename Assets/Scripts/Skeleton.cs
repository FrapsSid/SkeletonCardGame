//Skeleton Class Placeholder
public class Skeleton
{
    public Hand Hand { get; private set; }
    public readonly Team team;

    public Skeleton(Team team)
    {
        Hand = new Hand();
        this.team = team;
    }
}