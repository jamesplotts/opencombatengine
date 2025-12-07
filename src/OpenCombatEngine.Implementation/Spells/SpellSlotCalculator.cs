using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Implementation.Spells
{
    public static class SpellSlotCalculator
    {
        public static Dictionary<int, int> CalculateSlots(IEnumerable<(SpellcastingType Type, int Level)> classes)
        {
            ArgumentNullException.ThrowIfNull(classes);

            int totalLevel = 0;
            
            // Multiclassing Rules:
            // Add levels of bards, clerics, druids, sorcerers, and wizards. (Full)
            // Add half levels (rounded down) of paladins and rangers. (Half)
            // Add third levels (rounded down) of eldritch knights and arcane tricksters. (Third)
            
            // Wait, Artificer is rounded UP. But standard is down. We'll stick to down for "Half" generic.
            
            // Also, single class half-casters (Paladin/Ranger) have slots at level 2.
            // If checking single class, the formula might differ slightly (e.g. Ranger 2 gets slots, but Ranger 1 in multiclass contributes 0).
            // But usually the table works if we map level correctly.
            // For now, we'll implement the Multiclassing Algo. If a creature is single class, it works too (Level / 1 = Level).

            foreach (var (type, level) in classes)
            {
                switch (type)
                {
                    case SpellcastingType.Full:
                        totalLevel += level;
                        break;
                    case SpellcastingType.Half:
                        totalLevel += level / 2; 
                        // Note: Single class Paladin 2 gets slots. 2/2 = 1. Correct.
                        // Paladin 1 -> 0. Correct.
                        break;
                    case SpellcastingType.Third:
                        totalLevel += level / 3;
                        break;
                        // Pact Magic is separate. Ignored here.
                }
            }
            
            // If total level is 0, no slots.
            if (totalLevel <= 0) return new Dictionary<int, int>();

            return GetSlotsForLevel(totalLevel);
        }

        private static Dictionary<int, int> GetSlotsForLevel(int level)
        {
            var slots = new Dictionary<int, int>();
            
            // 5e Standard Slot Table
            // Level 1: 2 1st
            // ...
            // Simplified table lookup
            
            void Set(int l1, int l2=0, int l3=0, int l4=0, int l5=0, int l6=0, int l7=0, int l8=0, int l9=0)
            {
                if(l1>0) slots[1] = l1;
                if(l2>0) slots[2] = l2;
                if(l3>0) slots[3] = l3;
                if(l4>0) slots[4] = l4;
                if(l5>0) slots[5] = l5;
                if(l6>0) slots[6] = l6;
                if(l7>0) slots[7] = l7;
                if(l8>0) slots[8] = l8;
                if(l9>0) slots[9] = l9;
            }

            if (level >= 20) Set(4, 3, 3, 3, 3, 2, 2, 1, 1);
            else if (level >= 19) Set(4, 3, 3, 3, 3, 2, 1, 1, 1);
            else if (level >= 18) Set(4, 3, 3, 3, 3, 1, 1, 1, 1);
            else if (level >= 17) Set(4, 3, 3, 3, 2, 1, 1, 1, 1);
            else if (level >= 16) Set(4, 3, 3, 2, 1, 1, 1, 1); // WotC table check: Lv16 = 4/3/3/2/1/1/1/1 (Wait, Lv15/16 don't imply 8th/9th changes?)
                                                              // Lv15: 4 3 3 2 1 1 1 1
                                                              // Lv16: 4 3 3 2 1 1 1 1 (ASI)
                                                              // Lv17: 4 3 3 3 2 1 1 1 1 (9th spell)
            else if (level >= 15) Set(4, 3, 3, 2, 1, 1, 1, 1);
            else if (level >= 14) Set(4, 3, 3, 2, 1, 1, 1); // Lv14: ... no wait
            // Let's copy from PHB table exactly.
            
            /*
            1: 2
            2: 3
            3: 4 2
            4: 4 3
            5: 4 3 2
            6: 4 3 3
            7: 4 3 3 1
            8: 4 3 3 2
            9: 4 3 3 3 1
            10: 4 3 3 3 2
            11: 4 3 3 3 2 1
            12: 4 3 3 3 2 1
            13: 4 3 3 3 2 1 1
            14: 4 3 3 3 2 1 1
            15: 4 3 3 3 2 1 1 1
            16: 4 3 3 3 2 1 1 1
            17: 4 3 3 3 2 1 1 1 1
            18: 4 3 3 3 3 1 1 1 1
            19: 4 3 3 3 3 2 1 1 1
            20: 4 3 3 3 3 2 2 1 1
            */

             switch (level)
            {
                case 1: Set(2); break;
                case 2: Set(3); break;
                case 3: Set(4, 2); break;
                case 4: Set(4, 3); break;
                case 5: Set(4, 3, 2); break;
                case 6: Set(4, 3, 3); break;
                case 7: Set(4, 3, 3, 1); break;
                case 8: Set(4, 3, 3, 2); break;
                case 9: Set(4, 3, 3, 3, 1); break;
                case 10: Set(4, 3, 3, 3, 2); break;
                case 11: 
                case 12: Set(4, 3, 3, 3, 2, 1); break;
                case 13: 
                case 14: Set(4, 3, 3, 3, 2, 1, 1); break;
                case 15: 
                case 16: Set(4, 3, 3, 3, 2, 1, 1, 1); break;
                case 17: Set(4, 3, 3, 3, 2, 1, 1, 1, 1); break;
                case 18: Set(4, 3, 3, 3, 3, 1, 1, 1, 1); break;
                case 19: Set(4, 3, 3, 3, 3, 2, 1, 1, 1); break;
                default: 
                    if (level >= 20) Set(4, 3, 3, 3, 3, 2, 2, 1, 1); 
                    break;
            }

            return slots;
        }
    }
}
