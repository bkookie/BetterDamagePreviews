using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BetterDamagePreviews.Preview;

internal static class HitCountILScanner
{
    public static bool TryDetectHitCount(CardModel card, out int constHitCount, out string? hitCountVarName, out Type? hitCountVarType, out Func<Creature?, int>? hitCountFunc, out string? errorMsg)
    {
        constHitCount = -1;
        hitCountVarName = null;
        hitCountVarType = null;
        hitCountFunc = null;
        errorMsg = null;

        MethodInfo method = AccessTools.AsyncMoveNext(AccessTools.Method(card.GetType(), "OnPlay"));

        if (method == null || !BasicILScanner.TryReadIL(method, out List<Instruction>? instructions, out HashSet<int>? branchTargets))
        {
            errorMsg = method == null ? "method 'OnPlay' not found" : "failed to read methodBody of 'OnPlay'";
            return false;
        }

        SeekState seekState = SeekState.WithHitCount;
        SeekType seekType = SeekType.Other;
        int seekDepth = 0;

        MethodBase called = null!;
        bool isCall = false;
        OpCode markedOpCode = default;
        byte[]? markedOperand = null;

        for (int i = instructions.Count - 1; i >= 0; i--)
        {
            Instruction instruction = instructions[i];

            if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
            {
                int token = BitConverter.ToInt32(instruction.OperandIL, 0); // Operand is a 4-byte metadata token
                called = method.Module.ResolveMethod(token, method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments())!;
                isCall = true;

                if (called == null)
                {
                    errorMsg = "Unresolved method token";
                    return false;
                }
            }
            else
            {
                called = null!;
                isCall = false;
            }

            if (seekDepth <= 0)
            {
                if (seekState == SeekState.WithHitCount)
                {
                    if (isCall && called.Name == "WithHitCount")
                    {
                        seekState = SeekState.Argument;
                        seekType = SeekType.Numeric;
                    }
                    else if (isCall && called.Name == "CardAttack" && called.DeclaringType!.FullName == "BaseLib.Utils.CommonActions")
                    {
                        seekState = SeekState.Argument;
                        seekType = SeekType.Numeric;
                        seekDepth = 3;
                    }
                }
                else if (seekState == SeekState.Argument)
                {
                    if (BasicILScanner.IsLoad(instruction.OpCode))
                    {
                        if (branchTargets.Contains(instruction.Position))
                        {
                            errorMsg = "Branching encountered";
                            break;
                        }

                        seekState = SeekState.FindStore;
                        markedOpCode = BasicILScanner.StoreLoadReciprocals[instruction.OpCode];
                        markedOperand = instruction.OperandIL;
                    }
                    else if (seekType == SeekType.Numeric)
                    {
                        if (BasicILScanner.IsLoadNumericConstant(instruction.OpCode))
                        {
                            if (branchTargets.Contains(instruction.Position))
                            {
                                errorMsg = "Branching encountered";
                                break;
                            }

                            int hitCount = BasicILScanner.GetIntFromIL(instruction.OpCode, instruction.OperandIL);
                            constHitCount = hitCount;
                            if (hitCount != 1)
                            {
                                hitCountFunc = _ => hitCount;
                            }
                            break;
                        }
                        else if (BasicILScanner.IsArithmatic(instruction.OpCode))
                        {
                            errorMsg = "Arithmatic encountered";
                            break;
                        }
                        else if (isCall && called.Name == "get_IntValue" && called.DeclaringType!.IsAssignableTo(typeof(DynamicVar)))
                        {
                            seekState = SeekState.GetDynamicVar;
                        }
                        else if (isCall && called.Name.Contains("Calculate"))
                        {
                            seekState = SeekState.GetDynamicVar;
                        }
                        else if (isCall && (called.Name == "ResolveEnergyXValue" || called.Name == "ResolveStarXValue"))
                        {
                            bool usesEnergy = called.Name == "ResolveEnergyXValue";
                            hitCountFunc = _ =>
                            {
                                int xValue = usesEnergy ? card.Owner.PlayerCombatState?.Energy ?? 0 : card.Owner.PlayerCombatState?.Stars ?? 0;
                                xValue = (card.CombatState != null) ? Hook.ModifyXValue(card.CombatState, card, xValue) : xValue;
                                return xValue;
                            };
                            break;
                        }
                    }
                    else if (seekType == SeekType.String && instruction.OpCode == OpCodes.Ldstr)
                    {
                        hitCountVarName = BasicILScanner.GetStringFromIL(method.Module, instruction.OperandIL);
                        break;
                    }
                }
                else if (seekState == SeekState.GetDynamicVar)
                {
                    if (isCall && called.Name.StartsWith("get_Item") && called.DeclaringType == typeof(DynamicVarSet))
                    {
                        seekState = SeekState.Argument;
                        seekType = SeekType.String;
                    }
                    else if (isCall && called.GetUnderlyingType().IsAssignableTo(typeof(DynamicVar)))
                    {
                        if (called.Name.StartsWith("get_") && called.DeclaringType == typeof(DynamicVarSet))
                        {
                            hitCountVarName = called.Name[4..];
                        }
                        hitCountVarType = called.GetUnderlyingType();
                        break;
                    }
                }
                else if (seekState == SeekState.FindStore)
                {
                    if (instruction.OpCode == markedOpCode && instruction.OperandIL.SequenceEqual(markedOperand))
                    {
                        if (branchTargets.Contains(instruction.Position))
                        {
                            errorMsg = "Branching encountered";
                            break;
                        }

                        seekState = SeekState.Argument;
                        markedOpCode = default;
                        markedOperand = null;
                    }
                }
            }
            else
            {
                seekDepth -= BasicILScanner.NetStackDepth(instruction.OpCode, called);
            }
        }

        return seekState != SeekState.WithHitCount && constHitCount != 1;
    }

    private enum SeekState
    {
        None,
        WithHitCount,
        Argument,
        GetDynamicVar,
        FindStore,
    }

    private enum SeekType
    {
        Numeric,
        String,
        Other
    }
}
