﻿Imports Mono.Cecil
Imports Mono.Cecil.Cil
Imports Mono.Cecil.Rocks
Imports Helper.CecilHelper
Imports Helper.RandomizeHelper
Imports Implementer.Engine.Processing
Imports Implementer.Core.Obfuscation.Builder
Imports Implementer.Core.Obfuscation.Exclusion

Namespace Core.Obfuscation.Protection
    Public NotInheritable Class Pinvoke
        Inherits Source

#Region " Fields "
        Private Shared LoaderInvoke As Stub
        Private Shared PinvokeCreate As PinvokeModifier
#End Region

#Region " Constructor "
        Shared Sub New()
            PinvokeCreate = New PinvokeModifier
        End Sub
#End Region

#Region " Methods "

        Friend Shared Sub DoJob(asm As AssemblyDefinition, Framework$, Exclude As ExcludeList, Optional ByVal packIt As Boolean = False)
            AssemblyDef = asm
            Frmwk = Framework
            Pack = packIt

            Dim Types As New List(Of TypeDefinition)
            Dim HasPinvokeCalls As Boolean
            For Each mo As ModuleDefinition In asm.Modules
                For Each t In mo.GetAllTypes()
                    If t.Methods.Any(Function(f) f.IsPInvokeImpl) Then
                        Types.Add(t)
                        HasPinvokeCalls = True
                    End If
                Next
            Next

            If HasPinvokeCalls Then
                LoaderInvoke = New Stub(Randomizer.GenerateNewAlphabetic, Randomizer.GenerateNewAlphabetic, Randomizer.GenerateNewAlphabetic,
                              Randomizer.GenerateNewAlphabetic, Randomizer.GenerateNewAlphabetic)
                With LoaderInvoke
                    .ResolveTypeFromFile(DynamicInvokeStub(.className, .funcName1, .funcName2, .funcName3), Finder.FindDefaultNamespace(asm, Pack), Randomizer.GenerateNew, Randomizer.GenerateNew, Randomizer.GenerateNew, Randomizer.GenerateNew)
                    .InjectType(asm)
                    completedMethods.Add(.GetMethod1)
                    completedMethods.Add(.GetMethod2)
                    completedMethods.Add(.GetMethod3)
                End With

                PinvokeCreate.AddModuleRef(asm.MainModule)

                For Each type As TypeDefinition In Types
                    If NameChecker.IsRenamable(type) Then
                        If Exclude.isHideCallsExclude(type) = False Then
                            IterateType(type)
                        End If
                    End If
                Next

                LoaderInvoke.DeleteDll()
                PinvokeCreate.Dispose()
            End If

            Types.Clear()
        End Sub

        Private Shared Sub IterateType(td As TypeDefinition)
            Dim publicMethods As New List(Of MethodDefinition)()
            publicMethods.AddRange(From m In td.Methods Where (m.HasBody AndAlso m.Body.Instructions.Count > 1 AndAlso Not completedMethods.Contains(m) AndAlso Utils.HasUnsafeInstructions(m) = False))

            Try
                For Each md In publicMethods
                    If publicMethods.Contains(md) Then
                        md.Body.SimplifyMacros
                        For i = 0 To md.Body.Instructions.Count - 1

                            Dim item As Instruction = md.Body.Instructions.Item(i)
                            If (item.OpCode = OpCodes.Call) Then
                                Try
                                    Dim originalReference As MethodReference = DirectCast(item.Operand, MethodReference)
                                    Dim originalMethod As MethodDefinition = originalReference.Resolve

                                    If Not originalMethod Is Nothing AndAlso Not originalMethod.DeclaringType Is Nothing AndAlso Not completedMethods.Contains(originalMethod) Then
                                        If originalMethod.IsPInvokeImpl Then

                                            'If originalMethod.Name = "SendMessage" OrElse originalMethod.Name = "PostMessage" OrElse Not originalMethod.ReturnType.ToString = "System.Void" Then
                                            '    Continue For
                                            'End If

                                            If originalMethod.Name = "SendMessage" OrElse originalMethod.Name = "PostMessage" OrElse Not originalMethod.ReturnType.IsGenericInstance Then
                                                Continue For
                                            End If

                                            If originalMethod.PInvokeInfo.EntryPoint.StartsWith("#") Then
                                                originalMethod = Renamer.RenameMethod(originalMethod.DeclaringType, originalMethod)
                                            Else
                                                PinvokeCreate.InitPinvokeInfos(originalMethod, td)
                                                PinvokeCreate.CreatePinvokeBody(LoaderInvoke)
                                            End If
                                            completedMethods.Add(originalMethod)
                                        End If
                                    End If
                                Catch ex As AssemblyResolutionException
                                    Continue For
                                End Try
                            End If

                        Next
                        md.Body.OptimizeMacros
                        md.Body.ComputeOffsets()
                        md.Body.ComputeHeader()
                    End If
                Next
            Catch ex As Exception
                MsgBox(ex.ToString)
            End Try
            publicMethods.Clear()
        End Sub

#End Region

    End Class

End Namespace

