using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMLConnect
{
    public class MATWriter
    {
        #region Members
        // File access
        //private static System.IO.FileStream mlFile = null;

        // Main header field consisting of text
        private static byte[] headerTextField = null;

        // Header text default string
        private static string headerTextFieldDefault = null;

        // Data description field or "Tag Field"
        private static byte[] tagField = null;

        // Data of arrayfield
        private static byte[] arrayField = null;

        // Data description field or "Tag Field"
        private static byte[] arrayTagField = null;

        // Data of dimensions field
        private static byte[] dimField = null;

        // Data description field or "Tag Field"
        private static byte[] dimTagField = null;

        // Data of name field
        private static byte[] nameField = null;

        // Data description field or "Tag Field"
        private static byte[] nameTagField = null;

        // Data of datatype field
        private static byte[] dataType = null;

        // Data of datapadding
        private static byte[] dataPadding = null;
        #endregion

        public static void ToMatFile(string name, string filepath, ushort[] data, int height, int width)
        {
            #region HeaderFields
            // allocate headerTextField
            headerTextField = new byte[128];

            // allocate tagField
            tagField = new byte[8];

            // Set default string
            headerTextFieldDefault = "MATLAB 5.0 MAT-file, Platform: WIN64, Created on: Thu Nov 13 10:10:27 1997";

            // Write header string (ASCII)
            System.Buffer.BlockCopy(Encoding.ASCII.GetBytes(headerTextFieldDefault), 0, headerTextField, 0, headerTextFieldDefault.Length);

            // Write standard header for creating a MAT-file
            System.Buffer.BlockCopy(new byte[] { 0x01, 0x00 }, 0, headerTextField, 124, 2);

            // Write MI characters, to signify, that a small endian is used.
            System.Buffer.BlockCopy(Encoding.ASCII.GetBytes("MI"), 0, headerTextField, 126, 2);

            // Write the data type field (Uint16)
            System.Buffer.BlockCopy(new byte[] { 0x00, 0x00, 0x00, 0x0E }, 0, tagField, 0, 4);

            #endregion

            #region Array type field

            // allocate arrayTagField
            arrayTagField = new byte[8];

            // allocate arrayField
            arrayField = new byte[8];

            // write the needed values for a standard Uint16 array
            System.Buffer.BlockCopy(new byte[] { 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x08 }, 0, arrayTagField, 0, 8);

            // write the needed values for a standard Uint16 array
            System.Buffer.BlockCopy(new byte[] { 0x00, 0x00, 0x04, 0x0B, 0x00, 0x00, 0x00, 0x00 }, 0, arrayField, 0, 8);

            #endregion

            #region Dimensions field

            // allocate arrayTagField
            dimTagField = new byte[8];

            // allocate arrayField
            dimField = new byte[8];

            // write the needed values for a standard Uint16 array
            System.Buffer.BlockCopy(new byte[] { 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x08 }, 0, dimTagField, 0, 8);

            // write the needed values for a standard Uint16 array
            System.Buffer.BlockCopy(BitConverter.GetBytes((uint)width).Reverse().ToArray(), 0, dimField, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes((uint)height).Reverse().ToArray(), 0, dimField, 4, 4);

            #endregion

            #region name field

            // allocate arrayTagField
            nameTagField = new byte[8];

            // allocate arrayField
            nameField = new byte[8];

            // write the needed values for a standard Uint16 array
            System.Buffer.BlockCopy(new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x08 }, 0, nameTagField, 0, 8);

            if (name.Length < 8)
                name = name + "________"; // adds spaces, to fill out the name (has to be 8 characters) 

            // write the needed values for a standard Uint16 array
            System.Buffer.BlockCopy(Encoding.ASCII.GetBytes(name.ToCharArray(0, 8)), 0, nameField, 0, 8);

            #endregion

            #region DataType

            // Allocate datatype
            dataType = new byte[8];

            // write the datatype for a standard Uint16 array
            System.Buffer.BlockCopy(new byte[] { 0x00, 0x00, 0x00, 0x04 }, 0, dataType, 0, 4);

            #endregion

            #region DataPadding

            int dataLength = 2 * width * height;
            int rem = (dataLength + 4) % 8;
            if (rem > 0)
                dataPadding = new byte[rem];

            #endregion

            #region Data length calculation

            int length = arrayField.Length + arrayTagField.Length
                            + dimField.Length + dimTagField.Length
                            + nameField.Length + nameTagField.Length
                            + dataType.Length + dataPadding.Length
                            + 2 * width * height;

            #endregion

            #region Writing to the .mat file 
            // Open and ready it for file writing
            System.IO.FileStream mlFile = new System.IO.FileStream(filepath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);
            System.IO.BinaryWriter bw = new System.IO.BinaryWriter(mlFile);

            // Write the amount of data of the complete matrix (with array headers and so on)
            System.Buffer.BlockCopy(BitConverter.GetBytes((uint)length).Reverse().ToArray(), 0, tagField, 4, 4);

            // write the amount of actual data bytes
            System.Buffer.BlockCopy(BitConverter.GetBytes((uint)dataLength).Reverse().ToArray(), 0, dataType, 4, 4);

            // Open file and write to it
            bw.Write(headerTextField);
            bw.Write(tagField);
            bw.Write(arrayTagField);
            bw.Write(arrayField);
            bw.Write(dimTagField);
            bw.Write(dimField);
            bw.Write(nameTagField);
            bw.Write(nameField);
            bw.Write(dataType);
            foreach (ushort depthpixel in data)
            {
                bw.Write(BitConverter.GetBytes(depthpixel).Reverse().ToArray());
            }
            if (rem > 0)
                bw.Write(dataPadding);
            bw.Close();
            mlFile.Close();
            #endregion
        }        
    }
}
