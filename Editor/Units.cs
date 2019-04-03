using System;

namespace UnityEditor.Performance.ProfileAnalyzer
{
    public enum Units
    {
        Milliseconds,
        Microseconds,
    };

    class DisplayUnits
    {
        public readonly Units Units;

        public DisplayUnits(Units units)
        {
            Units = units;
        }

        public string Postfix()
        {
            switch (Units)
            {
                default:
                case Units.Milliseconds:
                    return "ms";
                case Units.Microseconds:
                    return "us";
            }
        }

        private int ClampToRange(int value, int min, int max)
        {
            if (value < min)
                value = min;
            if (value > max)
                value = max;

            return value;
        }

        public string ToString(float ms, bool showUnits, int limitToNDigits)
        {
            float value = ms;
            int unitPower = -3;

            int maxDecimalPlaces = 0;
            switch (Units)
            {
                default:
                case Units.Milliseconds:
                    maxDecimalPlaces = 2;
                    break;
                case Units.Microseconds:
                    maxDecimalPlaces = 0;
                    value *= 1000f;
                    unitPower -= 3;
                    break;
            }


            int numberOfDecimalPlaces = maxDecimalPlaces;

            if (limitToNDigits>0)
            {
                int originalUnitPower = unitPower;

                float limitRange = (float)Math.Pow(10, limitToNDigits);

                if (limitRange > 0 && value >= limitRange)
                {
                    while (value >= 1000f && unitPower < 9)
                    {
                        value /= 1000f;
                        unitPower += 3;
                    }
                }

                if (unitPower != originalUnitPower)
                    showUnits = true;
            
                int numberOfSignificantFigures = showUnits ? (limitToNDigits - 2) : limitToNDigits;
                int numberOfDigitsBeforeDecimalPoint = 1 + Math.Max(0, (int)Math.Log10((int)value));
                numberOfDecimalPlaces = ClampToRange(numberOfSignificantFigures - numberOfDigitsBeforeDecimalPoint, 0, 2);
            }

            string siUnitString = showUnits ? GetSIUnitString(unitPower) + "s" : "";

            string formatString = string.Concat("{0:f", numberOfDecimalPlaces, "}{1}");

            return string.Format(formatString, value, siUnitString);
        }

        public string GetSIUnitString(int unitPower)
        {
            switch (unitPower)
            {
                case -6:
                    return "u";
                case -3:
                    return "m";
                case 0:
                    return "";
                case 3:
                    return "k";
                case 6:
                    return "m";
            }

            return "?";
        }

        public string ToString(double ms, bool showUnits, int limitToNDigits)
        {
            return ToString((float)ms, showUnits, limitToNDigits);
        }
    }
}