using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CBR
{
    
    /*
    public static class TypeGuesser
    {
        public static string ValueHandler(byte[] fieldValue)
        {
            byte[] valueBytes = fieldValue;
            string valueString = BitConverter.ToString(valueBytes).Replace("-", "");

            string output = valueString;
            string parsedOutput = "";

            // determine valid field type
            if (TryParse(valueString, out parsedOutput) == true)
            {
                Console.WriteLine("Found type");

                output = parsedOutput;
            }

            //Console.WriteLine($"Input: {valueString}");
            Console.WriteLine($"Value Handler:\n\tfrom: {valueString}\n\tto: {output}");

            return output;
        }

        public static bool TryParse(string value, out string parsedString)
        {
            bool outcome = false;
            parsedString = null;

            if (value.Length%2 == 0 && value.Length > 4 * 2 && value.Substring(value.Length - 2, 2) == "00")
            {
                parsedString = Utility.HexToString(value);
                outcome = true;
            }

            return outcome;
        }
    }
    */
}
