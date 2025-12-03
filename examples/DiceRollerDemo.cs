using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.Dice;

namespace OpenCombatEngine.Demo
{
    /// <summary>
    /// Demo program showing IDiceRoller functionality
    /// </summary>
    public class DiceRollerDemo
    {
        /// <summary>
        /// Main entry point for the demo
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("=== OpenCombatEngine Dice Roller Demo ===\n");

            // Create a dice roller instance
            IDiceRoller roller = new StandardDiceRoller();

            // Demo 1: Basic Rolls
            Console.WriteLine("1. Basic Dice Rolls:");
            Console.WriteLine("--------------------");
            DemoBasicRolls(roller);
            
            Console.WriteLine("\n2. Advantage/Disadvantage:");
            Console.WriteLine("---------------------------");
            DemoAdvantageDisadvantage(roller);
            
            Console.WriteLine("\n3. Critical Detection:");
            Console.WriteLine("----------------------");
            DemoCriticals(roller);
            
            Console.WriteLine("\n4. Invalid Notation Handling:");
            Console.WriteLine("-----------------------------");
            DemoErrorHandling(roller);
            
            Console.WriteLine("\n5. Reproducible Rolls with Seed:");
            Console.WriteLine("---------------------------------");
            DemoReproducibleRolls();

            Console.WriteLine("\n=== Demo Complete ===");
        }

        /// <summary>
        /// Demonstrates basic dice rolling functionality
        /// </summary>
        private static void DemoBasicRolls(IDiceRoller roller)
        {
            string[] notations = { "1d20", "3d6+2", "2d8-1", "d20", "5" };
            
            foreach (var notation in notations)
            {
                var result = roller.Roll(notation);
                if (result.IsSuccess)
                {
                    Console.WriteLine($"  {notation,-10} → {result.Value}");
                }
            }
        }

        /// <summary>
        /// Demonstrates advantage and disadvantage mechanics
        /// </summary>
        private static void DemoAdvantageDisadvantage(IDiceRoller roller)
        {
            var notation = "1d20+5";
            
            // Normal roll
            var normal = roller.Roll(notation);
            Console.WriteLine($"  Normal:       {normal.Value}");
            
            // Advantage
            var advantage = roller.RollWithAdvantage(notation);
            if (advantage.IsSuccess)
            {
                Console.WriteLine($"  Advantage:    {advantage.Value}");
            }
            
            // Disadvantage
            var disadvantage = roller.RollWithDisadvantage(notation);
            if (disadvantage.IsSuccess)
            {
                Console.WriteLine($"  Disadvantage: {disadvantage.Value}");
            }
        }

        /// <summary>
        /// Demonstrates critical success/failure detection
        /// </summary>
        private static void DemoCriticals(IDiceRoller roller)
        {
            // Set seed to force specific rolls for demo
            roller.Seed = 42;
            
            // Simulate rolling until we get a critical
            Console.WriteLine("  Rolling 1d20 until we see a critical...");
            
            int attempts = 0;
            bool foundCritical = false;
            
            while (!foundCritical && attempts < 50)
            {
                attempts++;
                var result = roller.Roll("1d20");
                
                if (result.IsSuccess)
                {
                    if (result.Value.IsCriticalSuccess)
                    {
                        Console.WriteLine($"  Attempt {attempts}: {result.Value}");
                        foundCritical = true;
                    }
                    else if (result.Value.IsCriticalFailure)
                    {
                        Console.WriteLine($"  Attempt {attempts}: {result.Value}");
                        foundCritical = true;
                    }
                }
            }
            
            if (!foundCritical)
            {
                Console.WriteLine($"  No criticals in {attempts} attempts (unlucky!)");
            }
            
            // Reset seed
            roller.Seed = null;
        }

        /// <summary>
        /// Demonstrates error handling for invalid notation
        /// </summary>
        private static void DemoErrorHandling(IDiceRoller roller)
        {
            string[] invalidNotations = { "", "abc", "1d", "0d20", "1d0" };
            
            foreach (var notation in invalidNotations)
            {
                var result = roller.Roll(notation);
                if (result.IsFailure)
                {
                    Console.WriteLine($"  '{notation,-6}' → Error: {result.Error}");
                }
            }
        }

        /// <summary>
        /// Demonstrates reproducible rolls using seed
        /// </summary>
        private static void DemoReproducibleRolls()
        {
            const int seed = 12345;
            const string notation = "3d6+2";
            
            // First roller with seed
            IDiceRoller roller1 = new StandardDiceRoller();
            roller1.Seed = seed;
            
            // Second roller with same seed
            IDiceRoller roller2 = new StandardDiceRoller();
            roller2.Seed = seed;
            
            Console.WriteLine($"  Both rollers using seed {seed}:");
            Console.WriteLine("  Rolling {0} three times with each roller:", notation);
            
            for (int i = 1; i <= 3; i++)
            {
                var result1 = roller1.Roll(notation);
                var result2 = roller2.Roll(notation);
                
                Console.WriteLine($"    Roll {i}: Roller1={result1.Value.Total,2}, " +
                                $"Roller2={result2.Value.Total,2} " +
                                $"[Match: {result1.Value.Total == result2.Value.Total}]");
            }
        }
    }
}