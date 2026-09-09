using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BetterDamagePreviews.Preview;

internal class CardILReader
{
    private readonly record struct Instruction(OpCode OpCode, byte[] OperandIL, int Position);

    private static readonly Dictionary<OpCode, OpCode> reciprocals = [];
    private static readonly Dictionary<short, OpCode> ops = [];

    static CardILReader()
    {
        foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            ops[((OpCode)f.GetValue(null)!).Value] = (OpCode)f.GetValue(null)!;
        }

        reciprocals[OpCodes.Stfld] = OpCodes.Ldfld;
        reciprocals[OpCodes.Stloc] = OpCodes.Ldloc;
        reciprocals[OpCodes.Stloc_0] = OpCodes.Ldloc_0;
        reciprocals[OpCodes.Stloc_1] = OpCodes.Ldloc_1;
        reciprocals[OpCodes.Stloc_2] = OpCodes.Ldloc_2;
        reciprocals[OpCodes.Stloc_3] = OpCodes.Ldloc_3;
        reciprocals[OpCodes.Stloc_S] = OpCodes.Ldloc_S;
        foreach (var kvp in reciprocals.ToArray())
        {
            reciprocals[kvp.Value] = kvp.Key;
        }
    }

    public static bool TryDetectHitCount(CardModel card, out int constHitCount, out string? hitCountVarName, out Type? hitCountVarType, out Func<Creature?, int>? hitCountFunc, out string? errorMsg)
    {
        constHitCount = -1;
        hitCountVarName = null;
        hitCountVarType = null;
        hitCountFunc = null;
        errorMsg = null;

        MethodInfo method = AccessTools.AsyncMoveNext(AccessTools.Method(card.GetType(), "OnPlay"));
        byte[]? il = method?.GetMethodBody()?.GetILAsByteArray();

        if (method == null || il == null)
            return false;

        List<Instruction> instructions = [];
        HashSet<int> branchTargets = [];
        int size;

        try
        {
            for (int pos = 0; pos < il.Length; pos += size)
            {
                int p = pos;

                ushort code = il[pos++];
                if (code == 0xFE)
                    code = (ushort)(0xFE00 + il[pos++]);

                OpCode op = ops[(short)code];
                size = OperandSize(op);

                if (size == -1)
                {
                    size = 4 + (BitConverter.ToInt32(il, pos) * 4); // variable size switch op
                }

                byte[] operand = il[pos..(pos + size)];
                instructions.Add(new(op, operand, p));

                switch (op.OperandType)
                {
                    case OperandType.ShortInlineBrTarget:
                        {
                            int target = pos + 1 + (sbyte)il[pos]; // 1-byte signed offset, relative to NEXT instruction
                            branchTargets.Add(target);
                            break;
                        }
                    case OperandType.InlineBrTarget:
                        {
                            int offset = BitConverter.ToInt32(il, pos); // 4-byte signed offset, relative to NEXT instruction
                            int target = pos + 4 + offset;
                            branchTargets.Add(target);
                            break;
                        }
                    case OperandType.InlineSwitch:
                        {
                            int count = BitConverter.ToInt32(il, pos);
                            int baseAddr = pos + 4 + (count * 4); // end of switch operand
                            for (int i = 0; i < count; i++)
                            {
                                int offset = BitConverter.ToInt32(il, pos + 4 + i * 4);
                                branchTargets.Add(baseAddr + offset);
                            }
                            break;
                        }
                }
            }

            return TryResolveHitCount(instructions, branchTargets, card, method, ref constHitCount, ref hitCountVarName, ref hitCountVarType, ref hitCountFunc, ref errorMsg);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(ex.Message);
            return false;
        }
    }

    private static bool TryResolveHitCount(List<Instruction> instructions, HashSet<int> branchTargets, CardModel card, MethodInfo method, ref int constHitCount, ref string? hitCountVarName, ref Type? hitCountVarType, ref Func<Creature?, int>? hitCountFunc, ref string? errorMsg)
    {
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
                    return false;
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
                    if (IsLoadCode(instruction.OpCode))
                    {
                        if (branchTargets.Contains(instruction.Position))
                        {
                            errorMsg = "Branching encountered";
                            break;
                        }

                        seekState = SeekState.FindStore;
                        markedOpCode = reciprocals[instruction.OpCode];
                        markedOperand = instruction.OperandIL;
                    }
                    else if (seekType == SeekType.Numeric)
                    {
                        if (IsLoadNumericConstant(instruction.OpCode))
                        {
                            if (branchTargets.Contains(instruction.Position))
                            {
                                errorMsg = "Branching encountered";
                                break;
                            }

                            int hitCount = GetIntFromIL(instruction.OpCode, instruction.OperandIL);
                            constHitCount = hitCount;
                            if (hitCount != 1)
                            {
                                hitCountFunc = _ => hitCount;
                            }
                            break;
                        }
                        else if (IsArithmatic(instruction.OpCode))
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
                        hitCountVarName = GetStringFromIL(method.Module, instruction.OperandIL);
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
                seekDepth -= NetStackDepth(instruction.OpCode, called);
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

    private static int OperandSize(OpCode op) => op.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget => 1,
        OperandType.ShortInlineI => 1,
        OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 => 8,
        OperandType.InlineR => 8,
        OperandType.InlineSwitch => -1, // special: 4 + 4*n
        _ => 4 // InlineMethod, InlineString, InlineI, InlineField, etc.
    };

    private static bool IsLoadNumericConstant(OpCode op) =>
        op == OpCodes.Ldc_I4_M1
        || op == OpCodes.Ldc_I4_0
        || op == OpCodes.Ldc_I4_1
        || op == OpCodes.Ldc_I4_2
        || op == OpCodes.Ldc_I4_3
        || op == OpCodes.Ldc_I4_4
        || op == OpCodes.Ldc_I4_5
        || op == OpCodes.Ldc_I4_6
        || op == OpCodes.Ldc_I4_7
        || op == OpCodes.Ldc_I4_8
        || op == OpCodes.Ldc_I4_S
        || op == OpCodes.Ldc_I4
        || op == OpCodes.Ldc_I8
        || op == OpCodes.Ldc_R4
        || op == OpCodes.Ldc_R8;

    private static bool IsArithmatic(OpCode op) =>
       op == OpCodes.Add
       || op == OpCodes.Sub
       || op == OpCodes.Mul;

    private static bool IsLoadCode(OpCode op) =>
        op == OpCodes.Ldfld
        || op == OpCodes.Ldloc
        || op == OpCodes.Ldloc_0
        || op == OpCodes.Ldloc_1
        || op == OpCodes.Ldloc_2
        || op == OpCodes.Ldloc_3
        || op == OpCodes.Ldloc_S;

    private static bool IsStoreCode(OpCode op) =>
        op == OpCodes.Stfld
        || op == OpCodes.Stloc
        || op == OpCodes.Stloc_0
        || op == OpCodes.Stloc_1
        || op == OpCodes.Stloc_2
        || op == OpCodes.Stloc_3
        || op == OpCodes.Stloc_S;

    private static int NetStackDepth(OpCode opCode, MethodBase? called)
    {
        if (called != null && (opCode == OpCodes.Call || opCode == OpCodes.Callvirt || opCode == OpCodes.Newobj)) // box and unbox too?
        {
            ParameterInfo[] parms = called.GetParameters();

            int paramCount = parms.Length + (called.IsStatic ? 0 : 1);
            int returns = (called is MethodInfo m && m.ReturnType != typeof(void)) ? 1 : 0;

            if (opCode == OpCodes.Newobj)
            {
                paramCount -= 1; // no 'this' for ctor
                returns = 1; // pushes the new instance
            }

            int depth = returns - paramCount;
            return depth;
        }
        else
        {
            return opCode.EvaluationStackDelta;
        }
    }

    private static int GetIntFromIL(OpCode opCode, byte[] operandIL)
    {
        return opCode.Value switch
        {
            0x15 => -1, // ldc.i4.m1
            0x16 => 0,  // ldc.i4.0
            0x17 => 1,  // ldc.i4.1
            0x18 => 2,  // ldc.i4.2
            0x19 => 3,  // ldc.i4.3
            0x1A => 4,  // ldc.i4.4
            0x1B => 5,  // ldc.i4.5
            0x1C => 6,  // ldc.i4.6
            0x1D => 7,  // ldc.i4.7
            0x1E => 8,  // ldc.i4.8
            0x1F => (int)(sbyte)operandIL[0], // ldc.i4.s — 1 signed byte
            0x20 => BitConverter.ToInt32(operandIL, 0), // ldc.i4 — 4 bytes, little-endian
            0x21 => (int)BitConverter.ToInt64(operandIL, 0), // or keep as long  // ldc.i8 — 8 bytes, little-endian
            0X22 => (int)BitConverter.ToSingle(operandIL, 0), // ldc.r4 (float)
            0X23 => (int)BitConverter.ToDouble(operandIL, 0), // ldc.r8 (double)
            _ => -1,
        };
    }

    private static string GetStringFromIL(Module module, byte[] operandIL)
    {
        int token = BitConverter.ToInt32(operandIL, 0);
        string s = module.ResolveString(token);
        return s;
    }
}
