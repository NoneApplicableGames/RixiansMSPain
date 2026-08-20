// using HarmonyLib;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using System.Reflection.Emit;

// namespace HideDetailsMod.HideDetailsModCode.HarmonyUtils;

// /// <summary>
// /// A generic transpiler utility for Harmony designed to manipulate fluent builder chains inside C# async state machines.
// /// Safely removes builder method calls and injects C# static interceptor callbacks while maintaining evaluation stack balance.
// /// </summary>
// /// <typeparam name="TBuilder">The type of the fluent builder class being chained.</typeparam>
// public class FluentChainTool<TBuilder> where TBuilder : class
// {
//     private readonly List<CodeInstruction> _instructions;
//     private readonly Dictionary<string, FieldInfo> _hoistedFields;

//     /// <summary>
//     /// Initializes a new instance of the <see cref="FluentChainTool{TBuilder}"/> class.
//     /// </summary>
//     /// <param name="instructions">The IL instruction sequence passed to the transpiler.</param>
//     /// <param name="stateMachineType">
//     /// The compiler-generated async state machine type (<c>original.DeclaringType</c>).
//     /// </param>
//     public FluentChainTool(IEnumerable<CodeInstruction> instructions, Type stateMachineType)
//     {
//         _instructions = instructions.ToList();

//         // Collect all compiler-hoisted fields on the async state machine struct/class
//         _hoistedFields = stateMachineType
//             .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
//             .ToDictionary(f => f.Name, f => f);
//     }

//     /// <summary>
//     /// Gets the modified IL instruction sequence ready to be returned by a Harmony transpiler.
//     /// </summary>
//     public IEnumerable<CodeInstruction> Instructions => _instructions;

//     /// <summary>
//     /// Removes all calls to a target builder method along with their pushed arguments.
//     /// </summary>
//     /// <param name="targetMethod">The method in the builder chain to remove (e.g., <c>AttackCommand.WithHitFx</c>).</param>
//     /// <returns>The current <see cref="FluentChainTool{TBuilder}"/> instance for method chaining.</returns>
//     public FluentChainTool<TBuilder> RemoveCall(MethodInfo targetMethod)
//     {
//         var matcher = new CodeMatcher(_instructions);
//         while (matcher.MatchStartForward(new CodeMatch(i => i.Calls(targetMethod))).IsValid)
//         {
//             int argCount = targetMethod.GetParameters().Length;

//             // Rewind past argument pushing instructions and remove them along with callvirt
//             matcher.Advance(-argCount);
//             matcher.RemoveInstructions(argCount + 1);
//         }
//         return this;
//     }

//     /// <summary>
//     /// Intercepts a method call within a fluent chain and executes a custom static C# interceptor.
//     /// Automatically binds parameters using naming conventions (<c>__builder</c>, <c>__instance</c>, <c>__args</c>, or argument names).
//     /// </summary>
//     /// <param name="targetMethod">The method call in the fluent chain to target.</param>
//     /// <param name="interceptor">
//     /// The static interceptor method to execute. Must return either <c>void</c> or <typeparamref name="TBuilder"/>.
//     /// </param>
//     /// <param name="generator">The <see cref="ILGenerator"/> supplied by the Harmony transpiler.</param>
//     /// <param name="insertBefore">
//     /// If <c>true</c>, executes the interceptor before <paramref name="targetMethod"/>; otherwise, executes after.
//     /// </param>
//     /// <returns>The current <see cref="FluentChainTool{TBuilder}"/> instance for method chaining.</returns>
//     /// <exception cref="InvalidOperationException">
//     /// Thrown when parameter binding fails or when <paramref name="interceptor"/> returns an invalid type.
//     /// </exception>
//     public FluentChainTool<TBuilder> InjectInterceptor(
//         MethodInfo targetMethod,
//         MethodInfo interceptor,
//         ILGenerator generator,
//         bool insertBefore = true)
//     {
//         // Validate return type signature
//         bool returnsBuilder = typeof(TBuilder).IsAssignableFrom(interceptor.ReturnType);
//         bool returnsVoid = interceptor.ReturnType == typeof(void);

//         if (!returnsBuilder && !returnsVoid)
//         {
//             throw new InvalidOperationException(
//                 $"[FluentChainTool] Interceptor '{interceptor.Name}' must return either 'void' or '{typeof(TBuilder).Name}'. Found: '{interceptor.ReturnType.Name}'.");
//         }

//         var matcher = new CodeMatcher(_instructions);

//         if (insertBefore)
//         {
//             if (!matcher.MatchStartForward(new CodeMatch(i => i.Calls(targetMethod))).IsValid)
//                 return this;
//         }
//         else
//         {
//             if (!matcher.MatchEndForward(new CodeMatch(i => i.Calls(targetMethod))).IsValid)
//                 return this;

//             matcher.Advance(1); // Position cursor directly after the target call instruction
//         }

//         var emittedInstructions = EmitInterceptorInvocation(interceptor, generator, returnsBuilder);
//         matcher.Insert(emittedInstructions.ToArray());

//         return this;
//     }

//     private List<CodeInstruction> EmitInterceptorInvocation(
//         MethodInfo interceptor,
//         ILGenerator generator,
//         bool returnsBuilder)
//     {
//         var code = new List<CodeInstruction>();
//         var interceptorParams = interceptor.GetParameters();

//         foreach (var param in interceptorParams)
//         {
//             // 1. __builder or TBuilder type -> Duplicate the builder instance sitting on the IL stack
//             if (param.Name == "__builder" || param.ParameterType == typeof(TBuilder))
//             {
//                 code.Add(new CodeInstruction(OpCodes.Dup));
//                 continue;
//             }

//             // 2. __instance -> Load outer class 'this' hoisted into async state machine
//             if (param.Name == "__instance")
//             {
//                 var thisField = FindHoistedField("this", param.ParameterType)
//                              ?? FindHoistedField("<>4__this", param.ParameterType);

//                 if (thisField == null)
//                     throw new InvalidOperationException($"Could not resolve hoisted 'this' field for type '{param.ParameterType.Name}'.");

//                 code.Add(new CodeInstruction(OpCodes.Ldarg_0));
//                 code.Add(new CodeInstruction(OpCodes.Ldfld, thisField));
//                 continue;
//             }

//             // 3. __args -> Pack all hoisted method arguments into an object[] array
//             if (param.Name == "__args" && param.ParameterType == typeof(object[]))
//             {
//                 var argFields = _hoistedFields.Values
//                     .Where(f => !f.Name.Contains("this") && !f.Name.StartsWith("<>"))
//                     .ToList();

//                 var arrayLocal = generator.DeclareLocal(typeof(object[]));

//                 code.Add(new CodeInstruction(OpCodes.Ldc_I4, argFields.Count));
//                 code.Add(new CodeInstruction(OpCodes.Newarr, typeof(object)));
//                 code.Add(new CodeInstruction(OpCodes.Stloc, arrayLocal));

//                 for (int i = 0; i < argFields.Count; i++)
//                 {
//                     code.Add(new CodeInstruction(OpCodes.Ldloc, arrayLocal));
//                     code.Add(new CodeInstruction(OpCodes.Ldc_I4, i));

//                     code.Add(new CodeInstruction(OpCodes.Ldarg_0));
//                     code.Add(new CodeInstruction(OpCodes.Ldfld, argFields[i]));

//                     if (argFields[i].FieldType.IsValueType)
//                         code.Add(new CodeInstruction(OpCodes.Box, argFields[i].FieldType));

//                     code.Add(new CodeInstruction(OpCodes.Stelem_Ref));
//                 }

//                 code.Add(new CodeInstruction(OpCodes.Ldloc, arrayLocal));
//                 continue;
//             }

//             // 4. Parameter Binding by Name (e.g. 'card', 'someStuff')
//             var matchedField = FindHoistedField(param.Name, param.ParameterType);
//             if (matchedField != null)
//             {
//                 code.Add(new CodeInstruction(OpCodes.Ldarg_0));
//                 code.Add(new CodeInstruction(OpCodes.Ldfld, matchedField));
//                 continue;
//             }

//             throw new InvalidOperationException(
//                 $"[FluentChainTool] Unable to bind parameter '{param.Name}' ({param.ParameterType.Name}) for interceptor method '{interceptor.Name}'.");
//         }

//         // Call the static interceptor method
//         code.Add(new CodeInstruction(OpCodes.Call, interceptor));

//         // Stack Balancing
//         if (returnsBuilder)
//         {
//             // Stack contains: [ OriginalBuilder, ReturnedBuilder ]
//             var tempLocal = generator.DeclareLocal(typeof(TBuilder));

//             code.Add(new CodeInstruction(OpCodes.Stloc, tempLocal)); // Save ReturnedBuilder
//             code.Add(new CodeInstruction(OpCodes.Pop));              // Pop OriginalBuilder
//             code.Add(new CodeInstruction(OpCodes.Ldloc, tempLocal)); // Restore ReturnedBuilder
//         }
//         // If void: Stack retains [ OriginalBuilder ] via original dup instruction

//         return code;
//     }

//     private FieldInfo? FindHoistedField(string name, Type type)
//     {
//         return _hoistedFields.Values.FirstOrDefault(f =>
//             f.FieldType == type &&
//             (f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
//              f.Name.Contains($"<{name}>")));
//     }
// }