using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EqualityComparer
{
    public class DepthObjectEqualityComparer
    {
        private static DepthObjectEqualityComparer instance;
        private readonly Dictionary<Type, KeyValuePair<DynamicMethod, Delegate>> comparers;
        private readonly MethodInfo consoleWriteLine;

        private DepthObjectEqualityComparer()
        {
            comparers = new Dictionary<Type, KeyValuePair<DynamicMethod, Delegate>>();
            consoleWriteLine = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
        }

        public static DepthObjectEqualityComparer EqualityComparer
        {
            get { return instance ?? (instance = new DepthObjectEqualityComparer()); }
        }

        public bool AreEquals(object objA, object objB)
        {
            var typeA = objA.GetType();
            var typeB = objB.GetType();
            if (typeA != typeB)
                throw new ArgumentException("Parametros não são do mesmo tipo");

            var comparer = CreateOrGetComparer(typeA);
            return (bool)comparer.Value.DynamicInvoke(objA, objB);
        }

        private KeyValuePair<DynamicMethod, Delegate> CreateOrGetComparer(Type type)
        {
            KeyValuePair<DynamicMethod, Delegate> dynamicMethodAndDelegate;
            if (comparers.ContainsKey(type))
                dynamicMethodAndDelegate = comparers[type];
            else
            {
                var method = CreateDynamicMethod(type);

                var boolType = typeof(bool);
                var funcType = typeof(Func<,,>);
                var genericType = funcType.MakeGenericType(new[] { type, type, boolType });
                dynamicMethodAndDelegate = new KeyValuePair<DynamicMethod, Delegate>(method, method.CreateDelegate(genericType));
                comparers.Add(type, dynamicMethodAndDelegate);
            }
            return dynamicMethodAndDelegate;
        }

        private DynamicMethod CreateDynamicMethod(Type type)
        {
            var boolType = typeof(bool);
            //public bool CompareNomeClasse(obj a, obj b)
            var method = new DynamicMethod("Compare" + type.Name, boolType, new[] { type, type }, type.Module);

            var ilGenerator = method.GetILGenerator();
            var carregaFalse = ilGenerator.DefineLabel();
            var iniciaComparacoesDePropriedade = ilGenerator.DefineLabel();

            //Varivel que vai ser usada para interar no for quando tiver propriedades de listagem
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(int));

            //Se os dois parametro forem nulos, retorna true
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Brtrue_S, iniciaComparacoesDePropriedade);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Brtrue_S, iniciaComparacoesDePropriedade);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator.MarkLabel(iniciaComparacoesDePropriedade);

            foreach (var propertyInfo in type.GetProperties())
            {
                //Carrega o valor da propriedade do primeiro objeto
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Callvirt, propertyInfo.GetMethod);
                //Carrega o valor da propriedade do segundo objeto
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Callvirt, propertyInfo.GetMethod);
                //Verifica se os dois são iguais, do contrario ela é derirecionado para o retorno de falso
                DefineEqualsMethod(ilGenerator, carregaFalse, propertyInfo);
            }

            ilGenerator.Emit(OpCodes.Ldc_I4_1); //Carrega true
            ilGenerator.Emit(OpCodes.Ret); //Retorna true
            ilGenerator.MarkLabel(carregaFalse);
            ilGenerator.Emit(OpCodes.Ldc_I4_0); //Carrega false
            ilGenerator.Emit(OpCodes.Ret); //Retorna false
            return method;
        }

        private void LogaAlgo(ILGenerator ilGenerator, string textToLog)
        {
            ilGenerator.Emit(OpCodes.Ldstr, textToLog);
            ilGenerator.Emit(OpCodes.Call, consoleWriteLine);
        }

        private void DefineEqualsMethod(ILGenerator ilGenerator, Label carregaFalse, PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            var equalityMethod = propertyType.GetMethod("op_Equality");
            var getMethod = propertyInfo.GetMethod;

            DefineEquals(ilGenerator, carregaFalse, propertyType, equalityMethod, getMethod);
        }

        private void DefineEquals(ILGenerator ilGenerator, Label carregaFalse, Type propertyType, MethodInfo equalityMethod, MethodInfo getMethod)
        {
            var propertyLabel = ilGenerator.DefineLabel();
            var objectType = typeof(object);
            if (equalityMethod != null)
            {
                //Carrego o operador == e uso ele para avaliar
                var opEqualityMethod = equalityMethod;
                ilGenerator.Emit(OpCodes.Call, opEqualityMethod);
                ilGenerator.Emit(OpCodes.Brfalse, carregaFalse);
            }
            else if (ImplementaIEnumerable(propertyType))
            {
                var iniciaComparacaoDeLista = ilGenerator.DefineLabel();
                TestIfBothListAreNull(ilGenerator, getMethod, iniciaComparacaoDeLista, propertyLabel);
                ilGenerator.MarkLabel(iniciaComparacaoDeLista);

                var listItemType = propertyType.GetGenericArguments().First();
                TestIfListHaveSameCount(ilGenerator, carregaFalse, getMethod, listItemType);

                ForEachElementTestIfTheyAreEquals(ilGenerator, carregaFalse, propertyLabel, getMethod, listItemType);
            }
            else if (EhUmaStructPropria(propertyType) || propertyType.Name.Contains("Nullable"))
            {
                //Carrego o método Equals de Object e uso ele para avaliar
                var opEqualityMethod = objectType.GetMethod("Equals", new[] { objectType, objectType });
                //Removo os valores atuais da pilha
                ilGenerator.Emit(OpCodes.Pop);
                ilGenerator.Emit(OpCodes.Pop);
                //Carrega o valor da propriedade do primeiro objeto
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Callvirt, getMethod);
                ilGenerator.Emit(OpCodes.Box, propertyType); //Converto para o tipo de Origem
                //Carrega o valor da propriedade do segundo objeto
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Callvirt, getMethod);
                ilGenerator.Emit(OpCodes.Box, propertyType); //Converto para o tipo de Origem
                ilGenerator.Emit(OpCodes.Call, opEqualityMethod);
                ilGenerator.Emit(OpCodes.Brfalse, carregaFalse);
            }
            else if (propertyType.IsClass)
            {
                CompareClassInstance(ilGenerator, carregaFalse, propertyType);
            }
            else
                ilGenerator.Emit(OpCodes.Bne_Un, carregaFalse);
            ilGenerator.MarkLabel(propertyLabel);
        }

        private void CompareClassInstance(ILGenerator ilGenerator, Label carregaFalse, Type propertyType)
        {
            //Criou o recupero o Comparador para esse tipo e uso ele
            var comparer = CreateOrGetComparer(propertyType);
            ilGenerator.Emit(OpCodes.Call, comparer.Key);
            ilGenerator.Emit(OpCodes.Brfalse, carregaFalse);
        }

        private void ForEachElementTestIfTheyAreEquals(ILGenerator ilGenerator, Label carregaFalse, Label propertyLabel, MethodInfo getMethod, Type listItemType)
        {
            var inicioDoCorpoDoFor = ilGenerator.DefineLabel();
            var elementAt = typeof(Enumerable).GetMethods().First(m => m.Name == "ElementAt");
            var elementAtGeneric = elementAt.MakeGenericMethod(new[] { listItemType });
            //Caso o count da listagem for 0, ele tranfere o processo para a comparação da proxima propriedade
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Beq, propertyLabel);
            //Inicio o for para testar a igualdade dos valores
            ilGenerator.Emit(OpCodes.Ldc_I4_0); // var i = 0
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.MarkLabel(inicioDoCorpoDoFor);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Callvirt, getMethod);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, elementAtGeneric); // listaA.ElementAt(i)
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Callvirt, getMethod);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, elementAtGeneric); // listaB.ElementAt(i)
            if (listItemType.IsClass)
            {
                CompareClassInstance(ilGenerator, carregaFalse, listItemType);
                //Incremento o valor de i para poder ir para o proximo elemento
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Add);
                //Verifico se o valor de i é menor que o total de elemento da lista, se for compara o proximo elemento
                ilGenerator.Emit(OpCodes.Stloc_0);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Ldloc_1);
                ilGenerator.Emit(OpCodes.Blt, inicioDoCorpoDoFor);
            }
        }

        private static void TestIfListHaveSameCount(ILGenerator ilGenerator, Label carregaFalse, MethodInfo getMethod, Type listItemType)
        {
            var count = typeof(Enumerable).GetMethods().First(m => m.Name == "Count");
            var countGeneric = count.MakeGenericMethod(new[] { listItemType });
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Callvirt, getMethod);
            ilGenerator.Emit(OpCodes.Call, countGeneric);
            ilGenerator.Emit(OpCodes.Stloc_1); ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Callvirt, getMethod);
            ilGenerator.Emit(OpCodes.Call, countGeneric);
            ilGenerator.Emit(OpCodes.Bne_Un, carregaFalse);
        }

        private static void TestIfBothListAreNull(ILGenerator ilGenerator, MethodInfo getMethod, Label iniciaComparacaoDeLista, Label propertyLabel)
        {
            //Removo os valores atuais da pilha
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Pop);
            //Verifico se as duas listagem são nulas, se forem, continua com o processo, do contrario, retorna false
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Callvirt, getMethod);
            ilGenerator.Emit(OpCodes.Brtrue_S, iniciaComparacaoDeLista);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Callvirt, getMethod);
            ilGenerator.Emit(OpCodes.Brtrue_S, iniciaComparacaoDeLista);
            ilGenerator.Emit(OpCodes.Br, propertyLabel);
        }

        private bool ImplementaIEnumerable(Type propertyType)
        {
            var contains = propertyType.GetInterfaces().Any(t => t.Name.Contains("IEnumerable"));
            return contains;
        }

        private static bool EhUmaStructPropria(Type propertyType)
        {
            return propertyType.IsValueType && !propertyType.IsPrimitive && !propertyType.Namespace.Contains("System");
        }
    }
}