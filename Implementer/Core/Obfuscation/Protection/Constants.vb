﻿Imports Mono.Cecil
Imports Mono.Cecil.Cil
Imports Mono.Cecil.Rocks
Imports Helper.CecilHelper
Imports Implementer.Core.Obfuscation.Exclusion

Namespace Core.Obfuscation.Protection
    Public NotInheritable Class Constants

#Region " Fields "
        Private Shared Rand As Random
        Private Shared Types As New List(Of TypeDefinition)
        Private Shared completedInstructions As Mono.Collections.Generic.Collection(Of Instruction)
#End Region

#Region " Constructor "
        Shared Sub New()
            Rand = New Random
            completedInstructions = New Mono.Collections.Generic.Collection(Of Instruction)
        End Sub
#End Region

#Region " Methods "
        Friend Shared Sub DoJob(AssDef As AssemblyDefinition, Exclude As ExcludeList)
            For Each m As ModuleDefinition In AssDef.Modules
                Types.AddRange(m.GetAllTypes())
                For Each type As TypeDefinition In Types
                    If Exclude.isIntegerEncodExclude(type) = False Then
                        IterateType(type)
                    End If
                Next
                Types.Clear()
            Next
        End Sub
        Private Shared Sub IterateType(td As TypeDefinition)

            For Each mDef In (From mtd In td.Methods
                              Where mtd.HasBody AndAlso Not Finder.HasCustomAttributeByName(mtd.DeclaringType, "EditorBrowsableAttribute") AndAlso Utils.HasUnsafeInstructions(mtd) = False
                              Select mtd)
                Dim num% = 0
                Dim tRef As TypeReference = Nothing
                mDef.Body.SimplifyMacros()

                For i = 0 To mDef.Body.Instructions.Count - 1
                    Dim instruct = mDef.Body.Instructions(i)
                    If Not completedInstructions.Contains(instruct) Then

                        If Not instruct.OpCode = OpCodes.Ldc_I4 Then
                            Continue For
                        End If

                        If instruct.Operand >= Integer.MaxValue Then
                            Continue For
                        End If

                        Select Case Rand.Next(1, 8)
                            Case 1
                                tRef = td.Module.Assembly.MainModule.Import(GetType(Integer))
                                num = 4
                                Exit Select
                            Case 2
                                tRef = td.Module.Assembly.MainModule.Import(GetType(SByte))
                                num = 1
                                Exit Select
                            Case 3
                                tRef = td.Module.Assembly.MainModule.Import(GetType(Byte))
                                num = 1
                                Exit Select
                            Case 4
                                tRef = td.Module.Assembly.MainModule.Import(GetType(Boolean))
                                num = 1
                                Exit Select
                            Case 5
                                tRef = td.Module.Assembly.MainModule.Import(GetType(Decimal))
                                num = 16
                                Exit Select
                            Case 6
                                tRef = td.Module.Assembly.MainModule.Import(GetType(Short))
                                num = 2
                                Exit Select
                            Case 7
                                tRef = td.Module.Assembly.MainModule.Import(GetType(Long))
                                num = 8
                                Exit Select
                        End Select

                        Try
                            Dim nmr% = Rand.Next(1, 1000)
                            Dim flag As Boolean = Convert.ToBoolean(Rand.Next(0, 2))
                            Select Case If(num <> 0, If((Convert.ToInt32(instruct.Operand) Mod num = 0), Rand.Next(1, 4), Rand.Next(1, 4)), Rand.Next(1, 4))
                                Case 1
                                    Dim newOp = ((Convert.ToInt32(instruct.Operand) - num) + If(flag, -nmr, nmr))
                                    With mDef.Body
                                        .Instructions.Insert((i + 1), Instruction.Create(OpCodes.Sizeof, tRef))
                                        .Instructions.Insert((i + 2), Instruction.Create(OpCodes.Add))
                                        instruct.Operand = newOp
                                        .Instructions.Insert((i + 3), Instruction.Create(OpCodes.Ldc_I4, nmr))
                                        .Instructions.Insert((i + 4), Instruction.Create(If(flag, OpCodes.Add, OpCodes.Sub)))
                                    End With
                                    i = (i + 2)
                                    Exit Select
                                Case 2
                                    Dim newOp = ((Convert.ToInt32(instruct.Operand) + num) + If(flag, -nmr, nmr))
                                    With mDef.Body
                                        .Instructions.Insert((i + 1), Instruction.Create(OpCodes.Sizeof, tRef))
                                        .Instructions.Insert((i + 2), Instruction.Create(OpCodes.Sub))
                                        instruct.Operand = newOp
                                        .Instructions.Insert((i + 3), Instruction.Create(OpCodes.Ldc_I4, nmr))
                                        .Instructions.Insert((i + 4), Instruction.Create(If(flag, OpCodes.Add, OpCodes.Sub)))
                                    End With
                                    i = (i + 2)
                                    Exit Select
                                Case 3
                                    Dim newOp = ((Convert.ToInt32(instruct.Operand) + If(flag, -nmr, nmr)) * num)
                                    With mDef.Body
                                        .Instructions.Insert((i + 1), Instruction.Create(OpCodes.Sizeof, tRef))
                                        .Instructions.Insert((i + 2), Instruction.Create(OpCodes.Div))
                                        instruct.Operand = newOp
                                        .Instructions.Insert((i + 3), Instruction.Create(OpCodes.Ldc_I4, nmr))
                                        .Instructions.Insert((i + 4), Instruction.Create(If(flag, OpCodes.Add, OpCodes.Sub)))
                                    End With
                                    i = (i + 2)
                                    Exit Select
                            End Select
                        Catch ex As Exception
                            Continue For
                        End Try
                        completedInstructions.Add(instruct)
                    End If
                Next
                mDef.Body.OptimizeMacros
                mDef.Body.ComputeOffsets()
                mDef.Body.ComputeHeader()
            Next
        End Sub
#End Region

    End Class
End Namespace
