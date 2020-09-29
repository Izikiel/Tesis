### Tesis Reescritura casos de test xunit.

## Casos a reescribir

| Tipo | Descripcion | Dificultad |
| :--- | :--- | :--- |
| static async Task test() | Test estático sin parámetros | Caso más sencillo. |
| static async Task test(T1 param 1, T2 param 2, ...) | Test paramétrico estático |  Más complejo |
| async Task test() | Test de instancia sin parámetros | Se complica con la instancia, hay que ver como pasar la referencia o crear el objeto |
| async Task test() setup y teardown | Test de instancia sin parámetros con constructor y destructor | Más complicado aún, pq para ejecutar varias iteraciones de un test es necesario crear y destruir el objeto |
| async Task test(T1 param 1, T2 param 2, ...) | Test paramétrico de instancia | Como el sin parametros, pero peor |
| async Task test(T1 param 1, T2 param 2, ...) setup y teardown | Test paramétrico de instancia con constructor y destructor | Como el sin parametros, pero peor |
| static async Task test<T1, T2>(T1 param 1, T2 param 2, ...) | Test paramétrico genérico estático | Se complica severamente la cosa |
| async Task test<T1, T2>(T1 param 1, T2 param 2, ...) | Test paramétrico genérico de instancia| Se complica severamente la cosa + |
| async Task test<T1, T2>(T1 param 1, T2 param 2, ...) setup y teardown | Test paramétrico genérico de instancia con constructor y destructor | Se complica severamente la cosa +++ |

## Ideas
Ver de usar Roslyn para crear una lambda custom. 2 opciones acá, hacerlo dentro de coyote y ejecutar el código generado ahí (+Simplifica la reescritura, - hay que generar diferentes llamadas de métodos para cualquier signatura posible). Hacerlo fuera de coyote y modificar la clase donde está el test original (+ no hace falta modificar coyote, - puede ser imposible).

La clase lambda custom a representar debería ser algo así:

```csharp
public class GeneratedLambda_XXX // (XXX should be the test method to rewrite)
{
    private object[] args;
    private MethodType method; // MethodType could be a custom delegate generated or Func<T1, ..., T16, Task> .
                               // In theory, a C# method could have 16383 parameters and run correctly, and it's possible for it to have much more.
                               // https://stackoverflow.com/questions/12658883/what-is-the-maximum-number-of-parameters-that-a-c-sharp-method-can-be-defined-as

    public GeneratedLambda_XXX(MethodType method, params object[] args)
    {
        this.args = args;
        this.method = method;
    }

    public Task ToFuncTask() => this.method(args[0], ...., args[n]); // This part with numbered args needs to be generated.
}
```

Se puede guardar en dll generadas on the fly, y despues importar el tipo con cecil y pasar una referencia al constructor.

Para el tema de las funciones , en realidad, cualquier función que retorne Task de hasta 16 parámetros, se puede hacer lo siguiente:

```csharp
    class FuncConstructorGenerator
    {
        static Type[] FuncTypes { get; } = new Type[]
        {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
            typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>),
            typeof(Func<,,,,,,,>),
            typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,,>),
        };

        public static ConstructorInfo GetConstructorInfo(params object[] argsForConstructor)
        {
            var funcType = FuncTypes[argsForConstructor.Length];

            var typeArguments = argsForConstructor.Select(a => a.GetType()).Append(typeof(Task)).ToArray();

            var instantiatedFuncType = funcType.MakeGenericType(typeArguments);

            return instantiatedFuncType.GetConstructors()[0];
        }
    }
```