using System;

namespace Cross.Drawing.ColorSpaces
{
    /// <summary>
    /// A helper class to convert colors
    /// </summary>
    public class ColorConverter
    {
        /// <summary>
        /// Converts a hex string to color
        /// </summary>
        public static Color FromHexa(string hexColor)
        {
            Color result = null;
            string r, g, b;

            if (hexColor != String.Empty)
            {
                hexColor = hexColor.Trim().ToUpper();
                if (hexColor[0] == '#') hexColor = hexColor.Substring(1, hexColor.Length - 1);

                r = hexColor.Substring(0, 2);
                g = hexColor.Substring(2, 2);
                b = hexColor.Substring(4, 2);

                r = Convert.ToString(16 * GetIntFromHex(r.Substring(0, 1)) + GetIntFromHex(r.Substring(1, 1)));
                g = Convert.ToString(16 * GetIntFromHex(g.Substring(0, 1)) + GetIntFromHex(g.Substring(1, 1)));
                b = Convert.ToString(16 * GetIntFromHex(b.Substring(0, 1)) + GetIntFromHex(b.Substring(1, 1)));

                result = Color.Create(Convert.ToInt32(r), Convert.ToInt32(g), Convert.ToInt32(b));
            }
            else result = Colors.Transparent;

            return result;
        }

        /// <summary>
        /// Gets the int equivalent for a hexadecimal value.
        /// </summary>
        private static int GetIntFromHex(string strHex)
        {
            switch (strHex)
            {
                case ("A"):
                    {
                        return 10;
                    }
                case ("B"):
                    {
                        return 11;
                    }
                case ("C"):
                    {
                        return 12;
                    }
                case ("D"):
                    {
                        return 13;
                    }
                case ("E"):
                    {
                        return 14;
                    }
                case ("F"):
                    {
                        return 15;
                    }
                default:
                    {
                        return int.Parse(strHex);
                    }
            }
        }

        /// <summary>
        /// Converts a color to hex string
        /// </summary>
        public static string ToHexa(Color value)
        {
            return string.Format("#{0:x2}{1:x2}{2:x2}", value.Red, value.Green, value.Blue).ToUpper();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ColorConverter()
        { }
    }
}
