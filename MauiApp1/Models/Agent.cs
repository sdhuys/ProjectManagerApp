namespace MauiApp1.Models;

public class Agent
{
    public string Name { get; set; }
    public decimal FeeDecimal { get; set; }

    private static Dictionary<string, Agent> existingAgents = new Dictionary<string, Agent>();

    public Agent(string name, decimal feeDecimal)
    {
        Name = name;
        FeeDecimal = feeDecimal;

        string key = GetAgentKey();
        if (!existingAgents.ContainsKey(key))
        {
            existingAgents[key] = this;
        }
    }

    private string GetAgentKey()
    {
        return $"{Name}_{FeeDecimal}";
    }

    public static Agent GetExistingAgent(string name, decimal feeDecimal)
    {
        var key = $"{name}_{feeDecimal}";
        if (existingAgents.ContainsKey(key))
        {
            return existingAgents[key];
        }
        return null;
    }
}