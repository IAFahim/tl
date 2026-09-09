using Tl;

internal static class Verification
{
    public static void Run()
    {
        foreach (var pattern in Enum.GetValues<TickPattern>())
        {
            var sum = new SumBenchmarks { Pattern = pattern };
            sum.Setup();
            Console.WriteLine($"sum/{pattern}: {sum.PublicScalar()}");

            var combat = new CombatBenchmarks { Pattern = pattern };
            combat.Setup();
            Console.WriteLine($"combat/{pattern}: {combat.PublicScalar()}");
        }
    }
}
