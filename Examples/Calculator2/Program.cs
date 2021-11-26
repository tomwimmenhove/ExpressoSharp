/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;

namespace Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.Error.WriteLine("The only accepted argument is the name of the system type to use.");
                Environment.Exit(1);
            }

            /* Use doubles by default */
            if (args.Length == 0)
            {
                var calc = new Calc<double>(false);
                calc.Run();

                return;
            }

            /* Create an instance of Calc<> with the given type */
            var valuetypeName = args[0];

            /* Special case for dynamic type */
            if (valuetypeName == "dynamic")
            {
                var calc = new Calc<dynamic>(true);
                calc.Run();
                
                return;
            }

            var valueType = Type.GetType(valuetypeName);
            if (valueType == null)
            {
                Console.Error.WriteLine($"{valuetypeName} is not a known type");
                Environment.Exit(1);
            }

            Console.WriteLine($"Using type: {valueType}");

            var calcType = typeof(Calc<>).MakeGenericType(valueType);
            var instance = Activator.CreateInstance(calcType, args: new object[] { false });
            instance.GetType().GetMethod(nameof(Calc<object>.Run)).Invoke(instance, null);
        }
    }
}
