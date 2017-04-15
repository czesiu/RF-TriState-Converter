using System;
using System.Globalization;

namespace RF_Link_TriState_code_converter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Tool for converting RF-Link TriState code into rc-switch TriState code.");
            Console.WriteLine("Use RF-Link to detect code and look for similiar line in RF-Link console:");
            Console.WriteLine("  20;14;TriState;ID=08252a;SWITCH=3;CMD=ON;");
            Console.WriteLine();

            // Example RF-Link TriState: 20;14;TriState;ID=08252a;SWITCH=3;CMD=ON;
            Console.Write("Please write RF-Link TriState ID and press enter: ");
            var rfLinkTriStateId = Console.ReadLine();
            if (rfLinkTriStateId == null || rfLinkTriStateId.Length != 6)
            {
                Console.WriteLine("Invalid TriState ID. Should be 6 letters");
                return;
            }

            Console.Write("Please write RF-Link TriState switch number (SWITCH) and press enter [optional]: ");
            var rfLinkTriStateSwitchNumber = Console.ReadLine();

            Console.Write("Please write RF-Link TriState command (CMD) and press enter [optional]: ");
            var rfLinkTriStateCommand = Console.ReadLine();

            // Example: 08252a HEX -> 8540832 decimal
            var bitstream = RFLinkTriStateToBitStream(rfLinkTriStateId, rfLinkTriStateSwitchNumber, rfLinkTriStateCommand);
            Console.WriteLine($"Bitstream unsigned long = {bitstream}");

            // Example: 8540832 decimal -> reversed 4262102402/689538 -> F00F110FFF00
            var x = BitstreamToTriStateStream(bitstream);
            Console.WriteLine($"(RESULT) TriState stream = {x}");
            Console.ReadKey();
        }

        private static uint RFLinkTriStateToBitStream(string code, string address, string command)
        {
            var temp = "" + (char)0x30 + (char)0x78 + code;

            Console.WriteLine($"Source for bitstream calculation = {temp}");

            var bitstream = uint.Parse(code, NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier);

            bitstream = bitstream << 4;

            var addressCode = !string.IsNullOrEmpty(address) ? int.Parse(address) & 0x03 : 3;
            if (command?.ToUpper() == "ON")
            {
                if (addressCode == 0x0) bitstream |= 0x0000000b; // 0011
                if (addressCode == 0x1) bitstream |= 0x0000000c; // 1011
                if (addressCode == 0x2) bitstream |= 0x00000001; // 0001
            }
            else
            {
                if (addressCode == 0x0) bitstream |= 0x0000000c; // 1100 
                if (addressCode == 0x1) bitstream |= 0x0000000e; // 1110
                if (addressCode == 0x2) bitstream |= 0x00000004; // 0100 
            }

            return bitstream;
        }

        static string BitstreamToTriStateStream(UInt32 bitstream)
        {
            var result = "";

            UInt32 fdatabit = 0;
            UInt32 fdatamask = 0x00000003;
            UInt32 fsendbuff = 0;

            // reverse data bits (2 by 2)
            for (ushort i = 0; i < 12; i++)
            {
                // reverse data bits (12 times 2 bits = 24 bits in total)
                fsendbuff <<= 2;
                fsendbuff |= (bitstream & 0x03);
                bitstream >>= 2;
            }
            bitstream = fsendbuff; // store result    

            Console.WriteLine($"Bitstream unsigned long reversed = {bitstream}");

            fsendbuff = bitstream;
            // Send command
            for (int i = 0; i < 12; i++)
            {
                // 12 times 2 bits = 24 bits in total
                // read data bit
                fdatabit = fsendbuff & fdatamask; // Get most right 2 bits
                fsendbuff = (fsendbuff >> 2); // Shift right
                // data can be 0, 1 or float. 
                if (fdatabit == 0)
                {
                    // Write 0
                    result += "0";
                }
                else if (fdatabit == 1)
                {
                    // Write 1
                    result += "1";
                }
                else
                {
                    // Write float
                    result += "F";
                }
            }

            return result;
        }
    }
}
