using System;
using System.Collections.Generic;
using System.Diagnostics;
using WasmLib.FileFormat;
using WasmLib.FileFormat.Instructions;
using WasmLib.Utils;

namespace WasmLib.Decompilation.Intermediate
{
    public class CallInstruction : IntermediateInstruction
    {
        public string? Name { get; private set; }
        public bool IsIndirect { get; }
        public FunctionSignature Signature { get; }
        
        public CallInstruction(WasmFile file, Instruction instruction)
        {
            uint index = instruction.UIntOperand;

            if (instruction.OpCode == InstructionKind.Call) {
                IsIndirect = false;
                Name = "fun_" + index.ToString("X8");

                // skipping signatures for imported functions
                Signature = file.FunctionTypes[file.Functions[index - file.ImportedFunctionCount]];
            }
            else if (instruction.OpCode == InstructionKind.CallIndirect) {
                IsIndirect = true;
                Signature = file.FunctionTypes[index];
            }
            else {
                throw new WrongInstructionPassedException(instruction, nameof(CallInstruction));
            }
        }
        
        public override void Handle(ref IntermediateContext context)
        {
            if (Signature.ReturnParameter.Length > 1) {
                throw new Exception("Not implemented");
            }

            if (IsIndirect) {
                // could be resolved if we know this value
                Name = $"ELEM[{context.Pop()}]";
            }
            
            var paramList = new List<Variable>();

            // pop parameters
            foreach (ValueKind param in Signature.Parameters) {
                var popped = context.Pop();
                Debug.Assert(popped.Type == param);
                paramList.Add(popped);
            }

            string parameters = string.Join(", ", paramList); // TODO: check order

            if (Signature.ReturnParameter.Length == 1) {
                var pushed = context.Push(Signature.ReturnParameter[0]);
                context.WriteFull($"{pushed} = {Name}({parameters})");
            }
            else {
                context.WriteFull($"{Name}({parameters})");
            }
        }
    }
}