namespace MauiApp1.Models;

// Wrapper class to void null agent not showing up as "None" in CollectionView
public class AgentWrapper
{
    public Agent Agent { get; }

    public AgentWrapper(Agent agent)
    {
        Agent = agent;
    }
}
